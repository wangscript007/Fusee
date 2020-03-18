using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Infrastructure;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusee.Engine.Imp.Graphics.WebAsm
{
    /// <summary>
    /// Base class for all canvas operations
    /// </summary>
    public class FusCanvas : ComponentBase
    {
        /// <summary>
        /// Current height of canvas
        /// </summary>
        [Parameter]
        public long Height { get; set; }

        /// <summary>
        /// Current width of canvas
        /// </summary>
        [Parameter]
        public long Width { get; set; }

        /// <summary>
        /// ID of current canvas element
        /// </summary>
        protected readonly string Id = Guid.NewGuid().ToString();

        /// <summary>
        /// Reference on the used canvas (inside index.html)
        /// </summary>
        public ElementReference CanvasReference;

        /// <summary>
        /// This is the context which is generated via _canvas.GetContext(3D)
        /// </summary>
        protected WebGLContext _context;

        [Inject]
        internal IJSRuntime JSRuntime { get; set; }

        /// <summary>
        /// This method sets the dimensions of the currently used canvas
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetDims(int width, int height)
        {
            Width = width;
            Height = height;         
        }

        /// <summary>
        /// This method is invoked via javascript inside the index.html
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        [JSInvokable]
        public static async Task OnKeyDown(KeyboardEventArgs e)
        {
            Console.WriteLine("OnKeyDown");
            Console.WriteLine(e.Code);
        }

        /// <summary>
        /// This method is invoked via javascript inside the index.html
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        [JSInvokable]
        public static async Task OnMouseDown(MouseEventArgs e)
        {
            Console.WriteLine("OnMouseDown");
            Console.WriteLine(e.Button);
        }

    }
}
