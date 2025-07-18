using System.Numerics;
using Friflo.Engine.ECS;
using Raylib_cs;

namespace RayLikeShared;

public struct Cube : IComponent { };

public class Game {
	private static Texture2D logo;
	private static Vector3 cubePosition;
	private static Camera3D camera;

	private static EntityStore world;
	private static ArchetypeQuery<Position, Velocity> query;

	public static void Init() {
		// Console.WriteLine("Init");
		Raylib.SetTargetFPS(60);
		Raylib.InitWindow(800, 480, "RayLike");
		logo = Raylib.LoadTexture("Resources/raylib_logo.png");
		InitCamera();
		cubePosition = new(0.0f, 0.0f, 0.0f);

		world = new EntityStore();
		world.CreateEntity(
			new Position(0, 0, 0),
			new Velocity { value = new Vector3(0.1f, 0, 0) },
			new Cube()
		);

		query = world.Query<Position, Velocity>();
	}

	public static void Update() {
		// Console.WriteLine("Update");
		if (Raylib.IsMouseButtonPressed(MouseButton.Left)) { Console.WriteLine($"Mouse down"); }
		if (Raylib.IsKeyDown(KeyboardKey.Space)) { Console.WriteLine($"KeyDown"); }

		query.ForEachEntity((ref Position position, ref Velocity velocity, Entity entity) => {
			position.value += velocity.value * (float)Math.Sin(Raylib.GetTime() * 5);
		});
	}

	public static void Draw() {
		// Console.WriteLine("Draw");
		Raylib.BeginDrawing();
		Raylib.ClearBackground(Color.SkyBlue);

		Raylib.DrawFPS(4, 4);
		Raylib.DrawText(Test(), 12, 12, 20, Color.RayWhite);
		Raylib.DrawTexture(logo, 4, 64, Color.White);

		Raylib.BeginMode3D(camera);


		world.Query<Position, Cube>().ForEachEntity((ref Position position, ref Cube cube, Entity entity) => {
			// position.value += velocity.value;
			Raylib.DrawCube(Helpers.ToVec3(position), 2.0f, 2.0f, 2.0f, Color.Red);
			Raylib.DrawCubeWires(Helpers.ToVec3(position), 2.0f, 2.0f, 2.0f, Color.Maroon);
		});


		Raylib.DrawGrid(10, 1.0f);

		Raylib.EndMode3D();

		Raylib.EndDrawing();
	}

	private static string Test() {
		return "Test from class library";
	}

	private static void InitCamera() {
		camera = new Camera3D(
		new Vector3(0.0f, 10.0f, 10.0f),
		new Vector3(0.0f, 0.0f, 0.0f),
		new Vector3(0.0f, 1.0f, 0.0f),
		45.0f,
		CameraProjection.Perspective
		);
	}
}
