using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

struct IsVisible : ITag;
struct IsExplored : ITag;
struct VisionSource() : IComponent {
	public int Range = 5;
}

class Vision : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.PostApplyActions.Add(new RecalculateVisionSystem());
	}
}

internal class RecalculateVisionSystem : QuerySystem<GridPosition, VisionSource> {
	private ArchetypeQuery<GridPosition> VisibleObjectsQuery;

	protected override void OnAddStore(EntityStore store) {
		base.OnAddStore(store);
		VisibleObjectsQuery = store.Query<GridPosition>().AllTags(Tags.Get<IsVisible>());
		foreach (var query in Queries) {
			query.EventFilter.ComponentAdded<GridPosition>();
		}
	}

	protected override void OnUpdate() {
		Query.ForEachEntity((ref GridPosition pos, ref VisionSource vision, Entity entt) => {
			if (Query.HasEvent(entt.Id)) {
				RecalculateVision(ref pos, ref vision, entt);
			}
		});
	}

	private void RecalculateVision(ref GridPosition source, ref VisionSource vision, Entity entt) {
		// Set all previous visible tiles to explored
		var cmds = CommandBuffer;
		VisibleObjectsQuery.ForEachEntity((ref GridPosition pos, Entity entt) => {
			cmds.RemoveTag<IsVisible>(entt.Id);
		});

		// Calculate new visible tiles
		var grid = Singleton.Entity.GetComponent<Grid>();

		for (int i = -vision.Range; i < vision.Range + 1; i++) {
			for (int j = -vision.Range; j < vision.Range + 1; j++) {
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

		// New FOV algo - inspired by FOV pass in https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter23_Crowd_Pathfinding_and_Steering_Using_Flow_Field_Tiles.pdf
		Queue<(Vec2I, Vec2I, int)> toVisit = new();
		toVisit.Enqueue((source.Value, new Vec2I(0, 0), 0));
		HashSet<Vec2I> visited = [source.Value];
		while (toVisit.Count > 0) {
			var (currPos, fromDir, distance) = toVisit.Dequeue();

			if (grid.IsBlocking(currPos)) {
				var corners = GetCorners(currPos, fromDir);
				var isFOVCorner = !grid.IsBlocking(corners.Item1)
					|| !grid.IsBlocking(corners.Item2);
				if (isFOVCorner) {
					ref var color = ref grid.Tile[currPos.X, currPos.Y].GetComponent<ColorComp>();
					color.Value = Color.Red;
					Console.WriteLine($"fov corner: {currPos}");
				}
				continue;
			}

			if (distance >= vision.Range)
				continue;
			foreach (var neighDir in Grid.NeighborsCardinal) {
				var neighPos = currPos + neighDir;
				if (!grid.IsInsideGrid(neighPos) || visited.Contains(neighPos))
					continue;
				toVisit.Enqueue((neighPos, neighDir, distance + 1));
				visited.Add(neighPos);
			}
		}
		Console.WriteLine($"Finished FOV pass");
	}

	// Only works for cardinal directions
	private (Vec2I, Vec2I) GetCorners(Vec2I currPos, Vec2I fromDir) {
		if (fromDir.Y != 0)
			return (currPos + new Vec2I(1, 0), currPos + new Vec2I(-1, 0));
		else
			return (currPos + new Vec2I(0, 1), currPos + new Vec2I(0, -1));
	}
}