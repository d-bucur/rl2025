using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

// TODO could maybe use relationships between Item and HealingConsumable
struct Item : IComponent {
	required public IConsumable Consumable;
}
// a bit redundant, but allows for tag queries
struct ItemTag : ITag;

internal interface IConsumable {
	// TODO should add print message to return type?
	public bool Consume(Entity target, Entity itemEntt);
}
struct HealingConsumable : IConsumable {
	required public int Amount;

	public bool Consume(Entity target, Entity itemEntt) {
		ref var fighter = ref target.GetComponent<Fighter>();
		var amount = (fighter.MaxHP - fighter.HP) * 0.75f;
		int recovered = fighter.Heal((int)amount);
		if (recovered > 0) {
			MessageLog.Print($"You consume the {itemEntt.Name.value} and healed for {recovered} HP");
			return true;
		}
		else {
			MessageLog.Print($"Your health is already full");
			return false;
		}
	}
}

struct LightningDamageConsumable : IConsumable {
	required public int Damage;
	required public int MaximumRange;

	public bool Consume(Entity source, Entity itemEntt) {
		var query = Singleton.World.Query<GridPosition>()
			.AllTags(Tags.Get<Enemy, IsVisible>())
			.WithoutAllTags(Tags.Get<Corpse>());
		var sourcePos = source.GetComponent<GridPosition>();
		var closest = (new Entity(), int.MaxValue);
		query.ForEachEntity((ref GridPosition enemyPos, Entity enemyEntt) => {
			int dist = Pathfinder.DiagonalDistance(sourcePos.Value, enemyPos.Value);
			if (dist < closest.Item2) closest = (enemyEntt, dist);
		});
		if (closest.Item1.IsNull) {
			MessageLog.Print($"No enemy in range");
			return false;
		}
		ref var f = ref closest.Item1.GetComponent<Fighter>();
		Combat.ApplyDamage(closest.Item1, source, Damage);
		MessageLog.Print($"A lightning bolt strikes {closest.Item1.Name.value} for {Damage} damage!");
		return true;
	}
}

struct ConfusionConsumable : IConsumable {
	required public int Turns;

	public bool Consume(Entity source, Entity itemEntt) {
		var mouseVal = Singleton.Entity.GetComponent<MouseTarget>().Value;
		if (mouseVal is not Vec2I targetPos) {
			MessageLog.Print($"No valid target");
			return false;
		}
		ref var grid = ref Singleton.Entity.GetComponent<Grid>();
		var target = grid.Character[targetPos.X, targetPos.Y];
		if (target.IsNull) {
			MessageLog.Print($"No valid target");
			return false;
		}

		target.Add(new IsConfused { TurnsRemaining = Turns });
		target.Add(new Team { Value = source.GetComponent<Team>().Value });
		MessageLog.Print($"{target.Name.value} is confused");
		return true;
	}
}

struct FireballConsumable : IConsumable {
	required public int Damage;
	required public int Range;

	public bool Consume(Entity source, Entity itemEntt) {
		// TODO check if in line of sight
		var mouseVal = Singleton.Entity.GetComponent<MouseTarget>().Value;
		if (mouseVal is not Vec2I targetPos) {
			MessageLog.Print($"No valid target");
			return false;
		}
		ref var grid = ref Singleton.Entity.GetComponent<Grid>();

		int hitCount = 0;
		foreach (var tilePos in AffectedTiles()) {
			var target = grid.Character[tilePos.X, tilePos.Y];
			if (target.IsNull) continue;
			hitCount++;
			Combat.ApplyDamage(target, source, Damage, tilePos - targetPos);
		}

		if (hitCount == 0) {
			MessageLog.Print($"No valid target in range");
			return false;
		}

		MessageLog.Print($"Hit {hitCount} targets with fireball");
		return true;
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
		if (action.Item.GetComponent<Item>().Consumable.Consume(action.Target, action.Item)) {
			action.Item.DeleteEntity();
			return ActionProcessor.Result.Done;
		}
		else return ActionProcessor.Result.Invalid;
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
			MessageLog.Print($"You picked up {item.Name.value}");
			PrefabTransformations.PickupItem(item);
			action.Target.AddRelation(new InventoryItem { Item = item });
			pickedCount++;
		}
		if (pickedCount == 0) {
			MessageLog.Print($"You couldn't find anything");
			return ActionProcessor.Result.Invalid;
		}
		else return ActionProcessor.Result.Done;
	}
}