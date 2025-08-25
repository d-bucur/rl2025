using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

struct InputReceiver() : IComponent {
    internal List<Entity> BlockedReceivers = new(1);
}

struct MouseTarget : IComponent {
    internal Vec2I? Value;
    internal Entity? Entity;
    internal static readonly Vector3 tileOffset = new(0.5f, -0.5f, 0.5f);
}

class InputModule : IModule {
    public void Init(EntityStore world) {
        Singleton.Entity.AddComponent<MouseTarget>();

        UpdatePhases.Input.Add(new CameraInputSystem());
        UpdatePhases.Input.Add(new PlayerInputSystem());
        UpdatePhases.Input.Add(new GameInputSystem());
        UpdatePhases.Input.Add(new UpdateMousePosition());
    }

    // Push new receiver to top. Will become the only one receiving inputs
    internal static void PushReceiver(Entity selector, ref CommandBuffer cmds) {
        var query = Singleton.World.Query<InputReceiver>(new QueryFilter().AllTags(Tags.Get<InputEnabled>()));
        ref var receiver = ref selector.GetComponent<InputReceiver>();
        receiver.BlockedReceivers = [.. query.Entities];
        foreach (var e in receiver.BlockedReceivers) {
            cmds.RemoveTag<InputEnabled>(e.Id);
        }
        cmds.AddTag<InputEnabled>(selector.Id);
    }

    // Disable receiving inputs on the receiver and restore the receivers it was blocking
    internal static void PopReceiver(Entity selector, ref CommandBuffer cmds) {
        ref var receiver = ref selector.GetComponent<InputReceiver>();
        foreach (var e in receiver.BlockedReceivers) {
            cmds.AddTag<InputEnabled>(e.Id);
        }
    }
}

file class UpdateMousePosition : QuerySystem {
    public UpdateMousePosition() => Filter.AllTags(Tags.Get<Player>());
    protected override void OnUpdate() {
        if (Query.Count == 0) return;
        ref var mouseTarget = ref Singleton.Get<MouseTarget>();
        mouseTarget.Value = null;
        mouseTarget.Entity = null;
        Camera3D camera = Singleton.Camera.GetComponent<Camera>().Value;
        var ray = Raylib.GetScreenToWorldRay(Raylib.GetMousePosition(), camera);

        // Avoid div0
        if (Math.Abs(ray.Direction.Y) < 1e-6)
            return;

        // Get plane intersection
        float t = -ray.Position.Y / ray.Direction.Y;
        Vector3 intersection = ray.Position + t * ray.Direction + MouseTarget.tileOffset;
        Vec2I mousePosI = Vec2I.FromWorldPos(intersection);
        ref Grid grid = ref Singleton.Get<Grid>();
        if (!grid.IsInside(mousePosI))
            return;
        mouseTarget.Value = mousePosI;
        Entity charAt = grid.Character[mousePosI.X, mousePosI.Y];
        mouseTarget.Entity = charAt.IsNull ? null : charAt;
    }
}

file class PlayerInputSystem : QuerySystem<InputReceiver> {
    public PlayerInputSystem() => Filter.AllTags(Tags.Get<CanAct, InputEnabled>());

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

            bool anyInput = false;
            if (keyMovement.HasValue) {
                HandleMovementInput(entt, cmd, keyMovement.Value);
                anyInput = true;
            }
            else if (IsActionPressed(KeyboardKey.R)) {
                // TurnsManagement.QueueAction(cmd, new RestAction(), true);
                TurnsManagement.QueueAction(cmd, new MovementAction(entt, 0, 0), entt);
                cmd.RemoveTag<CanAct>(entt.Id);
                anyInput = true;
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.F)) {
                if (HandleInteractInput(entt, cmd)) {
                    cmd.RemoveTag<CanAct>(entt.Id);
                    anyInput = true;
                }
            }
            else if (CheckInventoryInput(entt, cmd)) {
                anyInput = true;
            }
            ref var path = ref entt.GetComponent<PathMovement>();
            path.clearedForMovement = true;
            if (anyInput) path.Clear();

            if (Raylib.IsKeyReleased(KeyboardKey.Backspace)) {
                Game.Instance.ResetWorld();
            }
        });
    }

    private static bool CheckInventoryInput(Entity entt, CommandBuffer cmd) {
        for (int i = 0; i < Config.InventoryLimit; i++) {
            if (Raylib.IsKeyReleased(i + KeyboardKey.One)) {
                var items = entt.GetRelations<InventoryItem>();
                if (items.Length > i) {
                    TurnsManagement.QueueAction(cmd, new ConsumeItemAction { Target = entt, Item = items[i].Item }, entt);
                }
                else {
                    MessageLog.Print($"Item slot empty");
                }
                cmd.RemoveTag<CanAct>(entt.Id);
                return true;
            }
        }
        return false;
    }

    static void HandleMovementInput(Entity entt, CommandBuffer cmd, Vec2I keyMovement) {
        Vec2I prevPos = entt.GetComponent<GridPosition>().Value;
        var newPos = prevPos + keyMovement;
        var grid = Singleton.Get<Grid>();
        if (!grid.IsInside(newPos)) return;
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

    private static bool HandleInteractInput(Entity entt, CommandBuffer cmd) {
        Vec2I pos = entt.GetComponent<GridPosition>().Value;
        ref var grid = ref Singleton.Get<Grid>();
        Grid.Other? others = grid.Others[pos.X, pos.Y];
        if (others == null) return false;
        if (others.Value.Value.Any(e => e.Tags.Has<ItemTag>())) {
            TurnsManagement.QueueAction(cmd, new PickupAction { Target = entt, Position = pos }, entt);
            return true;
        }
        if (others.Value.Value.Any(e => e.Tags.Has<Stairs>())) {
            TurnsManagement.QueueAction(cmd, new NextLevelAction { Target = entt }, entt);
            return true;
        }
        return false;
    }

    static bool IsActionPressed(KeyboardKey key) {
        return Raylib.IsKeyPressed(key) || Raylib.IsKeyDown(key);
    }
}

file class GameInputSystem : QuerySystem {
    // not sure how to execute a system only once, so adding a player tag to the filter
    public GameInputSystem() => Filter.AllTags(Tags.Get<Player>());
    protected override void OnUpdate() {
        ref Settings settings = ref Singleton.Get<Settings>();
        // Minimap
        if (Raylib.IsKeyPressed(KeyboardKey.M))
            settings.MinimapEnabled = !settings.MinimapEnabled;
        // Debug colors
        if (Raylib.IsKeyPressed(KeyboardKey.K) && IsDevKeyModifier())
            settings.DebugColorsEnabled = !settings.DebugColorsEnabled;
        // Debug pathfinding
        if (Raylib.IsKeyPressed(KeyboardKey.P) && IsDevKeyModifier())
            settings.DebugPathfinding = !settings.DebugPathfinding;
        // Debug AI trees
        if (Raylib.IsKeyPressed(KeyboardKey.I) && IsDevKeyModifier())
            settings.DebugAI = !settings.DebugAI;
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
        // Free XP hack
        if (Raylib.IsKeyPressed(KeyboardKey.U) && IsDevKeyModifier()) {
            Progression.AddXP(ref Singleton.Player.GetComponent<Level>(), 250);
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
                ref var settings = ref Singleton.Get<Settings>();
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