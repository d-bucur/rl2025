using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

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

// Single value per IStatusEffect, non fragmenting
struct StatusEffect : IRelation<IStatusEffect> {
	public required IStatusEffect Value;
	public IStatusEffect GetRelationKey() => Value;
}

struct RageEffect : IStatusEffect, IComponent {
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

class ApplyStatusEffects : QuerySystem {
	public ApplyStatusEffects() => Filter.AllTags(Tags.Get<CanAct, TurnStarted>());
	List<IStatusEffect> removeBuffer = [];
	protected override void OnUpdate() {
		foreach (Entity entt in Query.Entities) {
			foreach (var effect in entt.GetRelations<StatusEffect>()) {
				effect.Value.Tick(entt);
				CommandBuffer.RemoveTag<TurnStarted>(entt.Id);
				if (effect.Value.EndCondition(entt)) {
					effect.Value.OnEnd(entt);
					removeBuffer.Add(effect.Value);
				}
			}
			foreach (var effect in removeBuffer) {
				entt.RemoveRelation<StatusEffect, IStatusEffect>(effect);
			}
			removeBuffer.Clear();
		}
	}
}