using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using RayLikeShared;

class StatusModule : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.StatusEffects.Add(new ApplyStatusEffects());
	}
}

interface IStatusEffect {
	// void OnAdded() { } // yagni
	void Tick(Entity e);
	void OnEnd(Entity e);
	bool EndCondition(Entity e);
}

// TODO maybe use a non fragmenting relationship instead
// Trying something different from interfaces. 
internal struct StatusEffect() : IComponent {
	public required IStatusEffect Value;
}

struct RageEffect : IStatusEffect {
	public required int OldGain;
	public required int Duration;
	public int Elapsed;

	public bool EndCondition(Entity e) => Elapsed >= Duration;

	public void Tick(Entity e) {
		Elapsed++;
		Console.WriteLine($"Ticking rage effect {Elapsed}");
	}

	public void OnEnd(Entity e) {
		ref var energy = ref e.GetComponent<Energy>();
		energy.GainPerTick = OldGain;
		MessageLog.Print($"The rage effect has worn off");
		Console.WriteLine($"Reverting gain to {OldGain}");
	}
}

internal class ApplyStatusEffects : QuerySystem<StatusEffect> {
	public ApplyStatusEffects() => Filter.AllTags(Tags.Get<CanAct, TurnStarted>());
	protected override void OnUpdate() {
		Query.ForEachEntity((ref StatusEffect effect, Entity e) => {
			effect.Value.Tick(e);
			CommandBuffer.RemoveTag<TurnStarted>(e.Id);
			if (effect.Value.EndCondition(e)) {
				effect.Value.OnEnd(e);
				CommandBuffer.RemoveComponent<StatusEffect>(e.Id);
			}
		});
	}
}