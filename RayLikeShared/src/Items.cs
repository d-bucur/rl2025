using System.Numerics;
using Friflo.Engine.ECS;

namespace RayLikeShared;

// TODO could maybe use relationships between Item and HealingConsumable. See how ActiveStatusEffect works
struct Item : IComponent {
	required public IConsumable Consumable;
}
// a bit redundant, but allows for tag queries
struct ItemTag : ITag;

internal interface IConsumable {
	string Description();
	ActionProcessor.Result Consume(Entity target, Entity itemEntt, Entity? actionEntity = null, Vec2I? targetPos = null);
	IEnumerable<Vec2I> AffectedTiles(Vec2I source) => [Vec2I.Zero];
}

struct HealingConsumable : IConsumable {
	public string Description() => "Heals 75% of missing health";
	required public int Amount; // TODO not used

	public ActionProcessor.Result Consume(Entity target, Entity itemEntt, Entity? actionEntity = null, Vec2I? targetPos = null) {
		ref var fighter = ref target.GetComponent<Fighter>();
		var amount = (fighter.MaxHP - fighter.HP) * 0.75f;
		int recovered = fighter.Heal((int)amount);
		if (recovered > 0) {
			MessageLog.Print($"You consume the {itemEntt.Name.value} and healed for {recovered} HP");
			GUI.SpawnDamageFx(recovered, target.GetComponent<Position>().value, Raylib_cs.Color.Green, Vector3.Zero);
			return ActionProcessor.Result.Done;
		}
		else {
			MessageLog.Print($"Your health is already full");
			return ActionProcessor.Result.Invalid;
		}
	}
}

struct RageConsumable : IConsumable {
	// TODO descriptions should be cached in ctor to avoid allocations
	public string Description() => $"Makes you act faster for {Duration} turns";
	required public Func<Energy, int> GainCalc;
	public int Duration;

	public ActionProcessor.Result Consume(Entity target, Entity itemEntt, Entity? actionEntity = null, Vec2I? targetPos = null) {
		ref var energy = ref target.GetComponent<Energy>();
		var oldGain = energy.GainPerTick;
		var newGain = GainCalc(energy);
		energy.GainPerTick = newGain;
		target.AddRelation(new StatusEffect {
			// hardcoded value might cause problems
			Value = new RageEffect() { Duration = Duration, OldGain = 5 }
		});
		MessageLog.Print($"You feel enraged! You will act much faster");

		Animations.ExplosionFX(Prefabs.SpawnProjectile(
			target.GetComponent<GridPosition>().Value,
			Prefabs.ConsumableType.RagePotion,
			new Vector3(0, 1, 0)
		));
		return ActionProcessor.Result.Done;
	}
}

struct LightningDamageConsumable : IConsumable {
	public string Description() => "Damages the closest enemy";
	required public int Damage;
	required public int MaximumRange;
	static ArchetypeQuery<GridPosition>? cachedQuery;

	public ActionProcessor.Result Consume(Entity source, Entity itemEntt, Entity? actionEntity = null, Vec2I? targetPos = null) {
		var sourcePos = source.GetComponent<GridPosition>();
		var closest = GetClosest(sourcePos.Value);
		if (closest.IsNull) {
			MessageLog.Print($"No enemy in range");
			return ActionProcessor.Result.Invalid;
		}

		// FX and apply on end callback
		var damage = Damage; // captured
		Entity projectile = Prefabs.SpawnProjectile(sourcePos.Value, Prefabs.ConsumableType.LightningDamageScroll);

		Animations.ProjectileFollowFX(projectile, sourcePos.Value.ToWorldPos(), closest, () => {
			ref var f = ref closest.GetComponent<Fighter>();
			Combat.ApplyDamage(closest, source, damage);
			MessageLog.Print($"A lightning bolt strikes {closest.Name.value} for {damage} damage!");
			actionEntity?.AddTag<IsActionFinished>();
			var explosion = Prefabs.SpawnProjectile(closest.GetComponent<GridPosition>().Value, Prefabs.ConsumableType.LightningDamageScroll);
			Animations.ExplosionFX(explosion);
		});
		return actionEntity.HasValue ? ActionProcessor.Result.Running : ActionProcessor.Result.Done;
	}

	Entity GetClosest(Vec2I pos) {
		cachedQuery ??= Singleton.World.Query<GridPosition>()
			.AllTags(Tags.Get<Enemy, IsVisible>())
			.WithoutAllTags(Tags.Get<Corpse>());
		var closest = (new Entity(), int.MaxValue);
		cachedQuery.ForEachEntity((ref GridPosition enemyPos, Entity enemyEntt) => {
			int dist = Pathfinder.DiagonalDistance(pos, enemyPos.Value);
			if (dist < closest.Item2) closest = (enemyEntt, dist);
		});

		return closest.Item2 <= MaximumRange ? closest.Item1 : default;
	}

	public IEnumerable<Vec2I> AffectedTiles(Vec2I source) {
		Entity entity = GetClosest(source);
		if (entity.IsNull)
			return [];
		return [entity.GetComponent<GridPosition>().Value];
	}

}

struct ConfusionConsumable : IConsumable {
	public string Description() => $"Makes target harmless for {Turns} turns";
	required public int Turns;

	public ActionProcessor.Result Consume(Entity source, Entity itemEntt, Entity? actionEntity = null, Vec2I? inputPos = null) {
		// TODO refactor part with FireballConsumable
		if (inputPos == null) {
			var mouseVal = Singleton.Get<MouseTarget>().Value;
			if (mouseVal is null) {
				MessageLog.Print($"No valid target");
				return ActionProcessor.Result.Invalid;
			}
			inputPos = mouseVal;
		}
		var targetPos = inputPos.Value;
		ref var grid = ref Singleton.Get<Grid>();

		var target = grid.Character[targetPos.X, targetPos.Y];
		if (target.IsNull || target == Singleton.Player) {
			MessageLog.Print($"Not a valid target");
			return ActionProcessor.Result.Invalid;
		}

		target.AddRelation(new StatusEffect { Value = new IsConfused { Duration = Turns } });
		MessageLog.Print($"{target.Name.value} is confused");

		Animations.ExplosionFX(Prefabs.SpawnProjectile(
			target.GetComponent<GridPosition>().Value,
			Prefabs.ConsumableType.ConfusionScroll,
			new Vector3(0, 1, 0)
		));
		return ActionProcessor.Result.Done;
	}
}

struct FireballConsumable : IConsumable {
	public string Description() => "Damages all targets around target location";
	required public int Damage;
	required public int Range;
	static List<Entity> TargetsCache = new();
	static List<Vec2I> HitTilesCache = new();

	public ActionProcessor.Result Consume(Entity source, Entity itemEntt, Entity? actionEntity = null, Vec2I? inputPos = null) {
		// TODO check if in line of sight
		if (inputPos == null) {
			var mouseVal = Singleton.Get<MouseTarget>().Value;
			if (mouseVal is null) {
				MessageLog.Print($"No valid target");
				return ActionProcessor.Result.Invalid;
			}
			inputPos = mouseVal;
		}
		var targetPos = inputPos.Value;
		ref var grid = ref Singleton.Get<Grid>();

		int hitCount = 0;
		TargetsCache.Clear();
		HitTilesCache.Clear();
		foreach (var tilePos in AffectedTiles(targetPos)) {
			if (!grid.CheckTile<BlocksFOV>(tilePos))
				HitTilesCache.Add(tilePos);
			var target = grid.Character[tilePos.X, tilePos.Y];
			if (target.IsNull) continue;
			hitCount++;
			TargetsCache.Add(target);
		}

		if (hitCount == 0) {
			MessageLog.Print($"No valid target in range");
			return ActionProcessor.Result.Invalid;
		}

		// FX and apply on end callback
		var damage = Damage; // captured
		Vec2I sourcePos = source.GetComponent<GridPosition>().Value;
		Entity projectile = Prefabs.SpawnProjectile(sourcePos, Prefabs.ConsumableType.FireballScroll);
		Animations.ProjectilePosFX(projectile, sourcePos.ToWorldPos(), targetPos.ToWorldPos(), () => {
			MessageLog.Print($"Hit {hitCount} targets with fireball");
			foreach (var target in TargetsCache) {
				Combat.ApplyDamage(target, source, damage, Vec2I.Zero);
			}
			foreach (var tile in HitTilesCache) {
				var explosion = Prefabs.SpawnProjectile(tile, Prefabs.ConsumableType.FireballScroll);
				Animations.ExplosionFX(explosion);
			}
			actionEntity?.AddTag<IsActionFinished>();
		});
		return actionEntity.HasValue ? ActionProcessor.Result.Running : ActionProcessor.Result.Done;
	}

	public IEnumerable<Vec2I> AffectedTiles(Vec2I targetPos) {
		var grid = Singleton.Get<Grid>();
		for (int x = -Range + 1; x < Range; x++) {
			for (int y = -Range + 1; y < Range; y++) {
				var tilePos = new Vec2I(x + targetPos.X, y + targetPos.Y);
				if (!grid.IsInside(tilePos)) continue;
				yield return tilePos;
			}
		}
	}
}

struct NecromancyConsumable : IConsumable {
	public string Description() => "Resurrects target corpse to fight for you";
	public ActionProcessor.Result Consume(Entity target, Entity itemEntt, Entity? actionEntity = null, Vec2I? targetPos = null) {
		var mouseTarget = Singleton.Get<MouseTarget>().Value;
		ref var grid = ref Singleton.Get<Grid>();
		if (mouseTarget == null || !grid.IsInside(mouseTarget.Value)) return ActionProcessor.Result.Invalid;

		foreach (var other in grid.Others[mouseTarget.Value.X, mouseTarget.Value.Y]?.Value ?? []) {
			if (!other.Tags.Has<Corpse>()) continue;
			if (other.IsNull) return ActionProcessor.Result.Invalid;
			
			MessageLog.Print($"You resurrect {other.Name.value}. It will now fight for you");
			var fx = Prefabs.SpawnProjectile(mouseTarget.Value, Prefabs.ConsumableType.NecromancyScroll);
			Animations.Fall(fx, 2, true);
			PrefabTransformations.ResurrectCorpse(other, mouseTarget.Value);
			return ActionProcessor.Result.Done;
		}
		MessageLog.Print($"The spell only works on corpses");
		return ActionProcessor.Result.Invalid;
	}
}

// TODO Use ILinkComponent to Item?
struct ConsumeItemAction : IGameAction {
	required public Entity Target;
	required public Entity Item;
	public Vec2I? Pos;

	// Not really the source, could cause problems
	public Entity GetSource() => Target;
	public Entity GetIndexedValue() => Item;
}

struct PickupAction : IGameAction {
	required public Entity Target;
	required public Vec2I Position;

	public Entity GetSource() => Target;
}

struct InventoryItem : ILinkRelation {
	public Entity Item;
	public Entity GetRelationKey() => Item;
}

class Items : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.ApplyActions.Add(ActionProcessor.FromFunc<PickupAction>(ProcessPickupAction));
		UpdatePhases.ApplyActions.Add(ActionProcessor.FromFunc<ConsumeItemAction>(ProcessConsumeItemAction));
	}

	ActionProcessor.Result ProcessConsumeItemAction(ref ConsumeItemAction action, Entity actionEntt) {
		ref var item = ref action.Item.GetComponent<Item>();
		var result = item.Consumable.Consume(action.Target, action.Item, actionEntt, action.Pos);
		switch (result) {
			case ActionProcessor.Result.Done:
			case ActionProcessor.Result.Running:
				action.Item.DeleteEntity();
				break;
		}
		return result;
	}

	private ActionProcessor.Result ProcessPickupAction(ref PickupAction action, Entity actionEntt) {
		ref var grid = ref Singleton.Get<Grid>();
		var others = grid.Others[action.Position.X, action.Position.Y];

		int pickedCount = 0;
		foreach (var item in new List<Entity>(others?.Value ?? [])) {
			if (!item.Tags.Has<ItemTag>()) continue;
			if (action.Target.GetRelations<InventoryItem>().Length >= Config.InventoryLimit) {
				MessageLog.Print($"Inventory is full");
				return ActionProcessor.Result.Invalid;
			}
			pickedCount++;
			var target = action.Target;
			MessageLog.Print($"You picked up {item.Name.value}");
			target.AddRelation(new InventoryItem { Item = item });
			Animations.PickupItem(action.Target, item, () => {
				PrefabTransformations.PickupItem(item);
			});
		}
		if (pickedCount == 0) {
			MessageLog.Print($"You couldn't find anything");
			return ActionProcessor.Result.Invalid;
		}
		else return ActionProcessor.Result.Done;
	}
}