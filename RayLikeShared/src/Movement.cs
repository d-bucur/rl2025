using System.Diagnostics;
using System.Numerics;
using Friflo.Engine.ECS;
using Raylib_cs;

namespace RayLikeShared;

internal struct InputReceiver : IComponent;

internal record class MovementAction(Entity Entity, int Dx, int Dy, bool IsBlocking = true) : IAction {
    private bool _isFinished = false;

    public bool Blocking => IsBlocking;
    public bool Finished => _isFinished;

    public void Execute(EntityStore world) {
        // TODO check destination
        // TODO finish logic
        // var grid = Singleton.Entity.GetComponent<Grid>();
        // var gridPos = Entity.GetComponent<GridPosition>();
        // var newPos = gridPos.Value += new Vec2I(Dx, Dy);

        // grid.Value[gridPos.Value.X, gridPos.Value.Y] = default;
        // Debug.Assert(grid.Value[gridPos.Value.X, gridPos.Value.Y].IsNull);

        // gridPos.Value.X += Dx;
        // gridPos.Value.Y += Dy;
        // grid.Value[gridPos.Value.X, gridPos.Value.Y] = Entity;

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
        UpdatePhases.Input.Add(
            LambdaSystems.New((ref ActionBuffer buffer, Entity e) => {
                (int, int)? action = null;
                if (Raylib.IsKeyPressed(KeyboardKey.A))
                    action = (-1, 0);
                if (Raylib.IsKeyPressed(KeyboardKey.D))
                    action = (1, 0);
                if (Raylib.IsKeyPressed(KeyboardKey.S))
                    action = (0, 1);
                if (Raylib.IsKeyPressed(KeyboardKey.W))
                    action = (0, -1);

                if (action.HasValue) {
                    var receivers = world.Query<InputReceiver>();
                    foreach (var entity in receivers.Entities) {
                        buffer.Value.Enqueue(new MovementAction(entity, action.Value.Item1, action.Value.Item2));
                    }
                }

                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                    Console.WriteLine($"Mouse press");

                if (Raylib.IsKeyDown(KeyboardKey.Backspace))
                    buffer.Value.Enqueue(new EscapeAction());
            })
        );
    }
}