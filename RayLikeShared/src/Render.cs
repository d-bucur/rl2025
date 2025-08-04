using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

public struct Cube() : IComponent {
};

public struct Billboard : IComponent { }

public struct TextureWithSource : IComponent {
	public TextureWithSource(Texture2D texture, Rectangle? source = null) {
		Texture = texture;
		Source = source ?? new Rectangle(0, 0, Texture.Width, Texture.Height);
	}
	public Texture2D Texture;
	public Rectangle Source;
	// Only works with texture grid, not atlas
	public Vec2I TileSize; // Could save with texture to avoid duplication
	Vec2I _tileIdx;
	public Vec2I TileIdx {
		get => _tileIdx;
		set {
			_tileIdx = value;
			Source = new Rectangle(value.X * TileSize.X, value.Y * TileSize.Y, TileSize.X, TileSize.Y);
		}
	}
};

public struct Mesh : IComponent {
	public Model Model;
	public Vector3 Offset;

	public Mesh(Model model, Vector3 offset = default) {
		Model = model;
		Offset = offset;
		SetShader();
	}

	private readonly unsafe void SetShader() {
		// temp disable shaders
		// for (int i = 0; i < Model.MaterialCount; i++) {
		// 	Model.Materials[i].Shader = Assets.meshShader;
		// }
	}
}

public struct ColorComp : IComponent {
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

public struct IsSeeThrough : ITag;

struct Camera() : IComponent {
	public required Camera3D Value;
}

class Render : IModule {
	public void Init(EntityStore world) {
		InitCamera(world);
		RenderPhases.Render.Add(new FadeScenery());
		RenderPhases.Render.Add(new RenderCubes());
		RenderPhases.Render.Add(new RenderBillboards());
		RenderPhases.Render.Add(new RenderMeshes());
		RenderPhases.Render.Add(new RenderMinimap());
		// disabled for now. can fix later and use for health as well
		// RenderPhases.Render.Add(new RenderInGameUI());
	}

	private static void InitCamera(EntityStore world) {
		Singleton.Camera = world.CreateEntity(
			new Camera() {
				Value = new Camera3D(
					new Vector3(2, 0f, 2f),
					new Vector3(2.5f, 0.0f, 2.5f),
					new Vector3(0.0f, 1.0f, 0.0f),
					35.0f,
					CameraProjection.Perspective
				)
			});
	}
}

internal class RenderCubes : QuerySystem<Position, Scale3, Cube, ColorComp> {
	protected override void OnUpdate() {
		Raylib.BeginMode3D(Singleton.Camera.GetComponent<Camera>().Value);

		Query.ForEachEntity((ref Position pos, ref Scale3 scale, ref Cube cube, ref ColorComp color, Entity e) => {
			var posWithOffset = pos.value + new Vector3(Config.GRID_SIZE) / 2;
			Raylib.DrawCubeV(posWithOffset, scale.value, color.Value);
			Raylib.DrawCubeWiresV(posWithOffset, scale.value, Raylib.Fade(Color.Black, 0.2f));
		});

		Raylib.EndMode3D();

	}
}

internal class RenderBillboards : QuerySystem<Position, Scale3, Billboard, TextureWithSource, ColorComp> {
	public RenderBillboards() => Filter.AnyTags(Tags.Get<IsVisible>());

	protected override void OnUpdate() {
		Raylib.BeginShaderMode(Assets.billboardShader);
		Camera3D camera = Singleton.Camera.GetComponent<Camera>().Value;
		Raylib.BeginMode3D(camera);
		var matrix = Raylib.GetCameraMatrix(camera);
		// camera up vector is second row of view matrix
		var cameraUp = new Vector3(matrix.M21, matrix.M22, matrix.M23);

		Query.ForEachEntity((ref Position pos, ref Scale3 scale, ref Billboard billboard, ref TextureWithSource tex, ref ColorComp color, Entity e) => {
			Raylib.DrawBillboardPro(
				camera,
				tex.Texture,
				tex.Source,
				pos.value,
				cameraUp, Vector2.One, Vector2.UnitX, 0, color.Value
			);
		});

		Raylib.EndMode3D();
		Raylib.EndShaderMode();
	}
}

internal class RenderMeshes : QuerySystem<Position, Scale3, Mesh, ColorComp> {
	public RenderMeshes() => Filter.AnyTags(Tags.Get<IsVisible, IsExplored>());

	protected override void OnUpdate() {
		// not really being used, but material on each mesh has its own shader
		Raylib.BeginShaderMode(Assets.meshShader);
		Raylib.BeginMode3D(Singleton.Camera.GetComponent<Camera>().Value);

		Query.ForEachEntity((ref Position pos, ref Scale3 scale, ref Mesh mesh, ref ColorComp color, Entity e) => {
			var posWithOffset = pos.value - new Vector3(Config.GRID_SIZE, 0, Config.GRID_SIZE) / 2 + mesh.Offset;
			var normalColor = e.Tags.Has<IsVisible>()
				? Raylib.ColorTint(color.Value, Color.White)
				: Raylib.ColorBrightness(color.Value, Palette.NotVisibleFade);
			var colorFinal = Singleton.Entity.GetComponent<Settings>().DebugColorsEnabled
				? (color.DebugColor ?? normalColor)
				: normalColor;
			Raylib.DrawModelEx(mesh.Model, posWithOffset, Vector3.UnitY, 0, scale.value, colorFinal);
		});

		Raylib.EndMode3D();
		Raylib.EndShaderMode();
		DebugStuff();
	}

	private static void DebugStuff() {
		// Raylib.DrawFPS(4, 4);
		// Raylib.DrawText("text test", 12, 12, 20, Color.RayWhite);
		// Raylib.DrawTexture(Assets.rayLogoTexture, 4, 30, Color.White);
	}
}

internal class FadeScenery : QuerySystem<GridPosition> {
	public FadeScenery() => Filter.AllTags(Tags.Get<Character>());

	const byte FadeAlpha = 200; // range: 0-255
	private ArchetypeQuery<ColorComp> SeeThroughQuery;
	private Vec2I[] PositionsBelow = [new Vec2I(0, 1)];

	protected override void OnAddStore(EntityStore store) {
		SeeThroughQuery = store.Query<ColorComp>().AllTags(Tags.Get<IsSeeThrough>());
	}

	protected override void OnUpdate() {
		SeeThroughQuery.ForEachEntity((ref ColorComp color, Entity entt) => {
			color.Value.A = 255;
		});

		var grid = Singleton.Entity.GetComponent<Grid>();
		Query.ForEachEntity((ref GridPosition pos, Entity entt) => {
			foreach (var delta in PositionsBelow) {
				var posBelow = pos.Value + delta;
				if (!grid.IsInsideGrid(posBelow))
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

internal class RenderInGameUI : QuerySystem<Energy, Position> {
	// buggy rendering of energy bars
	private RenderTexture2D ForegroundTex;
	private RenderTexture2D BackgroundTex;

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

// Optimization: could only redraw on change
internal class RenderMinimap : QuerySystem {
	private Image MinimapImage;
	private Texture2D MinimapTexture;

	protected override void OnAddStore(EntityStore store) {
		MinimapImage = Raylib.GenImageColor(Config.MAP_SIZE_X, Config.MAP_SIZE_Y, Palette.Transparent);
		MinimapTexture = Raylib.LoadTextureFromImage(MinimapImage);
	}

	protected override unsafe void OnUpdate() {
		if (!Singleton.Entity.GetComponent<Settings>().MinimapEnabled)
			return;

		var grid = Singleton.Entity.GetComponent<Grid>();
		for (int x = 0; x < grid.Tile.GetLength(0); x++) {
			for (int y = 0; y < grid.Tile.GetLength(1); y++) {
				Raylib.ImageDrawPixel(ref MinimapImage, x, y, GetColor(grid, x, y));
			}
		}
		Raylib.UpdateTexture(MinimapTexture, Raylib.LoadImageColors(MinimapImage));
		const int SCALE = 5;
		Raylib.DrawTextureEx(MinimapTexture,
			new Vector2(Raylib.GetScreenWidth() - MinimapImage.Width * SCALE, 0),
			0,
			SCALE,
			Raylib.Fade(Color.White, 0.5f)
		);
	}

	private static unsafe Color GetColor(Grid grid, int x, int y) {
		Entity tile = grid.Tile[x, y];
		if (!tile.Tags.Has<IsExplored>())
			return Palette.Transparent;
		var color = grid.Tile[x, y].GetComponent<ColorComp>().Value;
		Entity character = grid.Character[x, y];
		if (!character.IsNull) {
			if (character.Tags.Has<Player>())
				color = Color.Green;
			if (character.Tags.Has<Enemy>() && tile.Tags.Has<IsVisible>())
				color = Color.Red;
		}
		return color;
	}
}