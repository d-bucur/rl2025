using BehaviorTree;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using BT = BehaviorTree;
using static RayLikeShared.Nodes;

namespace RayLikeShared;

struct EnemyAI() : IComponent {
	public bool isOnPlayerSide; // hack to make it work with current AI
	public BT.BehaviorTree BTree;
	public Dictionary<string, object> Board;
}

class EnemyAIModule : IModule {
	public void Init(EntityStore world) {
		// UpdatePhases.Input.Add(new EnemyMovementSystem());
		UpdatePhases.Input.Add(new EnemyAIBehavior());
	}

	internal static void AddEnemyAI(Entity entt) {
		/* High level description:
		.select()
			.sequence()
				.condition(IsConfused)
				.select()
					.condition(RandomChance) //combine below
					.action(HurtSelf)
				.action(RandomMovement)
			.select()
				.sequence()
					.condition(IsOnPlayerSide)
					.condition(IsEnemyInSight)
					.action(SetDestination(enemy))
				.sequence()
					.condition(IsPlayerVisible) // IsVisible(enemy)
					.action(SetDestination(player))
			.sequence()
				.condition(HasDestination) // combine condition with action below?
				.action(MoveTowards(destination))
			.action(RandomMovement)
		*/
		// TODO Remove board and just pass entity into Tick()? or capture it directly here
		Dictionary<string, object> board = new();
		board["entt"] = entt;
		BT.BehaviorTree tree = new() {
			Root =
			new Select([
				new Sequence([
					new Condition(() => board.Get<Entity>("entt").TryGetRelation<StatusEffect, Type>(typeof(IsConfused), out var statusEffect)),
					new Select([
						RandomChance(IsConfused.HurtSelfChance),
						PrintAction($"{entt.Name.value} hurting self")
					])
				]),
			]),
		};
		entt.Add(new EnemyAI { Board = board, BTree = tree });
	}
}

file class EnemyAIBehavior : QuerySystem<EnemyAI> {
	public EnemyAIBehavior() => Filter.AllTags(Tags.Get<CanAct>());

	protected override void OnUpdate() {
		Query.ForEachEntity((ref EnemyAI ai, Entity enemyEntt) => {
			ai.BTree.Tick();
			CommandBuffer.RemoveTag<CanAct>(enemyEntt.Id);
		});
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

static class Nodes {
	public static BT.Action PrintAction(string text) {
		return new BT.Action(() => Console.WriteLine(text));
	}
	public static Condition RandomChance(float p) {
		return new Condition(() => Random.Shared.NextSingle() < p);
	}
}