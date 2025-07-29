using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

struct IsVisible : ITag;
struct IsExplored : ITag;
struct VisionSource : ITag; // TODO turn into comp with params

class Vision : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.PostApplyActions.Add(new RecalculateVisionSystem());
	}
}

internal class RecalculateVisionSystem : QuerySystem<GridPosition> {
	public RecalculateVisionSystem() => Filter.AllTags(Tags.Get<VisionSource>());
	private ArchetypeQuery<GridPosition> VisibleObjectsQuery;

	protected override void OnAddStore(EntityStore store) {
		base.OnAddStore(store);
		VisibleObjectsQuery = store.Query<GridPosition>().AllTags(Tags.Get<IsVisible>());
		foreach (var query in Queries) {
			query.EventFilter.ComponentAdded<GridPosition>();
		}
	}

	protected override void OnUpdate() {
		Query.ForEachEntity((ref GridPosition pos, Entity entt) => {
			if (Query.HasEvent(entt.Id)) {
				RecalculateVision(ref pos, entt);
			}
		});
	}

	private void RecalculateVision(ref GridPosition source, Entity entt) {
		// Set all previous visible tiles to explored
		var cmds = CommandBuffer;
		VisibleObjectsQuery.ForEachEntity((ref GridPosition pos, Entity entt) => {
			cmds.RemoveTag<IsVisible>(entt.Id);
		});

		// Calculate new visible tiles
		var grid = Singleton.Entity.GetComponent<Grid>();
		int visionDistance = 4;

		for (int i = -visionDistance; i < visionDistance + 1; i++) {
			for (int j = -visionDistance; j < visionDistance + 1; j++) {
				var pos = source.Value + new Vec2I(i, j);
				if (!grid.IsInsideGrid(pos))
					continue;
				Entity tile = grid.Tile[pos.X, pos.Y];
				Entity character = grid.Character[pos.X, pos.Y];
				if (!tile.IsNull) {
					cmds.AddTags(tile.Id, Tags.Get<IsVisible, IsExplored>());
					if (!character.IsNull)
						cmds.AddTag<IsVisible>(character.Id);
				}
			}
		}
	}
}