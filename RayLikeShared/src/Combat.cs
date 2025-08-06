using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

internal struct Figher : IComponent {
	public int MaxHP;
	public int HP;
	public int Defense;
	public int Power;

	public Figher(int maxHP, int defense, int power) {
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
internal record struct MeleeAction(Entity Source, Entity Target, int Dx, int Dy) : IComponent { }

internal class Combat : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.ApplyActions.Add(new ProcessMeleeSystem());
	}
}

internal class ProcessMeleeSystem : QuerySystem<MeleeAction> {
	public ProcessMeleeSystem() => Filter.AllTags(Tags.Get<IsActionExecuting, IsActionWaiting>());

	protected override void OnUpdate() {
		Query.ForEachEntity((ref MeleeAction action, Entity actionEntt) => {
			CommandBuffer.RemoveTag<IsActionWaiting>(actionEntt.Id);
			Vector3 startPos = action.Source.GetComponent<GridPosition>().Value.ToWorldPos();
			Animations.Bump(
				action.Source,
				startPos,
				startPos + new Vector3(action.Dx, 0, action.Dy) / 2,
				(ref Position p) => actionEntt.AddTag<IsActionFinished>()
			);

			// Do the actual damage
			ref var sourceFighter = ref action.Source.GetComponent<Figher>();
			ref var targetFighter = ref action.Target.GetComponent<Figher>();
			var damage = sourceFighter.Power - targetFighter.Defense;
			var desc = $"{action.Source.GetComponent<Name>().Value} attacks {action.Target.GetComponent<Name>().Value}";
			if (damage > 0) {
				targetFighter.ApplyDamage(damage);
				if (targetFighter.HP <= 0) {
					CommandBuffer.DeleteEntity(action.Target.Id);
					Singleton.Entity.GetComponent<Grid>()
						.RemoveCharacter(action.Target.GetComponent<GridPosition>().Value);
					Console.WriteLine($"{desc} for {damage} HP and killed it");
				}
				else
					Console.WriteLine($"{desc} for {damage} HP");
			}
			else
				Console.WriteLine($"{desc} but does not damage");
		});
	}
}