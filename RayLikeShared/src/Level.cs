using Friflo.Engine.ECS;
using Raylib_cs;

namespace RayLikeShared;

class Level : IModule {
	public void Init(EntityStore world) {
		world.CreateEntity(
			new GridPosition(0, 0),
			new InputReceiver(),
			new Position(0, 0, 0),
			new Scale3(Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f),
			new Cube()
		);

		// world.CreateEntity(
		// 	new GridPosition(2, 2),
		// 	new InputReceiver(),
		// 	new Position(2, 0, 2),
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

		InitWalls(world);
	}

	private void InitWalls(EntityStore world) {
		for (int i = -5; i < 5; i++) {
			world.CreateEntity(
				new Position(i, 0, -2),
				new Scale3(Config.GRID_SIZE, Config.GRID_SIZE, Config.GRID_SIZE),
				new Cube() { Color = Color.DarkBlue }
			);
		}

		for (int i = -5; i < 5; i++) {
			world.CreateEntity(
				new Position(i, 0, 3),
				new Scale3(Config.GRID_SIZE, Config.GRID_SIZE, Config.GRID_SIZE),
				new Cube() { Color = Raylib.Fade(Color.DarkBlue, 0.3f) }
			);
		}
	}
}