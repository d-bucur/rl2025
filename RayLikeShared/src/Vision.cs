using Friflo.Engine.ECS;

namespace RayLikeShared;

struct IsVisible : ITag;
struct IsExplored : ITag;
struct VisionSource : ITag; // TODO turn into comp with params

class Vision : IModule {
	private EntityStore World;
	private ArchetypeQuery<GridPosition> VisibleTilesQuery;

	public void Init(EntityStore world) {
		World = world;
		VisibleTilesQuery = world.Query<GridPosition>().AllTags(Tags.Get<IsVisible>());
		var q = world.Query<GridPosition>().AllTags(Tags.Get<VisionSource>());

		// TODO rewrite using world event so multiple sources are recalculated on change
		foreach (var entt in q.Entities) {
			entt.OnComponentChanged += RecalculateVision;
		}
	}

	private void RecalculateVision(ComponentChanged changed) {
		if (changed.Type != typeof(GridPosition) || changed.Action != ComponentChangedAction.Update)
			return;

		// Set all previous visible tiles to explored
		var cmds = World.GetCommandBuffer();
		VisibleTilesQuery.ForEachEntity((ref GridPosition pos, Entity entt) => {
			cmds.RemoveTag<IsVisible>(entt.Id);
		});
		cmds.Playback();

		// Calculate new visible tiles
		var grid = Singleton.Entity.GetComponent<Grid>();
		int visionDistance = 4;
		var source = changed.Component<GridPosition>();
		Console.WriteLine($"Recalculating vision from {source.Value}");

		for (int i = -visionDistance; i < visionDistance + 1; i++) {
			for (int j = -visionDistance; j < visionDistance + 1; j++) {
				var pos = source.Value + new Vec2I(i, j);
				if (!grid.IsInsideGrid(pos))
					continue;
				Entity entt = grid.Value[pos.X, pos.Y];
				// TODO bug: prev tile is null. Need layers of tiles+characters
				if (!entt.IsNull) {
					entt.AddTag<IsVisible>();
					entt.AddTag<IsExplored>();
				}
			}
		}
	}
}