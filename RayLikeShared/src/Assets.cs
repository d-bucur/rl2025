using Friflo.Engine.ECS;
using Raylib_cs;

namespace RayLikeShared;

class Assets: IModule {
	internal static Texture2D logo;

	public void Init(EntityStore world) {
		logo = Raylib.LoadTexture("Resources/raylib_logo.png");
	}
}