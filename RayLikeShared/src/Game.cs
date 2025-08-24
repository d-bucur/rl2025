using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

public class Game {
	internal static Game Instance;
	EntityStore? World;
	SystemRoot UpdateRootSystems;
	SystemRoot RenderRootSystems;
	List<IModule> Modules;

	public Game() {
		Instance = this;
		Raylib.SetTargetFPS(60);
		if (!OperatingSystem.IsBrowser()) {
			Console.WriteLine($"Setting ResizableWindow");
			Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
		}
		Raylib.SetWindowMinSize(Config.WinSizeX, Config.WinSizeY);
		Raylib.InitWindow(Config.WinSizeX, Config.WinSizeY, "RaygueLike Challenge");
		RegisterComponentsForNativeAot();
		ResetWorld();
	}

	public void ResetWorld() {
		var oldWorld = World;
		World = new EntityStore();
		World.EventRecorder.Enabled = true;
		Singleton.Init(World);

		// TODO resetting world goes into speed mode for some reason!
		// Better to recreate level instead of clearing the world
		UpdateRootSystems ??= new SystemRoot();
		if (oldWorld == null) {
			UpdateRootSystems.AddStore(World);
			UpdatePhases.All.ForEach(p => UpdateRootSystems.Add(p));
		}
		else {
			UpdateRootSystems.RemoveStore(oldWorld);
			UpdateRootSystems.AddStore(World);
		}

		RenderRootSystems ??= new SystemRoot();
		if (oldWorld == null) {
			RenderRootSystems.AddStore(World);
			RenderPhases.All.ForEach(p => RenderRootSystems.Add(p));
		}
		else {
			RenderRootSystems.RemoveStore(oldWorld);
			RenderRootSystems.AddStore(World);
		}

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
			new Items(),
			new StatusModule(),
		];
		Modules.ForEach(m => m.Init(World));

		// UpdateRootSystems.SetMonitorPerf(true);
		// RenderRootSystems.SetMonitorPerf(true);
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
		// Console.WriteLine(UpdateRootSystems.GetPerfLog());
		// Console.WriteLine(RenderRootSystems.GetPerfLog());
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
		aot.RegisterComponent<EntityName>();
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
		aot.RegisterComponent<EnemyAI>();
		aot.RegisterComponent<MessageLog>();
		aot.RegisterComponent<RotationSingle>();
		aot.RegisterComponent<TextFX>();
		aot.RegisterComponent<Pathfinder>();
		aot.RegisterComponent<PathMovement>();
		aot.RegisterComponent<Team>();
		aot.RegisterComponent<Item>();
		aot.RegisterComponent<MouseTarget>();
		aot.RegisterComponent<IsConfused>();
		aot.RegisterComponent<TurnData>();

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
		aot.RegisterTag<ItemTag>();
		aot.RegisterTag<Walkable>();
		aot.RegisterTag<Projectile>();
		aot.RegisterTag<TurnStarted>();
		aot.RegisterTag<LevelLifetime>();
		aot.RegisterTag<AboveGround>();
		aot.RegisterTag<Stairs>();

		aot.RegisterComponent<MovementAction>();
		aot.RegisterComponent<MeleeAction>();
		aot.RegisterComponent<RestAction>();
		aot.RegisterComponent<ConsumeItemAction>();
		aot.RegisterComponent<PickupAction>();
		aot.RegisterComponent<NextLevelAction>();

		aot.RegisterTag<IsActionWaiting>();
		aot.RegisterTag<IsActionExecuting>();
		aot.RegisterTag<IsActionFinished>();
		aot.RegisterTag<IsActionBlocking>();

		aot.RegisterLinkRelation<InventoryItem>();
		
		aot.RegisterRelation<StatusEffect, Type>();

		var schema = aot.CreateSchema();
	}
}
