using System.Reflection;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

public struct Cube : IComponent { };

public class Game {
	private static EntityStore World;
	private static SystemRoot UpdateRootSystems;
	private static SystemRoot RenderRootSystems;
	private static List<IModule> Modules;

	public static void Init() {
		Raylib.SetTargetFPS(60);
		Raylib.InitWindow(1024, 600, "RayLike Challenge");
		RegisterComponentsForNativeAot();

		World = new EntityStore();

		UpdateRootSystems = new SystemRoot(World) {
			UpdatePhases.Input,
			UpdatePhases.ApplyActions,
		};

		RenderRootSystems = new SystemRoot(World) {
			RenderPhases.Render,
		};

		Modules = [
			new Assets(),
			new Render(),
			new ActionsModule(),
			new Movement(),
		];
		Modules.ForEach(m => m.Init(World));

		// TODO move
		World.CreateEntity(
			new InputReceiver(),
			new Position(0, 0, 0),
			new Cube()
		);
	}

	public static void Update() {
		UpdateRootSystems.Update(GetUpdateTick());
	}

	public static void Draw() {
		Raylib.BeginDrawing();
		Raylib.ClearBackground(Color.DarkGray);
		RenderRootSystems.Update(GetUpdateTick());
		Raylib.EndDrawing();
	}

	private static UpdateTick GetUpdateTick() {
		return new UpdateTick(Raylib.GetFrameTime(), (float)Raylib.GetTime());
	}

	private static void RegisterComponentsForNativeAot() {
		// Need to register schema for AOT (wasm)
		// can maybe move to wasm project?
		var aot = new NativeAOT();
		// TODO register new component here
		// would be cool to have this codegened from interfaces
		aot.RegisterComponent<Position>();
		aot.RegisterComponent<Cube>();
		aot.RegisterComponent<ActionBuffer>();
		aot.RegisterComponent<InputReceiver>();
		// aot.RegisterTag      <MyTag1>();
		// aot.RegisterScript   <MyScript>();
		var schema = aot.CreateSchema();
	}
}
