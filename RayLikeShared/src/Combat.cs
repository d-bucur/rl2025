using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

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
			Console.WriteLine($"{action.Source} attacks {action.Target}");
			CommandBuffer.RemoveTag<IsActionWaiting>(actionEntt.Id);
			Vector3 startPos = action.Source.GetComponent<GridPosition>().Value.ToWorldPos();
			Animations.Bump(
				action.Source,
				startPos,
				startPos + new Vector3(action.Dx, 0, action.Dy) / 2,
				(ref Position p) => actionEntt.AddTag<IsActionFinished>()
			);
		});
	}
}