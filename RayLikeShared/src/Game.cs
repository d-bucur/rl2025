using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

public class Game {
	private EntityStore World;
	private SystemRoot UpdateRootSystems;
	private SystemRoot RenderRootSystems;
	private List<IModule> Modules;

	public Game() {
		Raylib.SetTargetFPS(60);
		Raylib.InitWindow(1024, 600, "RaygueLike Challenge");
		RegisterComponentsForNativeAot();

		World = new EntityStore();
		Singleton.Init(World);

		UpdateRootSystems = new SystemRoot(World);
		UpdatePhases.All.ForEach(p => UpdateRootSystems.Add(p));

		RenderRootSystems = new SystemRoot(World) {
			RenderPhases.PreRender,
			RenderPhases.Render,
		};

		Modules = [
			new Assets(),
			new Render(),
			new Level(),
			new Main(),
			new ActionsModule(),
			new Movement(),
		];
		Modules.ForEach(m => m.Init(World));
	}

	public void Update() {
		UpdateRootSystems.Update(GetUpdateTick());
	}

	public void Draw() {
		Raylib.BeginDrawing();
		Raylib.ClearBackground(Palette.Colors[3]);
		RenderRootSystems.Update(GetUpdateTick());
		Raylib.EndDrawing();
	}

	public static UpdateTick GetUpdateTick() {
		return new UpdateTick(Raylib.GetFrameTime(), (float)Raylib.GetTime());
	}

	private static void RegisterComponentsForNativeAot() {
		// Need to register schema for AOT (wasm)
		// can maybe move to wasm project?
		var aot = new NativeAOT();
		// TODO register new components here
		// would be cool to have this codegened from interfaces
		aot.RegisterComponent<Position>();
		aot.RegisterComponent<Scale3>();
		aot.RegisterComponent<Cube>();
		aot.RegisterComponent<ActionBuffer>();
		aot.RegisterComponent<InputReceiver>();
		aot.RegisterComponent<Camera>();
		aot.RegisterComponent<Tween>();
		aot.RegisterComponent<Grid>();
		aot.RegisterComponent<GridPosition>();
		aot.RegisterComponent<CameraFollowTarget>();
		
		aot.RegisterTag<Character>();
		aot.RegisterTag<Player>();
		aot.RegisterTag<Enemy>();
		aot.RegisterTag<BlocksFOV>();
		aot.RegisterTag<BlocksPathing>();
		// aot.RegisterScript   <MyScript>();
		var schema = aot.CreateSchema();
	}
}
