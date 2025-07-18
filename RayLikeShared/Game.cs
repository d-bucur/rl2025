using System.Numerics;
using Friflo.Engine.ECS;
using Raylib_cs;

namespace RayLikeShared;

public struct Cube : IComponent { };

public class Game {
	private static EntityStore world;

	public static void Init() {
		Raylib.SetTargetFPS(60);
		Raylib.InitWindow(1024, 600, "RayLike Challenge");

		Assets.Init(world);
		Render.Init(world);
		RegisterComponentsForNativeAot();

		world = new EntityStore();
		world.CreateEntity(
			new Position(0, 0, 0),
			new Velocity { value = new Vector3(0.1f, 0, 0) },
			new Cube()
		);
	}

	public static void Update() {
		Movement.Update(world);
	}

	public static void Draw() {
		// Console.WriteLine("Draw");
		Raylib.BeginDrawing();
		Raylib.ClearBackground(Color.SkyBlue);
		Render.Draw(world);
		Raylib.EndDrawing();
	}

	private static void RegisterComponentsForNativeAot() {
		// Need to register schema for AOT (wasm)
		// can maybe move to wasm project?
		var aot = new NativeAOT();
		aot.RegisterComponent<Position>();
		aot.RegisterComponent<Velocity>();
		aot.RegisterComponent<Cube>();
		// aot.RegisterTag      <MyTag1>();
		// aot.RegisterScript   <MyScript>();
		var schema = aot.CreateSchema();
	}
}
