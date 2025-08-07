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
			if (grid.IsVisible(enemyPos.Value)) {
				var playerPos = Singleton.Player.GetComponent<GridPosition>();
				ai.LastFollowPos = playerPos.Value;
			}

			if (ai.LastFollowPos.HasValue) {
				// If has a target go towards that
				var diff = ai.LastFollowPos.Value - enemyPos.Value;
				diff = (Math.Clamp(diff.X, -1, 1), Math.Clamp(diff.Y, -1, 1));
				// TODO proper pathfinding
				var dest = enemyPos.Value + diff;
				Entity destEntt = grid.Character[dest.X, dest.Y];
				if (!destEntt.IsNull && !destEntt.Tags.Has<Enemy>()) {
					TurnsManagement.QueueAction(cmds,
						new MeleeAction(enemyEntt, destEntt, diff.X, diff.Y));
				}
				else {
					var action = new MovementAction(enemyEntt, diff.X, diff.Y);
					TurnsManagement.QueueAction(cmds, action, false);
					Console.WriteLine($"{enemyEntt.GetComponent<Name>().Value} moving towards {ai.LastFollowPos.Value}");
				}
			}
			else {
				// Random movement
				// TODO don't bump into walls
				if (Random.Shared.NextSingle() < 0.5) {
					var action = new MovementAction(
						enemyEntt, Random.Shared.Next(-1, 2), Random.Shared.Next(-1, 2)
					);
					TurnsManagement.QueueAction(cmds, action, false);
				}
			}

			cmds.RemoveTag<CanAct>(enemyEntt.Id);
		});
	}
}