using Friflo.Engine.ECS;

namespace RayLikeShared;

class Level : IModule {
	public void Init(EntityStore world) {
		world.CreateEntity(
			new InputReceiver(),
			new Position(0, 0, 0),
			new Scale3(Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f),
			new Cube()
		);
	}
}