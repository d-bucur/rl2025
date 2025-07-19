using Friflo.Engine.ECS;

namespace RayLikeShared;

struct Grid(int sizeX, int sizeY) : IComponent {
	internal Entity[,] Value = new Entity[sizeX, sizeY];
}

struct GridPosition(int x, int y) : IComponent {
	internal Vec2I Value = new Vec2I(x, y);
}

struct Vec2I(int x, int y) {
	public int X = x;
	public int Y = y;

	public static Vec2I operator +(Vec2I a, Vec2I b) => new(a.X + b.X, a.Y + b.Y);
	public static Vec2I operator -(Vec2I a, Vec2I b) => new(a.X - b.X, a.Y - b.Y);
	public static Vec2I operator *(Vec2I a, int b) => new(a.X * b, a.Y * b);
	public static Vec2I operator /(Vec2I a, int b) => new(a.X / b, a.Y / b);
}