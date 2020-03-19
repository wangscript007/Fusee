using Fusee.Engine.Imp.Graphics.WebAsm;
using Fusee.Math.Core;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using WebAssembly;

namespace Fusee.Base.Imp.WebAsm
{
    public class WebAsmProgram
    {
        protected static readonly float4 CanvasColor = new float4(255, 0, 255, 255);
        protected static double previousMilliseconds;
        protected static WebAsmBase mainExecutable;

        public static void Start(WebAsmBase webAsm)
        {
            mainExecutable = webAsm;

            mainExecutable.Init(webAsm.Canvas, CanvasColor);
            mainExecutable.Run();
               

            AddEnterFullScreenHandler();
            AddResizeHandler();

            RequestAnimationFrame();
        }

        private static void AddResizeHandler()
        {
           // call fusee resize
           //mainExecutable.Resize(Canvas, windowHeight);               
        }

        private static void RequestFullscreen(FusCanvas canvas)
        {
            //if (canvas.GetObjectProperty("requestFullscreen") != null)
            //    canvas.Invoke("requestFullscreen");
            //if (canvas.GetObjectProperty("mozRequestFullScreen") != null)
            //    canvas.Invoke("mozRequestFullScreen");
            //if (canvas.GetObjectProperty("webkitRequestFullscreen") != null)
            //    canvas.Invoke("webkitRequestFullscreen");
            //if (canvas.GetObjectProperty("msRequestFullscreen") != null)
            //    canvas.Invoke("msRequestFullscreen");
        }

        private static void AddEnterFullScreenHandler()
        {
            //using (var canvas = (JSObject)Runtime.GetGlobalObject(canvasName))
            //{
            //    canvas.Invoke("addEventListener", "dblclick", new Action<JSObject>((o) =>
            //    {
            //        using (var d = (JSObject)Runtime.GetGlobalObject("document"))
            //        {
            //            var canvasObject = (JSObject)d.Invoke("getElementById", canvasName);

            //            RequestFullscreen(canvasObject);

            //            var width = (int)canvasObject.GetObjectProperty("clientWidth");
            //            var height = (int)canvasObject.GetObjectProperty("clientHeight");

            //            SetNewCanvasSize(canvasObject, width, height);

            //            // call fusee resize
            //            mainExecutable.Resize(width, height);
            //        }

            //        o.Dispose();
            //    }), false);
            //}
        }

        private static void SetNewCanvasSize(FusCanvas canvasObject, int newWidth, int newHeight)
        {
            canvasObject.SetDims(newWidth, newHeight);
        }

        [JSInvokableAttribute("Loop")]
        public static void Loop(double milliseconds)
        {
            Console.WriteLine($"Loop called {milliseconds} ms");
            var elapsedMilliseconds = milliseconds - previousMilliseconds;
            previousMilliseconds = milliseconds;

            mainExecutable.Update(elapsedMilliseconds);
            mainExecutable.Draw();

            RequestAnimationFrame();
        }

        private static void RequestAnimationFrame()
        {
            Console.WriteLine("Request Animation frame called");
            mainExecutable.Canvas.JSRuntime.InvokeVoidAsync("requestLoop");        
        }
    }

}
