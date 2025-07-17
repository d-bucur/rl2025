using Raylib_cs;
using RayLikeShared;

namespace HelloWorld;

class DesktopEntry
{
    // STAThread is required if you deploy using NativeAOT on Windows - See https://github.com/raylib-cs/raylib-cs/issues/301
    [STAThread]
    public static void Main()
    {
        Game.Init();

        while (!Raylib.WindowShouldClose())
        {
            Game.Update();
            Game.Draw();
        }

        Raylib.CloseWindow();
    }
}