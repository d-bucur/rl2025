using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

struct Energy() : IComponent {
	internal int Current = 0;
	internal required int GainPerTick;
	internal int AmountToAct = 10;
}

struct CanAct : ITag;
// TODO states are a bit contrived. can use less states?
struct IsActionWaiting : ITag;
struct IsActionExecuting : ITag;
struct IsActionFinished : ITag;
struct IsActionBlocking : ITag;

internal struct EscapeAction : IComponent {
	public void Execute(Entity actionEntt) {
		Console.WriteLine("Escape action not implemented");
	}
}

class TurnsManagement : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.ApplyActions.Add(new ProcessActionsSystem());
		UpdatePhases.ProgressTurns.Add(new TickEnergySystem());
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
		actionsInPipelineQuery = store.Query().AnyTags(Tags.Get<IsActionWaiting, IsActionExecuting>());
	}

	protected override void OnUpdate() {
		// Progress only if no actions currently in pipeline
		if (actionsInPipelineQuery.Count > 0)
			return;

		// If nobody can act, then progress through the energy system until someone can
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
		foreach (var entt in finishedQuery.Entities) {
			Console.WriteLine($"deleting finished action: {entt}");
			cmds.DeleteEntity(entt.Id);
		}
		cmds.Playback();

		if (blockingQuery.Count > 0)
			return;

		// if no blocking action then execute new ones
		foreach (var entt in waitingQuery.Entities) {
			// TODO Should wait for currently execting to finish?
			cmds.AddTag<IsActionExecuting>(entt.Id);
			if (entt.Tags.Has<IsActionBlocking>()) {
				break;
			}
		}
		cmds.Playback();
	}
}