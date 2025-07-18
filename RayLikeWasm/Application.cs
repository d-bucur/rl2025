using System.Runtime.InteropServices.JavaScript;
using RayLikeShared;

namespace RayLikeWasm
{
    public partial class Application
    {
        static Game game;

        /// <summary>
        /// Application entry point
        /// </summary>
        public static void Main() {
            game = new();
        }

        /// <summary>
        /// Updates frame
        /// </summary>
        [JSExport]
        public static void UpdateFrame()
        {
            game.Update();
            game.Draw();
        }
    }
}
