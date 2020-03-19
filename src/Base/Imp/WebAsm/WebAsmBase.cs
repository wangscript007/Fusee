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
        public FusCanvas Canvas { get; private set; }

        protected WebGLContext gl;
        protected float4 clearColor;
        protected int canvasWidth;
        protected int canvasHeight;

        public virtual bool EnableFullScreen => true;

        public virtual void Init(FusCanvas canvas, float4 clearColor)
        {
            this.clearColor = clearColor;
            Canvas = canvas;

            canvasWidth = (int)canvas.Width;
            canvasHeight = (int)canvas.Height;           

            gl = canvas.CreateWebGL(new WebGLContextAttributes { Alpha = true, Antialias = true });
        }

        public virtual void Run()
        {
        }

        public virtual void Update(double elapsedMilliseconds)
        {
        }

        public virtual void Draw()
        {
            gl.Enable(EnableCap.DEPTH_TEST);

            gl.Viewport(0, 0, canvasWidth, canvasHeight);
            Resize(canvasWidth, canvasHeight);

            gl.ClearColor(clearColor.x, clearColor.y, clearColor.z, clearColor.w);
            gl.Clear(BufferBits.COLOR_BUFFER_BIT | BufferBits.DEPTH_BUFFER_BIT);
        }

        public virtual void Resize(int width, int height)
        {
            gl.Viewport(0, 0, width, height);
            canvasWidth = width;
            canvasHeight = height;
        }      
    }
}
