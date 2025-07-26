using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

// Enables turn taking based on energy levels
struct Energy() : IComponent {
	internal int Current = 0;
	internal required int GainPerTick;
	internal int AmountToAct = 10;
}
// Added once Energy has reached a threshold to act (take a turn)
struct CanAct : ITag;

// Is waiting for execution, similar to a queue. Will be removed once the action is processed (once)
struct IsActionWaiting : ITag;
// Has been cleared for execution. Not that it can be both waiting and executing at the same time
struct IsActionExecuting : ITag;
// Is added once the action has finished everything like animations etc
struct IsActionFinished : ITag;
// Marks that other actions should not be executed until this is finished
struct IsActionBlocking : ITag;

internal struct EscapeAction : IComponent {}

class TurnsManagement : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.ProgressTurns.Add(new TickEnergySystem());
		UpdatePhases.ApplyActions.Add(new ProcessActionsSystem());
	}

	public static void QueueAction<T>(CommandBuffer cmd, T movementAction, bool isActionBlocking = true) where T : struct, IComponent {
		var a = cmd.CreateEntity();
		cmd.AddComponent(a, movementAction);
		if (isActionBlocking)
			cmd.AddTags(a, Tags.Get<IsActionWaiting, IsActionBlocking>());
		else
			cmd.AddTags(a, Tags.Get<IsActionWaiting>());
	}
}

internal class TickEnergySystem : QuerySystem<Energy> {
	private ArchetypeQuery canActQuery;
	private ArchetypeQuery actionsInPipelineQuery;

	protected override void OnAddStore(EntityStore store) {
		canActQuery = store.Query().AllTags(Tags.Get<CanAct>());
		// tried to wait on blocking actions only, but then non blocking ones bug out if done fast
		actionsInPipelineQuery = store.Query().AnyTags(Tags.Get<IsActionWaiting, IsActionExecuting>());
	}

	protected override void OnUpdate() {
		// Progress only if no actions currently in pipeline
		if (actionsInPipelineQuery.Count > 0)
			return;

		// If nobody can act, then progress through the energy components until someone can
		var buffer = CommandBuffer;
		while (canActQuery.Count == 0) {
			Query.ForEachEntity((ref Energy energy, Entity e) => {
				energy.Current += energy.GainPerTick;
				var remaining = energy.Current - energy.AmountToAct;
				// Console.WriteLine($"Progressing {e} to energy {energy.Current}");
				if (remaining >= 0) {
					Console.WriteLine($"{e}'s turn to act");
					energy.Current = remaining;
					buffer.AddTag<CanAct>(e.Id);
				}
			});
			buffer.Playback();
		}
	}
}

/// <summary>
/// Does maintenance of currently executing actions
/// </summary>
internal class ProcessActionsSystem : QuerySystem {
	private ArchetypeQuery finishedQuery;
	private ArchetypeQuery waitingQuery;
	private ArchetypeQuery blockingQuery;

	protected override void OnAddStore(EntityStore store) {
		finishedQuery = store.Query().AllTags(Tags.Get<IsActionFinished>());
		waitingQuery = store.Query().AllTags(Tags.Get<IsActionWaiting>());
		blockingQuery = store.Query().AllTags(Tags.Get<IsActionBlocking, IsActionExecuting>());
	}

	protected override void OnUpdate() {
		var cmds = CommandBuffer;
		// Remove finished actions
		foreach (var entt in finishedQuery.Entities) {
			Console.WriteLine($"deleting finished action: {entt}");
			cmds.DeleteEntity(entt.Id);
		}
		cmds.Playback();

		// if no blocking action then clear new ones for execution
		if (blockingQuery.Count > 0)
			return;
		foreach (var entt in waitingQuery.Entities) {
			cmds.AddTag<IsActionExecuting>(entt.Id);
			if (entt.Tags.Has<IsActionBlocking>()) {
				break;
			}
		}
		cmds.Playback();
	}
}