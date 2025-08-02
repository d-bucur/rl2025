using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

internal struct InputReceiver : IComponent;

internal record struct MovementAction(Entity Entity, int Dx, int Dy) : IComponent { }

internal class Movement : IModule {
    public void Init(EntityStore world) {
        UpdatePhases.Input.Add(new PlayerInputSystem());
        UpdatePhases.Input.Add(new EnemyMovementSystem());
        UpdatePhases.ApplyActions.Add(new ProcessMovementSystem());
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

internal class EnemyMovementSystem : QuerySystem<GridPosition> {
    public EnemyMovementSystem() => Filter.AllTags(Tags.Get<Enemy, CanAct>());

    protected override void OnUpdate() {
        var cmds = CommandBuffer;
        Query.ForEachEntity((ref GridPosition pos, Entity e) => {
            var action = new MovementAction(
                e, Random.Shared.Next(-1, 2), Random.Shared.Next(-1, 2)
            );
            TurnsManagement.QueueAction(cmds, action, false);

            cmds.RemoveTag<CanAct>(e.Id);
        });
    }
}

internal class ProcessMovementSystem : QuerySystem<MovementAction> {
    public ProcessMovementSystem() => Filter.AllTags(Tags.Get<IsActionExecuting, IsActionWaiting>());

    protected override void OnUpdate() {
        var cmds = CommandBuffer;
        Query.ThrowOnStructuralChange = false;
        Query.ForEachEntity((ref MovementAction action, Entity entt) => {
            // Console.WriteLine($"Executing action: {action} --- {entt}");
            cmds.RemoveTag<IsActionWaiting>(entt.Id);
            var oldPos = action.Entity.GetComponent<GridPosition>().Value;
            if (!TryPerformMove(action)) {
                // Could play a fail animation here
                cmds.AddTag<IsActionFinished>(entt.Id);
                return;
            }
            // Add movement animations
            Vector3 currPos = action.Entity.GetComponent<GridPosition>().Value.ToWorldPos();
            Animations.Move(action.Entity, entt, oldPos, currPos);
        });
    }

    private bool TryPerformMove(MovementAction action) {
        var grid = Singleton.Entity.GetComponent<Grid>();
        ref var gridPos = ref action.Entity.GetComponent<GridPosition>();
        var newPos = gridPos.Value + new Vec2I(action.Dx, action.Dy);

        // Check if move is valid
        if (!grid.IsInsideGrid(newPos))
            return false;

        if (!IsTileFree(grid, newPos))
            return false;

        // Perform the move
        grid.MoveCharacterPos(gridPos.Value, newPos);
        gridPos.Value = newPos;
        action.Entity.Set(gridPos); // trigger update hooks
        return true;
    }

    private static bool IsTileFree(Grid grid, Vec2I pos) {
        var destTile = grid.Tile[pos.X, pos.Y];
        bool isTileFree = destTile.IsNull || (!destTile.Tags.Has<BlocksPathing>());
        var destChar = grid.Character[pos.X, pos.Y];
        bool isCharFree = destChar.IsNull || (!destChar.Tags.Has<BlocksPathing>());
        return isTileFree && isCharFree;
    }
}