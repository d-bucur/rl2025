using System.Numerics;
using Friflo.Engine.ECS;

namespace RayLikeShared;

class Level : IModule {
	public void Init(EntityStore world) {
		Grid grid = new(Config.MapSizeX, Config.MapSizeY);
		Singleton.Entity.AddComponent(grid);

		world.OnComponentAdded += OnGridPositionAdded;
		world.OnComponentRemoved += OnGridPositionRemoved;

		var rooms = GenerateDungeon(world);
		var center = rooms[0].Center();

		Singleton.Player = Prefabs.SpawnPlayer(center);

		ref var camera = ref Singleton.Camera.GetComponent<Camera>();
		camera.Value.Position = new Vector3(center.X, 0, center.Y);
		camera.Value.Target = new Vector3(center.X, 0, center.Y);

		SpawnInEmptyTiles(Config.MaxEnemiesPerLevel, (pos) => Prefabs.SpawnEnemy(pos, Helpers.GetRandomEnum<Prefabs.EnemyType>()));
		SpawnInEmptyTiles(Config.MaxItemsPerLevel, Prefabs.SpawnRandomConsumable);
	}

	static void OnGridPositionAdded(ComponentChanged change) {
		if (change.Type != typeof(GridPosition) || change.Action != ComponentChangedAction.Add) return;
		var grid = Singleton.Entity.GetComponent<Grid>();
		var pos = change.Component<GridPosition>().Value;
		if (change.Entity.Tags.Has<Character>())
			grid.Character[pos.X, pos.Y] = change.Entity;
		else if (change.Entity.HasComponent<Item>())
			grid.AddOther(change.Entity, pos);
		else
			grid.Tile[pos.X, pos.Y] = change.Entity;
	}

	private void OnGridPositionRemoved(ComponentChanged change) {
		if (change.Type != typeof(GridPosition) || change.Action != ComponentChangedAction.Remove) return;
		var grid = Singleton.Entity.GetComponent<Grid>();
		var pos = change.OldComponent<GridPosition>().Value;
		if (change.Entity.HasComponent<Item>()) {
			grid.Others[pos.X, pos.Y].GetValueOrDefault().Value.Remove(change.Entity);
		}
	}

	static void SpawnInEmptyTiles(int count, Func<Vec2I, Entity> SpawnAction) {
		ref var grid = ref Singleton.Entity.GetComponent<Grid>();
		var walkableTilesQuery = Singleton.World.Query().AllTags(Tags.Get<Walkable>());
		var walkableTiles = walkableTilesQuery.ToEntityList();
		for (int i = 0; i < count; i++) {
			var entt = walkableTiles[Random.Shared.Next(walkableTiles.Count)];
			var pos = entt.GetComponent<GridPosition>().Value;
			if (grid.Character[pos.X, pos.Y].IsNull
				&& grid.Others[pos.X, pos.Y] == null) {
				SpawnAction(pos);
			}
		}
	}

	List<Room> GenerateDungeon(EntityStore world) {
		// true if tile is empty, false if walled
		var map = new bool[Config.MapSizeX, Config.MapSizeY];

		RandomizeTiles(map, 0.7);
		List<Room> rooms = GenerateRooms(map, 3);
		// DigTunnelsBetweenRooms(map, rooms);
		for (int i = 0; i < Config.CASimSteps; i++) {
			map = CASimStep(map);
		}
		GenerateRooms(map, Config.MaxRoomCount);
		DigTunnelsBetweenRooms(map, rooms);

		SpawnTiles(world, map);
		return rooms;
	}

	List<Room> GenerateRooms(bool[,] map, int roomCount) {
		// Generate rooms and tunnels and save them to a grid
		List<Room> rooms = new();
		for (int i = 0; i < roomCount; i++) {
			int width = Random.Shared.Next(Config.RoomSizeMin, Config.RoomSizeMax);
			int height = Random.Shared.Next(Config.RoomSizeMin, Config.RoomSizeMax);
			var room = new Room(
				Random.Shared.Next(0, Config.MapSizeX - width),
				Random.Shared.Next(0, Config.MapSizeY - height),
				width,
				height
			);
			if (rooms.Any(r => room.Intersects(r)))
				continue;
			DigRoom(map, room);
			if (i > 0)
				DigTunnel(map, rooms.Last().Center(), room.Center(), true);
			rooms.Add(room);
		}
		return rooms;
	}

	void DigTunnelsBetweenRooms(bool[,] map, List<Room> rooms) {
		for (int i = 1; i < rooms.Count; i++) {
			float r = Random.Shared.NextSingle();
			// middle intersection of 33% will draw both high and low tunnel
			if (r < 0.6) DigTunnel(map, rooms[i].Center(), rooms[i - 1].Center(), true);
			if (r > 0.4) DigTunnel(map, rooms[i].Center(), rooms[i - 1].Center(), false);
		}
	}

	static void DigRoom(bool[,] map, Room room) {
		for (int i = room.StartX + 1; i < room.EndX - 1; i++) {
			for (int j = room.StartY + 1; j < room.EndY - 1; j++) {
				map[i, j] = true;
			}
		}
	}

	static void DigTunnel(bool[,] map, Vec2I start, Vec2I end, bool high) {
		var highest = start.Y >= end.Y ? start : end;
		var lowest = start.Y < end.Y ? start : end;
		var fixedX = high ? lowest : highest;
		var fixedY = high ? highest : lowest;
		for (int x = Math.Min(start.X, end.X); x <= Math.Max(start.X, end.X); x++) {
			map[x, fixedY.Y] = true;
		}
		for (int y = lowest.Y; y <= highest.Y; y++) {
			map[fixedX.X, y] = true;
		}
	}

	void RandomizeTiles(bool[,] map, double emptyChance) {
		// Randomize map for cellular automata
		for (int i = 0; i < Config.MapSizeX; i++) {
			for (int j = 0; j < Config.MapSizeY; j++) {
				if (Random.Shared.NextSingle() > emptyChance)
					map[i, j] = true;
			}
		}
	}

	// Single simulation step of the cellular automata
	bool[,] CASimStep(bool[,] map) {
		var newMap = new bool[Config.MapSizeX, Config.MapSizeY];
		for (int i = 0; i < Config.MapSizeX; i++) {
			for (int j = 0; j < Config.MapSizeY; j++) {
				var count = CountEmptyNeighbors(map, i, j);
				if (map[i, j])
					// cell is empty
					newMap[i, j] = !(count <= Config.CADeathLimit);
				else
					// cell is walled
					newMap[i, j] = count >= Config.CABirthLimit;
			}
		}
		return newMap;
	}

	(int, int)[] _neighbors = [
		(-1,-1), (0, -1), (1, -1),
		(-1, 0), (1, 0),
		(-1, 1), (0, 1), (1, 1)];
	int CountEmptyNeighbors(bool[,] map, int i, int j) {
		int count = 0;
		foreach (var (x, y) in _neighbors) {
			bool isInBounds = i + x >= 0 && i + x < map.GetLength(0)
				&& j + y >= 0 && j + y < map.GetLength(1);
			if (isInBounds && map[i + x, j + y])
				count += 1;
		}
		return count;
	}

	static void SpawnTiles(EntityStore world, bool[,] map) {
		// Go through the grid and instantiate ECS entities
		for (int i = 0; i < map.GetLength(0); i++) {
			for (int j = 0; j < map.GetLength(1); j++) {
				if (!map[i, j])
					// spawn wall tile
					world.CreateEntity(
						new GridPosition(i, j),
						new Position(i, 0.4f, j),
						new Scale3(Config.GridSize / 2, Config.GridSize / 2, Config.GridSize / 2),
						// new Cube() { Color = Palette.Colors[2] },
						new Mesh(Assets.wallModel),
						new ColorComp(Palette.Wall),
						Tags.Get<BlocksPathing, BlocksFOV, IsSeeThrough>()
					);
				else
					// spawn floor tile
					world.CreateEntity(
						new GridPosition(i, j),
						new Position(i, -0.5f, j),
						new Scale3(Config.GridSize / 2, Config.GridSize / 2, Config.GridSize / 2),
						// new Cube() { Color = Palette.Colors[2] },
						new Mesh(Assets.wallModel),
						new ColorComp(Palette.Floor),
						Tags.Get<Walkable>()
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

	public override string ToString() {
		return $"Room({StartX}, {StartY}, {EndX},  {EndY})";
	}
}