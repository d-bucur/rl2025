using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

public struct Cube() : IComponent {
	public Color Color = Color.Red;
};

struct Camera() : IComponent {
	public required Camera3D Value;
}

class Render : IModule {
	public void Init(EntityStore world) {
		InitCamera(world);
		RenderPhases.Render.Add(new RenderCubes());
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

internal class RenderCubes : QuerySystem<Position, Scale3, Cube> {
	protected override void OnUpdate() {
		Raylib.BeginMode3D(Singleton.Camera.GetComponent<Camera>().Value);

		Query.ForEachEntity((ref Position pos, ref Scale3 scale, ref Cube cube, Entity e) => {
			var posWithOffset = pos.value + new Vector3(Config.GRID_SIZE) / 2;
			Raylib.DrawCubeV(posWithOffset, scale.value, cube.Color);
			Raylib.DrawCubeWiresV(posWithOffset, scale.value, Raylib.Fade(Color.Black, 0.2f));
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