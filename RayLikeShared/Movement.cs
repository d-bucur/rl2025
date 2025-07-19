using System.Numerics;
using Friflo.Engine.ECS;
using Raylib_cs;

namespace RayLikeShared;

internal struct InputReceiver : IComponent;

internal record struct MovementAction(int Dx, int Dy) : IAction {
    public void Execute(EntityStore world) {
        // need to copy for lambda to work...
        var self = this;
        world.Query<Position, Scale3>()
            .AllComponents(ComponentTypes.Get<InputReceiver>())
            .ForEachEntity((ref Position pos, ref Scale3 scale, Entity e) => {
                // xz anim
                new Tween(e).With(
                    (ref Position p, Vector3 v) => { p.x = v.X; p.z = v.Z; },
                    pos.value, pos.value + new Vector3(self.Dx * Config.GRID_SIZE, 0, self.Dy * Config.GRID_SIZE),
                    0.2f, Ease.SineOut, Vector3.Lerp
                ).RegisterEcs();
                // y anim
                const float jumpHeight = 0.3f;
                new Tween(e).With(
                    (ref Position p, float v) => { p.y = v; },
                    0, jumpHeight,
                    0.1f, Ease.Linear
                ).With(
                    (ref Position p, float v) => { p.y = v; },
                    jumpHeight, 0,
                    0.1f, Ease.Linear
                ).RegisterEcs();
                // scale
                var startScale = scale.x;
                new Tween(e).With(
                    (ref Scale3 s, float v) => { s.x = v; },
                    startScale, 0.5f,
                    0.2f, Ease.Linear
                ).With(
                    (ref Scale3 s, float v) => { s.x = v; },
                    0.5f, startScale,
                    0.2f, Ease.Linear
                ).RegisterEcs();
            });
    }
}


internal class Movement : IModule {
    public void Init(EntityStore world) {
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