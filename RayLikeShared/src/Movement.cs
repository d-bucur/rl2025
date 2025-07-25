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
            (int, int)? action = null;
            if (IsActionPressed(KeyboardKey.A))
                action = (-1, 0);
            if (IsActionPressed(KeyboardKey.D))
                action = (1, 0);
            if (IsActionPressed(KeyboardKey.S))
                action = (0, 1);
            if (IsActionPressed(KeyboardKey.W))
                action = (0, -1);

            if (action.HasValue) {
                var movementAction = new MovementAction(entt, action.Value.Item1, action.Value.Item2);
                TurnsManagement.QueueAction(cmd, movementAction);

                cmd.RemoveTag<CanAct>(entt.Id);
            }

            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                Console.WriteLine($"Mouse press");

            if (Raylib.IsKeyDown(KeyboardKey.Backspace))
                TurnsManagement.QueueAction(cmd, new EscapeAction(), false);
        });
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
        Query.ForEachEntity((ref MovementAction action, Entity entt) => {
            Console.WriteLine($"Executing action: {action} --- {entt}");
            cmds.RemoveTag<IsActionWaiting>(entt.Id);
            if (!TryPerformMove(action)) {
                // Could play a fail animation here
                cmds.AddTag<IsActionFinished>(entt.Id);
                return;
            }
            PlayMoveAnimation(action, entt);
        });
    }

    private bool TryPerformMove(MovementAction action) {
        var grid = Singleton.Entity.GetComponent<Grid>();
        ref var gridPos = ref action.Entity.GetComponent<GridPosition>();
        var newPos = gridPos.Value + new Vec2I(action.Dx, action.Dy);

        // Check if move is valid
        if (!grid.IsInsideGrid(newPos))
            return false;

        var entt = grid.Value[newPos.X, newPos.Y];
        bool isTileFree = entt.IsNull || (!entt.Tags.Has<BlocksPathing>());
        if (!isTileFree)
            return false;

        // Perform the move
        grid.SetPos(gridPos.Value, newPos);
        gridPos.Value = newPos;
        return true;
    }

    private void PlayMoveAnimation(MovementAction action, Entity actionEntt) {
        var Entity = action.Entity;
        // Add movement animations
        var pos = Entity.GetComponent<Position>();
        // xz anim
        new Tween(Entity).With(
            (ref Position p, Vector3 v) => { p.x = v.X; p.z = v.Z; },
            pos.value, pos.value + new Vector3(action.Dx * Config.GRID_SIZE, 0, action.Dy * Config.GRID_SIZE),
            0.2f, Ease.SineOut, Vector3.Lerp,
            OnEnd: (ref Position p) => {
                Console.WriteLine($"Finished action: {actionEntt}");
                actionEntt.AddTag<IsActionFinished>();
            }
        ).RegisterEcs();

        // y anim
        const float jumpHeight = 0.3f;
        new Tween(Entity).With(
            (ref Position p, float v) => { p.y = v; },
            0, jumpHeight,
            0.1f, Ease.Linear
        ).With(
            (ref Position p, float v) => { p.y = v; },
            jumpHeight, 0,
            0.1f, Ease.Linear
        ).RegisterEcs();

        // scale
        var scale = Entity.GetComponent<Scale3>();
        var startScale = scale.x;
        new Tween(Entity).With(
            (ref Scale3 s, float v) => { s.x = v; },
            startScale, 0.5f,
            0.2f, Ease.Linear
        ).With(
            (ref Scale3 s, float v) => { s.x = v; },
            0.5f, startScale,
            0.2f, Ease.Linear
        ).RegisterEcs();
    }
}