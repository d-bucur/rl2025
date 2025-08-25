using BehaviorTree;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using BT = BehaviorTree;
using static RayLikeShared.BTExtension;

namespace RayLikeShared;

struct EnemyAI() : IComponent {
	public BT.BehaviorTree BTree;
	public bool isOnPlayerSide; // hack to make it work with current AI
	public BTLog LastExecution = new();
}

class EnemyAIModule : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.Input.Add(new EnemyAIBehavior());
	}

	internal static void AddEnemyAI(Entity entt) {
		BT.BehaviorTree tree = new() {
			Root =
			new Select("Root", true, [
				new Sequence("ConfusedBranch", [
					new Condition(IsConfusedCond).Named("IsConfused"),
					new Select("ConfusionChoice", [
						new Sequence("Self hurt", [
							RandomChance(IsConfused.HurtSelfChance).Named("HurtChance"),
							new Do(HurtSelf).Named("HurtSelf"),
						]),
						new Do(RandomMovement).Named("RandomMovement"),
					]),
				]),
				new Force(BTStatus.Failure,
					new Select("ChooseDestination", false, [
						// can probably unite these 2 branches
						new Sequence("Enemies", [
							new Condition(IsInPlayerView).Named("IsInPlayerView"),
							new Do(MoveToClosestEnemy).Named("MoveToClosestEnemy"),
						]),
						new Sequence("Friendlies", [
							new Condition(IsOnPlayerSide).Named("IsOnPlayerTeam"),
							new Do(MoveToClosestEnemy).Named("MoveToClosestEnemy"),
						]),
					])
				).Named("DestinationSelection"),
				new Do(UseAbilities).Named("UseAbilities"),
				new Do(MoveToDestination()).Named("MoveToDestination"),
				new Do(RandomMovement).Named("RandomMovement"),
			]),
		};
		entt.Add(new EnemyAI { BTree = tree });
	}
}

file class EnemyAIBehavior : QuerySystem<EnemyAI, GridPosition> {
	public EnemyAIBehavior() => Filter.AllTags(Tags.Get<CanAct>()).WithoutAllTags(Tags.Get<Corpse>());

	protected override void OnUpdate() {
		Query.ForEachEntity((ref EnemyAI ai, ref GridPosition pos, Entity enemyEntt) => {
			var ctx = new Context {
				Entt = enemyEntt,
				Pos = pos.Value,
				cmds = CommandBuffer,
			};
			var status = ai.BTree.Tick(ref ctx);
			// Console.WriteLine($"Ticked {enemyEntt.Name.value}: {status}");
			ai.LastExecution = ctx.ExecutionLog;
			CommandBuffer.RemoveTag<CanAct>(enemyEntt.Id);
		});
	}
}

// TODO Remove after testing new AI
file class EnemyMovementSystemOld : QuerySystem<GridPosition, EnemyAI, PathMovement, Pathfinder> {
	public EnemyMovementSystemOld() => Filter.AllTags(Tags.Get<CanAct>());

	protected override void OnUpdate() {
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
}

static class BTExtension {
	public static Do PrintAction(string text) =>
		new((ref Context c) => {
			Console.WriteLine(text);
			return BTStatus.Success;
		}) { Name = text };

	public static Condition RandomChance(float p) =>
		new((ref Context c) => Random.Shared.NextSingle() < p);

	public static bool IsOnPlayerSide(ref Context c) =>
		Singleton.Player.GetComponent<Team>().Value == c.Entt.GetComponent<Team>().Value;

	public static BTStatus HurtSelf(ref Context ctx) {
		TurnsManagement.QueueAction(ctx.cmds, new MeleeAction(ctx.Entt, ctx.Entt, 0, 1), ctx.Entt);
		MessageLog.Print($"{ctx.Entt.Name.value} hurt itself in its confusion!", Raylib_cs.Color.Orange);
		return BTStatus.Success;
	}

	public static bool IsInPlayerView(ref Context c) =>
		c.Entt.Tags.Has<IsVisible>();

	public static bool IsConfusedCond(ref Context c) =>
		c.Entt.TryGetRelation<StatusEffect, Type>(typeof(IsConfused), out var statusEffect);

	public static BTStatus RandomMovement(ref Context ctx) {
		var pos = ctx.Pos;
		var moves = Grid.NeighborsCardinal.Concat(Grid.NeighborsDiagonal)
			.Where(p => !Singleton.Get<Grid>().BlocksPathing(pos + p))
			.ToList();

		if (moves.Count > 0) {
			var randomMove = moves[Random.Shared.Next(moves.Count)];
			TurnsManagement.QueueAction(ctx.cmds,
				new MovementAction(ctx.Entt, randomMove.X, randomMove.Y), ctx.Entt);
		}
		return BTStatus.Success;
	}

	// public static bool HasDestination(ref Context c) =>
	// 	c.Entt.GetComponent<PathMovement>().Destination.HasValue;
	public static ActionFunc SetDestination(Entity target) {
		return (ref Context ctx) => {
			ref var path = ref ctx.Entt.GetComponent<PathMovement>();
			path.Destination = target.GetComponent<GridPosition>().Value;
			return BTStatus.Success;
		};
	}

	public static BTStatus MoveToClosestEnemy(ref Context ctx) {
		var closest = GetClosestEnemy(ctx.Entt, ctx.Pos);
		if (!closest.IsNull) {
			ref var path = ref ctx.Entt.GetComponent<PathMovement>();
			path.Destination = closest.GetComponent<GridPosition>().Value;
			return BTStatus.Success;
		}
		return BTStatus.Failure;
	}

	public static ActionFunc MoveToDestination() {
		return (ref Context ctx) => {
			ref var path = ref ctx.Entt.GetComponent<PathMovement>();
			if (!path.Destination.HasValue) return BTStatus.Failure;

			// TODO minor enemies do not reuse hero pathfinder
			ref var pathfinder = ref ctx.Entt.GetComponent<Pathfinder>();
			// If destination is set follow path towards it
			var newPath = pathfinder
				.Goal(path.Destination.Value)
				.PathFrom(ctx.Pos)
				.Skip(1).ToList();
			if (newPath.Count == 0) {
				// TODO still a bug sometimes
				var debugPath = pathfinder
					.Goal(path.Destination.Value)
					.PathFrom(ctx.Pos)
					.ToList();
				Console.WriteLine($"Usually a bug: Couldn't find path from {ctx.Pos} to {path.Destination.Value}. Path: {debugPath}");
				return BTStatus.Failure;
			}
			path.NewDestination(path.Destination.Value, newPath);
			return BTStatus.Running;
		};
	}

	internal static BTStatus UseAbilities(ref Context ctx) {
		// TODO add cooldown
		// TODO add LOS
		var items = ctx.Entt.GetRelations<InventoryItem>();
		var closest = GetClosestEnemy(ctx.Entt, ctx.Pos);
		if (closest.IsNull || items.Length < 1 || !ctx.Entt.Tags.Has<IsVisible>()) return BTStatus.Failure;

		Vec2I closestPos = closest.GetComponent<GridPosition>().Value;
		var dist = Pathfinder.DiagonalDistance(closestPos, ctx.Pos);
		if (dist > 8 || dist < 4) return BTStatus.Failure;

		TurnsManagement.QueueAction(ctx.cmds,
			new ConsumeItemAction {
				Target = ctx.Entt,
				Item = items[0].Item,
				Pos = closestPos
			},
			ctx.Entt
		);
		return BTStatus.Success;
	}

	internal static Entity GetClosestEnemy(Entity sourceEntt, Vec2I sourcePos) {
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
