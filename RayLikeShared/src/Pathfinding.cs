using System.Numerics;
using System.Runtime.CompilerServices;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

// TODO pathfinding and PathFrom generate a lot of garbage
struct Pathfinder : IComponent {
	Grid grid;
	internal Vec2I goal;
	internal PriorityQueue<Vec2I, int> frontier = new();
	internal HashSet<Vec2I> frontierItems = new();
	internal Dictionary<Vec2I, Visited> visited = new();
	internal struct Visited {
		public Vec2I cameFrom;
		public int costSoFar;
		public int priority;
	}

	public Pathfinder(Grid grid) {
		this.grid = grid;
	}

	public Pathfinder Goal(Vec2I goal) {
		if (this.goal == goal)
			return this;
		this.goal = goal;
		Reset();
		return this;
	}

	internal void Reset() {
		// Console.WriteLine($"Path reset");
		visited.Clear();
		frontier.Clear();
		frontierItems.Clear();
		frontier.Enqueue(goal, 0);
		frontierItems.Add(goal);
		visited[goal] = new Visited { cameFrom = Vec2I.Zero, costSoFar = 0 };
	}

	/// <summary>
	/// Iterates path from start to goal
	/// </summary>
	internal IEnumerable<Vec2I> PathFrom(Vec2I start) {
		if (grid.CheckTile<BlocksPathing>(start)) {
			// No point to continue, as this would calculate the entire grid
			return [];
		}
		Recalculate(start);
		return PathFromIterator(start);
	}

	IEnumerable<Vec2I> PathFromIterator(Vec2I start) {
		var p = start;
		while (p != goal) {
			yield return p;
			if (!visited.ContainsKey(p)) {
				// This sometimes happens when the mouse is in weird poisitions, like at the start of the game
				// Console.WriteLine($"Invalid point {p} start: {start} to {goal}");
				yield break;
			}
			p = visited[p].cameFrom;
		}
		yield return goal;
	}

	void Recalculate(Vec2I start) {
		// Exit early if path already available
		if (visited.ContainsKey(start) && !frontierItems.Contains(start))
			return;

		// Recalculate priorities for frontier, since start can be different
		// Doesn't make a lot of sense to recalculate existing ones
		// since expanding them all could be done in the same time
		// and would have the same effect of having an up to date frontier.
		// Or could just skip rebuilding, but then the path would not be guaranteed optimal
		RebuildFrontier(start);

		// astar
		int visitedCount = 0;
		while (frontier.Count > 0) {
			var current = frontier.Dequeue();
			frontierItems.Remove(current);
			visitedCount++;

			foreach (var nextDir in Grid.NeighborsCardinal.Concat(Grid.NeighborsDiagonal)) {
				var next = current + nextDir;
				if (!grid.IsInside(next))
					continue;
				int nextCost = GetCost(next, nextDir);
				if (nextCost >= 1000)
					continue;
				var newCost = visited[current].costSoFar + nextCost;
				if (!visited.ContainsKey(next) || newCost < visited[next].costSoFar) {
					var priority = newCost + Heuristic(start, next);
					visited[next] = new Visited { cameFrom = current, costSoFar = newCost, priority = priority };
					frontier.Enqueue(next, priority);
					frontierItems.Add(next);
				}
			}
			// make sure to add neighbors to frontier before to allow incremental builds
			if (current == start)
				break;
		}
		// Console.WriteLine($"Recalculate Path incremental {visitedCount}, visited: {visited.Count}, frontier: {frontier.Count}");
	}

	void RebuildFrontier(Vec2I start) {
		frontier.Clear();
		foreach (var next in frontierItems) {
			var priority = visited[next].costSoFar + Heuristic(start, next);
			frontier.Enqueue(next, priority);
			Visited v = visited[next];
			visited[next] = v with { priority = priority };
		}
	}

	// other heuristics: https://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html#heuristics-for-grid-maps
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	int Heuristic(Vec2I a, Vec2I b) => ManhattanDistance(a, b);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ManhattanDistance(Vec2I a, Vec2I b) {
		return (Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y)) * Config.CostHorizontal;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int DiagonalDistance(Vec2I a, Vec2I b) {
		var dx = Math.Abs(a.X - b.X);
		var dy = Math.Abs(a.Y - b.Y);
		return Config.CostHorizontal * Math.Max(dx, dy)
			+ (Config.CostDiagonal - 2 * Config.CostHorizontal) * Math.Min(dx, dy);
	}

	int GetCost(Vec2I to, Vec2I dir) {
		// wall
		if (grid.CheckTile<BlocksPathing>(to))
			return 1000;
		// avoid unexplored
		else if (!grid.CheckTile<IsExplored>(to))
			return 900;
		// avoid other characters
		else if (!grid.Character[to.X, to.Y].IsNull)
			return 10;
		// diagonal
		else if (Math.Abs(dir.X) > 0 && Math.Abs(dir.Y) > 0)
			return Config.CostDiagonal;
		// normal
		else
			return Config.CostHorizontal;
	}
}

class PathfinderModule : IModule {
	public void Init(EntityStore world) {
		// UpdatePhases.TurnStart.Add(new ResetPaths());
		RenderPhases.Render.Add(new DebugPathfinding());
	}
}

// TODO only reset when gridposition changes
internal class ResetPaths : QuerySystem<Pathfinder, GridPosition> {
	ArchetypeQuery actionQuery;

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

internal class DebugPathfinding : QuerySystem<Pathfinder> {
	RenderTexture2D renderTex;
	Camera camera;

	public DebugPathfinding() {
		Filter.AllTags(Tags.Get<Player>());
		const int Size = 20;
		renderTex = Raylib.LoadRenderTexture(Size, Size);
	}

	protected override void OnUpdate() {
		if (!Singleton.Entity.GetComponent<Settings>().DebugPathfinding)
			return;

		camera = Singleton.Camera.GetComponent<Camera>();
		Raylib.BeginShaderMode(Assets.billboardShader);
		Query.ForEachEntity((ref Pathfinder pathfinder, Entity e) => {
			var frontierItems = pathfinder.frontier.UnorderedItems;
			foreach (var (pos, visited) in pathfinder.visited) {
				// Draw text to texture
				Raylib.BeginTextureMode(renderTex);
				Raylib.ClearBackground(new Color(0, 0, 0, 0));
				Raylib.DrawText($"{visited.costSoFar}", 0, 0, 10, Color.RayWhite);
				if (pathfinder.frontierItems.Contains(pos)) {
					Raylib.DrawText($"{visited.priority}", 8, 10, 10, Color.SkyBlue);
				}
				Raylib.EndTextureMode();

				// draw texture in 3d space
				Raylib.BeginMode3D(camera.Value);
				Raylib.DrawBillboardPro(
					camera.Value,
					renderTex.Texture,
					// height has to be inverted because weird opengl stuff
					new Rectangle(0, renderTex.Texture.Height, renderTex.Texture.Width, -renderTex.Texture.Height),
					pos.ToWorldPos() - Vector3.UnitX,
					-Vector3.UnitZ,
					Vector2.One, Vector2.Zero, 0, Color.White
				);

				Raylib.EndMode3D();
			}
		});
		Raylib.EndShaderMode();
	}
}