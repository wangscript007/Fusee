﻿using Fusee.Engine.Common;

namespace Fusee.Engine.Imp.Graphics.WebAsm
{
    /// <summary>
    /// RenderBuffer implementation for WebAsm.
    /// </summary>
    public class RenderBufferHandle : IBufferHandle
    {
        internal WebGLRenderbuffer RenderBuffer = null;
    }

    /// <summary>
    /// RenderBuffer implementation for WebAsm.
    /// </summary>
    public class FrameBufferHandle : IBufferHandle
    {
        internal WebGLFramebuffer FrameBuffer = null;
    }
}
