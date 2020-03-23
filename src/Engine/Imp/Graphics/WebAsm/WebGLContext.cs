///
/// Copyright (c) 2018 Blazor Extensions Contributors
/// https://github.com/BlazorExtensions/Canvas
///


using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Fusee.Engine.Imp.Graphics.WebAsm
{
    public class WebGLContext : BlazorRenderContext
    {
        #region Constants
        private const string CONTEXT_NAME = "WebGL2";
        private const string CLEAR_COLOR = "clearColor";
        private const string CLEAR = "clear";
        private const string DRAWING_BUFFER_WIDTH = "drawingBufferWidth";
        private const string DRAWING_BUFFER_HEIGHT = "drawingBufferHeight";
        private const string GET_CONTEXT_ATTRIBUTES = "getContextAttributes";
        private const string IS_CONTEXT_LOST = "isContextLost";
        private const string SCISSOR = "scissor";
        private const string VIEWPORT = "viewport";
        private const string ACTIVE_TEXTURE = "activeTexture";
        private const string BLEND_COLOR = "blendColor";
        private const string BLEND_EQUATION = "blendEquation";
        private const string BLEND_EQUATION_SEPARATE = "blendEquationSeparate";
        private const string BLEND_FUNC = "blendFunc";
        private const string BLEND_FUNC_SEPARATE = "blendFuncSeparate";
        private const string CLEAR_DEPTH = "clearDepth";
        private const string CLEAR_STENCIL = "clearStencil";
        private const string COLOR_MASK = "colorMask";
        private const string CULL_FACE = "cullFace";
        private const string DEPTH_FUNC = "depthFunc";
        private const string DEPTH_MASK = "depthMask";
        private const string DEPTH_RANGE = "depthRange";
        private const string DISABLE = "disable";
        private const string ENABLE = "enable";
        private const string FRONT_FACE = "frontFace";
        private const string GET_PARAMETER = "getParameter";
        private const string GET_ERROR = "getError";
        private const string HINT = "hint";
        private const string IS_ENABLED = "isEnabled";
        private const string LINE_WIDTH = "lineWidth";
        private const string PIXEL_STORE_I = "pixelStorei";
        private const string POLYGON_OFFSET = "polygonOffset";
        private const string SAMPLE_COVERAGE = "sampleCoverage";
        private const string STENCIL_FUNC = "stencilFunc";
        private const string STENCIL_FUNC_SEPARATE = "stencilFuncSeparate";
        private const string STENCIL_MASK = "stencilMask";
        private const string STENCIL_MASK_SEPARATE = "stencilMaskSeparate";
        private const string STENCIL_OP = "stencilOp";
        private const string STENCIL_OP_SEPARATE = "stencilOpSeparate";
        private const string BIND_BUFFER = "bindBuffer";
        private const string BUFFER_DATA = "bufferData";
        private const string BUFFER_SUB_DATA = "bufferSubData";
        private const string CREATE_BUFFER = "createBuffer";
        private const string DELETE_BUFFER = "deleteBuffer";
        private const string GET_BUFFER_PARAMETER = "getBufferParameter";
        private const string IS_BUFFER = "isBuffer";
        private const string BIND_FRAMEBUFFER = "bindFramebuffer";
        private const string CHECK_FRAMEBUFFER_STATUS = "checkFramebufferStatus";
        private const string CREATE_FRAMEBUFFER = "createFramebuffer";
        private const string DELETE_FRAMEBUFFER = "deleteFramebuffer";
        private const string FRAMEBUFFER_RENDERBUFFER = "framebufferRenderbuffer";
        private const string FRAMEBUFFER_TEXTURE_2D = "framebufferTexture2D";
        private const string GET_FRAMEBUFFER_ATTACHMENT_PARAMETER = "getFramebufferAttachmentParameter";
        private const string IS_FRAMEBUFFER = "isFramebuffer";
        private const string READ_PIXELS = "readPixels";
        private const string BIND_RENDERBUFFER = "bindRenderbuffer";
        private const string CREATE_RENDERBUFFER = "createRenderbuffer";
        private const string DELETE_RENDERBUFFER = "deleteRenderbuffer";
        private const string GET_RENDERBUFFER_PARAMETER = "getRenderbufferParameter";
        private const string IS_RENDERBUFFER = "isRenderbuffer";
        private const string RENDERBUFFER_STORAGE = "renderbufferStorage";
        private const string BIND_TEXTURE = "bindTexture";
        private const string COPY_TEX_IMAGE_2D = "copyTexImage2D";
        private const string COPY_TEX_SUB_IMAGE_2D = "copyTexSubImage2D";
        private const string CREATE_TEXTURE = "createTexture";
        private const string DELETE_TEXTURE = "deleteTexture";
        private const string GENERATE_MIPMAP = "generateMipmap";
        private const string GET_TEX_PARAMETER = "getTexParameter";
        private const string IS_TEXTURE = "isTexture";
        private const string TEX_IMAGE_2D = "texImage2D";
        private const string TEX_SUB_IMAGE_2D = "texSubImage2D";
        private const string TEX_PARAMETER_F = "texParameterf";
        private const string TEX_PARAMETER_I = "texParameteri";
        private const string ATTACH_SHADER = "attachShader";
        private const string BIND_ATTRIB_LOCATION = "bindAttribLocation";
        private const string COMPILE_SHADER = "compileShader";
        private const string CREATE_PROGRAM = "createProgram";
        private const string CREATE_SHADER = "createShader";
        private const string DELETE_PROGRAM = "deleteProgram";
        private const string DELETE_SHADER = "deleteShader";
        private const string DETACH_SHADER = "detachShader";
        private const string GET_ATTACHED_SHADERS = "getAttachedShaders";
        private const string GET_PROGRAM_PARAMETER = "getProgramParameter";
        private const string GET_PROGRAM_INFO_LOG = "getProgramInfoLog";
        private const string GET_SHADER_PARAMETER = "getShaderParameter";
        private const string GET_SHADER_PRECISION_FORMAT = "getShaderPrecisionFormat";
        private const string GET_SHADER_INFO_LOG = "getShaderInfoLog";
        private const string GET_SHADER_SOURCE = "getShaderSource";
        private const string IS_PROGRAM = "isProgram";
        private const string IS_SHADER = "isShader";
        private const string LINK_PROGRAM = "linkProgram";
        private const string SHADER_SOURCE = "shaderSource";
        private const string USE_PROGRAM = "useProgram";
        private const string VALIDATE_PROGRAM = "validateProgram";
        private const string DISABLE_VERTEX_ATTRIB_ARRAY = "disableVertexAttribArray";
        private const string ENABLE_VERTEX_ATTRIB_ARRAY = "enableVertexAttribArray";
        private const string GET_ACTIVE_ATTRIB = "getActiveAttrib";
        private const string GET_ACTIVE_UNIFORM = "getActiveUniform";
        private const string GET_ATTRIB_LOCATION = "getAttribLocation";
        private const string GET_UNIFORM = "getUniform";
        private const string GET_UNIFORM_LOCATION = "getUniformLocation";
        private const string GET_VERTEX_ATTRIB = "getVertexAttrib";
        private const string GET_VERTEX_ATTRIB_OFFSET = "getVertexAttribOffset";
        private const string UNIFORM = "uniform";
        private const string UNIFORM_MATRIX = "uniformMatrix";
        private const string VERTEX_ATTRIB = "vertexAttrib";
        private const string VERTEX_ATTRIB_POINTER = "vertexAttribPointer";
        private const string DRAW_ARRAYS = "drawArrays";
        private const string DRAW_ELEMENTS = "drawElements";
        private const string FINISH = "finish";
        private const string FLUSH = "flush";
        #endregion

        #region Properties
        public int DrawingBufferWidth { get; private set; }
        public int DrawingBufferHeight { get; private set; }
        #endregion

        internal WebGLContext(FusCanvas reference, WebGLContextAttributes attributes = null) : base(reference, CONTEXT_NAME, attributes)
        {
        }

        protected override async Task ExtendedInitializeAsync()
        {
            DrawingBufferWidth = await GetDrawingBufferWidthAsync();
            DrawingBufferHeight = await GetDrawingBufferHeightAsync();
        }

        #region Methods
        [Obsolete("Use the async version instead, which is already called internally.")]
        public void ClearColor(float red, float green, float blue, float alpha) => CallMethod<object>(CLEAR_COLOR, red, green, blue, alpha);
        public async Task ClearColorAsync(float red, float green, float blue, float alpha) => await BatchCallAsync(CLEAR_COLOR, isMethodCall: true, red, green, blue, alpha);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void Clear(BufferBits mask) => CallMethod<object>(CLEAR, mask);
        public async Task ClearAsync(BufferBits mask) => await BatchCallAsync(CLEAR, isMethodCall: true, mask);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public WebGLContextAttributes GetContextAttributes() => CallMethod<WebGLContextAttributes>(GET_CONTEXT_ATTRIBUTES);
        public async Task<WebGLContextAttributes> GetContextAttributesAsync() => await CallMethodAsync<WebGLContextAttributes>(GET_CONTEXT_ATTRIBUTES);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public bool IsContextLost() => CallMethod<bool>(IS_CONTEXT_LOST);
        public async Task<bool> IsContextLostAsync() => await CallMethodAsync<bool>(IS_CONTEXT_LOST);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void Scissor(int x, int y, int width, int height) => CallMethod<object>(SCISSOR, x, y, width, height);
        public async Task ScissorAsync(int x, int y, int width, int height) => await BatchCallAsync(SCISSOR, isMethodCall: true, x, y, width, height);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void Viewport(int x, int y, int width, int height) => CallMethod<object>(VIEWPORT, x, y, width, height);
        public async Task ViewportAsync(int x, int y, int width, int height) => await BatchCallAsync(VIEWPORT, isMethodCall: true, x, y, width, height);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void ActiveTexture(Texture texture) => CallMethod<object>(ACTIVE_TEXTURE, texture);
        public async Task ActiveTextureAsync(Texture texture) => await BatchCallAsync(ACTIVE_TEXTURE, isMethodCall: true, texture);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void BlendColor(float red, float green, float blue, float alpha) => CallMethod<object>(BLEND_COLOR, red, green, blue, alpha);
        public async Task BlendColorAsync(float red, float green, float blue, float alpha) => await BatchCallAsync(BLEND_COLOR, isMethodCall: true, red, green, blue, alpha);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void BlendEquation(BlendingEquation equation) => CallMethod<object>(BLEND_EQUATION, equation);
        public async Task BlendEquationAsync(BlendingEquation equation) => await BatchCallAsync(BLEND_EQUATION, isMethodCall: true, equation);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void BlendEquationSeparate(BlendingEquation modeRGB, BlendingEquation modeAlpha) => CallMethod<object>(BLEND_EQUATION_SEPARATE, modeRGB, modeAlpha);
        public async Task BlendEquationSeparateAsync(BlendingEquation modeRGB, BlendingEquation modeAlpha) => await BatchCallAsync(BLEND_EQUATION_SEPARATE, isMethodCall: true, modeRGB, modeAlpha);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void BlendFunc(BlendingMode sfactor, BlendingMode dfactor) => CallMethod<object>(BLEND_FUNC, sfactor, dfactor);
        public async Task BlendFuncAsync(BlendingMode sfactor, BlendingMode dfactor) => await BatchCallAsync(BLEND_FUNC, isMethodCall: true, sfactor, dfactor);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void BlendFuncSeparate(BlendingMode srcRGB, BlendingMode dstRGB, BlendingMode srcAlpha, BlendingMode dstAlpha) => CallMethod<object>(BLEND_FUNC_SEPARATE, srcRGB, dstRGB, srcAlpha, dstAlpha);
        public async Task BlendFuncSeparateAsync(BlendingMode srcRGB, BlendingMode dstRGB, BlendingMode srcAlpha, BlendingMode dstAlpha) => await BatchCallAsync(BLEND_FUNC_SEPARATE, isMethodCall: true, srcRGB, dstRGB, srcAlpha, dstAlpha);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void ClearDepth(float depth) => CallMethod<object>(CLEAR_DEPTH, depth);
        public async Task ClearDepthAsync(float depth) => await BatchCallAsync(CLEAR_DEPTH, isMethodCall: true, depth);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void ClearStencil(int stencil) => CallMethod<object>(CLEAR_STENCIL, stencil);
        public async Task ClearStencilAsync(int stencil) => await BatchCallAsync(CLEAR_STENCIL, isMethodCall: true, stencil);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void ColorMask(bool red, bool green, bool blue, bool alpha) => CallMethod<object>(COLOR_MASK, red, green, blue, alpha);
        public async Task ColorMaskAsync(bool red, bool green, bool blue, bool alpha) => await BatchCallAsync(COLOR_MASK, isMethodCall: true, red, green, blue, alpha);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void CullFace(Face mode) => CallMethod<object>(CULL_FACE, mode);
        public async Task CullFaceAsync(Face mode) => await BatchCallAsync(CULL_FACE, isMethodCall: true, mode);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void DepthFunc(CompareFunction func) => CallMethod<object>(DEPTH_FUNC, func);
        public async Task DepthFuncAsync(CompareFunction func) => await BatchCallAsync(DEPTH_FUNC, isMethodCall: true, func);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void DepthMask(bool flag) => CallMethod<object>(DEPTH_MASK, flag);
        public async Task DepthMaskAsync(bool flag) => await BatchCallAsync(DEPTH_MASK, isMethodCall: true, flag);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void DepthRange(float zNear, float zFar) => CallMethod<object>(DEPTH_RANGE, zNear, zFar);
        public async Task DepthRangeAsync(float zNear, float zFar) => await BatchCallAsync(DEPTH_RANGE, isMethodCall: true, zNear, zFar);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void Disable(EnableCap cap) => CallMethod<object>(DISABLE, cap);
        public async Task DisableAsync(EnableCap cap) => await BatchCallAsync(DISABLE, isMethodCall: true, cap);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void Enable(EnableCap cap) => CallMethod<object>(ENABLE, cap);
        public async Task EnableAsync(EnableCap cap) => await BatchCallAsync(ENABLE, isMethodCall: true, cap);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void FrontFace(FrontFaceDirection mode) => CallMethod<object>(FRONT_FACE, mode);
        public async Task FrontFaceAsync(FrontFaceDirection mode) => await BatchCallAsync(FRONT_FACE, isMethodCall: true, mode);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public T GetParameter<T>(Parameter parameter) => CallMethod<T>(GET_PARAMETER, parameter);
        public async Task<T> GetParameterAsync<T>(Parameter parameter) => await CallMethodAsync<T>(GET_PARAMETER, parameter);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public Error GetError() => CallMethod<Error>(GET_ERROR);
        public async Task<Error> GetErrorAsync() => await CallMethodAsync<Error>(GET_ERROR);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void Hint(HintTarget target, HintMode mode) => CallMethod<object>(HINT, target, mode);
        public async Task HintAsync(HintTarget target, HintMode mode) => await BatchCallAsync(HINT, isMethodCall: true, target, mode);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public bool IsEnabled(EnableCap cap) => CallMethod<bool>(IS_ENABLED, cap);
        public async Task<bool> IsEnabledAsync(EnableCap cap) => await CallMethodAsync<bool>(IS_ENABLED, cap);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void LineWidth(float width) => CallMethod<object>(LINE_WIDTH, width);
        public async Task LineWidthAsync(float width) => await CallMethodAsync<object>(LINE_WIDTH, width);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public bool PixelStoreI(PixelStorageMode pname, int param) => CallMethod<bool>(PIXEL_STORE_I, pname, param);
        public async Task<bool> PixelStoreIAsync(PixelStorageMode pname, int param) => await CallMethodAsync<bool>(PIXEL_STORE_I, pname, param);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void PolygonOffset(float factor, float units) => CallMethod<object>(POLYGON_OFFSET, factor, units);
        public async Task PolygonOffsetAsync(float factor, float units) => await BatchCallAsync(POLYGON_OFFSET, isMethodCall: true, factor, units);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void SampleCoverage(float value, bool invert) => CallMethod<object>(SAMPLE_COVERAGE, value, invert);
        public async Task SampleCoverageAsync(float value, bool invert) => await BatchCallAsync(SAMPLE_COVERAGE, isMethodCall: true, value, invert);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void StencilFunc(CompareFunction func, int reference, uint mask) => CallMethod<object>(STENCIL_FUNC, func, reference, mask);
        public async Task StencilFuncAsync(CompareFunction func, int reference, uint mask) => await BatchCallAsync(STENCIL_FUNC, isMethodCall: true, func, reference, mask);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void StencilFuncSeparate(Face face, CompareFunction func, int reference, uint mask) => CallMethod<object>(STENCIL_FUNC_SEPARATE, face, func, reference, mask);
        public async Task StencilFuncSeparateAsync(Face face, CompareFunction func, int reference, uint mask) => await BatchCallAsync(STENCIL_FUNC_SEPARATE, isMethodCall: true, face, func, reference, mask);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void StencilMask(uint mask) => CallMethod<object>(STENCIL_MASK, mask);
        public async Task StencilMaskAsync(uint mask) => await BatchCallAsync(STENCIL_MASK, isMethodCall: true, mask);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void StencilMaskSeparate(Face face, uint mask) => CallMethod<object>(STENCIL_MASK_SEPARATE, face, mask);
        public async Task StencilMaskSeparateAsync(Face face, uint mask) => await BatchCallAsync(STENCIL_MASK_SEPARATE, isMethodCall: true, face, mask);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void StencilOp(StencilFunction fail, StencilFunction zfail, StencilFunction zpass) => CallMethod<object>(STENCIL_OP, fail, zfail, zpass);
        public async Task StencilOpAsync(StencilFunction fail, StencilFunction zfail, StencilFunction zpass) => await BatchCallAsync(STENCIL_OP, isMethodCall: true, fail, zfail, zpass);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void StencilOpSeparate(Face face, StencilFunction fail, StencilFunction zfail, StencilFunction zpass) => CallMethod<object>(STENCIL_OP_SEPARATE, face, fail, zfail, zpass);
        public async Task StencilOpSeparateAsync(Face face, StencilFunction fail, StencilFunction zfail, StencilFunction zpass) => await BatchCallAsync(STENCIL_OP_SEPARATE, isMethodCall: true, face, fail, zfail, zpass);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void BindBuffer(BufferType target, WebGLBuffer buffer) => CallMethod<object>(BIND_BUFFER, target, buffer);
        public async Task BindBufferAsync(BufferType target, WebGLBuffer buffer) => await BatchCallAsync(BIND_BUFFER, isMethodCall: true, target, buffer);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void BufferData(BufferType target, int size, BufferUsageHint usage) => CallMethod<object>(BUFFER_DATA, target, size, usage);
        public async Task BufferDataAsync(BufferType target, int size, BufferUsageHint usage) => await BatchCallAsync(BUFFER_DATA, isMethodCall: true, target, size, usage);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void BufferData<T>(BufferType target, T[] data, BufferUsageHint usage) => CallMethod<object>(BUFFER_DATA, target, ConvertToByteArray(data), usage);
        public async Task BufferDataAsync<T>(BufferType target, T[] data, BufferUsageHint usage) => await BatchCallAsync(BUFFER_DATA, isMethodCall: true, target, ConvertToByteArray(data), usage);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void BufferSubData<T>(BufferType target, uint offset, T[] data) => CallMethod<object>(BUFFER_SUB_DATA, target, offset, ConvertToByteArray(data));
        public async Task BufferSubDataAsync<T>(BufferType target, uint offset, T[] data) => await BatchCallAsync(BUFFER_SUB_DATA, isMethodCall: true, target, offset, ConvertToByteArray(data));

        [Obsolete("Use the async version instead, which is already called internally.")]
        public WebGLBuffer CreateBuffer() => CallMethod<WebGLBuffer>(CREATE_BUFFER);
        public async Task<WebGLBuffer> CreateBufferAsync() => await CallMethodAsync<WebGLBuffer>(CREATE_BUFFER);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void DeleteBuffer(WebGLBuffer buffer) => CallMethod<WebGLBuffer>(DELETE_BUFFER, buffer);
        public async Task DeleteBufferAsync(WebGLBuffer buffer) => await BatchCallAsync(DELETE_BUFFER, isMethodCall: true, buffer);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public T GetBufferParameter<T>(BufferType target, BufferParameter pname) => CallMethod<T>(GET_BUFFER_PARAMETER, target, pname);
        public async Task<T> GetBufferParameterAsync<T>(BufferType target, BufferParameter pname) => await CallMethodAsync<T>(GET_BUFFER_PARAMETER, target, pname);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public bool IsBuffer(WebGLBuffer buffer) => CallMethod<bool>(IS_BUFFER, buffer);
        public async Task<bool> IsBufferAsync(WebGLBuffer buffer) => await CallMethodAsync<bool>(IS_BUFFER, buffer);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void BindFramebuffer(FramebufferType target, WebGLFramebuffer framebuffer) => CallMethod<object>(BIND_FRAMEBUFFER, target, framebuffer);
        public async Task BindFramebufferAsync(FramebufferType target, WebGLFramebuffer framebuffer) => await BatchCallAsync(BIND_FRAMEBUFFER, isMethodCall: true, target, framebuffer);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public FramebufferStatus CheckFramebufferStatus(FramebufferType target) => CallMethod<FramebufferStatus>(CHECK_FRAMEBUFFER_STATUS, target);
        public async Task<FramebufferStatus> CheckFramebufferStatusAsync(FramebufferType target) => await CallMethodAsync<FramebufferStatus>(CHECK_FRAMEBUFFER_STATUS, target);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public WebGLFramebuffer CreateFramebuffer() => CallMethod<WebGLFramebuffer>(CREATE_FRAMEBUFFER);
        public async Task<WebGLFramebuffer> CreateFramebufferAsync() => await CallMethodAsync<WebGLFramebuffer>(CREATE_FRAMEBUFFER);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void DeleteFramebuffer(WebGLFramebuffer buffer) => CallMethod<object>(DELETE_FRAMEBUFFER, buffer);
        public async Task DeleteFramebufferAsync(WebGLFramebuffer buffer) => await BatchCallAsync(DELETE_FRAMEBUFFER, isMethodCall: true, buffer);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void FramebufferRenderbuffer(FramebufferType target, FramebufferAttachment attachment, RenderbufferType renderbuffertarget, WebGLRenderbuffer renderbuffer) => CallMethod<object>(FRAMEBUFFER_RENDERBUFFER, target, attachment, renderbuffertarget, renderbuffer);
        public async Task FramebufferRenderbufferAsync(FramebufferType target, FramebufferAttachment attachment, RenderbufferType renderbuffertarget, WebGLRenderbuffer renderbuffer) => await BatchCallAsync(FRAMEBUFFER_RENDERBUFFER, isMethodCall: true, target, attachment, renderbuffertarget, renderbuffer);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void FramebufferTexture2D(FramebufferType target, FramebufferAttachment attachment, Texture2DType textarget, WebGLTexture texture, int level) => CallMethod<object>(FRAMEBUFFER_TEXTURE_2D, target, attachment, textarget, texture, level);
        public async Task FramebufferTexture2DAsync(FramebufferType target, FramebufferAttachment attachment, Texture2DType textarget, WebGLTexture texture, int level) => await BatchCallAsync(FRAMEBUFFER_TEXTURE_2D, isMethodCall: true, target, attachment, textarget, texture, level);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public T GetFramebufferAttachmentParameter<T>(FramebufferType target, FramebufferAttachment attachment, FramebufferAttachmentParameter pname) => CallMethod<T>(GET_FRAMEBUFFER_ATTACHMENT_PARAMETER, target, attachment, pname);
        public async Task<T> GetFramebufferAttachmentParameterAsync<T>(FramebufferType target, FramebufferAttachment attachment, FramebufferAttachmentParameter pname) => await CallMethodAsync<T>(GET_FRAMEBUFFER_ATTACHMENT_PARAMETER, target, attachment, pname);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public bool IsFramebuffer(WebGLFramebuffer framebuffer) => CallMethod<bool>(IS_FRAMEBUFFER, framebuffer);
        public async Task<bool> IsFramebufferAsync(WebGLFramebuffer framebuffer) => await CallMethodAsync<bool>(IS_FRAMEBUFFER, framebuffer);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void ReadPixels(int x, int y, int width, int height, PixelFormat format, PixelType type, byte[] pixels) => CallMethod<object>(READ_PIXELS, x, y, width, height, format, type, pixels); //pixels should be an ArrayBufferView which the data gets read into
        public async Task ReadPixelsAsync(int x, int y, int width, int height, PixelFormat format, PixelType type, byte[] pixels) => await BatchCallAsync(READ_PIXELS, isMethodCall: true, x, y, width, height, format, type, pixels); //pixels should be an ArrayBufferView which the data gets read into

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void BindRenderbuffer(RenderbufferType target, WebGLRenderbuffer renderbuffer) => CallMethod<object>(BIND_RENDERBUFFER, target, renderbuffer);
        public async Task BindRenderbufferAsync(RenderbufferType target, WebGLRenderbuffer renderbuffer) => await BatchCallAsync(BIND_RENDERBUFFER, isMethodCall: true, target, renderbuffer);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public WebGLRenderbuffer CreateRenderbuffer() => CallMethod<WebGLRenderbuffer>(CREATE_RENDERBUFFER);
        public async Task<WebGLRenderbuffer> CreateRenderbufferAsync() => await CallMethodAsync<WebGLRenderbuffer>(CREATE_RENDERBUFFER);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void DeleteRenderbuffer(WebGLRenderbuffer buffer) => CallMethod<object>(DELETE_RENDERBUFFER, buffer);
        public async Task DeleteRenderbufferAsync(WebGLRenderbuffer buffer) => await BatchCallAsync(DELETE_RENDERBUFFER, isMethodCall: true, buffer);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public T GetRenderbufferParameter<T>(RenderbufferType target, RenderbufferParameter pname) => CallMethod<T>(GET_RENDERBUFFER_PARAMETER, target, pname);
        public async Task<T> GetRenderbufferParameterAsync<T>(RenderbufferType target, RenderbufferParameter pname) => await CallMethodAsync<T>(GET_RENDERBUFFER_PARAMETER, target, pname);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public bool IsRenderbuffer(WebGLRenderbuffer renderbuffer) => CallMethod<bool>(IS_RENDERBUFFER, renderbuffer);
        public async Task<bool> IsRenderbufferAsync(WebGLRenderbuffer renderbuffer) => await CallMethodAsync<bool>(IS_RENDERBUFFER, renderbuffer);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void RenderbufferStorage(RenderbufferType type, RenderbufferFormat internalFormat, int width, int height) => CallMethod<object>(RENDERBUFFER_STORAGE, type, internalFormat, width, height);
        public async Task RenderbufferStorageAsync(RenderbufferType type, RenderbufferFormat internalFormat, int width, int height) => await BatchCallAsync(RENDERBUFFER_STORAGE, isMethodCall: true, type, internalFormat, width, height);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void BindTexture(TextureType type, WebGLTexture texture) => CallMethod<object>(BIND_TEXTURE, type, texture);
        public async Task BindTextureAsync(TextureType type, WebGLTexture texture) => await BatchCallAsync(BIND_TEXTURE, isMethodCall: true, type, texture);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void CopyTexImage2D(Texture2DType target, int level, PixelFormat format, int x, int y, int width, int height, int border) => CallMethod<object>(COPY_TEX_IMAGE_2D, target, level, format, x, y, width, height, border);
        public async Task CopyTexImage2DAsync(Texture2DType target, int level, PixelFormat format, int x, int y, int width, int height, int border) => await BatchCallAsync(COPY_TEX_IMAGE_2D, isMethodCall: true, target, level, format, x, y, width, height, border);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void CopyTexSubImage2D(Texture2DType target, int level, int xoffset, int yoffset, int x, int y, int width, int height) => CallMethod<object>(COPY_TEX_SUB_IMAGE_2D, target, level, xoffset, yoffset, x, y, width, height);
        public async Task CopyTexSubImage2DAsync(Texture2DType target, int level, int xoffset, int yoffset, int x, int y, int width, int height) => await BatchCallAsync(COPY_TEX_SUB_IMAGE_2D, isMethodCall: true, target, level, xoffset, yoffset, x, y, width, height);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public WebGLTexture CreateTexture() => CallMethod<WebGLTexture>(CREATE_TEXTURE);
        public async Task<WebGLTexture> CreateTextureAsync() => await CallMethodAsync<WebGLTexture>(CREATE_TEXTURE);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void DeleteTexture(WebGLTexture texture) => CallMethod<object>(DELETE_TEXTURE, texture);
        public async Task DeleteTextureAsync(WebGLTexture texture) => await BatchCallAsync(DELETE_TEXTURE, isMethodCall: true, texture);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void GenerateMipmap(TextureType target) => CallMethod<object>(GENERATE_MIPMAP, target);
        public async Task GenerateMipmapAsync(TextureType target) => await BatchCallAsync(GENERATE_MIPMAP, isMethodCall: true, target);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public T GetTexParameter<T>(TextureType target, TextureParameter pname) => CallMethod<T>(GET_TEX_PARAMETER, target, pname);
        public async Task<T> GetTexParameterAsync<T>(TextureType target, TextureParameter pname) => await CallMethodAsync<T>(GET_TEX_PARAMETER, target, pname);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public bool IsTexture(WebGLTexture texture) => CallMethod<bool>(IS_TEXTURE, texture);
        public async Task<bool> IsTextureAsync(WebGLTexture texture) => await CallMethodAsync<bool>(IS_TEXTURE, texture);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void TexImage2D<T>(Texture2DType target, int level, PixelFormat internalFormat, int width, int height, PixelFormat format, PixelType type, T[] pixels)
            where T : struct
            => CallMethod<object>(TEX_IMAGE_2D, target, level, internalFormat, width, height, format, type, pixels);
        public async Task TexImage2DAsync<T>(Texture2DType target, int level, PixelFormat internalFormat, int width, int height, PixelFormat format, PixelType type, T[] pixels)
            where T : struct
            => await BatchCallAsync(TEX_IMAGE_2D, isMethodCall: true, target, level, internalFormat, width, height, format, type, pixels);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void TexSubImage2D<T>(Texture2DType target, int level, int xoffset, int yoffset, int width, int height, PixelFormat format, PixelType type, T[] pixels)
            where T : struct
            => CallMethod<object>(TEX_SUB_IMAGE_2D, target, level, xoffset, yoffset, width, height, format, type, pixels);
        public async Task TexSubImage2DAsync<T>(Texture2DType target, int level, int xoffset, int yoffset, int width, int height, PixelFormat format, PixelType type, T[] pixels)
            where T : struct
            => await BatchCallAsync(TEX_SUB_IMAGE_2D, isMethodCall: true, target, level, xoffset, yoffset, width, height, format, type, pixels);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void TexParameter(TextureType target, TextureParameter pname, float param) => CallMethod<object>(TEX_PARAMETER_F, target, pname, param);
        public async Task TexParameterAsync(TextureType target, TextureParameter pname, float param) => await BatchCallAsync(TEX_PARAMETER_F, isMethodCall: true, target, pname, param);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void TexParameter(TextureType target, TextureParameter pname, int param) => CallMethod<object>(TEX_PARAMETER_I, target, pname, param);
        public async Task TexParameterAsync(TextureType target, TextureParameter pname, int param) => await BatchCallAsync(TEX_PARAMETER_I, isMethodCall: true, target, pname, param);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void AttachShader(WebGLProgram program, WebGLShader shader) => CallMethod<object>(ATTACH_SHADER, program, shader);
        public async Task AttachShaderAsync(WebGLProgram program, WebGLShader shader) => await BatchCallAsync(ATTACH_SHADER, isMethodCall: true, program, shader);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void BindAttribLocation(WebGLProgram program, uint index, string name) => CallMethod<object>(BIND_ATTRIB_LOCATION, program, index, name);
        public async Task BindAttribLocationAsync(WebGLProgram program, uint index, string name) => await BatchCallAsync(BIND_ATTRIB_LOCATION, isMethodCall: true, program, index, name);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void CompileShader(WebGLShader shader) => CallMethod<object>(COMPILE_SHADER, shader);
        public async Task CompileShaderAsync(WebGLShader shader) => await BatchCallAsync(COMPILE_SHADER, isMethodCall: true, shader);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public WebGLProgram CreateProgram() => CallMethod<WebGLProgram>(CREATE_PROGRAM);
        public async Task<WebGLProgram> CreateProgramAsync() => await CallMethodAsync<WebGLProgram>(CREATE_PROGRAM);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public WebGLShader CreateShader(ShaderType type) => CallMethod<WebGLShader>(CREATE_SHADER, type);
        public async Task<WebGLShader> CreateShaderAsync(ShaderType type) => await CallMethodAsync<WebGLShader>(CREATE_SHADER, type);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void DeleteProgram(WebGLProgram program) => CallMethod<object>(DELETE_PROGRAM, program);
        public async Task DeleteProgramAsync(WebGLProgram program) => await BatchCallAsync(DELETE_PROGRAM, isMethodCall: true, program);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void DeleteShader(WebGLShader shader) => CallMethod<object>(DELETE_SHADER, shader);
        public async Task DeleteShaderAsync(WebGLShader shader) => await BatchCallAsync(DELETE_SHADER, isMethodCall: true, shader);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void DetachShader(WebGLProgram program, WebGLShader shader) => CallMethod<object>(DETACH_SHADER, program, shader);
        public async Task DetachShaderAsync(WebGLProgram program, WebGLShader shader) => await BatchCallAsync(DETACH_SHADER, isMethodCall: true, program, shader);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public WebGLShader[] GetAttachedShaders(WebGLProgram program) => CallMethod<WebGLShader[]>(GET_ATTACHED_SHADERS, program);
        public async Task<WebGLShader[]> GetAttachedShadersAsync(WebGLProgram program) => await CallMethodAsync<WebGLShader[]>(GET_ATTACHED_SHADERS, program);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public T GetProgramParameter<T>(WebGLProgram program, ProgramParameter pname) => CallMethod<T>(GET_PROGRAM_PARAMETER, program, pname);
        public async Task<T> GetProgramParameterAsync<T>(WebGLProgram program, ProgramParameter pname) => await CallMethodAsync<T>(GET_PROGRAM_PARAMETER, program, pname);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public string GetProgramInfoLog(WebGLProgram program) => CallMethod<string>(GET_PROGRAM_INFO_LOG, program);
        public async Task<string> GetProgramInfoLogAsync(WebGLProgram program) => await CallMethodAsync<string>(GET_PROGRAM_INFO_LOG, program);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public T GetShaderParameter<T>(WebGLShader shader, ShaderParameter pname) => CallMethod<T>(GET_SHADER_PARAMETER, shader, pname);
        public async Task<T> GetShaderParameterAsync<T>(WebGLShader shader, ShaderParameter pname) => await CallMethodAsync<T>(GET_SHADER_PARAMETER, shader, pname);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public WebGLShaderPrecisionFormat GetShaderPrecisionFormat(ShaderType shaderType, ShaderPrecision precisionType) => CallMethod<WebGLShaderPrecisionFormat>(GET_SHADER_PRECISION_FORMAT, shaderType, precisionType);
        public async Task<WebGLShaderPrecisionFormat> GetShaderPrecisionFormatAsync(ShaderType shaderType, ShaderPrecision precisionType) => await CallMethodAsync<WebGLShaderPrecisionFormat>(GET_SHADER_PRECISION_FORMAT, shaderType, precisionType);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public string GetShaderInfoLog(WebGLShader shader) => CallMethod<string>(GET_SHADER_INFO_LOG, shader);
        public async Task<string> GetShaderInfoLogAsync(WebGLShader shader) => await CallMethodAsync<string>(GET_SHADER_INFO_LOG, shader);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public string GetShaderSource(WebGLShader shader) => CallMethod<string>(GET_SHADER_SOURCE, shader);
        public async Task<string> GetShaderSourceAsync(WebGLShader shader) => await CallMethodAsync<string>(GET_SHADER_SOURCE, shader);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public bool IsProgram(WebGLProgram program) => CallMethod<bool>(IS_PROGRAM, program);
        public async Task<bool> IsProgramAsync(WebGLProgram program) => await CallMethodAsync<bool>(IS_PROGRAM, program);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public bool IsShader(WebGLShader shader) => CallMethod<bool>(IS_SHADER, shader);
        public async Task<bool> IsShaderAsync(WebGLShader shader) => await CallMethodAsync<bool>(IS_SHADER, shader);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void LinkProgram(WebGLProgram program) => CallMethod<object>(LINK_PROGRAM, program);
        public async Task LinkProgramAsync(WebGLProgram program) => await BatchCallAsync(LINK_PROGRAM, isMethodCall: true, program);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void ShaderSource(WebGLShader shader, string source) => CallMethod<object>(SHADER_SOURCE, shader, source);
        public async Task ShaderSourceAsync(WebGLShader shader, string source) => await BatchCallAsync(SHADER_SOURCE, isMethodCall: true, shader, source);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void UseProgram(WebGLProgram program) => CallMethod<object>(USE_PROGRAM, program);
        public async Task UseProgramAsync(WebGLProgram program) => await BatchCallAsync(USE_PROGRAM, isMethodCall: true, program);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void ValidateProgram(WebGLProgram program) => CallMethod<object>(VALIDATE_PROGRAM, program);
        public async Task ValidateProgramAsync(WebGLProgram program) => await BatchCallAsync(VALIDATE_PROGRAM, isMethodCall: true, program);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void DisableVertexAttribArray(uint index) => CallMethod<object>(DISABLE_VERTEX_ATTRIB_ARRAY, index);
        public async Task DisableVertexAttribArrayAsync(uint index) => await BatchCallAsync(DISABLE_VERTEX_ATTRIB_ARRAY, isMethodCall: true, index);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void EnableVertexAttribArray(uint index) => CallMethod<object>(ENABLE_VERTEX_ATTRIB_ARRAY, index);
        public async Task EnableVertexAttribArrayAsync(uint index) => await BatchCallAsync(ENABLE_VERTEX_ATTRIB_ARRAY, isMethodCall: true, index);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public WebGLActiveInfo GetActiveAttrib(WebGLProgram program, uint index) => CallMethod<WebGLActiveInfo>(GET_ACTIVE_ATTRIB, program, index);
        public async Task<WebGLActiveInfo> GetActiveAttribAsync(WebGLProgram program, uint index) => await CallMethodAsync<WebGLActiveInfo>(GET_ACTIVE_ATTRIB, program, index);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public WebGLActiveInfo GetActiveUniform(WebGLProgram program, uint index) => CallMethod<WebGLActiveInfo>(GET_ACTIVE_UNIFORM, program, index);
        public async Task<WebGLActiveInfo> GetActiveUniformAsync(WebGLProgram program, uint index) => await CallMethodAsync<WebGLActiveInfo>(GET_ACTIVE_UNIFORM, program, index);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public int GetAttribLocation(WebGLProgram program, string name) => CallMethod<int>(GET_ATTRIB_LOCATION, program, name);
        public async Task<int> GetAttribLocationAsync(WebGLProgram program, string name) => await CallMethodAsync<int>(GET_ATTRIB_LOCATION, program, name);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public T GetUniform<T>(WebGLProgram program, WebGLUniformLocation location) => CallMethod<T>(GET_UNIFORM, program, location);
        public async Task<T> GetUniformAsync<T>(WebGLProgram program, WebGLUniformLocation location) => await CallMethodAsync<T>(GET_UNIFORM, program, location);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public WebGLUniformLocation GetUniformLocation(WebGLProgram program, string name) => CallMethod<WebGLUniformLocation>(GET_UNIFORM_LOCATION, program, name);
        public async Task<WebGLUniformLocation> GetUniformLocationAsync(WebGLProgram program, string name) => await CallMethodAsync<WebGLUniformLocation>(GET_UNIFORM_LOCATION, program, name);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public T GetVertexAttrib<T>(uint index, VertexAttribute pname) => CallMethod<T>(GET_VERTEX_ATTRIB, index, pname);
        public async Task<T> GetVertexAttribAsync<T>(uint index, VertexAttribute pname) => await CallMethodAsync<T>(GET_VERTEX_ATTRIB, index, pname);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public long GetVertexAttribOffset(uint index, VertexAttributePointer pname) => CallMethod<long>(GET_VERTEX_ATTRIB_OFFSET, index, pname);
        public async Task<long> GetVertexAttribOffsetAsync(uint index, VertexAttributePointer pname) => await CallMethodAsync<long>(GET_VERTEX_ATTRIB_OFFSET, index, pname);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void VertexAttribPointer(uint index, int size, DataType type, bool normalized, int stride, long offset) => CallMethod<object>(VERTEX_ATTRIB_POINTER, index, size, type, normalized, stride, offset);
        public async Task VertexAttribPointerAsync(uint index, int size, DataType type, bool normalized, int stride, long offset) => await BatchCallAsync(VERTEX_ATTRIB_POINTER, isMethodCall: true, index, size, type, normalized, stride, offset);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void Uniform(WebGLUniformLocation location, params float[] value)
        {
            switch (value.Length)
            {
                case 1:
                    CallMethod<object>(UNIFORM + "1fv", location, value);
                    break;
                case 2:
                    CallMethod<object>(UNIFORM + "2fv", location, value);
                    break;
                case 3:
                    CallMethod<object>(UNIFORM + "3fv", location, value);
                    break;
                case 4:
                    CallMethod<object>(UNIFORM + "4fv", location, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value.Length, "Value array is empty or too long");
            }
        }
        public async Task UniformAsync(WebGLUniformLocation location, params float[] value)
        {
            switch (value.Length)
            {
                case 1:
                    await BatchCallAsync(UNIFORM + "1fv", isMethodCall: true, location, value);
                    break;
                case 2:
                    await BatchCallAsync(UNIFORM + "2fv", isMethodCall: true, location, value);
                    break;
                case 3:
                    await BatchCallAsync(UNIFORM + "3fv", isMethodCall: true, location, value);
                    break;
                case 4:
                    await BatchCallAsync(UNIFORM + "4fv", isMethodCall: true, location, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value.Length, "Value array is empty or too long");
            }
        }

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void Uniform(WebGLUniformLocation location, params int[] value)
        {
            switch (value.Length)
            {
                case 1:
                    CallMethod<object>(UNIFORM + "1iv", location, value);
                    break;
                case 2:
                    CallMethod<object>(UNIFORM + "2iv", location, value);
                    break;
                case 3:
                    CallMethod<object>(UNIFORM + "3iv", location, value);
                    break;
                case 4:
                    CallMethod<object>(UNIFORM + "4iv", location, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value.Length, "Value array is empty or too long");
            }
        }
        public async Task UniformAsync(WebGLUniformLocation location, params int[] value)
        {
            switch (value.Length)
            {
                case 1:
                    await BatchCallAsync(UNIFORM + "1iv", isMethodCall: true, location, value);
                    break;
                case 2:
                    await BatchCallAsync(UNIFORM + "2iv", isMethodCall: true, location, value);
                    break;
                case 3:
                    await BatchCallAsync(UNIFORM + "3iv", isMethodCall: true, location, value);
                    break;
                case 4:
                    await BatchCallAsync(UNIFORM + "4iv", isMethodCall: true, location, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value.Length, "Value array is empty or too long");
            }
        }

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void UniformMatrix(WebGLUniformLocation location, bool transpose, float[] value)
        {
            switch (value.Length)
            {
                case 2 * 2:
                    CallMethod<object>(UNIFORM_MATRIX + "2fv", location, transpose, value);
                    break;
                case 3 * 3:
                    CallMethod<object>(UNIFORM_MATRIX + "3fv", location, transpose, value);
                    break;
                case 4 * 4:
                    CallMethod<object>(UNIFORM_MATRIX + "4fv", location, transpose, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value.Length, "Value array has incorrect size");
            }
        }
        public async Task UniformMatrixAsync(WebGLUniformLocation location, bool transpose, float[] value)
        {
            switch (value.Length)
            {
                case 2 * 2:
                    await BatchCallAsync(UNIFORM_MATRIX + "2fv", isMethodCall: true, location, transpose, value);
                    break;
                case 3 * 3:
                    await BatchCallAsync(UNIFORM_MATRIX + "3fv", isMethodCall: true, location, transpose, value);
                    break;
                case 4 * 4:
                    await BatchCallAsync(UNIFORM_MATRIX + "4fv", isMethodCall: true, location, transpose, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value.Length, "Value array has incorrect size");
            }
        }

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void VertexAttrib(uint index, params float[] value)
        {
            switch (value.Length)
            {
                case 1:
                    CallMethod<object>(VERTEX_ATTRIB + "1fv", index, value);
                    break;
                case 2:
                    CallMethod<object>(VERTEX_ATTRIB + "2fv", index, value);
                    break;
                case 3:
                    CallMethod<object>(VERTEX_ATTRIB + "3fv", index, value);
                    break;
                case 4:
                    CallMethod<object>(VERTEX_ATTRIB + "4fv", index, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value.Length, "Value array is empty or too long");
            }
        }
        public async Task VertexAttribAsync(uint index, params float[] value)
        {
            switch (value.Length)
            {
                case 1:
                    await BatchCallAsync(VERTEX_ATTRIB + "1fv", isMethodCall: true, index, value);
                    break;
                case 2:
                    await BatchCallAsync(VERTEX_ATTRIB + "2fv", isMethodCall: true, index, value);
                    break;
                case 3:
                    await BatchCallAsync(VERTEX_ATTRIB + "3fv", isMethodCall: true, index, value);
                    break;
                case 4:
                    await BatchCallAsync(VERTEX_ATTRIB + "4fv", isMethodCall: true, index, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value.Length, "Value array is empty or too long");
            }
        }

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void DrawArrays(Primitive mode, int first, int count) => CallMethod<object>(DRAW_ARRAYS, mode, first, count);
        public async Task DrawArraysAsync(Primitive mode, int first, int count) => await BatchCallAsync(DRAW_ARRAYS, isMethodCall: true, mode, first, count);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void DrawElements(Primitive mode, int count, DataType type, long offset) => CallMethod<object>(DRAW_ELEMENTS, mode, count, type, offset);
        public async Task DrawElementsAsync(Primitive mode, int count, DataType type, long offset) => await BatchCallAsync(DRAW_ELEMENTS, isMethodCall: true, mode, count, type, offset);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void Finish() => CallMethod<object>(FINISH);
        public async Task FinishAsync() => await BatchCallAsync(FINISH, isMethodCall: true);

        [Obsolete("Use the async version instead, which is already called internally.")]
        public void Flush() => CallMethod<object>(FLUSH);
        public async Task FlushAsync() => await BatchCallAsync(FLUSH, isMethodCall: true);

        private byte[] ConvertToByteArray<T>(T[] arr)
        {
            byte[] byteArr = new byte[arr.Length * Marshal.SizeOf<T>()];
            Buffer.BlockCopy(arr, 0, byteArr, 0, byteArr.Length);
            return byteArr;
        }
        private async Task<int> GetDrawingBufferWidthAsync() => await GetPropertyAsync<int>(DRAWING_BUFFER_WIDTH);
        private async Task<int> GetDrawingBufferHeightAsync() => await GetPropertyAsync<int>(DRAWING_BUFFER_HEIGHT);
        #endregion
    }
}
