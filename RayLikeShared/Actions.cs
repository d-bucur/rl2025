using Friflo.Engine.ECS;

namespace RayLikeShared;

internal struct ActionBuffer() : IComponent {
	internal Queue<IAction> Value = new();
}

internal interface IAction {
	public void Execute(EntityStore world);
}

internal struct EscapeAction : IAction {
	public void Execute(EntityStore world) {
		throw new NotImplementedException();
	}
}

class ActionsModule : IModule {
	public void Init(EntityStore world) {
		Singleton.Entity.AddComponent(new ActionBuffer());

		UpdatePhases.ApplyActions.Add(LambdaSystems.New((ref ActionBuffer buffer, Entity e) => {
			while (buffer.Value.Count > 0) {
				var action = buffer.Value.Dequeue();
				action.Execute(world);
			}
		}));
	}
}

// old attempt call with
// ActionSolvers.Value[action.GetType()].Update(Game.GetUpdateTick());
// class ActionSolvers {
// 	public static Dictionary<Type, SystemGroup> Value = new();
// }