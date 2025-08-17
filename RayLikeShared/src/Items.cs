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
	public ActionProcessor.Result Consume(Entity target, Entity itemEntt, Entity? actionEntity = null);
}

struct HealingConsumable : IConsumable {
	required public int Amount;

	public ActionProcessor.Result Consume(Entity target, Entity itemEntt, Entity? actionEntity = null) {
		ref var fighter = ref target.GetComponent<Fighter>();
		var amount = (fighter.MaxHP - fighter.HP) * 0.75f;
		int recovered = fighter.Heal((int)amount);
		if (recovered > 0) {
			MessageLog.Print($"You consume the {itemEntt.Name.value} and healed for {recovered} HP");
			GUI.SpawnDamageFx(Amount, target.GetComponent<Position>().value, Raylib_cs.Color.Green, Vector3.Zero);
			return ActionProcessor.Result.Done;
		}
		else {
			MessageLog.Print($"Your health is already full");
			return ActionProcessor.Result.Invalid;
		}
	}
}

struct RageConsumable : IConsumable {
	required public Func<Energy, int> GainCalc;
	public int Duration;

	public ActionProcessor.Result Consume(Entity target, Entity itemEntt, Entity? actionEntity = null) {
		ref var energy = ref target.GetComponent<Energy>();
		var oldGain = energy.GainPerTick;
		var newGain = GainCalc(energy);
		energy.GainPerTick = newGain;
		// TODO OldGain will be wrong if stacked effects
		target.AddRelation(new StatusEffect {
			Value = new RageEffect() { Duration = Duration, OldGain = oldGain }
		});
		MessageLog.Print($"You feel enraged! You will act much faster");

		var fx = Prefabs.SpawnProjectile(
			target.GetComponent<GridPosition>().Value,
			Prefabs.ConsumableType.RagePotion,
			new Vector3(0, 1, 0)
		);
		Animations.ExplosionFX(fx);
		return ActionProcessor.Result.Done;
	}
}

struct LightningDamageConsumable : IConsumable {
	required public int Damage;
	required public int MaximumRange;
	static ArchetypeQuery<GridPosition>? cachedQuery;

	public ActionProcessor.Result Consume(Entity source, Entity itemEntt, Entity? actionEntity = null) {
		cachedQuery ??= Singleton.World.Query<GridPosition>()
			.AllTags(Tags.Get<Enemy, IsVisible>())
			.WithoutAllTags(Tags.Get<Corpse>());
		var sourcePos = source.GetComponent<GridPosition>();
		var closest = (new Entity(), int.MaxValue);
		cachedQuery.ForEachEntity((ref GridPosition enemyPos, Entity enemyEntt) => {
			int dist = Pathfinder.DiagonalDistance(sourcePos.Value, enemyPos.Value);
			if (dist < closest.Item2) closest = (enemyEntt, dist);
		});
		if (closest.Item1.IsNull || closest.Item2 > MaximumRange) {
			MessageLog.Print($"No enemy in range");
			return ActionProcessor.Result.Invalid;
		}

		// FX and apply on end callback
		var damage = Damage; // captured
		Entity projectile = Prefabs.SpawnProjectile(sourcePos.Value, Prefabs.ConsumableType.LightningDamageScroll);

		Animations.ProjectileFollowFX(projectile, sourcePos.Value.ToWorldPos(), closest.Item1, () => {
			ref var f = ref closest.Item1.GetComponent<Fighter>();
			Combat.ApplyDamage(closest.Item1, source, damage);
			MessageLog.Print($"A lightning bolt strikes {closest.Item1.Name.value} for {damage} damage!");
			actionEntity?.AddTag<IsActionFinished>();
			var explosion = Prefabs.SpawnProjectile(closest.Item1.GetComponent<GridPosition>().Value, Prefabs.ConsumableType.LightningDamageScroll);
			Animations.ExplosionFX(explosion);
		});
		return actionEntity.HasValue ? ActionProcessor.Result.Running : ActionProcessor.Result.Done;
	}
}

struct ConfusionConsumable : IConsumable {
	required public int Turns;

	public ActionProcessor.Result Consume(Entity source, Entity itemEntt, Entity? actionEntity = null) {
		var mouseVal = Singleton.Entity.GetComponent<MouseTarget>().Value;
		if (mouseVal is not Vec2I targetPos) {
			MessageLog.Print($"Not a valid target");
			return ActionProcessor.Result.Invalid;
		}
		ref var grid = ref Singleton.Entity.GetComponent<Grid>();
		var target = grid.Character[targetPos.X, targetPos.Y];
		if (target.IsNull || target == Singleton.Player) {
			MessageLog.Print($"Not a valid target");
			return ActionProcessor.Result.Invalid;
		}

		target.Add(new IsConfused { TurnsRemaining = Turns });
		target.Add(new Team { Value = source.GetComponent<Team>().Value });
		MessageLog.Print($"{target.Name.value} is confused");
		return ActionProcessor.Result.Done;
	}
}

struct FireballConsumable : IConsumable {
	required public int Damage;
	required public int Range;
	static List<Entity> TargetsCache = new();
	static List<Vec2I> HitTilesCache = new();

	public ActionProcessor.Result Consume(Entity source, Entity itemEntt, Entity? actionEntity = null) {
		// TODO check if in line of sight
		var mouseVal = Singleton.Entity.GetComponent<MouseTarget>().Value;
		if (mouseVal is not Vec2I targetPos) {
			MessageLog.Print($"No valid target");
			return ActionProcessor.Result.Invalid;
		}
		ref var grid = ref Singleton.Entity.GetComponent<Grid>();

		int hitCount = 0;
		TargetsCache.Clear();
		HitTilesCache.Clear();
		foreach (var tilePos in AffectedTiles()) {
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

	public IEnumerable<Vec2I> AffectedTiles() {
		var grid = Singleton.Entity.GetComponent<Grid>();
		var mouseVal = Singleton.Entity.GetComponent<MouseTarget>().Value;
		if (mouseVal is not Vec2I targetPos) yield break;

		for (int x = -Range + 1; x < Range; x++) {
			for (int y = -Range + 1; y < Range; y++) {
				var tilePos = new Vec2I(x + targetPos.X, y + targetPos.Y);
				if (!grid.IsInside(tilePos)) continue;
				yield return tilePos;
			}
		}
	}
}

// TODO Use ILinkComponent to Item?
struct ConsumeItemAction : IGameAction {
	required public Entity Target;
	required public Entity Item;

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
		var result = item.Consumable.Consume(action.Target, action.Item, actionEntt);
		switch (result) {
			case ActionProcessor.Result.Done:
			case ActionProcessor.Result.Running:
				action.Item.DeleteEntity();
				break;
		}
		return result;
	}

	private ActionProcessor.Result ProcessPickupAction(ref PickupAction action, Entity actionEntt) {
		ref var grid = ref Singleton.Entity.GetComponent<Grid>();
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