using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

struct InputReceiver : IComponent;

class InputModule : IModule {
    public void Init(EntityStore world) {
        UpdatePhases.Input.Add(new CameraInputSystem());
        UpdatePhases.Input.Add(new PlayerInputSystem());
        UpdatePhases.Input.Add(new GameInputSystem());
    }
}

file class PlayerInputSystem : QuerySystem<InputReceiver> {
    public PlayerInputSystem() => Filter.AllTags(Tags.Get<CanAct>());

    protected override void OnUpdate() {
        var cmd = CommandBuffer;

        Query.ForEachEntity((ref InputReceiver receiver, Entity entt) => {
            Vec2I? keyMovement = null;
            if (IsActionPressed(KeyboardKey.A))
                keyMovement = (-1, 0);
            else if (IsActionPressed(KeyboardKey.D))
                keyMovement = (1, 0);
            else if (IsActionPressed(KeyboardKey.S) || IsActionPressed(KeyboardKey.X))
                keyMovement = (0, 1);
            else if (IsActionPressed(KeyboardKey.W))
                keyMovement = (0, -1);
            else if (IsActionPressed(KeyboardKey.Q))
                keyMovement = (-1, -1);
            else if (IsActionPressed(KeyboardKey.E))
                keyMovement = (1, -1);
            else if (IsActionPressed(KeyboardKey.Z))
                keyMovement = (-1, 1);
            else if (IsActionPressed(KeyboardKey.C))
                keyMovement = (1, 1);

            if (keyMovement.HasValue) {
                HandleMovementInput(entt, cmd, keyMovement.Value);
            }
            else if (IsActionPressed(KeyboardKey.R)) {
                // TurnsManagement.QueueAction(cmd, new RestAction(), true);
                TurnsManagement.QueueAction(cmd, new MovementAction(entt, 0, 0), entt);
                cmd.RemoveTag<CanAct>(entt.Id);
            }

            if (Raylib.IsKeyDown(KeyboardKey.Backspace)) {
                TurnsManagement.QueueAction(cmd, new EscapeAction(), entt);
                cmd.RemoveTag<CanAct>(entt.Id);
            }
        });
    }

    static void HandleMovementInput(Entity entt, CommandBuffer cmd, Vec2I keyMovement) {
        Vec2I prevPos = entt.GetComponent<GridPosition>().Value;
        var newPos = prevPos + keyMovement;
        var grid = Singleton.Entity.GetComponent<Grid>();
        if (grid.CheckTile<BlocksPathing>(newPos)) {
            // TODO Should turn into blocking action
            Animations.Bump(entt, prevPos.ToWorldPos(), prevPos.ToWorldPos() + keyMovement.ToWorldPos() * 0.3f);
            return;
        }
        Entity charAtPos = grid.Character[newPos.X, newPos.Y];
        if (charAtPos.IsNull) {
            var action = new MovementAction(entt, keyMovement.X, keyMovement.Y);
            TurnsManagement.QueueAction(cmd, action, entt);
        }
        else {
            var action = new MeleeAction(entt, charAtPos, keyMovement.X, keyMovement.Y);
            TurnsManagement.QueueAction(cmd, action, entt);
        }
        cmd.RemoveTag<CanAct>(entt.Id);
    }

    static bool IsActionPressed(KeyboardKey key) {
        return Raylib.IsKeyPressed(key) || Raylib.IsKeyDown(key);
    }
}

file class GameInputSystem : QuerySystem {
    protected override void OnUpdate() {
        ref Settings settings = ref Singleton.Entity.GetComponent<Settings>();
        // Minimap
        if (Raylib.IsKeyPressed(KeyboardKey.M))
            settings.MinimapEnabled = !settings.MinimapEnabled;
        // Debug colors
        if (Raylib.IsKeyPressed(KeyboardKey.K) && IsDevKeyModifier())
            settings.DebugColorsEnabled = !settings.DebugColorsEnabled;
        // Debug pathfinding
        if (Raylib.IsKeyPressed(KeyboardKey.P) && IsDevKeyModifier())
            settings.DebugPathfinding = !settings.DebugPathfinding;
        // Visibiltiy hack
        if (Raylib.IsKeyPressed(KeyboardKey.J) && IsDevKeyModifier()) {
            settings.VisibilityHack = !settings.VisibilityHack;
            RecalculateVisionSystem.MarkAllTiles<IsExplored>();
            RecalculateVisionSystem.MarkAllTiles<IsVisible>();
            RecalculateVisionSystem.MarkAllCharacters<IsVisible>();
        }
        // Exploration hack
        if (Raylib.IsKeyPressed(KeyboardKey.N) && IsDevKeyModifier()) {
            settings.ExplorationHack = true;
            RecalculateVisionSystem.MarkAllTiles<IsExplored>();
        }
    }

    static bool IsDevKeyModifier() {
        return Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
    }
}

file class CameraInputSystem : QuerySystem<CameraFollowTarget, Camera> {
    Vector3 prevOffset = new Vector3(0, 30, 1);
    float prevCameraTargetFollow = 1f;
    int? DragStart;
    // CameraProjection prevProjection = CameraProjection.Orthographic;
    // float prevFov = 10;

    protected override void OnUpdate() {
        Query.ForEachEntity((ref CameraFollowTarget follow, ref Camera cam, Entity e) => {
            follow.Offset.Y -= Raylib.GetMouseWheelMoveV().Y;
            if (Raylib.IsKeyPressed(KeyboardKey.Space)) {
                (follow.Offset, prevOffset) = (prevOffset, follow.Offset);
                (follow.SpeedTargetFact, prevCameraTargetFollow) = (prevCameraTargetFollow, follow.SpeedTargetFact);
                // (cam.Value.Projection, prevProjection) = (prevProjection, cam.Value.Projection);
                // (cam.Value.FovY, prevFov) = (prevFov, cam.Value.FovY);
                ref var settings = ref Singleton.Entity.GetComponent<Settings>();
                settings.IsOverhead = !settings.IsOverhead;
            }

            // TODO cam rotation is pretty buggy and not very useful. Remove?
            if (Raylib.IsMouseButtonPressed(MouseButton.Middle)) DragStart = Raylib.GetMouseX();
            if (DragStart is int start) {
				const float maxAngle = MathF.PI / 4;
				follow.Angle = MathF.Min(MathF.Max(follow.Angle + Raylib.GetMouseDelta().X / 400f, -maxAngle), maxAngle);
                follow.Offset = new Vector3(MathF.Sin(follow.Angle), 0, MathF.Cos(follow.Angle)) * follow.Distance;
                follow.Offset.Y = follow.Height;
                var playerPos = Singleton.Player.GetComponent<Position>();
                cam.Value.Position = playerPos.value + follow.Offset;
            }
            if (Raylib.IsMouseButtonReleased(MouseButton.Middle)) DragStart = null;
        });
    }
}