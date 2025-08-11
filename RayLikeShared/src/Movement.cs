using System.Diagnostics;
using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

record struct MovementAction(Entity Entity, int Dx, int Dy) : IComponent { }
record struct RestAction(Entity Entity) : IComponent { }

struct PathMovement() : IComponent {
    internal Vec2I? Destination;
    internal List<Vec2I> Path = new();
    internal int PathIndex;

    internal bool ShouldMove() {
        return Destination.HasValue;
    }

    internal void NewDestination(Vec2I posI, List<Vec2I> path) {
        Destination = posI;
        Path = path;
        PathIndex = 0;
    }

    // Progress to next point in the path
    internal Vec2I NextPoint() {
        if (Destination.HasValue && PathIndex >= Path.Count) {
            Vec2I d = Destination.Value;
            Clear();
            return d;
        }
        Vec2I p = Path[PathIndex++];
        if (PathIndex >= Path.Count) {
            Clear();
        }
        return p;
    }

    // Use to go back when forward action has failed for some reason
    internal void PrevPoint() {
        // Doesn't work if action was cleared before
        PathIndex--;
    }

    internal void Clear() {
        Destination = null;
        PathIndex = 0;
    }
}

class Movement : IModule {
    public void Init(EntityStore world) {
        UpdatePhases.Input.Add(new ProcessPathMovement());
        UpdatePhases.ApplyActions.Add(new ProcessMovementSystem());
        // UpdatePhases.ApplyActions.Add(new ProcessRestSystem());
    }
}

file class ProcessPathMovement : QuerySystem<PathMovement, GridPosition, Team> {
    public ProcessPathMovement() => Filter.AllTags(Tags.Get<CanAct>());

    protected override void OnUpdate() {
        Query.ForEachEntity((ref PathMovement path, ref GridPosition pos, ref Team team, Entity entt) => {
            if (!path.ShouldMove())
                return;
            Vec2I next = path.NextPoint();
            Vec2I diff = next - pos.Value;
            Debug.Assert(Math.Abs(diff.X) + Math.Abs(diff.Y) <= 2,
                $"BUG: Movement is too big: from {pos.Value} to {next}");

            var grid = Singleton.Entity.GetComponent<Grid>();
            Entity destEntt = grid.Character[next.X, next.Y];

            if (!destEntt.IsNull && team.Value != destEntt.GetComponent<Team>().Value) {
                TurnsManagement.QueueAction(CommandBuffer,
                    new MeleeAction(entt, destEntt, diff.X, diff.Y), entt);
                path.Clear();
            }
            else {
                TurnsManagement.QueueAction(CommandBuffer,
                    new MovementAction(entt, diff.X, diff.Y), entt);
            }
            CommandBuffer.RemoveTag<CanAct>(entt.Id);
        });
    }
}

file class ProcessMovementSystem : QuerySystem<MovementAction> {
    public ProcessMovementSystem() => Filter.AllTags(Tags.Get<IsActionExecuting, IsActionWaiting>());

    protected override void OnUpdate() {
        var cmds = CommandBuffer;
        Query.ThrowOnStructuralChange = false;
        Query.ForEachEntity((ref MovementAction action, Entity entt) => {
            cmds.RemoveTag<IsActionWaiting>(entt.Id);
            var grid = Singleton.Entity.GetComponent<Grid>();
            ref var gridPos = ref action.Entity.GetComponent<GridPosition>();
            var oldPos = gridPos.Value;
            var newPos = oldPos + new Vec2I(action.Dx, action.Dy);

            // Check if move is valid
            if (!grid.IsInside(newPos) || !IsTileFree(grid, newPos, action.Entity)) {
                // Could play a fail animation here
                cmds.AddTag<IsActionFinished>(entt.Id);
                return;
            }

            // Perform the move
            grid.MoveCharacterPos(gridPos.Value, newPos);
            gridPos.Value = newPos;
            action.Entity.Set(gridPos); // triggers update hooks

            // Calculate vision
            var wasVisible = grid.CheckTile<IsVisible>(oldPos);
            var isVisible = grid.CheckTile<IsVisible>(newPos);
            if (wasVisible != isVisible) action.Entity.AddTag<IsVisible>();

            // Add movement animations
            Vector3 currPos = action.Entity.GetComponent<GridPosition>().Value.ToWorldPos();
            var actionEntt = action.Entity;
            Animations.Move(action.Entity, entt, oldPos, currPos,
                () => { if (!isVisible) actionEntt.RemoveTag<IsVisible>(); });
        });
    }

    static bool IsTileFree(Grid grid, Vec2I pos, Entity entt) {
        var destTile = grid.Tile[pos.X, pos.Y];
        bool isTileFree = destTile.IsNull || (!destTile.Tags.Has<BlocksPathing>());
        var destChar = grid.Character[pos.X, pos.Y];
        bool isCharFree = destChar.IsNull || (!destChar.Tags.Has<BlocksPathing>());
        return isTileFree && (isCharFree || destChar == entt);
    }
}

// Not used for now. Current just using 0,0 MovementAction for rest.
// Might want to use this later if rest has some side effect. Or just add side effect to movement?
// internal class ProcessRestSystem : QuerySystem<RestAction> {
//     public ProcessRestSystem() => Filter.AllTags(Tags.Get<IsActionExecuting, IsActionWaiting>());

//     protected override void OnUpdate() {
//         Query.ThrowOnStructuralChange = false;
//         Query.ForEachEntity((ref RestAction action, Entity entt) => {
//             Console.WriteLine($"Executing action: {action} --- {entt}");
//             CommandBuffer.RemoveTag<IsActionWaiting>(entt.Id);
//             CommandBuffer.AddTag<IsActionFinished>(entt.Id);
//         });
//     }
// }