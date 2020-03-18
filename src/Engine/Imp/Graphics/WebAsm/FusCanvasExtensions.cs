using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusee.Engine.Imp.Graphics.WebAsm
{
    public static class FusCanvasExtensions
    {
        public static WebGLContext CreateWebGL(this FusCanvas canvas)
        {
            return new WebGLContext(canvas).InitializeAsync().GetAwaiter().GetResult() as WebGLContext;
        }

        public static async Task<WebGLContext> CreateWebGLAsync(this FusCanvas canvas)
        {
            return await new WebGLContext(canvas).InitializeAsync().ConfigureAwait(false) as WebGLContext;
        }

        public static WebGLContext CreateWebGL(this FusCanvas canvas, WebGLContextAttributes attributes)
        {
            return new WebGLContext(canvas, attributes).InitializeAsync().GetAwaiter().GetResult() as WebGLContext;
        }

        public static async Task<WebGLContext> CreateWebGLAsync(this FusCanvas canvas, WebGLContextAttributes attributes)
        {
            return await new WebGLContext(canvas, attributes).InitializeAsync().ConfigureAwait(false) as WebGLContext;
        }
    }
}

