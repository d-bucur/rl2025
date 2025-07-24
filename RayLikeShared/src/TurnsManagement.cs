using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

struct Energy() : IComponent {
	internal int Current = 0;
	internal required int GainPerTick;
	internal int AmountToAct = 10;
}

struct CanAct : ITag;

// TODO can maybe remove buffer once turn system in place?
internal struct ActionBuffer() : IComponent {
	internal Queue<IAction> Value = new();
	internal List<IAction> InExecution = new();
}

internal interface IAction {
	public bool Blocking { get => false; }
	public bool Finished { get => true; }
	public void Execute(EntityStore world);
}

internal struct EscapeAction : IAction {
	public void Execute(EntityStore world) {
		Console.WriteLine("Escape action not implemented");
	}
}

class TurnsManagement : IModule {
	public void Init(EntityStore world) {
		Singleton.Entity.AddComponent(new ActionBuffer());
		UpdatePhases.ApplyActions.Add(new ProcessActions());
		UpdatePhases.ProgressTurns.Add(new TickEnergy());
	}
}

internal class TickEnergy : QuerySystem<Energy> {
	private ArchetypeQuery canActQuery;

	protected override void OnAddStore(EntityStore store) {
		canActQuery = store.Query().AllTags(Tags.Get<CanAct>());
	}

	protected override void OnUpdate() {
		// TODO progress only when no blocking actions currently playing
		// If nobody can act, then progress through the energy system until someone can
		var buffer = CommandBuffer;
		while (canActQuery.Count == 0) {
			Query.ForEachEntity((ref Energy energy, Entity e) => {
				energy.Current += energy.GainPerTick;
				var remaining = energy.Current - energy.AmountToAct;
				Console.WriteLine($"Progressing {e} to energy {energy.Current}");
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

internal class ProcessActions : QuerySystem<ActionBuffer> {
	protected override void OnUpdate() {
		Query.ForEachEntity((ref ActionBuffer buffer, Entity e) => {
			// TODO this executing/blocking part could be done using ECS components
			// iterate over executing actions and remove finished ones
			var newInExecution = new List<IAction>();
			bool isBlockingInExecution = false;
			foreach (var action in buffer.InExecution) {
				if (!action.Finished) {
					newInExecution.Add(action);
					isBlockingInExecution |= action.Blocking;
				}
			}
			buffer.InExecution = newInExecution;

			// if no blocking action then execute new ones from the buffer
			if (isBlockingInExecution)
				return;
			while (buffer.Value.Count > 0) {
				var action = buffer.Value.Dequeue();
				buffer.InExecution.Add(action);
				action.Execute(Singleton.World);
				if (action.Blocking) {
					break;
				}
			}
		});
	}
}