using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

internal record struct MovementAction(Entity Entity, int Dx, int Dy) : IComponent { }

internal class Movement : IModule {
    public void Init(EntityStore world) {
        UpdatePhases.Input.Add(new EnemyMovementSystem());
        UpdatePhases.ApplyActions.Add(new ProcessMovementSystem());
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