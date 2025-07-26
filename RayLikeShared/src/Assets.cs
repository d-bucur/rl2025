using Friflo.Engine.ECS;
using Raylib_cs;

namespace RayLikeShared;

class Assets: IModule {
	internal static Texture2D logo;
	internal static Shader meshShader;

	public void Init(EntityStore world) {
		logo = Raylib.LoadTexture("Resources/raylib_logo.png");
		meshShader = Raylib.LoadShader("Resources/shaders/base.vs", "Resources/shaders/base.fs");
	}
}