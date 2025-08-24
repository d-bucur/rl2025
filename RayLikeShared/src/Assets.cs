using Friflo.Engine.ECS;
using Raylib_cs;

namespace RayLikeShared;

class Assets : IModule {
	internal static Shader meshShader;
	internal static Shader billboardShader;

	internal static Texture2D monsterTexture = Raylib.LoadTexture("Resources/sprites/monsters.png");
	internal static Texture2D heroesTexture = Raylib.LoadTexture("Resources/sprites/rogues.png");
	internal static Texture2D itemsTexture = Raylib.LoadTexture("Resources/sprites/items.png");
	internal static Texture2D tilesTexture = Raylib.LoadTexture("Resources/sprites/tiles.png");
	// internal static Texture2D rayLogoTexture = Raylib.LoadTexture("Resources/raylib_logo.png");

	internal static Model wallModel = Raylib.LoadModel("Resources/bricks_A.gltf");

	public void Init(EntityStore world) {
		Console.WriteLine($"OpenGL version: {Rlgl.GetVersion()}");
		var shadersRoot = Rlgl.GetVersion() == GlVersion.OpenGlEs20 ? "Resources/shaders/web" : "Resources/shaders";
		meshShader = Raylib.LoadShader($"{shadersRoot}/base.vs", $"{shadersRoot}/base.fs");
		billboardShader = Raylib.LoadShader("", $"{shadersRoot}/billboard.fs");
	}
}