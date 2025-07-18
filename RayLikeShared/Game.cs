using Friflo.Engine.ECS;
using Raylib_cs;

namespace RayLikeShared;

public struct Cube : IComponent { };

public class Game {
	private static EntityStore world;

	public static void Init() {
		Raylib.SetTargetFPS(60);
		Raylib.InitWindow(1024, 600, "RayLike Challenge");
		RegisterComponentsForNativeAot();

		world = new EntityStore();

		ActionsModule.Init(world);
		Assets.Init(world);
		Render.Init(world);

		world.CreateEntity(
			new InputReceiver(),
			new Position(0, 0, 0),
			new Cube()
		);
	}

	public static void Update() {
		ActionsModule.Update(world);
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
		// TODO register new component here
		aot.RegisterComponent<Position>();
		aot.RegisterComponent<Cube>();
		aot.RegisterComponent<ActionBuffer>();
		aot.RegisterComponent<InputReceiver>();
		// aot.RegisterTag      <MyTag1>();
		// aot.RegisterScript   <MyScript>();
		var schema = aot.CreateSchema();
	}
}
