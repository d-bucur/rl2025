using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

record struct Dice(int Count = 1, int Faces = 6) {
	public int Roll() {
		int total = 0;
		for (int i = 0; i < Count; i++) {
			total += Random.Shared.Next(Faces) + 1;
		}
		return total;
	}
}

struct Fighter : IComponent {
	public int MaxHP;
	public int HP;
	public Dice Defense;
	public int Power;

	public Fighter(int maxHP, Dice defense, int power) {
		MaxHP = maxHP;
		HP = maxHP;
		Defense = defense;
		Power = power;
	}

	internal void ApplyDamage(int damage) {
		// can calculate overkill this way
		HP -= damage;
		// HP = Math.Max(HP - damage, 0);
	}
}
record struct MeleeAction(Entity Source, Entity Target, int Dx, int Dy) : IComponent { }
struct Team : IComponent {
	required public int Value;
}

struct DeathSignal;

class Combat : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.ApplyActions.Add(new ProcessMeleeSystem());
	}

	internal static void EnemyDeath(Signal<DeathSignal> signal) {
		TurnToCorpse(signal.Entity);
	}

	internal static void PlayerDeath(Signal<DeathSignal> signal) {
		TurnToCorpse(signal.Entity);
	}

	private static void TurnToCorpse(Entity entity) {
		Vec2I pos = entity.GetComponent<GridPosition>().Value;
		Grid grid = Singleton.Entity.GetComponent<Grid>();
		grid.RemoveCharacter(pos);
		grid.AddOther(entity, pos);

		entity.Remove<EnemyAI, InputReceiver, Energy>(Tags.Get<BlocksPathing, Character>());
		entity.AddTag<Corpse>();
		ref var name = ref entity.GetComponent<Name>();
		name.Value = $"Remains of {name.Value}";

		if (entity.HasComponent<Billboard>()) {
			ref var bill = ref entity.GetComponent<Billboard>();
			bill.Up = new Vector3(0, 0, -1);
		}
		entity.AddComponent(new RotationSingle(Random.Shared.Next(25, 35)));
	}
}

file class ProcessMeleeSystem : QuerySystem<MeleeAction> {
	public ProcessMeleeSystem() => Filter.AllTags(Tags.Get<IsActionExecuting, IsActionWaiting>());

	protected override void OnUpdate() {
		// Action entities are disjoint from the entities they operate on. No need to throw on structural changes
		Query.ThrowOnStructuralChange = false;
		Query.ForEachEntity((ref MeleeAction action, Entity actionEntt) => {
			CommandBuffer.RemoveTag<IsActionWaiting>(actionEntt.Id);
			Vector3 startPos = action.Source.GetComponent<GridPosition>().Value.ToWorldPos();
			Vector3 actionDir = new(action.Dx, 0, action.Dy);
			Animations.Bump(
				action.Source,
				startPos,
				startPos + actionDir / 2,
				(ref Position p) => actionEntt.AddTag<IsActionFinished>()
			);

			// Do the actual damage
			ref var sourceFighter = ref action.Source.GetComponent<Fighter>();
			ref var targetFighter = ref action.Target.GetComponent<Fighter>();
			var damage = sourceFighter.Power
				- targetFighter.Defense.Roll();
			var desc = $"{action.Source.GetComponent<Name>().Value} attacks {action.Target.GetComponent<Name>().Value}";
			var isTargetPlayer = action.Target.Tags.Has<Player>();
			var color = isTargetPlayer ? Color.Red : Color.Orange;
			if (damage > 0) {
				targetFighter.ApplyDamage(damage);
				if (targetFighter.HP <= 0) {
					MessageLog.Print($"{desc} for {damage} HP and killed it", color);
					action.Target.EmitSignal(new DeathSignal());
				}
				else
					MessageLog.Print($"{desc} for {damage} HP", color);
				GUI.SpawnDamageFx(damage, action.Target.GetComponent<Position>(),
					isTargetPlayer ? Color.Red : Color.Orange, actionDir);
			}
			else
				MessageLog.Print($"{desc} but does no damage", color);
		});
	}
}