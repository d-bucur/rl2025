using Friflo.Engine.ECS;

namespace RayLikeShared;

static class Config {
    internal const float GridSize = 1;
    internal const int MapSizeX = 40;
    internal const int MapSizeY = 40;
    internal const int MaxRoomCount = 80;
    internal const int RoomSizeMin = 5;
    internal const int RoomSizeMax = 9;
    internal const int MaxEnemiesPerRoom = 3;
	internal const int MaxItemsPerLevel = 3;

    internal const int CADeathLimit = 3;
    internal const int CABirthLimit = 5;
    internal const int CASimSteps = 4;

    internal const int WinSizeX = 1024;
    internal const int WinSizeY = 600;

    internal const float VisionSensitivity = 0.4f; // range: 0 .. 0.5f

    internal const int CostHorizontal = 2;
    internal const int CostDiagonal = 3;
}

struct Settings() : IComponent {
    internal bool DebugColorsEnabled = false;
    internal bool MinimapEnabled = true;
    internal bool VisibilityHack = false;
    internal bool ExplorationHack = false;
    internal bool DebugPathfinding = false;
    internal bool IsOverhead = false;
}