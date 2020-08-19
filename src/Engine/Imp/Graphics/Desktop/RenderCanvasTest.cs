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

            Console.WriteLine("Icon created? " + appIcon != null);

            var canvasImp = new RenderCanvasImp(800, 600);
            Console.WriteLine("Canvas created? " + canvasImp != null);
        }

    }
}
