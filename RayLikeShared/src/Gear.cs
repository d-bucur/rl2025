using Friflo.Engine.ECS;

namespace RayLikeShared;

class GearModule : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.ApplyActions.Add(ActionProcessor.FromFunc<EquipAction>(ProcessEquipAction));
	}

	private ActionProcessor.Result ProcessEquipAction(ref EquipAction action, Entity actionEntt) {
		ref var grid = ref Singleton.Get<Grid>();
		var pos = action.Target.GetComponent<GridPosition>().Value;
		var others = grid.Others[pos.X, pos.Y];
		var wornGear = action.Target.GetRelations<WornGear>();
		ref var fighter = ref action.Target.GetComponent<Fighter>();

		var wearerEntt = action.Target;
		foreach (var item in new List<Entity>(others?.Value ?? [])) {
			// for each item on the ground
			if (!item.HasComponent<Gear>()) continue;
			MessageLog.Print($"You picked up {item.Name.value}");
			var newGear = item.GetComponent<Gear>();
			// check if there is already an item in the same slot
			// weird bug sometimes: An unhandled exception of type 'System.InvalidOperationException' occurred in Friflo.Engine.ECS.dll: 'Relations<WornGear> outdated. Added / Removed relations after calling GetRelations<WornGear>().'
			foreach (var wornEntt in wornGear) {
				Gear oldGear = wornEntt.Gear.GetComponent<Gear>();
				if (oldGear.GearType == newGear.GearType) {
					// same slot taken. Drop current item
					PrefabTransformations.DropItem(wornEntt.Gear, pos);
					wearerEntt.RemoveRelation<WornGear>(wornEntt.Gear);
					MessageLog.Print($"You dropped {wornEntt.Gear.Name.value}");
					fighter.ApplyGear(oldGear, -1);
				}
			}
			// Add new item to worn gear
			wearerEntt.AddRelation(new WornGear { Gear = item });
			fighter.ApplyGear(newGear);
			Animations.PickupItem(action.Target, item, () => {
				PrefabTransformations.PickupItem(item);
			});
			return ActionProcessor.Result.Done;
		}
		MessageLog.Print($"You couldn't find any gear to pick up");
		return ActionProcessor.Result.Invalid;
	}
}

struct EquipAction : IGameAction {
	required public Entity Target;
	public Entity GetSource() => Target;
}

struct WornGear : ILinkRelation {
	public Entity Gear;
	public Entity GetRelationKey() => Gear;
}

enum GearType {
	Weapon,
	Armor,
}

struct Gear : IComponent {
	required public GearType GearType;
	public int PowerDelta;
	public int DefenseDelta;
	public int HPDelta;
}

struct GearTag : ITag;