using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

struct Energy() : IComponent {
	internal int Current = 0;
	internal required int GainPerTick;
	internal int AmountToAct = 10;
}

struct CanAct : ITag;

class TurnsManagement : IModule {
	public void Init(EntityStore world) {
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