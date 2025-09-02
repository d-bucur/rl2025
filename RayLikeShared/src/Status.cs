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
	string Name();
	int TurnsRemaining(); // TODO merge with EndCondition?
}

// Single value per IStatusEffect, non fragmenting
struct StatusEffect() : IRelation<Type> {
	public required IStatusEffect Value;
	// register aot creates a default. This prevents it from breaking.
	public Type GetRelationKey() => Value != null ? Value.GetType() : typeof(StatusEffect);
}

struct RageEffect : IStatusEffect, IComponent {
	public string Name() => "Rage";
	public required int OldGain;
	public required int Duration;
	public int Elapsed;

	public bool EndCondition(Entity e) => Elapsed >= Duration;
	public void Tick(Entity e) => Elapsed++;

	public void OnEnd(Entity e) {
		ref var energy = ref e.GetComponent<Energy>();
		energy.GainPerTick = OldGain;
		MessageLog.Print($"The rage effect has worn off");
	}

	public int TurnsRemaining() => Duration - Elapsed;
}

struct IsConfused : IStatusEffect, IComponent {
	public string Name() => "Confusion";
	required internal int Duration;
	internal const float HurtSelfChance = 0.5f;

	public void Tick(Entity e) => Duration--;
	public bool EndCondition(Entity e) => Duration <= 0;

	public void OnEnd(Entity e) {
		ref var t = ref e.GetComponent<Team>();
		MessageLog.Print($"{e.Name.value} is no longer confused");
	}

	public int TurnsRemaining() => Duration;
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
				entt.RemoveRelation<StatusEffect, Type>(effect.GetType());
			}
			removeBuffer.Clear();
		}
	}
}