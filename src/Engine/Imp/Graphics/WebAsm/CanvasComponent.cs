using Microsoft.AspNetCore.Components;
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
        [Parameter]
        public long Height { get; set; }

        [Parameter]
        public long Width { get; set; }

        protected readonly string Id = Guid.NewGuid().ToString();
        public ElementReference CanvasRef;

        internal ElementReference CanvasReference => CanvasRef;

        [Inject]
        internal IJSRuntime JSRuntime { get; set; }
    }
}
