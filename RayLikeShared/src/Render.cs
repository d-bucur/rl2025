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
	// Only works with texture grid
	public Vec2I TileSize;
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
		for (int i = 0; i < Model.MaterialCount; i++) {
			Model.Materials[i].Shader = Assets.meshShader;
		}
	}
}

public struct ColorComp() : IComponent {
	public Color Value = Color.White;
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

		// Raylib.DrawGrid(30, Config.GRID_SIZE);
		Raylib.EndMode3D();

	}
}

internal class RenderBillboards : QuerySystem<Position, Scale3, Billboard, TextureWithSource, ColorComp> {
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
	protected override void OnUpdate() {
		Raylib.BeginShaderMode(Assets.meshShader);
		Raylib.BeginMode3D(Singleton.Camera.GetComponent<Camera>().Value);

		Query.ForEachEntity((ref Position pos, ref Scale3 scale, ref Mesh mesh, ref ColorComp color, Entity e) => {
			var posWithOffset = pos.value - new Vector3(Config.GRID_SIZE, 0, Config.GRID_SIZE) / 2 + mesh.Offset;
			Raylib.DrawModelEx(mesh.Model, posWithOffset, Vector3.UnitY, 0, scale.value, color.Value);
		});

		Raylib.EndMode3D();
		Raylib.EndShaderMode();
		// DebugStuff();
	}

	private static void DebugStuff() {
		Raylib.DrawFPS(4, 4);
		// Raylib.DrawText("text test", 12, 12, 20, Color.RayWhite);
		Raylib.DrawTexture(Assets.rayLogoTexture, 4, 30, Color.White);
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

				var enttBelow = grid.Value[posBelow.X, posBelow.Y];
				if (enttBelow.IsNull)
					return;

				if (enttBelow.Tags.Has<IsSeeThrough>()) {
					ref var color = ref enttBelow.GetComponent<ColorComp>();
					color.Value.A = FadeAlpha;
				}
			}
		});
	}
}