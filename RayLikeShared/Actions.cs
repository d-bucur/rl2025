using Friflo.Engine.ECS;

namespace RayLikeShared;

internal struct ActionBuffer() : IComponent {
	internal Queue<IAction> Value = new();
	internal List<IAction> InExecution = new();
}

internal interface IAction {
	public bool IsBlocking => false;
	public bool IsFinished => true;
	public void Execute(EntityStore world);
}

internal struct EscapeAction : IAction {
	public void Execute(EntityStore world) {
		Console.WriteLine("Escape action not implemented");
	}
}

class ActionsModule : IModule {
	public void Init(EntityStore world) {
		Singleton.Entity.AddComponent(new ActionBuffer());

		UpdatePhases.ApplyActions.Add(LambdaSystems.New((ref ActionBuffer buffer, Entity e) => {
			// todo this executing/blocking part could be done using ECS components
			// iterate over executing actions and remove finished ones
			var newInExecution = new List<IAction>();
			bool isBlockingInExecution = false;
			foreach (var action in buffer.InExecution) {
				if (!action.IsFinished) {
					newInExecution.Add(action);
					isBlockingInExecution |= action.IsBlocking;
				}
			}
			buffer.InExecution = newInExecution;

			// if no blocking action then execute new ones from the buffer
			if (isBlockingInExecution)
				return;
			while (buffer.Value.Count > 0) {
				var action = buffer.Value.Dequeue();
				buffer.InExecution.Add(action);
				action.Execute(world);
				if (action.IsBlocking) {
					break;
				}
			}
		}));
	}
}

// old attempt call with
// ActionSolvers.Value[action.GetType()].Update(Game.GetUpdateTick());
// class ActionSolvers {
// 	public static Dictionary<Type, SystemGroup> Value = new();
// }