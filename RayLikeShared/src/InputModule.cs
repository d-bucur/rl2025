using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

internal struct InputReceiver : IComponent;

class InputModule : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.Input.Add(new PlayerInputSystem());
		UpdatePhases.Input.Add(new GameInputSystem());
	}
}

internal class PlayerInputSystem : QuerySystem<InputReceiver> {
    public PlayerInputSystem() => Filter.AllTags(Tags.Get<CanAct>());

    protected override void OnUpdate() {
        var cmd = CommandBuffer;

        Query.ForEachEntity((ref InputReceiver receiver, Entity entt) => {
            Vec2I? keyMovement = null;
            if (IsActionPressed(KeyboardKey.A))
                keyMovement = (-1, 0);
            if (IsActionPressed(KeyboardKey.D))
                keyMovement = (1, 0);
            if (IsActionPressed(KeyboardKey.S) || IsActionPressed(KeyboardKey.X))
                keyMovement = (0, 1);
            if (IsActionPressed(KeyboardKey.W))
                keyMovement = (0, -1);
            if (IsActionPressed(KeyboardKey.Q))
                keyMovement = (-1, -1);
            if (IsActionPressed(KeyboardKey.E))
                keyMovement = (1, -1);
            if (IsActionPressed(KeyboardKey.Z))
                keyMovement = (-1, 1);
            if (IsActionPressed(KeyboardKey.C))
                keyMovement = (1, 1);

            if (keyMovement.HasValue) {
                HandleMovementInput(entt, cmd, keyMovement.Value);
            }

            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                Console.WriteLine($"Mouse press");

            if (Raylib.IsKeyDown(KeyboardKey.Backspace)) {
                TurnsManagement.QueueAction(cmd, new EscapeAction(), false);
                cmd.RemoveTag<CanAct>(entt.Id);
            }
        });
    }

    private static void HandleMovementInput(Entity entt, CommandBuffer cmd, Vec2I keyMovement) {
		Vec2I prevPos = entt.GetComponent<GridPosition>().Value;
		var newPos = prevPos + keyMovement;
        var grid = Singleton.Entity.GetComponent<Grid>();
        if (grid.IsBlocking(newPos)) {
            // Should turn into blocking action
            Animations.Bump(entt, prevPos.ToWorldPos(), prevPos.ToWorldPos() + keyMovement.ToWorldPos() * 0.3f);
            return;
        }
        Entity charAtPos = grid.Character[newPos.X, newPos.Y];
        if (charAtPos.IsNull) {
            var action = new MovementAction(entt, keyMovement.X, keyMovement.Y);
            TurnsManagement.QueueAction(cmd, action, true);
        }
        else {
            var action = new MeleeAction(entt, charAtPos, keyMovement.X, keyMovement.Y);
            TurnsManagement.QueueAction(cmd, action, true);
        }
        cmd.RemoveTag<CanAct>(entt.Id);
    }

    private static bool IsActionPressed(KeyboardKey key) {
        return Raylib.IsKeyPressed(key) || Raylib.IsKeyDown(key);
    }
}

internal class GameInputSystem : QuerySystem {
	protected override void OnUpdate() {
		ref Settings settings = ref Singleton.Entity.GetComponent<Settings>();
		if (Raylib.IsKeyPressed(KeyboardKey.K)) 
			settings.DebugColorsEnabled = !settings.DebugColorsEnabled;
		if (Raylib.IsKeyPressed(KeyboardKey.M)) 
			settings.MinimapEnabled = !settings.MinimapEnabled;
	}
}