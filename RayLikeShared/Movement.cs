using System.Numerics;
using Friflo.Engine.ECS;

namespace RayLikeShared;

internal struct InputReceiver : IComponent;

internal static class Helpers {
    // Easy way to do this with an implicit cast?
    internal static Vector3 ToVec3(Position p) => new Vector3(p.x, p.y, p.z);
}

internal class Movement {
    internal const float GRID_SIZE = 1;

    internal static void Update(EntityStore world) {
        // TODO need more flexible dispatch between modules
        world.Query<ActionBuffer>().ForEachEntity((ref ActionBuffer buffer, Entity e) => {
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
        });
    }
}