using Friflo.Engine.ECS;
using Raylib_cs;

namespace RayLikeShared;

internal struct ActionBuffer() : IComponent {
	internal Queue<IAction> Value = new();
}

internal interface IAction;

internal record struct MovementAction(int Dx, int Dy) : IAction;

internal struct EscapeAction : IAction;

class ActionsModule : IModule {
	public void Init(EntityStore world) {
		// Can probably use singleton
		// https://friflo.gitbook.io/friflo.engine.ecs/documentation/entity#unique-entity
		world.CreateEntity(
			new ActionBuffer()
		);

		UpdatePhases.Input.Add(
			LambdaSystems.New((ref ActionBuffer buffer, Entity e) => {
				if (Raylib.IsKeyPressed(KeyboardKey.Left))
					buffer.Value.Enqueue(new MovementAction(-1, 0));
				if (Raylib.IsKeyPressed(KeyboardKey.Right))
					buffer.Value.Enqueue(new MovementAction(1, 0));
				if (Raylib.IsKeyPressed(KeyboardKey.Down))
					buffer.Value.Enqueue(new MovementAction(0, 1));
				if (Raylib.IsKeyPressed(KeyboardKey.Up))
					buffer.Value.Enqueue(new MovementAction(0, -1));

				if (Raylib.IsMouseButtonPressed(MouseButton.Left))
					Console.WriteLine($"Mouse press");

				if (Raylib.IsKeyDown(KeyboardKey.Backspace))
					buffer.Value.Enqueue(new EscapeAction());
			})
		);
	}
}