using System.Numerics;
using Friflo.Engine.ECS;
using Raylib_cs;

namespace RayLikeShared;

class Render {
	private static Camera3D camera;

	internal static void Init(EntityStore world) {
		InitCamera();
	}

	internal static void Draw(EntityStore world) {
		Raylib.BeginMode3D(camera);
		
		world.Query<Position, Cube>().ForEachEntity((ref Position position, ref Cube cube, Entity entity) => {
			Raylib.DrawCube(Helpers.ToVec3(position), 2.0f, 2.0f, 2.0f, Color.Red);
			Raylib.DrawCubeWires(Helpers.ToVec3(position), 2.0f, 2.0f, 2.0f, Color.Maroon);
		});

		Raylib.DrawGrid(10, 1.0f);
		Raylib.EndMode3D();

		DebugStuff();
	}

	private static void InitCamera() {
		camera = new Camera3D(
			new Vector3(0.0f, 10.0f, 10.0f),
			new Vector3(0.0f, 0.0f, 0.0f),
			new Vector3(0.0f, 1.0f, 0.0f),
			45.0f,
			CameraProjection.Perspective
		);
	}

	private static void DebugStuff() {
		Raylib.DrawFPS(4, 4);
		Raylib.DrawText("text test", 12, 12, 20, Color.RayWhite);
		Raylib.DrawTexture(Assets.logo, 4, 64, Color.White);
	}
}