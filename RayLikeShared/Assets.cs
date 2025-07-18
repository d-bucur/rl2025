using Friflo.Engine.ECS;
using Raylib_cs;

namespace RayLikeShared;

class Assets {
	internal static Texture2D logo;

	internal static void Init(EntityStore world) {
		logo = Raylib.LoadTexture("Resources/raylib_logo.png");
	}
}