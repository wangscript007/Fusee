using OpenToolkit.Graphics.OpenGL;
using OpenToolkit.Windowing.Common;
using OpenToolkit.Windowing.Common.Input;
using OpenToolkit.Windowing.Desktop;
using OpenToolkit.Windowing.GraphicsLibraryFramework;
using OpenToolkit.Mathematics;
using System;
using System.Reflection;

namespace Fusee.Engine.Imp.Graphics.Desktop
{
    public static class RenderCanvasTest
    {

        public delegate void ExecFusAppDelegate();

        public static void ExecFusApp()
        {
            System.Drawing.Icon appIcon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            Console.WriteLine("Icon created?");
            Console.WriteLine(appIcon != null);

            var gameWindow = new GameWindow(GameWindowSettings.Default, new NativeWindowSettings()
            {
                Size = new Vector2i(800, 600),
                Title = "Fusee Engine",

            });

            Console.WriteLine("Game Window created?");
            Console.WriteLine(gameWindow != null);

            var canvasImp = new RenderCanvasImp();
            Console.WriteLine("Canvas created? ");
            Console.WriteLine(canvasImp != null);
        }

    }
}