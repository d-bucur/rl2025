using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

struct EnemyAI : IComponent {
	// add ai logic here
}

class EnemyAIModule : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.Input.Add(new EnemyMovementSystem());
	}
}

file class EnemyMovementSystem : QuerySystem<GridPosition, EnemyAI, PathMovement> {
	public EnemyMovementSystem() => Filter.AllTags(Tags.Get<CanAct>());

	protected override void OnUpdate() {
		var cmds = CommandBuffer;
		Query.ForEachEntity((ref GridPosition enemyPos, ref EnemyAI ai, ref PathMovement path, Entity enemyEntt) => {
			var grid = Singleton.Entity.GetComponent<Grid>();
			// Clear target if it's been reached
			if (path.Destination.HasValue && enemyPos.Value == path.Destination.Value) {
				path.Destination = null;
			}

			// If in range of player visibility then set that as a target
			var playerPos = Singleton.Player.GetComponent<GridPosition>();
			if (grid.CheckTile<IsVisible>(enemyPos.Value)) {
				path.Destination = playerPos.Value;
			}

			if (path.Destination.HasValue) {
				// If has a target go towards that
				// TODO pathfinding caching
				Pathfinder pathfinder = new(grid);
				var newPath = pathfinder
					.Goal(path.Destination.Value)
					.PathFrom(enemyPos.Value)
					.Skip(1).ToList();
				if (newPath.Count == 0) {
					// TODO handle this better. Sometimes enemies are inside a wall tile??
					var debugPath = new Pathfinder(grid)
						.Goal(path.Destination.Value)
						.PathFrom(enemyPos.Value)
						.ToArray();
					Console.WriteLine($"Usually a bug: Couldn't find path from {enemyPos.Value} to {path.Destination.Value}. Path: {debugPath}");
					cmds.RemoveTag<CanAct>(enemyEntt.Id);
					return;
				}
				path.NewDestination(path.Destination.Value, newPath);
				return;
			}
			else {
				// Random movement
				// TODO don't bump into walls
				if (Random.Shared.NextSingle() < 0.5) {
					var action = new MovementAction(
						enemyEntt, Random.Shared.Next(-1, 2), Random.Shared.Next(-1, 2)
					);
					TurnsManagement.QueueAction(cmds, action, enemyEntt);
				}
			}

			cmds.RemoveTag<CanAct>(enemyEntt.Id);
		});
	}
}