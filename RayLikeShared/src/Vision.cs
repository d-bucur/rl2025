using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

struct IsVisible : ITag;
struct IsExplored : ITag;
struct VisionSource() : IComponent {
	public required int Range;
}

class Vision : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.PostApplyActions.Add(new RecalculateVisionSystem());
	}
}

internal class RecalculateVisionSystem : QuerySystem<GridPosition, VisionSource> {
	private ArchetypeQuery<GridPosition> VisibleObjectsQuery;
	private ArchetypeQuery<ColorComp> ColoredQuery;

	protected override void OnAddStore(EntityStore store) {
		base.OnAddStore(store);
		VisibleObjectsQuery = store.Query<GridPosition>().AllTags(Tags.Get<IsVisible>());
		ColoredQuery = store.Query<ColorComp>();
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

	// Calculate new visible tiles
	private void RecalculateVision(ref GridPosition source, ref VisionSource vision, Entity _) {
		// Set all previous visible tiles to explored
		var cmds = CommandBuffer;
		VisibleObjectsQuery.ForEachEntity((ref GridPosition pos, Entity entt) => {
			cmds.RemoveTag<IsVisible>(entt.Id);
		});

		// reset debug colors
		ColoredQuery.ForEachEntity((ref ColorComp color, Entity _) => {
			color.Value = color.InitialValue;
		});

		// New FOV algo - inspired by FOV pass in https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter23_Crowd_Pathfinding_and_Steering_Using_Flow_Field_Tiles.pdf
		// breadth first search, mark FOV corners, draw lines from corners and block further search for tiles on the line
		var grid = Singleton.Entity.GetComponent<Grid>();
		Queue<(Vec2I, Vec2I)> toVisit = new();
		toVisit.Enqueue((source.Value, new Vec2I(0, 0)));
		HashSet<Vec2I> visited = [source.Value];
		HashSet<Vec2I> fovBlocked = [];
		while (toVisit.Count > 0) {
			var (currPos, fromDir) = toVisit.Dequeue();
			if (fovBlocked.Contains(currPos))
				continue;

			grid.MarkVisible(currPos, cmds);

			if (grid.IsBlocking(currPos)) {
				// Is a wall. Check if it's an FOV corner
				var corners = GetCorners(currPos, fromDir);
				var isFOVCorner = !grid.IsBlocking(corners.Item1)
					|| !grid.IsBlocking(corners.Item2);
				if (!isFOVCorner) {
					continue;
				}
				// TODO 2 lines to circle edges for better accuracy
				// Given a line form source to dest, returns the line continued behind it, clamped by distance
				Vector2 dirFloat = (currPos - source.Value).ToVector2();
				float dirLen = dirFloat.Length();
				var lineDir = (Vec2I)(dirFloat / dirLen * (vision.Range - dirLen));

				foreach (var linePoint in LinePoints(currPos, currPos + lineDir)) {
					if (currPos == linePoint)
						continue;
					fovBlocked.Add(linePoint);
					// grid.SetColorHelper(linePoint, Palette.DebugFOVBlocked);
					// not super sure about this early exit
					// if (grid.Tile[linePoint.X, linePoint.Y].Tags.Has<BlocksFOV>())
					// 	break;
				}
				// grid.SetColorHelper(currPos, Palette.DebugFOVCorner);
			}
			else {
				// Is not a wall. Queue neighbors to visit
				foreach (var neighDir in Grid.NeighborsCardinal) {
					var neighPos = currPos + neighDir;
					if (!grid.IsInsideGrid(neighPos)
						|| visited.Contains(neighPos)
						|| fovBlocked.Contains(neighPos))
						continue;
					if (MathF.Round((neighPos - source.Value).ToVector2().Length()) >= vision.Range)
						continue;
					toVisit.Enqueue((neighPos, neighDir));
					visited.Add(neighPos);
				}
			}
		}
	}

	// Enumerate discrete points on a line from p0 to p1
	private IEnumerable<Vec2I> LinePoints(Vec2I p0, Vec2I p1) {
		// using general version of https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm#All_cases
		int dx = Math.Abs(p1.X - p0.X);
		int sx = p0.X < p1.X ? 1 : -1;
		int dy = -Math.Abs(p1.Y - p0.Y);
		int sy = p0.Y < p1.Y ? 1 : -1;
		int error = dx + dy;

		while (true) {
			yield return new Vec2I(p0.X, p0.Y);
			var e2 = 2 * error;
			if (e2 >= dy) {
				if (p0.X == p1.X)
					break;
				error += dy;
				p0.X += sx;
			}
			if (e2 <= dx) {
				if (p0.Y == p1.Y)
					break;
				error += dx;
				p0.Y += sy;
			}
		}
	}

	// Get the 2 neighbors perpendicular to the direction. Only works for cardinal directions
	private (Vec2I, Vec2I) GetCorners(Vec2I currPos, Vec2I dir) {
		if (dir.Y != 0)
			return (currPos + new Vec2I(1, 0), currPos + new Vec2I(-1, 0));
		else
			return (currPos + new Vec2I(0, 1), currPos + new Vec2I(0, -1));
	}

	// Old, simple visibility algorithm marking all tiles in a square radius around the source
	private void SimpleVisionWalk(GridPosition source, VisionSource vision) {
		var grid = Singleton.Entity.GetComponent<Grid>();
		for (int i = -vision.Range; i < vision.Range + 1; i++) {
			for (int j = -vision.Range; j < vision.Range + 1; j++) {
				var pos = source.Value + new Vec2I(i, j);
				grid.MarkVisible(pos, CommandBuffer);
			}
		}
	}
}