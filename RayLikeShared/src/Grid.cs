using System.Diagnostics;
using Friflo.Engine.ECS;

namespace RayLikeShared;

// Similar to GameMap in tutorial
struct Grid(int sizeX, int sizeY) : IComponent {
	internal Entity[,] Tile = new Entity[sizeX, sizeY];
	internal Entity[,] Character = new Entity[sizeX, sizeY];

	public bool IsInsideGrid(Vec2I pos) {
		return pos.X < sizeX && pos.X >= 0
			&& pos.Y < sizeY && pos.Y >= 0;
	}

	public void MoveCharacterPos(Vec2I oldPos, Vec2I newPos) {
		var character = Character[oldPos.X, oldPos.Y];
		Character[oldPos.X, oldPos.Y] = default;
		Debug.Assert(Character[oldPos.X, oldPos.Y].IsNull);
		Character[newPos.X, newPos.Y] = character;
	}

	public bool IsBlocking(Vec2I pos) {
		if (!IsInsideGrid(pos))
			return true;
		return Tile[pos.X, pos.Y].Tags.Has<BlocksFOV>();
	}

	// public bool TryMovePos(Vec2I oldPos, Vec2I newPos) {
	// 	if (!IsInsideGrid(newPos))
	// 		return false;
	// 	MoveCharacterPos(oldPos, newPos);
	// 	return true;
	// }
	internal static readonly Vec2I[] NeighborsCardinal = [(0, -1), (-1, 0), (1, 0), (0, 1)];
	internal static readonly Vec2I[] NeighborsDiagonal = [(-1, -1), (1, -1), (-1, 1), (1, 1)];
}

struct GridPosition : IComponent {
	internal Vec2I Value;
	public GridPosition(int x, int y) {
		Value = new Vec2I(x, y);

	}
}

public struct Vec2I {
	public int X;
	public int Y;

	public Vec2I(int x, int y) {
		X = x;
		Y = y;
	}

	public static Vec2I operator +(Vec2I a, Vec2I b) => new(a.X + b.X, a.Y + b.Y);
	public static Vec2I operator -(Vec2I a, Vec2I b) => new(a.X - b.X, a.Y - b.Y);
	public static Vec2I operator *(Vec2I a, int b) => new(a.X * b, a.Y * b);
	public static Vec2I operator /(Vec2I a, int b) => new(a.X / b, a.Y / b);

	public static implicit operator Vec2I((int, int) t) => new(t.Item1, t.Item2);

	public override string ToString() {
		return $"V2I({X}, {Y})";
	}
}