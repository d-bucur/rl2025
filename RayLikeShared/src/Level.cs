using Friflo.Engine.ECS;

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

		world.CreateEntity(
			new GridPosition(2, 2),
			new InputReceiver(),
			new Position(2, 0, 2),
			new Scale3(Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f),
			new Cube()
		);

		world.CreateEntity(
			new GridPosition(-2, -2),
			new InputReceiver(),
			new Position(-2, 0, -2),
			new Scale3(Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f),
			new Cube()
		);

		world.CreateEntity(
			new GridPosition(-3, 3),
			new InputReceiver(),
			new Position(-3, 0, 3),
			new Scale3(Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f),
			new Cube()
		);
	}
}