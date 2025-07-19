using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

public struct Cube : IComponent { };

struct Camera() : IComponent {
	public required Camera3D Value;
}

public struct Mesh : IComponent {
	public Mesh(Model model) {
		Model = model;
		// SetShader();
	}
	public Model Model;
	public Color Color = Color.White;

	// private readonly unsafe void SetShader() {
	// 	for (int i = 0; i < Model.MaterialCount; i++) {
	// 		Model.Materials[i].Shader = Assets.meshShader;
	// 	}
	// }

};

class Render : IModule {
	public void Init(EntityStore world) {
		InitCamera(world);
		RenderPhases.Render.Add(new RenderCubes());
		RenderPhases.Render.Add(new RenderMeshes());
	}

	private static void InitCamera(EntityStore world) {
		Singleton.Entity.AddComponent(new Camera() {
			Value = new Camera3D(
				new Vector3(0.0f, 10.0f, 10.0f),
				new Vector3(0.0f, 0.0f, 0.0f),
				new Vector3(0.0f, 1.0f, 0.0f),
				30.0f,
				CameraProjection.Perspective
			)
		});
	}
}

internal class RenderCubes : QuerySystem<Position, Scale3> {
	public RenderCubes() => Filter.AllComponents(ComponentTypes.Get<Cube>());

	protected override void OnUpdate() {
		Raylib.BeginMode3D(Singleton.Entity.GetComponent<Camera>().Value);

		Query.ForEachEntity((ref Position pos, ref Scale3 scale, Entity e) => {
			var posWithOffset = pos.value + new Vector3(Config.GRID_SIZE, 0, Config.GRID_SIZE) / 2;
			Raylib.DrawCubeV(posWithOffset, scale.value, Color.Red);
			Raylib.DrawCubeWiresV(posWithOffset, scale.value, Color.Maroon);
		});

		// Raylib.DrawGrid(30, Config.GRID_SIZE);
		Raylib.EndMode3D();

		DebugStuff();
	}

	private static void DebugStuff() {
		Raylib.DrawFPS(4, 4);
		// Raylib.DrawText("text test", 12, 12, 20, Color.RayWhite);
		Raylib.DrawTexture(Assets.logo, 4, 30, Color.White);
	}
}

internal class RenderMeshes : QuerySystem<Position, Scale3, Mesh> {
	protected override void OnUpdate() {
		Raylib.BeginMode3D(Singleton.Entity.GetComponent<Camera>().Value);

		Query.ForEachEntity((ref Position pos, ref Scale3 scale, ref Mesh mesh, Entity e) => {
			var posWithOffset = pos.value + new Vector3(Config.GRID_SIZE, 0, Config.GRID_SIZE) / 2;
			Raylib.DrawModelEx(mesh.Model, posWithOffset, Vector3.UnitY, 0, scale.value, mesh.Color);
		});

		Raylib.EndMode3D();
	}
}