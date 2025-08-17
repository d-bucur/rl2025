using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

// Enables turn taking based on energy levels
struct Energy() : IComponent {
	internal int Current = 0;
	internal required int GainPerTick;
	internal int AmountToAct = 10;
	internal int TickProcessed = 0;
}
// Added once Energy has reached a threshold to act (take a turn)
struct CanAct : ITag;

// Is waiting for execution, similar to a queue. Will be removed once the action is processed (once)
struct IsActionWaiting : ITag;
// Has been cleared for execution. Note that it can be both waiting and executing at the same time
struct IsActionExecuting : ITag;
// Is added once the action has finished everything like animations etc
struct IsActionFinished : ITag;
// Marks that other actions should not be executed until this is finished
struct IsActionBlocking : ITag;

// Workaround to not use an event/signal for this
struct TurnStarted : ITag;

struct TurnData() : IComponent {
	internal int CurrentTick = 1;
}

class TurnsManagement : IModule {

	public void Init(EntityStore world) {
		Singleton.Entity.AddComponent(new TurnData());

		UpdatePhases.ProgressTurns.Add(new TickEnergySystem());
		UpdatePhases.ApplyActions.Add(new ProcessActionsSystem());
	}

	internal static void QueueAction<T>(CommandBuffer cmd, T action, Entity entt) where T : struct, IComponent {
		var a = cmd.CreateEntity();
		cmd.AddComponent(a, action);
		if (IsEntityImporant(entt))
			cmd.AddTags(a, Tags.Get<IsActionWaiting, IsActionBlocking>());
		else
			cmd.AddTags(a, Tags.Get<IsActionWaiting>());
	}

	internal static bool IsEntityImporant(Entity entt) {
		if (entt == Singleton.Player)
			return true;
		var enttPos = entt.GetComponent<GridPosition>().Value;
		var playerPos = Singleton.Player.GetComponent<GridPosition>().Value;

		return entt.Tags.Has<IsVisible>()
			&& Pathfinder.DiagonalDistance(enttPos, playerPos) <= 6;
	}

	static List<(Energy, Entity)> EnergyCache = new();
	// Same as TickEnergySystem. See comments on that
	internal static IEnumerable<(Entity, float)> SimTurns(int max = 10) {
		var query = Singleton.World.Query<Energy>().AllTags(Tags.Get<IsVisible>());
		var turnData = Singleton.Get<TurnData>();
		EnergyCache.Clear();
		query.ForEachEntity((ref Energy energy, Entity entity) => EnergyCache.Add((energy, entity)));
		EnergyCache.Sort((e1, e2) => e1.Item2.Id - e2.Item2.Id);
		int totalActed = 0;
		while (true) {
			for (int i = 0; i < EnergyCache.Count; i++) {
				var (energy, entt) = EnergyCache[i];
				if (energy.TickProcessed >= turnData.CurrentTick) continue;
				energy.TickProcessed = turnData.CurrentTick;
				energy.Current += energy.GainPerTick;
				var remaining = energy.Current - energy.AmountToAct;
				if (remaining >= 0) {
					energy.Current = remaining;
					yield return (entt, turnData.CurrentTick);
					if (++totalActed >= max) yield break;
				}
				EnergyCache[i] = (energy, entt);
			}
			turnData.CurrentTick++;
		}
	}
}

file class TickEnergySystem : QuerySystem<Energy> {
	ArchetypeQuery canActQuery;
	ArchetypeQuery blockingActionsQuery;
	List<Entity> entitiesSorted = new();

	protected override void OnAddStore(EntityStore store) {
		canActQuery = store.Query().AllTags(Tags.Get<CanAct>());
		blockingActionsQuery = store.Query().AnyTags(Tags.Get<IsActionBlocking>());
	}

	protected override void OnUpdate() {
		// Progress only if no blocking actions currently in pipeline
		if (blockingActionsQuery.Count > 0 || canActQuery.Count > 0)
			return;

		// This is basically the same as SimTurns(), just working on different data (in place mutation, vs copy)
		// It's a pain to maintain and keep in sync with each other. 
		// Would be better to use the same code in both, just doing different things
		// But hard to do since one is an iterator and works on copies etc.
		ref var turnData = ref Singleton.Get<TurnData>();
		// Sort entities by id to guarantee determinism with SimTurns(). Not sure how friflo orders
		entitiesSorted.Clear();
		entitiesSorted.AddRange(Query.Entities);
		entitiesSorted.Sort((e1, e2) => e1.Id - e2.Id);
		// If nobody can act, then progress through the energy components until someone can
		while (canActQuery.Count == 0) {
			foreach (var e in entitiesSorted) {
				ref var energy = ref e.GetComponent<Energy>();
				// Already processed this tick. Skip
				if (energy.TickProcessed >= turnData.CurrentTick) continue;
				energy.TickProcessed = turnData.CurrentTick;
				energy.Current += energy.GainPerTick;
				var remaining = energy.Current - energy.AmountToAct;
				if (remaining >= 0) {
					energy.Current = remaining;
					CommandBuffer.AddTag<CanAct>(e.Id);
					CommandBuffer.AddTag<TurnStarted>(e.Id);
					// return;
					// TODO if returning here then turns are deterministic
					// but movement is slow since entts have to pass through multiple system runs
					// need a way to progress all enemy systems in the same tick until player's turn to act.
					// Problem happens when enemies and player are in the same turn
					// Save into act queue?
				}
			}
			turnData.CurrentTick++;
			CommandBuffer.Playback();
		}
	}
}

/// <summary>
/// Does maintenance of currently executing actions
/// </summary>
file class ProcessActionsSystem : QuerySystem {
	ArchetypeQuery finishedQuery;
	ArchetypeQuery waitingQuery;
	ArchetypeQuery blockingQuery;

	protected override void OnAddStore(EntityStore store) {
		finishedQuery = store.Query().AllTags(Tags.Get<IsActionFinished>());
		waitingQuery = store.Query().AllTags(Tags.Get<IsActionWaiting>());
		blockingQuery = store.Query().AllTags(Tags.Get<IsActionBlocking, IsActionExecuting>());
	}

	protected override void OnUpdate() {
		var cmds = CommandBuffer;
		// Remove finished actions
		foreach (var entt in finishedQuery.Entities) {
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