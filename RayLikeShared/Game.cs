using Raylib_cs;

namespace RayLikeShared;

public class Game
{
	private static Texture2D logo;

	public static void Init()
	{
		// Console.WriteLine("Init");
		Raylib.SetTargetFPS(60);
		Raylib.InitWindow(800, 480, "RayLike");
		logo = Raylib.LoadTexture("Resources/raylib_logo.png");
	}
	public static void Update()
	{
		// Console.WriteLine("Update");
		if (Raylib.IsMouseButtonPressed(MouseButton.Left)) { Console.WriteLine($"Mouse down"); }
		if (Raylib.IsKeyDown(KeyboardKey.Space)) { Console.WriteLine($"KeyDown"); }
	}
	public static void Draw()
	{
		// Console.WriteLine("Draw");
		Raylib.BeginDrawing();
		Raylib.ClearBackground(Color.SkyBlue);

		Raylib.DrawFPS(4, 4);
		Raylib.DrawText(Test(), 12, 12, 20, Color.RayWhite);
		Raylib.DrawTexture(logo, 4, 64, Color.White);

		Raylib.EndDrawing();
	}
	public static string Test()
	{
		return "Test from class library";
	}
}
