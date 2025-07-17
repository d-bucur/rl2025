using System.Runtime.InteropServices.JavaScript;
using RayLikeShared;

namespace RayLikeWasm
{
    public partial class Application
    {
        /// <summary>
        /// Application entry point
        /// </summary>
        public static void Main()
        {
            Game.Init();
        }

        /// <summary>
        /// Updates frame
        /// </summary>
        [JSExport]
        public static void UpdateFrame()
        {
            Game.Update();
            Game.Draw();
        }
    }
}
