using Friflo.Engine.ECS;
using Raylib_cs;

namespace RayLikeShared;

class Assets: IModule {
	internal static Texture2D rayLogoTexture;
	internal static Texture2D monsterTexture;
	internal static Texture2D heroesTexture;

	internal static Shader meshShader;
	internal static Shader billboardShader;

	internal static Model heroModel;
	internal static Model enemyModel;
	internal static Model wallModel;
	internal static Model floorModel;

	public void Init(EntityStore world) {
		// could use dictionary here, not a huge benefit. Would need to use string paths in all call sites
		meshShader = Raylib.LoadShader("Resources/shaders/base.vs", "Resources/shaders/base.fs");
		billboardShader = Raylib.LoadShader("", "Resources/shaders/billboard.fs");

		rayLogoTexture = Raylib.LoadTexture("Resources/raylib_logo.png");
		monsterTexture = Raylib.LoadTexture("Resources/sprites/monsters.png");
		heroesTexture = Raylib.LoadTexture("Resources/sprites/rogues.png");

		heroModel = Raylib.LoadModel("Resources/character_rogue.gltf");
		enemyModel = Raylib.LoadModel("Resources/character_skeleton_minion.gltf");
		wallModel = Raylib.LoadModel("Resources/bricks_A.gltf");
		floorModel = Raylib.LoadModel("Resources/tileBrickB_small.gltf.glb");
	}
}