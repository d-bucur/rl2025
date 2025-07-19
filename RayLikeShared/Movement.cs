using System.Numerics;
using Friflo.Engine.ECS;

namespace RayLikeShared;

internal struct InputReceiver : IComponent;

internal class Movement : IModule {
    public void Init(EntityStore world) {
        UpdatePhases.ApplyActions.Add(
            LambdaSystems.New((ref ActionBuffer buffer, Entity e) => {
                // TODO need more flexible dispatch between modules
                while (buffer.Value.Count > 0) {
                    var action = buffer.Value.Dequeue();
                    switch (action) {
                        case MovementAction movement:
                            world.Query<Position, Scale3>()
                                .AllComponents(ComponentTypes.Get<InputReceiver>())
                                .ForEachEntity((ref Position pos, ref Scale3 scale, Entity e) => {
                                    // xz anim
                                    new Tween(e).With(
                                        (ref Position p, Vector3 v) => { p.x = v.X; p.z = v.Z; },
                                        pos.value, pos.value + new Vector3(movement.Dx * Config.GRID_SIZE, 0, movement.Dy * Config.GRID_SIZE),
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
                            break;
                    }
                }
            })
        );
    }
}