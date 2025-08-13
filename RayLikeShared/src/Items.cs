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
	public bool Consume(Entity target, Entity itemEntt);
}
struct HealingConsumable : IConsumable {
	required public int Amount;

	public bool Consume(Entity target, Entity itemEntt) {
		ref var fighter = ref target.GetComponent<Fighter>();
		int recovered = fighter.Heal(Amount);
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

// Use ILinkComponent?? causes fragmentation?
struct ConsumeItemAction : IGameAction {
	required public Entity Target;
	required public Entity Item;
	public Vec2I? pos;

	public Entity GetIndexedValue() {
		return Item;
	}
}

struct PickupAction : IComponent {
	required public Entity Target;
	required public Vec2I Position;
}

// TODO will need to store capacity somewhere
struct InventoryItem : ILinkRelation {
	public Entity Item;
	public Entity GetRelationKey() {
		return Item;
	}
}

class Items : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.ApplyActions.Add(new ProcessPickupActions());
		UpdatePhases.ApplyActions.Add(ActionProcessor.FromFunc<ConsumeItemAction>(ProcessConsumeItem));
	}

	bool ProcessConsumeItem(ref ConsumeItemAction action, Entity actionEntt) {
		if (action.Item.GetComponent<Item>().Consumable.Consume(action.Target, action.Item)) {
			action.Item.DeleteEntity();
		}
		return true;
	}
}

internal class ProcessPickupActions : QuerySystem<PickupAction> {
	public ProcessPickupActions() => Filter.AllTags(Tags.Get<IsActionExecuting, IsActionWaiting>());

	protected override void OnUpdate() {
		var cmds = CommandBuffer;
		Query.ThrowOnStructuralChange = false;
		Query.ForEachEntity((ref PickupAction action, Entity entt) => {
			cmds.RemoveTag<IsActionWaiting>(entt.Id);
			ref var grid = ref Singleton.Entity.GetComponent<Grid>();

			var others = grid.Others[action.Position.X, action.Position.Y];

			int pickedCount = 0;
			foreach (var item in new List<Entity>(others?.Value ?? [])) {
				if (!item.Tags.Has<ItemTag>()) continue;
				MessageLog.Print($"You picked up {item.Name.value}");
				PrefabTransformations.PickupItem(item);
				action.Target.AddRelation(new InventoryItem { Item = item });
				pickedCount++;
			}
			if (pickedCount == 0) MessageLog.Print($"You couldn't find anything");
			cmds.AddTag<IsActionFinished>(entt.Id);
		});
	}
}