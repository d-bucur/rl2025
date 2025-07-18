using System.Numerics;
using Friflo.Engine.ECS;
using Raylib_cs;

public struct Velocity : IComponent { public Vector3 value; };

public static class Helpers {
    // Easy way to do this with an implicit cast?
    public static Vector3 ToVec3(Position p) => new Vector3(p.x, p.y, p.z);
}

public class Movement {
	internal static void Update(EntityStore world) {
		if (Raylib.IsMouseButtonPressed(MouseButton.Left)) { Console.WriteLine($"Mouse down"); }
		if (Raylib.IsKeyDown(KeyboardKey.Space)) { Console.WriteLine($"KeyDown"); }

		world.Query<Position, Velocity>().ForEachEntity((ref Position position, ref Velocity velocity, Entity entity) => {
			position.value += velocity.value * (float)Math.Sin(Raylib.GetTime() * 5);
		});
	}
}