using Friflo.Engine.ECS;
using Raylib_cs;

namespace RayLikeShared;

class Level : IModule {
	public void Init(EntityStore world) {
		Singleton.Entity.AddComponent(new Grid(Config.MAP_SIZE_X, Config.MAP_SIZE_Y));

		world.OnComponentAdded += (change) => {
			if (change.Action == ComponentChangedAction.Add && change.Type == typeof(GridPosition)) {
				var grid = Singleton.Entity.GetComponent<Grid>();
				var pos = change.Component<GridPosition>();
				grid.Value[pos.Value.X, pos.Value.Y] = change.Entity;
			}
		};

		// player
		Singleton.Player = world.CreateEntity(
			new InputReceiver(),
			new GridPosition(2, 2),
			new Position(2, 0, 2), // TODO should automatically be Set by grid
			new Scale3(Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f),
			new Cube() { Color = Color.Green},
			Tags.Get<Player, Character, BlocksPathing>()
		);

		world.CreateEntity(
			new GridPosition(1, 1),
			new InputReceiver(),
			new Position(1, 0, 1),
			new Scale3(Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f),
			new Cube(),
			Tags.Get<Enemy, Character, BlocksPathing>()
		);

		world.CreateEntity(
			new GridPosition(3, 3),
			new InputReceiver(),
			new Position(3, 0, 3),
			new Scale3(Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f),
			new Cube(),
			Tags.Get<Enemy, Character, BlocksPathing>()
		);

		// world.CreateEntity(
		// 	new GridPosition(-3, 3),
		// 	new InputReceiver(),
		// 	new Position(-3, 0, 3),
		// 	new Scale3(Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f),
		// 	new Cube(),
		// Tags.Get<Character, BlocksPathing>()
		// );

		InitWalls(world);
	}

	private void InitWalls(EntityStore world) {
		for (int i = 0; i < 10; i++) {
			world.CreateEntity(
				new GridPosition(i, 0),
				new Position(i, 0, 0),
				new Scale3(Config.GRID_SIZE, Config.GRID_SIZE, Config.GRID_SIZE),
				new Cube() { Color = Color.DarkBlue },
				Tags.Get<BlocksPathing, BlocksFOV>()
			);
		}

		for (int i = 0; i < 10; i++) {
			world.CreateEntity(
				new GridPosition(i, 4),
				new Position(i, 0, 4),
				new Scale3(Config.GRID_SIZE, Config.GRID_SIZE, Config.GRID_SIZE),
				new Cube() { Color = Raylib.Fade(Color.DarkBlue, 0.3f) },
				Tags.Get<BlocksPathing, BlocksFOV>()
			);
		}
	}
}