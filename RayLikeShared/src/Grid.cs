using System.Diagnostics;
using System.Numerics;
using Friflo.Engine.ECS;

namespace RayLikeShared;

// Similar to GameMap in tutorial
struct Grid(int sizeX, int sizeY) : IComponent {
	// Combine into one array with a struct?
	internal Entity[,] Tile = new Entity[sizeX, sizeY];
	internal Entity[,] Character = new Entity[sizeX, sizeY];
	internal Other?[,] Others = new Other?[sizeX, sizeY];

	internal struct Other() {
		internal List<Entity> Value = new();
	}

	internal readonly bool IsInside(Vec2I pos) {
		return pos.X < sizeX && pos.X >= 0
			&& pos.Y < sizeY && pos.Y >= 0;
	}

	internal void MoveCharacterPos(Vec2I oldPos, Vec2I newPos) {
		var character = Character[oldPos.X, oldPos.Y];
		Character[oldPos.X, oldPos.Y] = default;
		Debug.Assert(Character[oldPos.X, oldPos.Y].IsNull);
		Character[newPos.X, newPos.Y] = character;
	}

	internal readonly bool IsBlocking(Vec2I pos) {
		if (!IsInside(pos))
			return true;
		return Tile[pos.X, pos.Y].Tags.Has<BlocksFOV>();
	}

	internal readonly bool Check<T>(Vec2I pos) where T : struct, ITag {
		if (!IsInside(pos))
			return false;
		return Tile[pos.X, pos.Y].Tags.Has<T>();
	}

	// Used only for debugging
	internal void SetDebugColor(Vec2I pos, Raylib_cs.Color color) {
		if (IsInside(pos)) {
			ref var colorComp = ref Tile[pos.X, pos.Y].GetComponent<ColorComp>();
			colorComp.DebugColor = color;
		}
	}

	internal void MarkVisible(Vec2I pos, CommandBuffer cmds) {
		if (!IsInside(pos))
			return;
		Entity tile = Tile[pos.X, pos.Y];
		if (!tile.IsNull) {
			cmds.AddTags(tile.Id, Tags.Get<IsVisible, IsExplored>());
			Entity character = Character[pos.X, pos.Y];
			if (!character.IsNull)
				cmds.AddTag<IsVisible>(character.Id);
			var other = Others[pos.X, pos.Y];
			foreach (var e in other?.Value ?? []) {
				cmds.AddTag<IsVisible>(e.Id);
			}
		}
	}

	internal void RemoveCharacter(Vec2I pos) {
		Character[pos.X, pos.Y] = new Entity();
	}

	internal void AddOther(Entity entity, Vec2I pos) {
		ref var other = ref Others[pos.X, pos.Y];
		if (!other.HasValue) {
			other = new Other();
		}
		other.Value.Value.Add(entity);
	}

	internal static readonly Vec2I[] NeighborsCardinal = [(0, -1), (-1, 0), (1, 0), (0, 1)];
	internal static readonly Vec2I[] NeighborsDiagonal = [(-1, -1), (1, -1), (-1, 1), (1, 1)];
}

struct GridPosition : IComponent {
	internal Vec2I Value;
	internal GridPosition(int x, int y) {
		Value = new Vec2I(x, y);
	}
}

struct Vec2I {
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
	public static bool operator ==(Vec2I a, Vec2I b) => a.X == b.X && a.Y == b.Y;
	public static bool operator !=(Vec2I a, Vec2I b) => a.X != b.X || a.Y != b.Y;

	public static implicit operator Vec2I((int, int) t) => new(t.Item1, t.Item2);
	public static explicit operator Vec2I(Vector2 t) => new((int)MathF.Round(t.X), (int)MathF.Round(t.Y));
	public Vector2 ToVector2() => new Vector2(X, Y);
	public Vector3 ToWorldPos() => new Vector3(X, 0, Y) * Config.GRID_SIZE;
	public static Vec2I FromWorldPos(Vector3 v) => new Vec2I((int)MathF.Round(v.X / Config.GRID_SIZE), (int)MathF.Round(v.Z / Config.GRID_SIZE));

	public override string ToString() => $"V2I({X}, {Y})";

	public static readonly Vec2I Zero = new(0, 0);
}