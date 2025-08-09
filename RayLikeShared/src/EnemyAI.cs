using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

struct EnemyAI : IComponent {
	public Vec2I? LastFollowPos;
}

class EnemyAIModule : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.Input.Add(new EnemyMovementSystem());
	}
}

file class EnemyMovementSystem : QuerySystem<GridPosition, EnemyAI> {
	public EnemyMovementSystem() => Filter.AllTags(Tags.Get<CanAct>());

	protected override void OnUpdate() {
		var cmds = CommandBuffer;
		Query.ForEachEntity((ref GridPosition enemyPos, ref EnemyAI ai, Entity enemyEntt) => {
			var grid = Singleton.Entity.GetComponent<Grid>();
			// Clear target if it's been reached
			if (ai.LastFollowPos.HasValue && enemyPos.Value == ai.LastFollowPos.Value) {
				ai.LastFollowPos = null;
			}

			// If in range of player visibility then set that as a target
			if (grid.Check<IsVisible>(enemyPos.Value)) {
				var playerPos = Singleton.Player.GetComponent<GridPosition>();
				ai.LastFollowPos = playerPos.Value;
			}

			if (ai.LastFollowPos.HasValue) {
				// If has a target go towards that
				// TODO pathfinding caching
				// TODO sometimes empty sequence?
				var dest = new Pathfinder(grid)
					.Goal(ai.LastFollowPos.Value)
					.PathFrom(enemyPos.Value)
					.Skip(1).First();
				var diff = dest - enemyPos.Value;

				Entity destEntt = grid.Character[dest.X, dest.Y];
				if (!destEntt.IsNull && !destEntt.Tags.Has<Enemy>()) {
					TurnsManagement.QueueAction(cmds,
						new MeleeAction(enemyEntt, destEntt, diff.X, diff.Y), true);
				}
				else {
					var action = new MovementAction(enemyEntt, diff.X, diff.Y);
					bool isActionBlocking = enemyEntt.Tags.Has<IsVisible>();
					TurnsManagement.QueueAction(cmds, action, isActionBlocking);
				}
			}
			else {
				// Random movement
				// TODO don't bump into walls
				if (Random.Shared.NextSingle() < 0.5) {
					var action = new MovementAction(
						enemyEntt, Random.Shared.Next(-1, 2), Random.Shared.Next(-1, 2)
					);
					TurnsManagement.QueueAction(cmds, action, enemyEntt.Tags.Has<IsVisible>());
				}
			}

			cmds.RemoveTag<CanAct>(enemyEntt.Id);
		});
	}
}