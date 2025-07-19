using Friflo.Engine.ECS;
using Raylib_cs;

namespace RayLikeShared;

class Level : IModule {
	public void Init(EntityStore world) {
		world.CreateEntity(
			new GridPosition(2, 2),
			new InputReceiver(),
			new Position(2, 0, 2),
			new Scale3(0.5f, 0.5f, 0.5f),
			// new Cube()
			new Mesh(Raylib.LoadModel("Resources/character_rogue.gltf"))
		);

		// world.CreateEntity(
		// 	new GridPosition(0, 0),
		// 	new InputReceiver(),
		// 	new Position(0, 0, 0),
		// 	new Scale3(Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f),
		// 	new Cube()
		// );

		// world.CreateEntity(
		// 	new GridPosition(-2, -2),
		// 	new InputReceiver(),
		// 	new Position(-2, 0, -2),
		// 	new Scale3(Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f),
		// 	new Cube()
		// );

		// world.CreateEntity(
		// 	new GridPosition(-3, 3),
		// 	new InputReceiver(),
		// 	new Position(-3, 0, 3),
		// 	new Scale3(Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f),
		// 	new Cube()
		// );

		InitLevel(world);
	}

	private void InitLevel(EntityStore world) {
		for (int i = -10; i < 10; i++) {
			for (int j = -10; j < 10; j++) {
				world.CreateEntity(
					new Position(i, -0.5f, j),
					new Scale3(0.5f, 0.5f, 0.5f),
					new Mesh(Raylib.LoadModel("Resources/tileBrickA_small.gltf.glb"))
				);
			}
		}

		for (int i = -5; i < 5; i++) {
			world.CreateEntity(
				new Position(i, -0.5f, 2),
				new Scale3(0.5f, 0.5f, 0.5f),
				new Mesh(Raylib.LoadModel("Resources/wall_end.gltf.glb")) {
					Color = Raylib.Fade(Color.White, 0.5f)
				}
			);
		}

		for (int i = -5; i < 5; i++) {
			world.CreateEntity(
				new Position(i, -0.5f, -2),
				new Scale3(0.5f, 0.5f, 0.5f),
				new Mesh(Raylib.LoadModel("Resources/wall_end.gltf.glb"))
			);
		}
	}
}