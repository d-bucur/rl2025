using Friflo.Engine.ECS;

namespace RayLikeShared;

static class Config {
    internal const float GRID_SIZE = 1;
    internal const int MAP_SIZE_X = 40;
    internal const int MAP_SIZE_Y = 40;
    internal const int MAX_ROOM_COUNT = 80;
    internal const int ROOM_SIZE_MIN = 5;
    internal const int ROOM_SIZE_MAX = 9;

    internal const int CA_DEATH_LIMIT = 3;
    internal const int CA_BIRTH_LIMIT = 5;
    internal const int CA_SIM_STEPS = 4;

    internal const int WIN_SIZE_X = 1024;
    internal const int WIN_SIZE_Y = 600;
    internal const int MAX_ENEMIES_PER_ROOM = 3;

    internal const float VIS_SENSITIVITY = 0.4f; // range: 0 .. 0.5f

    internal const int COST_HORIZONTAL = 2;
    internal const int COST_DIAGONAL = 3;
}

struct Settings() : IComponent {
    internal bool DebugColorsEnabled = false;
    internal bool MinimapEnabled = true;
    internal bool VisibilityHack = false;
    internal bool ExplorationHack = false;
    internal bool DebugPathfinding = false;
    internal bool IsOverhead = false;
}