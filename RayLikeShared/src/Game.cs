using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

public class Game {
	EntityStore World;
	SystemRoot UpdateRootSystems;
	SystemRoot RenderRootSystems;
	List<IModule> Modules;

	public Game() {
		Raylib.SetTargetFPS(60);
		if (!OperatingSystem.IsBrowser()) {
			Console.WriteLine($"Setting ResizableWindow");
			Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
		}
		Raylib.SetWindowMinSize(Config.WIN_SIZE_X, Config.WIN_SIZE_Y);
		Raylib.InitWindow(Config.WIN_SIZE_X, Config.WIN_SIZE_Y, "RaygueLike Challenge");
		RegisterComponentsForNativeAot();

		World = new EntityStore();
		World.EventRecorder.Enabled = true;
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
			new GuiModule(),
			new Level(),
			new Main(),
			new TurnsManagement(),
			new PathfinderModule(),
			new InputModule(),
			new EnemyAIModule(),
			new Movement(),
			new Vision(),
			new Combat(),
		];
		Modules.ForEach(m => m.Init(World));
	}

	public void Update() {
		// not ideal but starting drawing here allows for debug during Update()
		// Only works if there is 1 Update() followed by 1 Draw()
		Raylib.BeginDrawing();
		Raylib.ClearBackground(Palette.Background);
		UpdateRootSystems.Update(GetUpdateTick());
	}

	public void Draw() {
		RenderRootSystems.Update(GetUpdateTick());
		Raylib.EndDrawing();

		World.EventRecorder.ClearEvents();
	}

	public static UpdateTick GetUpdateTick() {
		return new UpdateTick(Raylib.GetFrameTime(), (float)Raylib.GetTime());
	}

	static void RegisterComponentsForNativeAot() {
		// Need to register schema for AOT (wasm)
		// can maybe move to wasm project?
		var aot = new NativeAOT();
		// TODO always remember to register new components here
		// would be cool to have this codegened from interfaces
		aot.RegisterComponent<Position>();
		aot.RegisterComponent<Scale3>();
		aot.RegisterComponent<Cube>();
		aot.RegisterComponent<InputReceiver>();
		aot.RegisterComponent<Camera>();
		aot.RegisterComponent<Tween>();
		aot.RegisterComponent<Grid>();
		aot.RegisterComponent<GridPosition>();
		aot.RegisterComponent<CameraFollowTarget>();
		aot.RegisterComponent<Energy>();
		aot.RegisterComponent<Billboard>();
		aot.RegisterComponent<TextureWithSource>();
		aot.RegisterComponent<Mesh>();
		aot.RegisterComponent<ColorComp>();
		aot.RegisterComponent<VisionSource>();
		aot.RegisterComponent<Settings>();
		aot.RegisterComponent<Fighter>();
		aot.RegisterComponent<Name>();
		aot.RegisterComponent<EnemyAI>();
		aot.RegisterComponent<MessageLog>();
		aot.RegisterComponent<RotationSingle>();
		aot.RegisterComponent<TextFX>();

		aot.RegisterTag<Character>();
		aot.RegisterTag<Corpse>();
		aot.RegisterTag<Player>();
		aot.RegisterTag<Enemy>();
		aot.RegisterTag<BlocksFOV>();
		aot.RegisterTag<BlocksPathing>();
		aot.RegisterTag<CanAct>();
		aot.RegisterTag<IsSeeThrough>();
		aot.RegisterTag<IsVisible>();
		aot.RegisterTag<IsExplored>();

		aot.RegisterComponent<MovementAction>();
		aot.RegisterComponent<EscapeAction>();
		aot.RegisterComponent<MeleeAction>();
		aot.RegisterComponent<RestAction>();

		aot.RegisterTag<IsActionWaiting>();
		aot.RegisterTag<IsActionExecuting>();
		aot.RegisterTag<IsActionFinished>();
		aot.RegisterTag<IsActionBlocking>();

		// aot.RegisterScript   <MyScript>();
		var schema = aot.CreateSchema();
	}
}
