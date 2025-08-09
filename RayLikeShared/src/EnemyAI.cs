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

file class EnemyMovementSystem : QuerySystem<GridPosition, EnemyAI, PathMovement, Pathfinder> {
	public EnemyMovementSystem() => Filter.AllTags(Tags.Get<CanAct>());

	protected override void OnUpdate() {
		var cmds = CommandBuffer;
		Query.ForEachEntity((ref GridPosition enemyPos, ref EnemyAI ai, ref PathMovement path, ref Pathfinder pathfinder, Entity enemyEntt) => {
			var grid = Singleton.Entity.GetComponent<Grid>();
			ref var usedPathfinder = ref pathfinder;
			// If in range of player visibility then set that as a target
			bool shouldChasePlayer = grid.CheckTile<IsVisible>(enemyPos.Value);
			if (shouldChasePlayer) {
				path.Destination = Singleton.Player.GetComponent<GridPosition>().Value;
				usedPathfinder = ref Singleton.Player.GetComponent<Pathfinder>();
				usedPathfinder.Goal(path.Destination.Value);
			}
			if (path.Destination.HasValue) {
				// If destination is set follow path towards it
				var newPath = usedPathfinder
					.Goal(path.Destination.Value)
					.PathFrom(enemyPos.Value)
					.Skip(1).ToList();
				if (newPath.Count == 0) {
					// TODO still a bug here?
					var debugPath = usedPathfinder
						.Goal(path.Destination.Value)
						.PathFrom(enemyPos.Value)
						.ToList();
					Console.WriteLine($"Usually a bug: Couldn't find path from {enemyPos.Value} to {path.Destination.Value}. Path: {debugPath}");
					cmds.RemoveTag<CanAct>(enemyEntt.Id);
					return;
				}
				path.NewDestination(path.Destination.Value, newPath);
			}
			else {
				// Random movement
				if (Random.Shared.NextSingle() < 0.75) {
					var pos = enemyPos.Value;
					var moves = Grid.NeighborsCardinal.Concat(Grid.NeighborsDiagonal)
						.Where(p => !grid.BlocksPathing(pos + p))
						.ToList();

					if (moves.Count > 0) {
						var randomMove = moves[Random.Shared.Next(moves.Count)];
						TurnsManagement.QueueAction(CommandBuffer,
							new MovementAction(enemyEntt, randomMove.X, randomMove.Y), enemyEntt);
					}
				}
				cmds.RemoveTag<CanAct>(enemyEntt.Id);
			}
		});
	}
}