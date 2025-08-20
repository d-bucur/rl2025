using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

struct Cube() : IComponent { }
struct Billboard() : IComponent {
	public Vector3? Up;
	public Vector2 Origin = new(0.5f, 0);
	public Vector3 Offset = Vector3.Zero;
}
struct TextureWithSource : IComponent {
	public TextureWithSource(Texture2D texture, Rectangle? source = null) {
		Texture = texture;
		Source = source ?? new Rectangle(0, 0, Texture.Width, Texture.Height);
	}
	public Texture2D Texture;
	public Rectangle Source;
	// Only works with texture grid, not atlas
	public Vec2I TileSize = new(32, 32); // Could save with texture to avoid duplication
	Vec2I _tileIdx;
	public Vec2I TileIdx {
		get => _tileIdx;
		set {
			_tileIdx = value;
			Source = new Rectangle(value.X * TileSize.X, value.Y * TileSize.Y, TileSize.X, TileSize.Y);
		}
	}
};

struct Mesh : IComponent {
	public Model Model;
	public Vector3 Offset;

	public Mesh(Model model, Vector3 offset = default) {
		Model = model;
		Offset = offset;
		SetShader();
	}

	readonly unsafe void SetShader() {
		// temp disable shaders
		// for (int i = 0; i < Model.MaterialCount; i++) {
		// 	Model.Materials[i].Shader = Assets.meshShader;
		// }
	}
}

struct ColorComp : IComponent {
	public Color Value = Color.White;
	public Color InitialValue = Color.White;
	public Color? DebugColor = null;

	public ColorComp(Color color) {
		Value = color;
		InitialValue = color;
	}

	public ColorComp() {
		Value = Color.White;
		InitialValue = Value;
	}
}

// Used to fade scenery when rendering
struct IsSeeThrough : ITag;

struct Camera() : IComponent {
	public required Camera3D Value;

	public Vector3 GetUpVec() {
		// camera up vector is second row of view matrix
		var matrix = Raylib.GetCameraMatrix(Value);
		return new Vector3(matrix.M21, matrix.M22, matrix.M23);
	}
}

class Render : IModule {
	public void Init(EntityStore world) {
		InitCamera(world);
		// RenderPhases.Render.Add(new FadeScenery()); // Disabled, using shaders now
		RenderPhases.Render.Add(new RenderCubes());
		RenderPhases.Render.Add(new RenderBillboards());
		RenderPhases.Render.Add(new RenderMeshes());
		// disabled for now. can fix later and use for health as well
		// RenderPhases.Render.Add(new RenderInGameUI());
	}

	static void InitCamera(EntityStore world) {
		Singleton.Camera = world.CreateEntity(
			new Camera() {
				Value = new Camera3D(
					new Vector3(2, 0f, 2f),
					new Vector3(2.5f, 0.0f, 2.5f),
					new Vector3(0.0f, 1.0f, 0.0f),
					35.0f,
					CameraProjection.Perspective
				// Alternative:
				// 10.0f,
				// CameraProjection.Orthographic
				)
			});
	}
}

file class RenderCubes : QuerySystem<Position, Scale3, Cube, ColorComp> {
	protected override void OnUpdate() {
		Raylib.BeginMode3D(Singleton.Camera.GetComponent<Camera>().Value);

		Query.ForEachEntity((ref Position pos, ref Scale3 scale, ref Cube cube, ref ColorComp color, Entity e) => {
			var posWithOffset = pos.value + new Vector3(Config.GridSize) / 2;
			Raylib.DrawCubeV(posWithOffset, scale.value, color.Value);
			Raylib.DrawCubeWiresV(posWithOffset, scale.value, Raylib.Fade(Color.Black, 0.2f));
		});

		Raylib.EndMode3D();
	}
}

file class RenderBillboards : QuerySystem<Position, RotationSingle, Billboard, TextureWithSource, Scale3> {
	public RenderBillboards() => Filter.AnyTags(Tags.Get<IsVisible>());

	protected override void OnUpdate() {
		Raylib.BeginShaderMode(Assets.billboardShader);
		Camera camera = Singleton.Camera.GetComponent<Camera>();
		Raylib.BeginMode3D(camera.Value);

		Query.ForEachEntity((ref Position pos, ref RotationSingle rot, ref Billboard billboard, ref TextureWithSource tex, ref Scale3 scale, Entity e) => {
			Vector2 scale2 = new(scale.value.X, scale.value.Y);
			Raylib.DrawBillboardPro(
				camera.Value,
				tex.Texture,
				tex.Source,
				pos.value - new Vector3(billboard.Origin.X, 0f, billboard.Origin.Y) + billboard.Offset,
				billboard.Up ?? camera.GetUpVec(),
				scale2,
				billboard.Origin * scale2,
				rot.Value, Color.White
			);
		});

		Raylib.EndMode3D();
		Raylib.EndShaderMode();
	}
}

file class RenderMeshes : QuerySystem<Position, Scale3, Mesh, ColorComp> {
	public RenderMeshes() => Filter.AnyTags(Tags.Get<IsVisible, IsExplored>());

	protected override void OnUpdate() {
		// not really being used, but material on each mesh has its own shader
		Raylib.BeginShaderMode(Assets.meshShader);
		Raylib.BeginMode3D(Singleton.Camera.GetComponent<Camera>().Value);

		Query.ForEachEntity((ref Position pos, ref Scale3 scale, ref Mesh mesh, ref ColorComp color, Entity e) => {
			var posWithOffset = pos.value - new Vector3(Config.GridSize, 0, Config.GridSize) / 2 + mesh.Offset;
			var normalColor = e.Tags.Has<IsVisible>()
				? Raylib.ColorTint(color.Value, Color.White)
				: Raylib.ColorBrightness(color.Value, Palette.NotVisibleFade);
			var colorFinal = Singleton.Get<Settings>().DebugColorsEnabled
				? (color.DebugColor ?? normalColor)
				: normalColor;
			Raylib.DrawModelEx(mesh.Model, posWithOffset, Vector3.UnitY, 0, scale.value, colorFinal);
		});

		Raylib.EndMode3D();
		Raylib.EndShaderMode();
		DebugStuff();
	}

	static void DebugStuff() {
		// Raylib.DrawFPS(4, 4);
		// Raylib.DrawText("text test", 12, 12, 20, Color.RayWhite);
		// Raylib.DrawTexture(Assets.rayLogoTexture, 4, 30, Color.White);
	}
}

file class FadeScenery : QuerySystem<GridPosition> {
	public FadeScenery() => Filter.AllTags(Tags.Get<Character>());

	const byte FadeAlpha = 200; // range: 0-255
	ArchetypeQuery<ColorComp> SeeThroughQuery;
	Vec2I[] PositionsBelow = [new Vec2I(0, 1)];

	protected override void OnAddStore(EntityStore store) {
		SeeThroughQuery = store.Query<ColorComp>().AllTags(Tags.Get<IsSeeThrough>());
	}

	protected override void OnUpdate() {
		SeeThroughQuery.ForEachEntity((ref ColorComp color, Entity entt) => {
			color.Value.A = 255;
		});

		var grid = Singleton.Get<Grid>();
		Query.ForEachEntity((ref GridPosition pos, Entity entt) => {
			foreach (var delta in PositionsBelow) {
				var posBelow = pos.Value + delta;
				if (!grid.IsInside(posBelow))
					return;

				var tileBelow = grid.Tile[posBelow.X, posBelow.Y];
				if (tileBelow.IsNull)
					return;

				if (tileBelow.Tags.Has<IsSeeThrough>()) {
					ref var color = ref tileBelow.GetComponent<ColorComp>();
					color.Value.A = FadeAlpha;
				}
			}
		});
	}
}

file class RenderInGameUI : QuerySystem<Energy, Position> {
	// TODO buggy rendering of energy bars
	RenderTexture2D ForegroundTex;
	RenderTexture2D BackgroundTex;

	protected override void OnAddStore(EntityStore store) {
		ForegroundTex = Raylib.LoadRenderTexture(5, 32);
		Raylib.BeginTextureMode(ForegroundTex);
		Raylib.DrawRectangle(0, 0, 5, 32, Color.White);
		Raylib.EndTextureMode();

		BackgroundTex = Raylib.LoadRenderTexture(5, 32);
		Raylib.BeginTextureMode(BackgroundTex);
		Raylib.DrawRectangleLinesEx(new Rectangle(0, 0, 5, 32), 1f, Color.White);
		Raylib.EndTextureMode();
	}

	protected override void OnUpdate() {
		Camera3D camera = Singleton.Camera.GetComponent<Camera>().Value;

		Raylib.BeginMode3D(camera);
		Raylib.BeginShaderMode(Assets.billboardShader);
		Query.ForEachEntity((ref Energy energy, ref Position pos, Entity entt) => {
			var percEnergy = (float)energy.Current / energy.AmountToAct;
			Raylib.DrawBillboardRec(
				camera,
				BackgroundTex.Texture,
				new Rectangle(0, 0, ForegroundTex.Texture.Width, ForegroundTex.Texture.Height),
				pos.value + new Vector3(0, 0.6f, 0),
				new Vector2(0.1f, 1),
				Raylib.Fade(Color.Black, 0.8f)
			);
			Raylib.DrawBillboardRec(
				camera,
				ForegroundTex.Texture,
				new Rectangle(0, 0, ForegroundTex.Texture.Width, ForegroundTex.Texture.Height),
				pos.value + new Vector3(0, 0.6f, 0),
				new Vector2(0.1f, percEnergy),
				Raylib.Fade(Color.DarkBlue, 0.7f)
			);
		});
		Raylib.EndShaderMode();
		Raylib.EndMode3D();
	}
}