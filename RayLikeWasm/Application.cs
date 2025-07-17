using System;
using System.Runtime.InteropServices.JavaScript;
using Raylib_cs;
using RayLikeShared;

namespace RayLikeWasm
{
    public partial class Application
    {
        private static Texture2D logo;

        /// <summary>
        /// Application entry point
        /// </summary>
        public static void Main()
        {
            Raylib.InitWindow(512, 512, "RayLikeWasm");
            Raylib.SetTargetFPS(60);

            logo = Raylib.LoadTexture("Resources/raylib_logo.png");
        }

        /// <summary>
        /// Updates frame
        /// </summary>
        [JSExport]
        public static void UpdateFrame()
        {
            Raylib.BeginDrawing();

            Raylib.ClearBackground(Color.White);

            Raylib.DrawFPS(4, 4);
            Raylib.DrawText(Shared.Test(), 4, 32, 20, Color.Maroon);
            if (Raylib.IsMouseButtonPressed(MouseButton.Left)) { Console.WriteLine($"Mouse down"); }
            if (Raylib.IsKeyDown(KeyboardKey.Space)) { Console.WriteLine($"KeyDown");}

            Raylib.DrawTexture(logo, 4, 64, Color.White);

            Raylib.EndDrawing();
        }
    }
}
