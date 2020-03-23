using Fusee.Engine.Common;
using Fusee.Engine.Imp.Graphics.WebAsm;
using Fusee.Math.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebAssembly;

namespace Fusee.Base.Imp.WebAsm
{
    public abstract class WebAsmBase
    {
        public FusCanvas Canvas { get; protected set; }

        protected WebGLContext gl;
        protected float4 clearColor;
        protected int canvasWidth;
        protected int canvasHeight;

        public virtual bool EnableFullScreen => true;

        public virtual async void Init(FusCanvas canvas, float4 clearColor)
        {
            Console.WriteLine("base.Init() called");
            this.clearColor = clearColor;
            Canvas = canvas;

            canvasWidth = (int)canvas.Width;
            canvasHeight = (int)canvas.Height; 
        }

        public virtual async Task Run()
        {
        }

        public virtual void Update(double elapsedMilliseconds)
        {
        }

        public async virtual void Draw()
        {
            Console.WriteLine("base.Draw() called");

            await gl.EnableAsync(EnableCap.DEPTH_TEST);

            await gl.ViewportAsync(0, 0, canvasWidth, canvasHeight);
            Resize(canvasWidth, canvasHeight);

            await gl.ClearColorAsync(clearColor.x, clearColor.y, clearColor.z, clearColor.w);
            await gl.ClearAsync(BufferBits.COLOR_BUFFER_BIT | BufferBits.DEPTH_BUFFER_BIT);
        }

        public async virtual void Resize(int width, int height)
        {
            await gl.ViewportAsync(0, 0, width, height);
            canvasWidth = width;
            canvasHeight = height;
        }      
    }
}
