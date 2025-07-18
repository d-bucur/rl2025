using System.Numerics;
using Friflo.Engine.ECS;

namespace RayLikeShared;

internal struct InputReceiver : IComponent;

internal class Movement : IModule {
    internal const float GRID_SIZE = 1;

    public void Init(EntityStore world) {
        UpdatePhases.ApplyActions.Add(
            LambdaSystems.New((ref ActionBuffer buffer, Entity e) => {
                // TODO need more flexible dispatch between modules
                while (buffer.Value.Count > 0) {
                    var action = buffer.Value.Dequeue();
                    switch (action) {
                        case MovementAction movement:
                            world.Query<Position>()
                                .AllComponents(ComponentTypes.Get<InputReceiver>())
                                .ForEachEntity((ref Position pos, Entity e) => {
                                    pos.x += movement.Dx * GRID_SIZE;
                                    pos.z += movement.Dy * GRID_SIZE;
                                });
                            break;
                    }
                }
            })
        );
    }
}