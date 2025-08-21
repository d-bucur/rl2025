using System.Diagnostics;
using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

record struct MovementAction(Entity Entity, int Dx, int Dy) : IGameAction {
    public readonly Entity GetSource() => Entity;
}

// Not used for now. Currently just using 0,0 MovementAction for rest.
// Might want to use this later if rest has some side effect. Or just add side effect to movement?
record struct RestAction(Entity Entity) : IComponent { }

struct PathMovement() : IComponent {
    internal Vec2I? Destination;
    internal List<Vec2I> Path = new();
    internal int PathIndex;
    internal bool clearedForMovement = false;

    internal bool ShouldMove() {
        return Destination.HasValue && clearedForMovement;
    }

    internal void NewDestination(Vec2I posI, List<Vec2I> path) {
        Destination = posI;
        Path = path;
        PathIndex = 0;
        clearedForMovement = true;
    }

    // Progress to next point in the path
    internal Vec2I NextPoint() {
        clearedForMovement = false;
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
        PathIndex = Math.Max(PathIndex - 1, 0);
    }

    internal void Clear() {
        Destination = null;
        PathIndex = 0;
        clearedForMovement = false;
    }
}

class Movement : IModule {
    public void Init(EntityStore world) {
        UpdatePhases.Input.Add(new ProcessPathMovement());
        UpdatePhases.ApplyActions.Add(ActionProcessor.FromFunc<MovementAction>(ProcessMovementAction));
    }

    internal static void OnEnemyVisibilityChange(TagsChanged changed) {
        if (changed.AddedTags.Has<IsVisible>()
            && !changed.OldTags.Has<IsVisible>()
            && !changed.Entity.Tags.Has<Corpse>()) {
            ref var path = ref Singleton.Player.GetComponent<PathMovement>();
            path.Clear();
        }
    }

    private ActionProcessor.Result ProcessMovementAction(ref MovementAction action, Entity actionEntt) {
        var source = action.Entity;
        var grid = Singleton.Get<Grid>();
        ref var gridPos = ref action.Entity.GetComponent<GridPosition>();
        var oldPos = gridPos.Value;
        Vec2I dir = new(action.Dx, action.Dy);
        var newPos = oldPos + dir;

        // Check if move is valid
        if (!grid.IsInside(newPos) || !IsTileFree(grid, newPos, action.Entity)) {
            // Player can harmlessly bump and not lose the turn if invalid
            // Enemies will lose their turn
            if (!source.Tags.Has<Player>()) return ActionProcessor.Result.Done;
            Animations.Bump(
                action.Entity,
                oldPos.ToWorldPos(),
                oldPos.ToWorldPos() + dir.ToWorldPos() * 0.3f,
                onEnd: (ref Position p) => {
                    actionEntt.AddTag<IsActionFinished>();
                    source.AddTag<CanAct>();
                }
            );
            return ActionProcessor.Result.Running;
        }

        // Perform the move
        grid.MoveCharacterPos(gridPos.Value, newPos);
        gridPos.Value = newPos;
        action.Entity.Set(gridPos); // triggers update hooks

        // Calculate vision. Not ideal here but would be much harder to move this into Vision
        var wasVisible = grid.CheckTile<IsVisible>(oldPos);
        var isVisible = grid.CheckTile<IsVisible>(newPos);
        if (wasVisible || isVisible) action.Entity.AddTag<IsVisible>();

        // Add movement animations
        Vector3 currPos = action.Entity.GetComponent<GridPosition>().Value.ToWorldPos();
        Animations.Move(action.Entity, actionEntt, oldPos, currPos,
            () => {
                Vec2I gridPos = source.GetComponent<GridPosition>().Value;
                if (!Singleton.Get<Grid>().CheckTile<IsVisible>(gridPos))
                    source.RemoveTag<IsVisible>();
            });
        return ActionProcessor.Result.Running;
    }

    static bool IsTileFree(Grid grid, Vec2I pos, Entity entt) {
        // TODO refactor grid
        var destTile = grid.Tile[pos.X, pos.Y];
        bool isTileFree = destTile.IsNull || (!destTile.Tags.Has<BlocksPathing>());
        var destChar = grid.Character[pos.X, pos.Y];
        bool isCharFree = destChar.IsNull || (!destChar.Tags.Has<BlocksPathing>());
        return isTileFree && (isCharFree || destChar == entt);
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
			// TODO not handling case where previous point was unreachable; still continues on the path which triggers this assert
			bool isMoveValid = Math.Abs(diff.X) + Math.Abs(diff.Y) <= 2;
            if (!isMoveValid) {
                // Debug.Assert(isMoveValid, $"BUG: Movement is too big: from {pos.Value} to {next}");
                path.Clear();
                Console.WriteLine($"BUG: Movement is too big: from {pos.Value} to {next}");
                CommandBuffer.RemoveTag<CanAct>(entt.Id);
                return;
            }

            var grid = Singleton.Get<Grid>();
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