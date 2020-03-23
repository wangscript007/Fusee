using Fusee.Engine.Imp.Graphics.WebAsm;
using Fusee.Math.Core;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using WebAssembly;

namespace Fusee.Base.Imp.WebAsm
{
    public class WebAsmProgram
    {
        protected static readonly float4 CanvasColor = new float4(255, 0, 255, 255);
        protected static double previousMilliseconds;
        protected static WebAsmBase mainExecutable;

        public static async Task Start(WebAsmBase webAsm)
        {
            Console.WriteLine("Start has been called");
            mainExecutable = webAsm;

            mainExecutable.Init(webAsm.Canvas, CanvasColor);

            Console.WriteLine("Before run");
            await mainExecutable.Run();
            Console.WriteLine("After run");

            AddEnterFullScreenHandler();
            AddResizeHandler();

            await RequestAnimationFrame();
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

        static int cnt = 0;

        [JSInvokable("Loop")]
        public static async void Loop(double milliseconds)
        {

            Console.WriteLine($"Loop called {milliseconds} ms");
            var elapsedMilliseconds = milliseconds - previousMilliseconds;
            previousMilliseconds = milliseconds;

            Console.WriteLine($"Loop().Update()");
            mainExecutable.Update(elapsedMilliseconds);
            Console.WriteLine($"Loop().Draw()");
            mainExecutable.Draw();

            //if(++cnt < 10)
            //    await RequestAnimationFrame(); //we just want to render 1 frame
        }

        private async static Task RequestAnimationFrame()
        {
            Console.WriteLine("Request Animation frame called");
            await mainExecutable.Canvas.JSRuntime.InvokeVoidAsync("requestLoop");
        }
    }

}
