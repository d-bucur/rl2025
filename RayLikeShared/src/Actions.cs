using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

interface IGameAction : IComponent {
	Entity GetSource();
}

// Some plumbing work for handling actions. Most processors follow this pattern
abstract class ActionProcessor<T> : QuerySystem<T> where T : struct, IGameAction {

	public ActionProcessor() => Filter.AllTags(Tags.Get<IsActionExecuting, IsActionWaiting>());
	protected override void OnUpdate() {
		// Processors shouldn't access other actions, so no need for this, the entities are disjoint
		Query.ThrowOnStructuralChange = false;
		Query.ForEachEntity((ref T action, Entity entt) => {
			CommandBuffer.RemoveTag<IsActionWaiting>(entt.Id);
			switch (Process(ref action, entt)) {
				case ActionProcessor.Result.Done:
					CommandBuffer.AddTag<IsActionFinished>(entt.Id);
					break;
				case ActionProcessor.Result.Invalid:
					CommandBuffer.AddTag<CanAct>(action.GetSource().Id);
					CommandBuffer.AddTag<IsActionFinished>(entt.Id);
					break;
				case ActionProcessor.Result.Running:
					// Don't do anything. Something else will have to finish it, ie a Tween callback
					break;
			}
		});
	}
	protected abstract ActionProcessor.Result Process(ref T action, Entity actionEntt);
}

// For shorter lambda syntax
static class ActionProcessor {
	internal enum Result {
		/// <summary>
		/// Return turn to entity that created the action
		/// </summary>
		Invalid,
		/// <summary>
		/// Something else will have to finish the action, like a Tween callback
		/// </summary>
		Running,
		/// <summary>
		/// Finish action immediately
		/// </summary>
		Done,
	}

	internal delegate Result ProcessActionDelegate<A>(ref A action, Entity actionEntt);

	public static LambdaProcessor<T> FromFunc<T>(ProcessActionDelegate<T> p) where T : struct, IGameAction {
		return new LambdaProcessor<T>(p);
	}

	internal class LambdaProcessor<T>(ProcessActionDelegate<T> ProcessFunc) : ActionProcessor<T> where T : struct, IGameAction {
		protected override Result Process(ref T action, Entity entt) {
			return ProcessFunc(ref action, entt);
		}
	}
}