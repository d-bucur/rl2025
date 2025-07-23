using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

internal struct InputReceiver : IComponent;

internal record class MovementAction(Entity Entity, int Dx, int Dy, bool IsBlocking = true) : IAction {
    private bool _isFinished = false;

    public bool Blocking => IsBlocking;
    public bool Finished => _isFinished;

    public void Execute(EntityStore world) {
        if (!TryPerformMove()) {
            _isFinished = true;
            return;
            // Could play a fail animation here
        }
        PlayMoveAnimation();
    }

    private bool TryPerformMove() {
        var grid = Singleton.Entity.GetComponent<Grid>();
        ref var gridPos = ref Entity.GetComponent<GridPosition>();
        var newPos = gridPos.Value + new Vec2I(Dx, Dy);

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

    private void PlayMoveAnimation() {
        // Add movement animations
        var pos = Entity.GetComponent<Position>();
        // xz anim
        new Tween(Entity).With(
            (ref Position p, Vector3 v) => { p.x = v.X; p.z = v.Z; },
            pos.value, pos.value + new Vector3(Dx * Config.GRID_SIZE, 0, Dy * Config.GRID_SIZE),
            0.2f, Ease.SineOut, Vector3.Lerp, OnEnd: (ref Position p) => { _isFinished = true; }
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


internal class Movement : IModule {
    public void Init(EntityStore world) {
        UpdatePhases.Input.Add(new InputSystem());
    }
}

internal class InputSystem : QuerySystem<ActionBuffer> {
    private float downThrottleTime = 0.3f;
    private float lastPressed;
    private ArchetypeQuery<InputReceiver> receiversQuery;

    protected override void OnAddStore(EntityStore store) {
        base.OnAddStore(store);
        receiversQuery = store.Query<InputReceiver>();
    }
    protected override void OnUpdate() {
        Query.ForEachEntity((ref ActionBuffer buffer, Entity e) => {
            (int, int)? action = null;
            // Would be better to check if animations have stopped rather that a fixed throttle
            var isActionEnabled = (Tick.time - lastPressed) > downThrottleTime;
            if (ActionPressed(isActionEnabled, KeyboardKey.A))
                action = (-1, 0);
            if (ActionPressed(isActionEnabled, KeyboardKey.D))
                action = (1, 0);
            if (ActionPressed(isActionEnabled, KeyboardKey.S))
                action = (0, 1);
            if (ActionPressed(isActionEnabled, KeyboardKey.W))
                action = (0, -1);

            if (action.HasValue) {
                lastPressed = Tick.time;
                foreach (var entity in receiversQuery.Entities) {
                    buffer.Value.Enqueue(new MovementAction(entity, action.Value.Item1, action.Value.Item2));
                }
            }

            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                Console.WriteLine($"Mouse press");

            if (Raylib.IsKeyDown(KeyboardKey.Backspace))
                buffer.Value.Enqueue(new EscapeAction());
        });

    }

    private static bool ActionPressed(bool isActionEnabled, KeyboardKey key) {
        return Raylib.IsKeyPressed(key) || (isActionEnabled && Raylib.IsKeyDown(key));
    }
}