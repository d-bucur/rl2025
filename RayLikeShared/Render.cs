using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

struct Camera() : IComponent {
	public required Camera3D Value;
}

class Render : IModule {
	public void Init(EntityStore world) {
		InitCamera(world);
		RenderPhases.Render.Add(new RenderCubes());
	}

	private static void InitCamera(EntityStore world) {
		Singleton.Entity.AddComponent(new Camera() {
			Value = new Camera3D(
				new Vector3(0.0f, 10.0f, 10.0f),
				new Vector3(0.0f, 0.0f, 0.0f),
				new Vector3(0.0f, 1.0f, 0.0f),
				45.0f,
				CameraProjection.Perspective
			)
		});
	}
}

internal class RenderCubes : QuerySystem<Position, Cube> {
	protected override void OnUpdate() {
		Raylib.BeginMode3D(Singleton.Entity.GetComponent<Camera>().Value);

		Query.ForEachEntity((ref Position position, ref Cube cube, Entity e) => {
			const float size = 1.0f;
			Raylib.DrawCube(Helpers.ToVec3(position), size, size, size, Color.Red);
			Raylib.DrawCubeWires(Helpers.ToVec3(position), size, size, size, Color.Maroon);
		});

		Raylib.DrawGrid(30, Movement.GRID_SIZE);
		Raylib.EndMode3D();

		DebugStuff();
	}

	private static void DebugStuff() {
		Raylib.DrawFPS(4, 4);
		Raylib.DrawText("text test", 12, 12, 20, Color.RayWhite);
		Raylib.DrawTexture(Assets.logo, 4, 64, Color.White);
	}
}