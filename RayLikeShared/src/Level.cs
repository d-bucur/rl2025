using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Transform.Query.Ops;
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

		var rooms = GenerateDungeon(world);
		var center = rooms[0].Center();

		// player
		Singleton.Player = world.CreateEntity(
			new InputReceiver(),
			new GridPosition(center.X, center.Y),
			new Position(center.X, 0, center.Y), // TODO should automatically be Set by grid
			new Scale3(Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f),
			new Cube() { Color = Color.Green },
			Tags.Get<Player, Character, BlocksPathing>()
		);
		ref var camera = ref Singleton.Camera.GetComponent<Camera>();
		camera.Value.Position = new Vector3(center.X, 0, center.Y);
		camera.Value.Target = new Vector3(center.X, 0, center.Y);

		// Test entities
		// world.CreateEntity(
		// 	new GridPosition(1, 1),
		// 	new InputReceiver(),
		// 	new Position(1, 0, 1),
		// 	new Scale3(Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f),
		// 	new Cube(),
		// 	Tags.Get<Enemy, Character, BlocksPathing>()
		// );

		// world.CreateEntity(
		// 	new GridPosition(3, 3),
		// 	new InputReceiver(),
		// 	new Position(3, 0, 3),
		// 	new Scale3(Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f),
		// 	new Cube(),
		// 	Tags.Get<Enemy, Character, BlocksPathing>()
		// );

		// world.CreateEntity(
		// 	new GridPosition(-3, 3),
		// 	new InputReceiver(),
		// 	new Position(-3, 0, 3),
		// 	new Scale3(Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f),
		// 	new Cube(),
		// Tags.Get<Character, BlocksPathing>()
		// );

		// InitWallsTest(world);
	}

	private List<Room> GenerateDungeon(EntityStore world) {
		var map = new bool[Config.MAP_SIZE_X, Config.MAP_SIZE_Y];
		List<Room> rooms = new();

		// Generate rooms and tunnels and save them to a grid
		for (int i = 0; i < Config.ROOM_COUNT; i++) {
			int width = Random.Shared.Next(Config.ROOM_SIZE_MIN, Config.ROOM_SIZE_MAX);
			int height = Random.Shared.Next(Config.ROOM_SIZE_MIN, Config.ROOM_SIZE_MAX);
			var room = new Room(
				Random.Shared.Next(0, Config.MAP_SIZE_X - width),
				Random.Shared.Next(0, Config.MAP_SIZE_Y - height),
				width,
				height
			);
			if (rooms.Any(r => room.Intersects(r)))
				continue;
			DigRoom(map, room);
			if (i > 0)
				DigTunnel(map, rooms.Last().Center(), room.Center());
			rooms.Add(room);
		}

		// Go through the grid and instantiate ECS entities
		for (int i = 0; i < map.GetLength(0); i++) {
			for (int j = 0; j < map.GetLength(1); j++) {
				if (!map[i, j])
					world.CreateEntity(
						new GridPosition(i, j),
						new Position(i, 0, j),
						new Scale3(Config.GRID_SIZE, Config.GRID_SIZE, Config.GRID_SIZE),
						new Cube() { Color = Color.DarkBlue },
						Tags.Get<BlocksPathing, BlocksFOV>()
					);
			}
		}

		return rooms;
	}

	private void DigRoom(bool[,] map, Room room) {
		for (int i = room.StartX + 1; i < room.EndX - 1; i++) {
			for (int j = room.StartY + 1; j < room.EndY - 1; j++) {
				map[i, j] = true;
			}
		}
	}

	private void DigTunnel(bool[,] map, Vec2I start, Vec2I end) {
		for (int i = Math.Min(start.X, end.X); i <= Math.Max(start.X, end.X); i++) {
			map[i, Math.Min(start.Y, end.Y)] = true;
			map[i, Math.Max(start.Y, end.Y)] = true;
		}
		for (int j = Math.Min(start.Y, end.Y); j <= Math.Max(start.Y, end.Y); j++) {
			map[Math.Min(start.X, end.X), j] = true;
			map[Math.Max(start.X, end.X), j] = true;
		}
	}

	// private void InitWallsTest(EntityStore world) {
	// 	for (int i = 0; i < 10; i++) {
	// 		world.CreateEntity(
	// 			new GridPosition(i, 4),
	// 			new Position(i, 0, 4),
	// 			new Scale3(Config.GRID_SIZE, Config.GRID_SIZE, Config.GRID_SIZE),
	// 			new Cube() { Color = Raylib.Fade(Color.DarkBlue, 0.3f) },
	// 			Tags.Get<BlocksPathing, BlocksFOV>()
	// 		);
	// 	}
	// }
}

struct Room {
	public Room(int startX, int startY, int width, int height) {
		StartX = startX;
		StartY = startY;
		EndX = startX + width;
		EndY = startY + height;
	}

	public int StartX;
	public int StartY;
	public int EndX;
	public int EndY;

	internal bool Intersects(Room r) {
		return StartX <= r.EndX
			&& EndX >= r.StartX
			&& StartY <= r.EndY
			&& EndY >= r.StartY;
	}

	internal Vec2I Center() {
		return new Vec2I(
			(StartX + EndX) / 2,
			(StartY + EndY) / 2
		);
	}
}