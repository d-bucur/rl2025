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
				new Vector3(0.0f, 15.0f, 10.0f),
				new Vector3(0.0f, 0.0f, 0.0f),
				new Vector3(0.0f, 1.0f, 0.0f),
				15.0f,
				CameraProjection.Orthographic
			)
		});
	}
}

internal class RenderCubes : QuerySystem<Position, Scale3> {
	public RenderCubes() => Filter.AllComponents(ComponentTypes.Get<Cube>());

	protected override void OnUpdate() {
		Raylib.BeginMode3D(Singleton.Entity.GetComponent<Camera>().Value);

		Query.ForEachEntity((ref Position pos, ref Scale3 scale, Entity e) => {
			var posWithOffset = pos.value + new Vector3(Config.GRID_SIZE) / 2;
			Raylib.DrawCubeV(posWithOffset, scale.value, Color.Red);
			Raylib.DrawCubeWiresV(posWithOffset, scale.value, Color.Maroon);
		});

		Raylib.DrawGrid(30, Config.GRID_SIZE);
		Raylib.EndMode3D();

		DebugStuff();
	}

	private static void DebugStuff() {
		Raylib.DrawFPS(4, 4);
		// Raylib.DrawText("text test", 12, 12, 20, Color.RayWhite);
		Raylib.DrawTexture(Assets.logo, 4, 30, Color.White);
	}
}