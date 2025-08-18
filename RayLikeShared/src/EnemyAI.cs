using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

struct EnemyAI : IComponent {
	public bool isOnPlayerSide; // hack to make it work with current AI
}

class EnemyAIModule : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.Input.Add(new EnemyMovementSystem());
	}
}

file class EnemyMovementSystem : QuerySystem<GridPosition, EnemyAI, PathMovement, Pathfinder> {
	public EnemyMovementSystem() => Filter.AllTags(Tags.Get<CanAct>());

	protected override void OnUpdate() {
		// TODO Super spaghetti. Should add proper state machine/behavior tree
		var cmds = CommandBuffer;
		Query.ForEachEntity((ref GridPosition enemyPos, ref EnemyAI ai, ref PathMovement path, ref Pathfinder pathfinder, Entity enemyEntt) => {
			ref var usedPathfinder = ref pathfinder;
			bool isConfused = enemyEntt.TryGetRelation<StatusEffect, Type>(typeof(IsConfused), out var statusEffect);
			if (isConfused) {
				// Chance to hurt itself
				if (Random.Shared.NextSingle() < IsConfused.HurtSelfChance) {
					TurnsManagement.QueueAction(cmds, new MeleeAction(enemyEntt, enemyEntt, 0, 1), enemyEntt);
					MessageLog.Print($"{enemyEntt.Name.value} hurt itself in its confusion!", Raylib_cs.Color.Orange);
					cmds.RemoveTag<CanAct>(enemyEntt.Id);
					return;
				}
			}
			else {
				if (ai.isOnPlayerSide) {
					var closest = GetClosestEnemy(enemyEntt, enemyPos.Value);
					if (!closest.IsNull) {
						path.Destination = closest.GetComponent<GridPosition>().Value;
						usedPathfinder.Goal(path.Destination.Value);
					}
				}
				else {
					// If in range of player visibility then set that as a target
					bool shouldChasePlayer = Singleton.Get<Grid>().CheckTile<IsVisible>(enemyPos.Value);
					if (shouldChasePlayer && !isConfused) {
						path.Destination = Singleton.Player.GetComponent<GridPosition>().Value;
						usedPathfinder = ref Singleton.Player.GetComponent<Pathfinder>();
						usedPathfinder.Goal(path.Destination.Value);
					}
				}
			}

			// Move to destination
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
						.Where(p => !Singleton.Get<Grid>().BlocksPathing(pos + p))
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

	private Entity GetClosestEnemy(Entity sourceEntt, Vec2I sourcePos) {
		var query = Singleton.World.Query<GridPosition, Team>().WithoutAllTags(Tags.Get<Corpse>());
		var closest = (new Entity(), int.MaxValue);
		var sourceTeam = sourceEntt.GetComponent<Team>().Value;
		query.ForEachEntity((ref GridPosition targetPos, ref Team targetTeam, Entity targetEntt) => {
			if (targetTeam.Value == sourceTeam) return;
			int dist = Pathfinder.DiagonalDistance(sourcePos, targetPos.Value);
			if (dist < closest.Item2) closest = (targetEntt, dist);
		});
		return closest.Item1;
	}
}