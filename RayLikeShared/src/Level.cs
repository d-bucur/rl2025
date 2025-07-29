using System.Numerics;
using Friflo.Engine.ECS;
using Raylib_cs;

namespace RayLikeShared;

class Level : IModule {
	Random Rand;
	public void Init(EntityStore world) {
		Rand = new Random();
		Singleton.Entity.AddComponent(new Grid(Config.MAP_SIZE_X, Config.MAP_SIZE_Y));

		world.OnComponentAdded += (change) => {
			if (change.Action == ComponentChangedAction.Add && change.Type == typeof(GridPosition)) {
				var grid = Singleton.Entity.GetComponent<Grid>();
				var pos = change.Component<GridPosition>();
				if (change.Entity.Tags.Has<Character>())
					grid.Character[pos.Value.X, pos.Value.Y] = change.Entity;
				else
					grid.Tile[pos.Value.X, pos.Value.Y] = change.Entity;
			}
		};

		var rooms = GenerateDungeon(world);
		var center = rooms[0].Center();

		SpawnPlayer(world, center);

		ref var camera = ref Singleton.Camera.GetComponent<Camera>();
		camera.Value.Position = new Vector3(center.X, 0, center.Y);
		camera.Value.Target = new Vector3(center.X, 0, center.Y);

		// Test enemies
		foreach (var room in rooms[1..]) {
			SpawnEnemy(world, room.Center());
		}
	}

	private static void SpawnPlayer(EntityStore world, Vec2I pos) {
		Singleton.Player = world.CreateEntity(
			new InputReceiver(),
			new GridPosition(pos.X, pos.Y),
			new Position(pos.X, 0, pos.Y), // TODO should automatically be Set by grid
			new Scale3(Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f),
			// new Cube() { Color = Palette.Colors[0] },
			// new Mesh(Assets.characterModel),
			// new Billboard(), new TextureWithSource(Assets.rayLogoTexture),
			new Billboard(), new TextureWithSource(Assets.heroesTexture) {
				TileSize = new Vec2I(32, 32),
				TileIdx = new Vec2I(2, 2)
			},
			new ColorComp(),
			new Energy() { GainPerTick = 5 },
			Tags.Get<Player, Character, BlocksPathing, VisionSource>()
		);
	}

	private static Entity SpawnEnemy(EntityStore world, Vec2I pos) {
		return world.CreateEntity(
			new GridPosition(pos.X, pos.Y),
			new Position(pos.X, 0, pos.Y),
			new Scale3(Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f, Config.GRID_SIZE * 0.8f),
			// new Cube() { Color = Palette.Colors[1]},
			// new Mesh(Assets.enemyModel),
			new Billboard(), new TextureWithSource(Assets.monsterTexture) {
				TileSize = new Vec2I(32, 32),
				TileIdx = new Vec2I(0, 4)
			},
			new ColorComp(),
			new Energy() { GainPerTick = 4 },
			Tags.Get<Enemy, Character, BlocksPathing>()
		);
	}

	private List<Room> GenerateDungeon(EntityStore world) {
		// true if tile is empty, false if walled
		var map = new bool[Config.MAP_SIZE_X, Config.MAP_SIZE_Y];

		RandomizeTiles(map, 0.7);
		List<Room> rooms = GenerateRooms(map, 3);
		// DigTunnelsBetweenRooms(map, rooms);
		for (int i = 0; i < Config.CA_SIM_STEPS; i++) {
			map = CASimStep(map);
		}
		GenerateRooms(map, Config.ROOM_COUNT);
		DigTunnelsBetweenRooms(map, rooms);

		SpawnTiles(world, map);
		return rooms;
	}

	private List<Room> GenerateRooms(bool[,] map, int roomCount) {
		List<Room> rooms = new();

		// Generate rooms and tunnels and save them to a grid
		for (int i = 0; i < roomCount; i++) {
			int width = Rand.Next(Config.ROOM_SIZE_MIN, Config.ROOM_SIZE_MAX);
			int height = Rand.Next(Config.ROOM_SIZE_MIN, Config.ROOM_SIZE_MAX);
			var room = new Room(
				Rand.Next(0, Config.MAP_SIZE_X - width),
				Rand.Next(0, Config.MAP_SIZE_Y - height),
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

		return rooms;
	}

	private void DigTunnelsBetweenRooms(bool[,] map, List<Room> rooms) {
		for (int i = 1; i < rooms.Count; i++) {
			DigTunnel(map, rooms[i].Center(), rooms[i - 1].Center());
		}
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

	private void RandomizeTiles(bool[,] map, double emptyChance) {
		// Randomize map for cellular automata
		for (int i = 0; i < Config.MAP_SIZE_X; i++) {
			for (int j = 0; j < Config.MAP_SIZE_Y; j++) {
				if (Rand.NextSingle() > emptyChance)
					map[i, j] = true;
			}
		}
	}

	private bool[,] CASimStep(bool[,] map) {
		var newMap = new bool[Config.MAP_SIZE_X, Config.MAP_SIZE_Y];
		for (int i = 0; i < Config.MAP_SIZE_X; i++) {
			for (int j = 0; j < Config.MAP_SIZE_Y; j++) {
				var count = CountEmptyNeighbors(map, i, j);
				if (map[i, j])
					// cell is empty
					newMap[i, j] = !(count <= Config.CA_DEATH_LIMIT);
				else
					// cell is walled
					newMap[i, j] = count >= Config.CA_BIRTH_LIMIT;
			}
		}
		return newMap;
	}

	(int, int)[] _neighbors = [
		(-1,-1), (0, -1), (1, -1),
		(-1, 0), (1, 0),
		(-1, 1), (0, 1), (1, 1)];
	private int CountEmptyNeighbors(bool[,] map, int i, int j) {
		int count = 0;
		foreach (var (x, y) in _neighbors) {
			bool isInBounds = i + x >= 0 && i + x < map.GetLength(0)
				&& j + y >= 0 && j + y < map.GetLength(1);
			if (isInBounds && map[i + x, j + y])
				count += 1;
		}
		return count;
	}

	private static void SpawnTiles(EntityStore world, bool[,] map) {
		// Go through the grid and instantiate ECS entities
		for (int i = 0; i < map.GetLength(0); i++) {
			for (int j = 0; j < map.GetLength(1); j++) {
				if (!map[i, j])
					// spawn wall tile
					world.CreateEntity(
						new GridPosition(i, j),
						new Position(i, 0.4f, j),
						new Scale3(Config.GRID_SIZE / 2, Config.GRID_SIZE / 2, Config.GRID_SIZE / 2),
						// new Cube() { Color = Palette.Colors[2] },
						new Mesh(Assets.wallModel),
						new ColorComp() { Value = Color.Gray },
						Tags.Get<BlocksPathing, BlocksFOV, IsSeeThrough>()
					);
				else
					// spawn floor tile
					world.CreateEntity(
						new GridPosition(i, j),
						new Position(i, -0.5f, j),
						new Scale3(Config.GRID_SIZE / 2, Config.GRID_SIZE / 2, Config.GRID_SIZE / 2),
						// new Cube() { Color = Palette.Colors[2] },
						new Mesh(Assets.wallModel),
						new ColorComp() { Value = Color.DarkGray }
					);
			}
		}
	}
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