using Friflo.Engine.ECS;
using Raylib_cs;

namespace RayLikeShared;

class Assets: IModule {
	internal static Texture2D logo;
	internal static Shader meshShader;
	internal static Model characterModel;
	internal static Model enemyModel;
	internal static Model wallModel;
	internal static Model floorModel;

	public void Init(EntityStore world) {
		logo = Raylib.LoadTexture("Resources/raylib_logo.png");
		meshShader = Raylib.LoadShader("Resources/shaders/base.vs", "Resources/shaders/base.fs");
		characterModel = Raylib.LoadModel("Resources/character_rogue.gltf");
		enemyModel = Raylib.LoadModel("Resources/character_skeleton_minion.gltf");
		wallModel = Raylib.LoadModel("Resources/bricks_A.gltf");
		floorModel = Raylib.LoadModel("Resources/tileBrickB_small.gltf.glb");
	}
}