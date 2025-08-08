using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

struct Pathfinder : IComponent {
	Grid grid;
	Vec2I goal;
	Dictionary<Vec2I, Visited> visited = new();

	struct Visited {
		public Vec2I cameFrom;
		public int costSoFar;
	}

	public Pathfinder(Grid grid) {
		this.grid = grid;
	}

	public Pathfinder Goal(Vec2I goal) {
		this.goal = goal;
		visited.Clear();
		return this;
	}

	public IEnumerable<Vec2I> PathFrom(Vec2I start) {
		Recalculate(start);
		var p = goal;
		while (p != start) {
			yield return p;
			p = visited[p].cameFrom;
		}
		yield return start;
	}

	private void Recalculate(Vec2I start) {
		// astar
		PriorityQueue<Vec2I, int> frontier = new();

		frontier.Enqueue(start, 0);
		visited[start] = new Visited { cameFrom = Vec2I.Zero, costSoFar = 0 };
		while (frontier.Count > 0) {
			var current = frontier.Dequeue();
			if (current == goal)
				break;

			foreach (var nextDir in Grid.NeighborsCardinal.Concat(Grid.NeighborsDiagonal)) {
				var next = current + nextDir;
				if (!grid.IsInside(next))
					continue;
				var newCost = visited[current].costSoFar + GetCost(next, nextDir);
				if (!visited.ContainsKey(next) || newCost < visited[next].costSoFar) {
					visited[next] = new Visited { cameFrom = current, costSoFar = newCost };
					var priority = newCost + Heuristic(goal, next);
					frontier.Enqueue(next, priority);
				}
			}
		}
	}

	private int Heuristic(Vec2I a, Vec2I b) {
		// diagonal distance
		return Math.Max(a.X - b.X, a.Y - b.Y);
		// Manhattan distance
		// return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
	}

	private int GetCost(Vec2I to, Vec2I dir) {
		// wall
		if (grid.Tile[to.X, to.Y].Tags.Has<BlocksPathing>())
			return 1000;
		// avoid other characters
		else if (!grid.Character[to.X, to.Y].IsNull)
			return 5;
		// diagonal
		else if (Math.Abs(dir.X) > 0 && Math.Abs(dir.Y) > 0)
			return 3;
		// normal
		else
			return 2;
	}
}

class PathfinderModule : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.TurnStart.Add(new ResetPaths());
	}
}

// TODO only reset when gridposition changes
internal class ResetPaths : QuerySystem<Pathfinder, GridPosition> {
	private ArchetypeQuery actionQuery;

	protected override void OnAddStore(EntityStore store) {
		actionQuery = store.Query().AllTags(Tags.Get<IsActionFinished>());
	}

	protected override void OnUpdate() {
		// reset paths only if an action was finished
		if (actionQuery.Count == 0)
			return;
		Query.ForEachEntity((ref Pathfinder pathfinder, ref GridPosition pos, Entity entt) => {
			pathfinder.Goal(pos.Value);
			Console.WriteLine($"Reset Paths");
		});
	}
}