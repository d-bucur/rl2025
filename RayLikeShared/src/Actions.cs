using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

interface IGameAction : IComponent;

// Some plumbing work for handling actions. Most processors follow this pattern
abstract class ActionProcessor<T> : QuerySystem<T> where T : struct, IGameAction {
	public ActionProcessor() => Filter.AllTags(Tags.Get<IsActionExecuting, IsActionWaiting>());
	protected override void OnUpdate() {
		// Processors shouldn't access other actions, so no need for this
		Query.ThrowOnStructuralChange = false;
		Query.ForEachEntity((ref T action, Entity entt) => {
			CommandBuffer.RemoveTag<IsActionWaiting>(entt.Id);
			if (Process(ref action, entt)) CommandBuffer.AddTag<IsActionFinished>(entt.Id);
		});
	}
	
	/// <summary>
	/// Return true if action has finished. 
	/// Otherwise return false, and something else will have to finish it, ie a Tween callback
	/// </summary>
	protected abstract bool Process(ref T action, Entity actionEntt);
}

// For shorter lambda syntax
static class ActionProcessor {
	public static LambdaProcessor<T> FromFunc<T>(ProcessActionDelegate<T> p) where T : struct, IGameAction {
		return new LambdaProcessor<T>(p);
	}
	internal delegate bool ProcessActionDelegate<A>(ref A action, Entity actionEntt);
	internal class LambdaProcessor<T>(ProcessActionDelegate<T> ProcessFunc) : ActionProcessor<T> where T : struct, IGameAction {
		protected override bool Process(ref T action, Entity entt) {
			return ProcessFunc(ref action, entt);
		}
	}
}

// alternative without inheritance?