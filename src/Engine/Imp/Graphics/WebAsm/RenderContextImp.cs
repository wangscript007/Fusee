using Fusee.Base.Common;
using Fusee.Base.Core;
using Fusee.Engine.Common;
using Fusee.Engine.Core.ShaderShards;
using Fusee.Engine.Imp.WebAsm;
using Fusee.Math.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Fusee.Engine.Imp.Graphics.WebAsm
{
    /// <summary>
    /// Implementation of the <see cref="IRenderContextImp" /> interface on top of WebGLDotNET.
    /// </summary>
    public class RenderContextImp : IRenderContextImp
    {
        /// <summary>
        /// The WebGL2 rendering context base.
        /// </summary>
        protected WebGLContext gl2;

        private int _textureCountPerShader;
        private readonly Dictionary<WebGLUniformLocation, int> _shaderParam2TexUnit;

        private uint _blendEquationAlpha;
        private uint _blendEquationRgb;
        private uint _blendDstRgb;
        private uint _blendSrcRgb;
        private uint _blendSrcAlpha;
        private uint _blendDstAlpha;

        private bool _isCullEnabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderContextImp"/> class.
        /// </summary>
        /// <param name="renderCanvasImp">The platform specific render canvas implementation.</param>
        public RenderContextImp(IRenderCanvasImp renderCanvasImp)
        {
            _textureCountPerShader = 0;
            _shaderParam2TexUnit = new Dictionary<WebGLUniformLocation, int>();

            gl2 = ((RenderCanvasImp)renderCanvasImp)._gl;

            //Console.WriteLine($"RenderContextImp got context: {gl2}");
            //Console.WriteLine($"RenderContextImp got canvas: {gl2.Canvas}");
            //Console.WriteLine($"RenderContextImp got canvas: {gl2.DrawingBufferHeight}");

            // Due to the right-handed nature of OpenGL and the left-handed design of FUSEE
            // the meaning of what's Front and Back of a face simply flips.
            // TODO - implement this in render states!!!
            //_blendSrcAlpha = (uint)gl2.GetParameter(BLEND_SRC_ALPHA);
            //_blendDstAlpha = (uint)gl2.GetParameter(BLEND_DST_ALPHA);
            //_blendDstRgb = (uint)gl2.GetParameter(BLEND_DST_RGB);
            //_blendSrcRgb = (uint)gl2.GetParameter(BLEND_SRC_RGB);
            //_blendEquationAlpha = (uint)gl2.GetParameter(BLEND_EQUATION_ALPHA);
            //_blendEquationRgb = (uint)gl2.GetParameter(BLEND_EQUATION_RGB);

        }

        #region Image data related Members

        private uint GetTexComapreMode(TextureCompareMode compareMode)
        {
            switch (compareMode)
            {
                case TextureCompareMode.NONE:
                    return 0;

                case TextureCompareMode.GL_COMPARE_REF_TO_TEXTURE:
                    return 1;

                default:
                    throw new ArgumentException("Invalid compare mode.");
            }
        }

        private Tuple<int, int> GetMinMagFilter(TextureFilterMode filterMode)
        {
            int minFilter;
            int magFilter;

            switch (filterMode)
            {
                case TextureFilterMode.NEAREST:
                    minFilter = (int)TextureParameterValue.NEAREST;
                    magFilter = (int)TextureParameterValue.NEAREST;
                    break;
                default:
                case TextureFilterMode.LINEAR:
                    minFilter = (int)TextureParameterValue.LINEAR;
                    magFilter = (int)TextureParameterValue.LINEAR;
                    break;
                case TextureFilterMode.NEAREST_MIPMAP_NEAREST:
                    minFilter = (int)TextureParameterValue.NEAREST_MIPMAP_NEAREST;
                    magFilter = (int)TextureParameterValue.LINEAR;
                    break;
                case TextureFilterMode.LINEAR_MIPMAP_NEAREST:
                    minFilter = (int)TextureParameterValue.LINEAR_MIPMAP_NEAREST;
                    magFilter = (int)TextureParameterValue.LINEAR;
                    break;
                case TextureFilterMode.NEAREST_MIPMAP_LINEAR:
                    minFilter = (int)TextureParameterValue.NEAREST_MIPMAP_LINEAR;
                    magFilter = (int)TextureParameterValue.LINEAR;
                    break;
                case TextureFilterMode.LINEAR_MIPMAP_LINEAR:
                    minFilter = (int)TextureParameterValue.NEAREST_MIPMAP_LINEAR;
                    magFilter = (int)TextureParameterValue.LINEAR;
                    break;
            }

            return new Tuple<int, int>(minFilter, magFilter);
        }

        private int GetWrapMode(TextureWrapMode wrapMode)
        {
            switch (wrapMode)
            {
                default:
                case TextureWrapMode.REPEAT:
                    return (int)TextureParameterValue.REPEAT;
                case TextureWrapMode.MIRRORED_REPEAT:
                    return (int)TextureParameterValue.MIRRORED_REPEAT;
                case TextureWrapMode.CLAMP_TO_EDGE:
                    return (int)TextureParameterValue.CLAMP_TO_EDGE;
                case TextureWrapMode.CLAMP_TO_BORDER:
                    {
#warning TextureWrapMode.CLAMP_TO_BORDER is not supported on Android. CLAMP_TO_EDGE is set instead.
                        return (int)TextureParameterValue.CLAMP_TO_EDGE;
                    }
            }
        }

        private uint GetDepthCompareFunc(Compare compareFunc)
        {
            switch (compareFunc)
            {
                case Compare.Never:
                    return (uint)CompareFunction.NEVER;

                case Compare.Less:
                    return (uint)CompareFunction.LESS;

                case Compare.Equal:
                    return (uint)CompareFunction.EQUAL;

                case Compare.LessEqual:
                    return (uint)CompareFunction.LEQUAL;

                case Compare.Greater:
                    return (uint)CompareFunction.GREATER;

                case Compare.NotEqual:
                    return (uint)CompareFunction.NOTEQUAL;

                case Compare.GreaterEqual:
                    return (uint)CompareFunction.GEQUAL;

                case Compare.Always:
                    return (uint)CompareFunction.ALWAYS;

                default:
                    throw new ArgumentOutOfRangeException("value");
            }
        }

        private TexturePixelInfo GetTexturePixelInfo(ITextureBase tex)
        {
            uint internalFormat;
            uint format;
            uint pxType;

            switch (tex.PixelFormat.ColorFormat)
            {
                case ColorFormat.RGBA:
                    internalFormat = (uint)PixelFormat.RGBA;
                    format = (uint)PixelFormat.RGBA;
                    pxType = (uint)PixelType.UNSIGNED_BYTE;
                    break;
                case ColorFormat.RGB:
                    internalFormat = (uint)PixelFormat.RGB;
                    format = (uint)PixelFormat.RGB;
                    pxType = (uint)PixelType.UNSIGNED_BYTE;
                    break;
                // TODO: Handle Alpha-only / Intensity-only and AlphaIntensity correctly.
                case ColorFormat.Intensity:
                    internalFormat = (uint)PixelFormat.ALPHA;
                    format = (uint)PixelFormat.ALPHA;
                    pxType = (uint)PixelType.UNSIGNED_BYTE;
                    break;
                case ColorFormat.Depth24:
                case ColorFormat.Depth16:
                    internalFormat = (uint)RenderbufferFormat.DEPTH_COMPONENT16;
                    format = (uint)RenderbufferFormat.DEPTH_COMPONENT16;
                    pxType = (uint)PixelType.FLOAT;
                    break;
                case ColorFormat.uiRgb8:
                case ColorFormat.fRGB32:
                case ColorFormat.fRGB16:
                default:
                    throw new ArgumentOutOfRangeException("CreateTexture: Image pixel format not supported");
            }

            return new TexturePixelInfo()
            {
                Format = format,
                InternalFormat = internalFormat,
                PxType = pxType

            };
        }

        /// <summary>
        /// Creates a new CubeMap and binds it to the shader.
        /// </summary>
        /// <param name="img">A given ImageData object, containing all necessary information for the upload to the graphics card.</param>
        /// <returns>An ITextureHandle that can be used for texturing in the shader. In this implementation, the handle is an integer-value which is necessary for OpenTK.</returns>
        public ITextureHandle CreateTexture(IWritableCubeMap img)
        {
            return CreateTextureAsync(img).GetAwaiter().GetResult();
        }


        /// <summary>
        /// Creates a new CubeMap and binds it to the shader.
        /// </summary>
        /// <param name="img">A given ImageData object, containing all necessary information for the upload to the graphics card.</param>
        /// <returns>An ITextureHandle that can be used for texturing in the shader. In this implementation, the handle is an integer-value which is necessary for OpenTK.</returns>
        public async Task<ITextureHandle> CreateTextureAsync(IWritableCubeMap img)
        {
            var id = await gl2.CreateTextureAsync();
            await gl2.BindTextureAsync(TextureType.TEXTURE_CUBE_MAP, id);

            var glMinMagFilter = GetMinMagFilter(img.FilterMode);
            var minFilter = glMinMagFilter.Item1;
            var magFilter = glMinMagFilter.Item2;

            var glWrapMode = GetWrapMode(img.WrapMode);
            var pxInfo = GetTexturePixelInfo(img);

            for (var i = 0; i < 6; i++)
            {
                await gl2.TexImage2DAsync((Texture2DType.TEXTURE_CUBE_MAP_POSITIVE_X + i),
                    0,
                    (PixelFormat)pxInfo.InternalFormat,
                    img.Width,
                    img.Height,
                    (PixelFormat)pxInfo.Format,
                    (PixelType)pxInfo.PxType,
                    new byte[] { }
                    );


            }
            await gl2.TexParameterAsync(TextureType.TEXTURE_CUBE_MAP, TextureParameter.TEXTURE_MAG_FILTER, magFilter);
            await gl2.TexParameterAsync(TextureType.TEXTURE_CUBE_MAP, TextureParameter.TEXTURE_MIN_FILTER, minFilter);
            await gl2.TexParameterAsync(TextureType.TEXTURE_CUBE_MAP, TextureParameter.TEXTURE_WRAP_S, glWrapMode);
            await gl2.TexParameterAsync(TextureType.TEXTURE_CUBE_MAP, TextureParameter.TEXTURE_WRAP_T, glWrapMode);

            ITextureHandle texID = new TextureHandle { TexHandle = id };

            return texID;
        }

        /// <summary>
        /// Creates a new Texture and binds it to the shader.
        /// </summary>
        /// <param name="img">A given ImageData object, containing all necessary information for the upload to the graphics card.</param> 
        /// <returns>An ITextureHandle that can be used for texturing in the shader. In this implementation, the handle is an integer-value which is necessary for OpenTK.</returns>
        public ITextureHandle CreateTexture(ITexture img)
        {
            return CreateTextureAsync(img).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates a new Texture and binds it to the shader.
        /// </summary>
        /// <param name="img">A given ImageData object, containing all necessary information for the upload to the graphics card.</param> 
        /// <returns>An ITextureHandle that can be used for texturing in the shader. In this implementation, the handle is an integer-value which is necessary for OpenTK.</returns>
        public async Task<ITextureHandle> CreateTextureAsync(ITexture img)
        {
            var id = await gl2.CreateTextureAsync();
            await gl2.BindTextureAsync(TextureType.TEXTURE_2D, id);

            var glMinMagFilter = GetMinMagFilter(img.FilterMode);
            var minFilter = glMinMagFilter.Item1;
            var magFilter = glMinMagFilter.Item2;

            var glWrapMode = GetWrapMode(img.WrapMode);
            var pxInfo = GetTexturePixelInfo(img);

            var imageData = img.PixelData;
            await gl2.TexImage2DAsync(Texture2DType.TEXTURE_2D, 0, (PixelFormat)pxInfo.InternalFormat, img.Width, img.Height, (PixelFormat)pxInfo.Format, (PixelType)pxInfo.PxType, imageData);

            if (img.DoGenerateMipMaps)
                await gl2.GenerateMipmapAsync(TextureType.TEXTURE_2D);

            await gl2.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_MAG_FILTER, magFilter);
            await gl2.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_MIN_FILTER, minFilter);
            await gl2.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_WRAP_S, glWrapMode);
            await gl2.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_WRAP_T, glWrapMode);

            ITextureHandle texID = new TextureHandle { TexHandle = id };

            return texID;
        }

        /// <summary>
        /// Creates a new Texture and binds it to the shader.
        /// </summary>
        /// <param name="img">A given ImageData object, containing all necessary information for the upload to the graphics card.</param> 
        /// <returns>An ITextureHandle that can be used for texturing in the shader. In this implementation, the handle is an integer-value which is necessary for OpenTK.</returns>
        public ITextureHandle CreateTexture(IWritableTexture img)
        {
            return CreateTextureAsync(img).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates a new Texture and binds it to the shader.
        /// </summary>
        /// <param name="img">A given ImageData object, containing all necessary information for the upload to the graphics card.</param> 
        /// <returns>An ITextureHandle that can be used for texturing in the shader. In this implementation, the handle is an integer-value which is necessary for OpenTK.</returns>
        public async Task<ITextureHandle> CreateTextureAsync(IWritableTexture img)
        {
            var id = await gl2.CreateTextureAsync();
            await gl2.BindTextureAsync(TextureType.TEXTURE_2D, id);

            var glMinMagFilter = GetMinMagFilter(img.FilterMode);
            var minFilter = glMinMagFilter.Item1;
            var magFilter = glMinMagFilter.Item2;

            var glWrapMode = GetWrapMode(img.WrapMode);
            var pxInfo = GetTexturePixelInfo(img);

            await gl2.TexImage2DAsync(Texture2DType.TEXTURE_2D, 0, (PixelFormat)pxInfo.InternalFormat, img.Width, img.Height, (PixelFormat)pxInfo.Format, (PixelType)pxInfo.PxType, new byte[] { });

            if (img.DoGenerateMipMaps)
                await gl2.GenerateMipmapAsync(TextureType.TEXTURE_2D);

            //gl2.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter., (int)GetTexComapreMode(img.CompareMode));
            //gl2.TexParameterAsync(TextureType.TEXTURE_2D, TEXTURE_COMPARE_FUNC, (int)GetDepthCompareFunc(img.CompareFunc));
            await gl2.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_MAG_FILTER, magFilter);
            await gl2.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_MIN_FILTER, minFilter);
            await gl2.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_WRAP_S, glWrapMode);
            await gl2.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_WRAP_T, glWrapMode);


            ITextureHandle texID = new TextureHandle { TexHandle = id };

            return texID;
        }


        /// <summary>
        /// Updates a specific rectangle of a texture.
        /// </summary>
        /// <param name="tex">The texture to which the ImageData is bound to.</param>
        /// <param name="img">The ImageData struct containing information about the image. </param>
        /// <param name="startX">The x-value of the upper left corner of th rectangle.</param>
        /// <param name="startY">The y-value of the upper left corner of th rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <remarks> /// <remarks>Look at the VideoTextureExample for further information.</remarks></remarks>
        public async Task UpdateTextureRegion(ITextureHandle tex, ITexture img, int startX, int startY, int width, int height)
        {
            var pixelFormat = GetTexturePixelInfo(img).Format;

            // copy the bytes from image to GPU texture
            var bytesTotal = width * height * img.PixelFormat.BytesPerPixel;
            var scanlines = img.ScanLines(startX, startY, width, height);
            var bytes = new byte[bytesTotal];
            var offset = 0;
            do
            {
                if (scanlines.Current != null)
                {
                    var lineBytes = scanlines.Current.GetScanLineBytes();
                    Buffer.BlockCopy(lineBytes, 0, bytes, offset, lineBytes.Length);
                    offset += lineBytes.Length;
                }

            } while (scanlines.MoveNext());

            await gl2.BindTextureAsync(TextureType.TEXTURE_2D, ((TextureHandle)tex).TexHandle);
            var imageData = bytes;
            await gl2.TexSubImage2DAsync(Texture2DType.TEXTURE_2D, 0, startX, startY, width, height, (PixelFormat)pixelFormat, PixelType.UNSIGNED_BYTE, imageData);

            await gl2.GenerateMipmapAsync(TextureType.TEXTURE_2D);

            await gl2.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_MAG_FILTER, (float)TextureParameterValue.LINEAR_MIPMAP_LINEAR);
            await gl2.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_MIN_FILTER, (float)TextureParameterValue.LINEAR_MIPMAP_LINEAR);
        }

        /// <summary>
        /// Free all allocated gpu memory that belong to a frame-buffer object.
        /// </summary>
        /// <param name="bh">The platform dependent abstraction of the gpu buffer handle.</param>
        public async Task DeleteFrameBuffer(IBufferHandle bh)
        {
            await gl2.DeleteFramebufferAsync(((FrameBufferHandle)bh).Handle);
        }

        /// <summary>
        /// Free all allocated gpu memory that belong to a render-buffer object.
        /// </summary>
        /// <param name="bh">The platform dependent abstraction of the gpu buffer handle.</param>
        public async Task DeleteRenderBuffer(IBufferHandle bh)
        {
            await gl2.DeleteRenderbufferAsync(((RenderBufferHandle)bh).Handle);
        }

        /// <summary>
        /// Free all allocated gpu memory that belong to the given <see cref="ITextureHandle"/>.
        /// </summary>
        /// <param name="textureHandle">The <see cref="ITextureHandle"/> which gpu allocated memory will be freed.</param>
        public async Task RemoveTextureHandle(ITextureHandle textureHandle)
        {
            var texHandle = (TextureHandle)textureHandle;

            if (texHandle.FrameBufferHandle != null)
            {
                await gl2.DeleteFramebufferAsync(texHandle.FrameBufferHandle);
            }

            if (texHandle.DepthRenderBufferHandle != null)
            {
                await gl2.DeleteRenderbufferAsync(texHandle.DepthRenderBufferHandle);
            }

            if (texHandle.TexHandle != null)
            {
                await gl2.DeleteTextureAsync(texHandle.TexHandle);
            }
        }

        #endregion

        #region Shader related Members

        /// <summary>
        /// Creates the shader program by using a valid GLSL vertex and fragment shader code. This code is compiled at runtime.
        /// Do not use this function in frequent updates.
        /// </summary>
        /// <param name="vs">The vertex shader code.</param>
        /// <param name="gs">The vertex shader code.</param>
        /// <param name="ps">The pixel(=fragment) shader code.</param>
        /// <returns>An instance of <see cref="IShaderHandle" />.</returns>
        /// <exception cref="ApplicationException">
        /// </exception>
        public IShaderHandle CreateShaderProgram(string vs, string ps, string gs = null)
        {
            return CreateShaderProgramAsync(vs, ps, gs).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates the shader program by using a valid GLSL vertex and fragment shader code. This code is compiled at runtime.
        /// Do not use this function in frequent updates.
        /// </summary>
        /// <param name="vs">The vertex shader code.</param>
        /// <param name="gs">The vertex shader code.</param>
        /// <param name="ps">The pixel(=fragment) shader code.</param>
        /// <returns>An instance of <see cref="IShaderHandle" />.</returns>
        /// <exception cref="ApplicationException">
        /// </exception>
        public async Task<IShaderHandle> CreateShaderProgramAsync(string vs, string ps, string gs = null)
        {
            if (gl2 == null)
                throw new ArgumentNullException("gl2 is NULL! :/");

            // any errors?
            var er = await gl2.GetErrorAsync();
            if (er != Error.NO_ERROR)
            {
                switch (er)
                {
                    case Error.NO_ERROR:
                        break;
                    case Error.INVALID_ENUM:
                        Diagnostics.Error("GL ERROR: INVALID_ENUM");
                        break;
                    case Error.INVALID_VALUE:
                        Diagnostics.Error("GL ERROR: INVALID_VALUE");
                        break;
                    case Error.INVALID_OPERATION:
                        Diagnostics.Error("GL ERROR: INVALID_OPERATION");
                        break;
                    case Error.OUT_OF_MEMORY:
                        Diagnostics.Error("GL ERROR: OUT_OF_MEMORY");
                        break;
                    case Error.CONTEXT_LOST_WEBGL:
                        Diagnostics.Error("GL ERROR: CONTEXT_LOST_WEBGL");
                        break;
                }
            }


            if (gs != null)
                Diagnostics.Warn("WARNING: Geometry Shaders are unsupported");

            if (vs == string.Empty || ps == string.Empty)
            {
                Diagnostics.Error("Pixel or vertex shader empty");
                throw new ArgumentException("Pixel or vertex shader empty");
            }

            var vertexObject = await gl2.CreateShaderAsync(ShaderType.VERTEX_SHADER);
            var fragmentObject = await gl2.CreateShaderAsync(ShaderType.FRAGMENT_SHADER);

            // Compile vertex shader
            await gl2.ShaderSourceAsync(vertexObject, vs);
            await gl2.CompileShaderAsync(vertexObject);

            if (!await gl2.GetShaderParameterAsync<bool>(vertexObject, ShaderParameter.COMPILE_STATUS))
            {
                var info = await gl2.GetShaderInfoLogAsync(vertexObject);
                await gl2.DeleteShaderAsync(vertexObject);
                Diagnostics.Error("An error occured while compiling the vertex shader " + info);
                throw new ArgumentException("An error occured while compiling the vertex shader " + info);
            }

            // Compile pixel shader
            await gl2.ShaderSourceAsync(fragmentObject, ps);
            await gl2.CompileShaderAsync(fragmentObject);

            if (!await gl2.GetShaderParameterAsync<bool>(fragmentObject, ShaderParameter.COMPILE_STATUS))
            {
                var info = await gl2.GetShaderInfoLogAsync(fragmentObject);
                await gl2.DeleteShaderAsync(fragmentObject);
                var error = await gl2.GetErrorAsync();
                Diagnostics.Error("An error occured while compiling the pixel shader " + info);
                Diagnostics.Error("ERROR " + error);
                throw new ArgumentException("An error occured while compiling the pixel shader  " + info);
            }

            var program = await gl2.CreateProgramAsync();
            await gl2.AttachShaderAsync(program, fragmentObject);
            await gl2.AttachShaderAsync(program, vertexObject);

            // enable GLSL (ES) shaders to use fuVertex, fuColor and fuNormal attributes
            await gl2.BindAttribLocationAsync(program, (uint)AttributeLocations.VertexAttribLocation, UniformNameDeclarations.Vertex);
            await gl2.BindAttribLocationAsync(program, (uint)AttributeLocations.ColorAttribLocation, UniformNameDeclarations.Color);
            await gl2.BindAttribLocationAsync(program, (uint)AttributeLocations.UvAttribLocation, UniformNameDeclarations.TextureCoordinates);
            await gl2.BindAttribLocationAsync(program, (uint)AttributeLocations.NormalAttribLocation, UniformNameDeclarations.Normal);
            await gl2.BindAttribLocationAsync(program, (uint)AttributeLocations.TangentAttribLocation, UniformNameDeclarations.TangentAttribName);
            await gl2.BindAttribLocationAsync(program, (uint)AttributeLocations.BoneIndexAttribLocation, UniformNameDeclarations.BoneIndex);
            await gl2.BindAttribLocationAsync(program, (uint)AttributeLocations.BoneWeightAttribLocation, UniformNameDeclarations.BoneWeight);
            await gl2.BindAttribLocationAsync(program, (uint)AttributeLocations.BitangentAttribLocation, UniformNameDeclarations.BitangentAttribName);

            await gl2.LinkProgramAsync(program);

            await gl2.DeleteShaderAsync(vertexObject);
            await gl2.DeleteShaderAsync(fragmentObject);


            if (!await gl2.GetProgramParameterAsync<bool>(program, ProgramParameter.LINK_STATUS))
            {
                var info = await gl2.GetProgramInfoLogAsync(program);
                Diagnostics.Error("An error occured while linking the program: " + info);
                throw new ArgumentException("An error occured while linking the program: " + info);
            }

            // await gl2.DeleteProgramAsync(program);

            Console.WriteLine("Shader created");

            return new ShaderHandleImp { Handle = program };
        }


        /// <inheritdoc />
        /// <summary>
        /// Removes shader from the GPU
        /// </summary>
        /// <param name="sp"></param>
        public async Task RemoveShader(IShaderHandle sp)
        {
            if (sp == null) return;

            var program = ((ShaderHandleImp)sp).Handle;

            // wait for all threads to be finished
            await gl2.FinishAsync();
            await gl2.FlushAsync();

            // cleanup
            // gl2.DeleteShader(program);
            await gl2.DeleteProgramAsync(program);
        }

        /// <summary>
        /// Sets the shader program onto the GL render context.
        /// </summary>
        /// <param name="program">The shader program.</param>
        public async Task SetShader(IShaderHandle program)
        {
            _textureCountPerShader = 0;
            _shaderParam2TexUnit.Clear();

            await gl2.UseProgramAsync(((ShaderHandleImp)program).Handle);
        }

        /// <summary>
        /// Set the width of line primitives.
        /// </summary>
        /// <param name="width"></param>
        public async Task SetLineWidth(float width)
        {
            await gl2.LineWidthAsync(width);
        }

        /// <summary>
        /// Gets the shader parameter.
        /// The Shader parameter is used to bind values inside of shader programs that run on the graphics card.
        /// Do not use this function in frequent updates as it transfers information from graphics card to the cpu which takes time.
        /// </summary>
        /// <param name="shaderProgram">The shader program.</param>
        /// <param name="paramName">Name of the parameter.</param>
        /// <returns>The Shader parameter is returned if the name is found, otherwise null.</returns>
        public IShaderParam GetShaderParam(IShaderHandle shaderProgram, string paramName)
        {
            return GetShaderParamAsync(shaderProgram, paramName).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets the shader parameter.
        /// The Shader parameter is used to bind values inside of shader programs that run on the graphics card.
        /// Do not use this function in frequent updates as it transfers information from graphics card to the cpu which takes time.
        /// </summary>
        /// <param name="shaderProgram">The shader program.</param>
        /// <param name="paramName">Name of the parameter.</param>
        /// <returns>The Shader parameter is returned if the name is found, otherwise null.</returns>
        public async Task<IShaderParam> GetShaderParamAsync(IShaderHandle shaderProgram, string paramName)
        {
            var h = await gl2.GetUniformLocationAsync(((ShaderHandleImp)shaderProgram).Handle, paramName);
            return (h == null) ? null : new ShaderParam { handle = h };
        }

        /// <summary>
        /// Gets the float parameter value inside a shader program by using a <see cref="IShaderParam" /> as search reference.
        /// Do not use this function in frequent updates as it transfers information from the graphics card to the cpu which takes time.
        /// </summary>
        /// <param name="program">The shader program.</param>
        /// <param name="param">The parameter.</param>
        /// <returns>The current parameter's value.</returns>
        public float GetParamValue(IShaderHandle program, IShaderParam param)
        {
            return GetParamValueAsync(program, param).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets the float parameter value inside a shader program by using a <see cref="IShaderParam" /> as search reference.
        /// Do not use this function in frequent updates as it transfers information from the graphics card to the cpu which takes time.
        /// </summary>
        /// <param name="program">The shader program.</param>
        /// <param name="param">The parameter.</param>
        /// <returns>The current parameter's value.</returns>
        public async Task<float> GetParamValueAsync(IShaderHandle program, IShaderParam param)
        {
            var f = await gl2.GetUniformAsync<float>(((ShaderHandleImp)program).Handle, ((ShaderParam)param).handle);
            return f;
        }


        /// <summary>
        /// Gets the shader parameter list of a specific <see cref="IShaderHandle" />. 
        /// </summary>
        /// <param name="shaderProgram">The shader program.</param>
        /// <returns>All Shader parameters of a shader program are returned.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public IList<ShaderParamInfo> GetShaderParamList(IShaderHandle shaderProgram)
        {
            return GetShaderParamListAsync(shaderProgram).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets the shader parameter list of a specific <see cref="IShaderHandle" />. 
        /// </summary>
        /// <param name="shaderProgram">The shader program.</param>
        /// <returns>All Shader parameters of a shader program are returned.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public async Task<IList<ShaderParamInfo>> GetShaderParamListAsync(IShaderHandle shaderProgram)
        {
            var sProg = (ShaderHandleImp)shaderProgram;
            var paramList = new List<ShaderParamInfo>();

            int nParams;
            nParams = await gl2.GetProgramParameterAsync<int>(sProg.Handle, ProgramParameter.ACTIVE_UNIFORMS);

            // Console.WriteLine($"Found program active uniforms: {nParams}");

            for (uint i = 0; i < nParams; i++)
            {
                var activeInfo = await gl2.GetActiveUniformAsync(sProg.Handle, i);

                // Console.WriteLine($"Found active info {activeInfo.Name} {activeInfo.Size}, {activeInfo.Type.ToString()}");

                var paramInfo = new ShaderParamInfo
                {
                    Name = activeInfo.Name,
                    Size = activeInfo.Size
                };

                paramInfo.Handle = await GetShaderParamAsync(sProg, paramInfo.Name);

                // Console.WriteLine($"Found paramInfohandle: {(paramInfo.Handle == null ? "<null>" : paramInfo.Handle.ToString())}");

                switch (activeInfo.Type)
                {
                    //case UniformType.INT:
                    //    paramInfo.Type = typeof(int);
                    //    break;

                    //case FLOAT:
                    //    paramInfo.Type = typeof(float);
                    //    break;

                    case UniformType.FLOAT_VEC2:
                        paramInfo.Type = typeof(float2);
                        break;

                    case UniformType.FLOAT_VEC3:
                        paramInfo.Type = typeof(float3);
                        break;

                    case UniformType.FLOAT_VEC4:
                        paramInfo.Type = typeof(float4);
                        break;

                    case UniformType.FLOAT_MAT4:
                        paramInfo.Type = typeof(float4x4);
                        break;

                    case UniformType.SAMPLER_2D:
                        //case UniformType.UNSIGNED_INT_SAMPLER_2D:
                        //case UniformType.INT_SAMPLER_2D:
                        //case UniformType.SAMPLER_2D_SHADOW:
                        paramInfo.Type = typeof(ITextureBase);
                        break;
                    //case UniformType.SAMPLER_CUBE_SHADOW:
                    case UniformType.SAMPLER_CUBE:
                        paramInfo.Type = typeof(IWritableCubeMap);
                        break;
                    case UniformType.INT_VEC2:
                        break;
                    case UniformType.INT_VEC3:
                        break;
                    case UniformType.INT_VEC4:
                        break;
                    case UniformType.BOOL:
                        break;
                    case UniformType.BOOL_VEC2:
                        break;
                    case UniformType.BOOL_VEC3:
                        break;
                    case UniformType.BOOL_VEC4:
                        break;
                    case UniformType.FLOAT_MAT2:
                        break;
                    case UniformType.FLOAT_MAT3:
                        break;
                    default:
                        paramInfo.Type = typeof(float);
                        break;
                        //throw new ArgumentOutOfRangeException();
                }

                paramList.Add(paramInfo);
            }
            return paramList;
        }

        /// <summary>
        /// Sets a float shader parameter.
        /// </summary>
        /// <param name="param">The parameter.</param>
        /// <param name="val">The value.</param>
        public async Task SetShaderParam(IShaderParam param, float val)
        {
            await gl2.UniformAsync(((ShaderParam)param).handle, val);
        }

        /// <summary>
        /// Sets a <see cref="float2" /> shader parameter.
        /// </summary>
        /// <param name="param">The parameter.</param>
        /// <param name="val">The value.</param>
        public async Task SetShaderParam(IShaderParam param, float2 val)
        {
            await gl2.UniformAsync(((ShaderParam)param).handle, val.x, val.y);
        }

        /// <summary>
        /// Sets a <see cref="float3" /> shader parameter.
        /// </summary>
        /// <param name="param">The parameter.</param>
        /// <param name="val">The value.</param>
        public async Task SetShaderParam(IShaderParam param, float3 val)
        {
            await gl2.UniformAsync(((ShaderParam)param).handle, val.x, val.y, val.z);
        }

        /// <summary>
        /// Sets a <see cref="float3" /> shader parameter.
        /// </summary>
        /// <param name="param">The parameter.</param>
        /// <param name="val">The value.</param>
        public async Task SetShaderParam(IShaderParam param, float3[] val)
        {
            var unpacked = new float[val.Length * 3];
            for (var i = 0; i < val.Length; i += 3)
            {
                unpacked[i] = val[i].x;
                unpacked[i + 1] = val[i].y;
                unpacked[i + 2] = val[i].z;
            }
            await gl2.UniformAsync(((ShaderParam)param).handle, unpacked);
        }

        /// <summary>
        /// Sets a <see cref="float4" /> shader parameter.
        /// </summary>
        /// <param name="param">The parameter.</param>
        /// <param name="val">The value.</param>
        public async Task SetShaderParam(IShaderParam param, float4 val)
        {
            await gl2.UniformAsync(((ShaderParam)param).handle, val.x, val.y, val.z, val.w);
        }


        /// <summary>
        /// Sets a <see cref="float4x4" /> shader parameter.
        /// </summary>
        /// <param name="param">The parameter.</param>
        /// <param name="val">The value.</param>
        public async Task SetShaderParam(IShaderParam param, float4x4 val)
        {
            await gl2.UniformMatrixAsync(((ShaderParam)param).handle, true, val.ToArray());
        }

        /// <summary>
        ///     Sets a <see cref="float4" /> array shader parameter.
        /// </summary>
        /// <param name="param">The parameter.</param>
        /// <param name="val">The value.</param>
        public async Task SetShaderParam(IShaderParam param, float4[] val)
        {
            var unpacked = new float[val.Length * 4];
            for (var i = 0; i < val.Length; i += 4)
            {
                unpacked[i] = val[i].x;
                unpacked[i + 1] = val[i].y;
                unpacked[i + 2] = val[i].z;
                unpacked[i + 3] = val[i].w;
            }

            await gl2.UniformAsync(((ShaderParam)param).handle, unpacked);
        }

        /// <summary>
        /// Sets a <see cref="float4x4" /> array shader parameter.
        /// </summary>
        /// <param name="param">The parameter.</param>
        /// <param name="val">The value.</param>
        public async Task SetShaderParam(IShaderParam param, float4x4[] val)
        {
            var tmpArray = new float4[val.Length * 4];

            for (var i = 0; i < val.Length; i++)
            {
                tmpArray[i * 4] = val[i].Column0;
                tmpArray[i * 4 + 1] = val[i].Column1;
                tmpArray[i * 4 + 2] = val[i].Column2;
                tmpArray[i * 4 + 3] = val[i].Column3;
            }

            var unpacked = new float[tmpArray.Length * 4];
            for (var i = 0; i < tmpArray.Length; i += 4)
            {
                unpacked[i] = tmpArray[i].x;
                unpacked[i + 1] = tmpArray[i].y;
                unpacked[i + 2] = tmpArray[i].z;
                unpacked[i + 3] = tmpArray[i].w;
            }

            await gl2.UniformMatrixAsync(((ShaderParam)param).handle, true, unpacked);
        }

        /// <summary>
        /// Sets a int shader parameter.
        /// </summary>
        /// <param name="param">The parameter.</param>
        /// <param name="val">The value.</param>
        public async Task SetShaderParam(IShaderParam param, int val)
        {
            await gl2.UniformAsync(((ShaderParam)param).handle, val);
        }

        private async Task BindTextureByTarget(ITextureHandle texId, TextureType texTarget)
        {
            switch (texTarget)
            {
                case TextureType.TEXTURE_2D:
                    await gl2.BindTextureAsync(TextureType.TEXTURE_2D, ((TextureHandle)texId).TexHandle);
                    break;
                case TextureType.TEXTURE_CUBE_MAP:
                    await gl2.BindTextureAsync(TextureType.TEXTURE_CUBE_MAP, ((TextureHandle)texId).TexHandle);
                    break;
                default:
                    Diagnostics.Error("OpenTK ES31 does not support Texture1D.");
                    break;
            }
        }

        /// <summary>
        /// Sets a texture active and binds it.
        /// </summary>
        /// <param name="param">The shader parameter, associated with this texture.</param>
        /// <param name="texId">The texture handle.</param>
        /// <param name="texTarget">The texture type, describing to which texture target the texture gets bound to.</param>
        public async Task SetActiveAndBindTexture(IShaderParam param, ITextureHandle texId, TextureType texTarget)
        {
            var iParam = ((ShaderParam)param).handle;
            if (!_shaderParam2TexUnit.TryGetValue(iParam, out var texUnit))
            {
                _textureCountPerShader++;
                texUnit = _textureCountPerShader;
                _shaderParam2TexUnit[iParam] = texUnit;
            }

            await gl2.ActiveTextureAsync(Texture.TEXTURE0 + texUnit);
            BindTextureByTarget(texId, texTarget);
        }

        ///// <summary>
        ///// Sets a texture active and binds it.
        ///// </summary>
        ///// <param name="param">The shader parameter, associated with this texture.</param>
        ///// <param name="texId">The texture handle.</param>
        ///// <param name="texTarget">The texture type, describing to which texture target the texture gets bound to.</param>
        ///// <param name="texUnit">The texture unit.</param>
        //public async Task<int> SetActiveAndBindTexture(IShaderParam param, ITextureHandle texId, TextureType texTarget)
        //{
        //    var iParam = ((ShaderParam)param).handle;
        //    if (!_shaderParam2TexUnit.TryGetValue(iParam, var out texUnit))
        //    {
        //        _textureCountPerShader++;
        //        texUnit = _textureCountPerShader;
        //        _shaderParam2TexUnit[iParam] = texUnit;
        //    }

        //    await gl2.ActiveTextureAsync(Texture.TEXTURE0 + texUnit);
        //    BindTextureByTarget(texId, texTarget);

        //    return texUnit;
        //}

        /// <summary>
        /// Sets a given Shader Parameter to a created texture
        /// </summary>
        /// <param name="param">Shader Parameter used for texture binding</param>
        /// <param name="texIds">An array of ITextureHandles returned from CreateTexture method or the ShaderEffectManager.</param>
        /// /// <param name="texTarget">The texture type, describing to which texture target the texture gets bound to.</param>
        public async Task SetActiveAndBindTextureArray(IShaderParam param, ITextureHandle[] texIds, TextureType texTarget)
        {
            var iParam = ((ShaderParam)param).handle;
            var texUnitArray = new int[texIds.Length];

            if (!_shaderParam2TexUnit.TryGetValue(iParam, out var firstTexUnit))
            {
                _textureCountPerShader++;
                firstTexUnit = _textureCountPerShader;
                _textureCountPerShader += texIds.Length;
                _shaderParam2TexUnit[iParam] = firstTexUnit;
            }

            for (var i = 0; i < texIds.Length; i++)
            {
                texUnitArray[i] = firstTexUnit + i;

                await gl2.ActiveTextureAsync(Texture.TEXTURE0 + firstTexUnit + i);
                BindTextureByTarget(texIds[i], texTarget);
            }
        }

        ///// <summary>
        ///// Sets a texture active and binds it.
        ///// </summary>
        ///// <param name="param">The shader parameter, associated with this texture.</param>
        ///// <param name="texIds">An array of ITextureHandles returned from CreateTexture method or the ShaderEffectManager.</param>
        ///// <param name="texTarget">The texture type, describing to which texture target the texture gets bound to.</param>
        ///// <param name="texUnitArray">The texture units.</param>
        //public async Task SetActiveAndBindTextureArray(IShaderParam param, ITextureHandle[] texIds, TextureType texTarget, out int[] texUnitArray)
        //{
        //    var iParam = ((ShaderParam)param).handle;
        //    texUnitArray = new int[texIds.Length];

        //    if (!_shaderParam2TexUnit.TryGetValue(iParam, out int firstTexUnit))
        //    {
        //        _textureCountPerShader++;
        //        firstTexUnit = _textureCountPerShader;
        //        _textureCountPerShader += texIds.Length;
        //        _shaderParam2TexUnit[iParam] = firstTexUnit;
        //    }

        //    for (int i = 0; i < texIds.Length; i++)
        //    {
        //        texUnitArray[i] = firstTexUnit + i;

        //        await gl2.ActiveTextureAsync(Texture.TEXTURE0 + firstTexUnit + i);
        //        BindTextureByTarget(texIds[i], texTarget);
        //    }
        //}

        /// <summary>
        /// Sets a given Shader Parameter to a created texture
        /// </summary>
        /// <param name="param">Shader Parameter used for texture binding</param>
        /// <param name="texId">An ITextureHandle probably returned from CreateTexture method</param>
        /// <param name="texTarget">The texture type, describing to which texture target the texture gets bound to.</param>
        public async Task SetShaderParamTexture(IShaderParam param, ITextureHandle texId, TextureType texTarget)
        {
            throw new NotImplementedException();
            // var out int is not possible with async!
            //SetActiveAndBindTexture(param, texId, texTarget);
            //await gl2.UniformAsync(((ShaderParam)param).handle, unit);
        }

        /// <summary>
        /// Sets a given Shader Parameter to a created texture
        /// </summary>
        /// <param name="param">Shader Parameter used for texture binding</param>
        /// <param name="texIds">An array of ITextureHandles probably returned from CreateTexture method</param>
        /// <param name="texTarget">The texture type, describing to which texture target the texture gets bound to.</param>
        public async Task SetShaderParamTextureArray(IShaderParam param, ITextureHandle[] texIds, TextureType texTarget)
        {
            throw new NotImplementedException();
            // var out int is not possible with async!
            //await SetActiveAndBindTextureArray(param, texIds, texTarget);
            //await gl2.UniformAsync(((ShaderParam)param).handle, unitArr[0]);
        }

        #endregion

        #region Clear

        /// <summary>
        /// Gets and sets the color of the background.
        /// </summary>
        /// <value>
        /// The color of the clear.
        /// </value>
        [Obsolete("Use GetClearColorAsync() and SetClearColorAsync() instead.")]
        public float4 ClearColor
        {
            get
            {
                var retArr = GetClearColorAsync().GetAwaiter().GetResult();
                //var ret = new float[4];
                //ret[0] = (c & 0xff000000) >> 32;
                //ret[1] = (c & 0x00ff0000) >> 16;
                //ret[2] = (c & 0x0000ff00) >> 8;
                //ret[3] = (c & 0x000000ff) >> 0;
                return new float4(retArr[0], retArr[1], retArr[2], retArr[3]);
            }
            set => SetClearColorAsync(value);
        }

        /// <summary>
        /// Gets the color of the background.
        /// </summary>
        /// <value>
        /// The color of the clear.
        /// </value>
        public async Task<float[]> GetClearColorAsync()
        {
            return await gl2.GetParameterAsync<float[]>(Parameter.COLOR_CLEAR_VALUE);
        }

        /// <summary>
        /// Sets the color of the background.
        /// </summary>
        /// <value>
        /// The color of the clear.
        /// </value>
        public async Task SetClearColorAsync(float4 value)
        {
            await gl2.ClearColorAsync(value.x, value.y, value.z, value.w);
        }

        /// <summary>
        /// Gets and sets the clear depth value which is used to clear the depth buffer.
        /// </summary>
        /// <value>
        /// Specifies the depth value used when the depth buffer is cleared. The initial value is 1. This value is clamped to the range [0,1].
        /// </value>
        public float ClearDepth
        {
            get => GetClearDepthAsync().GetAwaiter().GetResult();
            set => SetClearDepthAsync(value);
        }

        /// <summary>
        /// Gets the color of the background.
        /// </summary>
        /// <value>
        /// The color of the clear.
        /// </value>
        public async Task<float> GetClearDepthAsync()
        {
            return await gl2.GetParameterAsync<float>(Parameter.DEPTH_CLEAR_VALUE);
        }

        /// <summary>
        /// Sets the color of the background.
        /// </summary>
        /// <value>
        /// The color of the clear.
        /// </value>
        public async Task SetClearDepthAsync(float value)
        {
            await gl2.ClearDepthAsync(value);
        }

        /// <summary>
        /// Clears the specified flags.
        /// </summary>
        /// <param name="flags">The flags.</param>
        public async Task Clear(ClearFlags flags)
        {
            // ACCUM is ignored in Webgl2...
            var wglFlags =
                  ((flags & ClearFlags.Depth) != 0 ? BufferBits.DEPTH_BUFFER_BIT : 0)
                | ((flags & ClearFlags.Stencil) != 0 ? BufferBits.STENCIL_BUFFER_BIT : 0)
                | ((flags & ClearFlags.Color) != 0 ? BufferBits.COLOR_BUFFER_BIT : 0);
            await gl2.ClearAsync(wglFlags);
        }

        #endregion

        #region Rendering related Members

        /// <summary>
        /// Only pixels that lie within the scissor box can be modified by drawing commands.
        /// Note that the Scissor test must be enabled for this to work.
        /// </summary>
        /// <param name="x">X Coordinate of the lower left point of the scissor box.</param>
        /// <param name="y">Y Coordinate of the lower left point of the scissor box.</param>
        /// <param name="width">Width of the scissor box.</param>
        /// <param name="height">Height of the scissor box.</param>
        public async Task Scissor(int x, int y, int width, int height)
        {
            await gl2.ScissorAsync(x, y, width, height);
        }

        /// <summary>
        /// The clipping behavior against the Z position of a vertex can be turned off by activating depth clamping. 
        /// This is done with glEnable(GL_DEPTH_CLAMP). This will cause the clip-space Z to remain unclipped by the front and rear viewing volume.
        /// See: https://www.khronos.org/opengl/wiki/Vertex_Post-Processing#Depth_clamping
        /// </summary>
        public void EnableDepthClamp()
        {
            //gl2.Enable(DEPTH_RANGE)
            //throw new NotImplementedException("Depth clamping isn't implemented yet!");
        }

        /// <summary>
        /// Disables depths clamping. <seealso cref="EnableDepthClamp"/>
        /// </summary>
        public void DisableDepthClamp()
        {
            throw new NotImplementedException("Depth clamping isn't implemented yet!");
        }

        /// <summary>
        /// Create one single multi-purpose attribute buffer
        /// </summary>
        /// <param name="attributes"></param>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        public IAttribImp CreateAttributeBuffer(float3[] attributes, string attributeName)
        {
            return CreateAttributeBufferAsync(attributes, attributeName).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Create one single multi-purpose attribute buffer
        /// </summary>
        /// <param name="attributes"></param>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        public async Task<IAttribImp> CreateAttributeBufferAsync(float3[] attributes, string attributeName)
        {
            if (attributes == null || attributes.Length == 0)
            {
                throw new ArgumentException("Vertices must not be null or empty");
            }

            int vboBytes;
            var vertsBytes = attributes.Length * 3 * sizeof(float);
            var handle = gl2.CreateBufferAsync().GetAwaiter().GetResult();

            await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, handle);
            await gl2.BufferDataAsync(BufferType.ARRAY_BUFFER, attributes, BufferUsageHint.STATIC_DRAW);
            vboBytes = await gl2.GetBufferParameterAsync<int>(BufferType.ARRAY_BUFFER, BufferParameter.BUFFER_SIZE);
            if (vboBytes != vertsBytes)
            {
                throw new ApplicationException(string.Format(
                    "Problem uploading attribute buffer to VBO ('{2}'). Tried to upload {0} bytes, uploaded {1}.",
                    vertsBytes, vboBytes, attributeName));
            }

            await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, null);

            return new AttributeImp { AttributeBufferObject = handle };
        }

        /// <summary>
        /// Remove an attribute buffer previously created with <see cref="CreateAttributeBuffer"/> and release all associated resources
        /// allocated on the GPU.
        /// </summary>
        /// <param name="attribHandle">The attribute handle.</param>
        public async Task DeleteAttributeBuffer(IAttribImp attribHandle)
        {
            if (attribHandle != null)
            {
                var handle = ((AttributeImp)attribHandle).AttributeBufferObject;
                if (handle != null)
                {
                    await gl2.DeleteBufferAsync(handle);
                    ((AttributeImp)attribHandle).AttributeBufferObject = null;
                }
            }
        }

        /// <summary>
        /// Binds the vertices onto the GL render context and assigns an VertexBuffer index to the passed <see cref="IMeshImp" /> instance.
        /// </summary>
        /// <param name="mr">The <see cref="IMeshImp" /> instance.</param>
        /// <param name="vertices">The vertices.</param>
        /// <exception cref="ArgumentException">Vertices must not be null or empty</exception>
        /// <exception cref="ApplicationException"></exception>
        public async Task SetVertices(IMeshImp mr, float3[] vertices)
        {
            Diagnostics.Debug("[SetVertices]");
            if (vertices == null || vertices.Length == 0)
            {
                throw new ArgumentException("Vertices must not be null or empty");
            }

            int vboBytes;
            var vertsBytes = vertices.Length * 3 * sizeof(float);
            if (((MeshImp)mr).VertexBufferObject == null)
                ((MeshImp)mr).VertexBufferObject = await gl2.CreateBufferAsync();

            await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, ((MeshImp)mr).VertexBufferObject);
            Diagnostics.Debug("[Before gl2.BufferData]");

            var verticesFlat = new float[vertices.Length * 3];
            unsafe
            {
                fixed (float3* pBytes = &vertices[0])
                {
                    Marshal.Copy((IntPtr)(pBytes), verticesFlat, 0, verticesFlat.Length);
                }
            }
            await gl2.BufferDataAsync(BufferType.ARRAY_BUFFER, verticesFlat, BufferUsageHint.STATIC_DRAW);

            Diagnostics.Debug("[After gl2.BufferData]");
            vboBytes = await gl2.GetBufferParameterAsync<int>(BufferType.ARRAY_BUFFER, BufferParameter.BUFFER_SIZE);
            if (vboBytes != vertsBytes)
                throw new ApplicationException(string.Format("Problem uploading vertex buffer to VBO (vertices). Tried to upload {0} bytes, uploaded {1}.", vertsBytes, vboBytes));

        }

        /// <summary>
        /// Binds the tangents onto the GL render context and assigns an TangentBuffer index to the passed <see cref="IMeshImp" /> instance.
        /// </summary>
        /// <param name="mr">The <see cref="IMeshImp" /> instance.</param>
        /// <param name="tangents">The tangents.</param>
        /// <exception cref="ArgumentException">Tangents must not be null or empty</exception>
        /// <exception cref="ApplicationException"></exception>
        public async Task SetTangents(IMeshImp mr, float4[] tangents)
        {
            if (tangents == null || tangents.Length == 0)
            {
                throw new ArgumentException("Tangents must not be null or empty");
            }

            int vboBytes;
            var tangentBytes = tangents.Length * 4 * sizeof(float);
            if (((MeshImp)mr).TangentBufferObject == null)
                ((MeshImp)mr).TangentBufferObject = await gl2.CreateBufferAsync(); ;

            await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, ((MeshImp)mr).TangentBufferObject);

            var tangentsFlat = new float[tangents.Length * 4];
            unsafe
            {
                fixed (float4* pBytes = &tangents[0])
                {
                    Marshal.Copy((IntPtr)(pBytes), tangentsFlat, 0, tangentsFlat.Length);
                }
            }

            await gl2.BufferDataAsync(BufferType.ARRAY_BUFFER, tangentsFlat, BufferUsageHint.STATIC_DRAW);
            vboBytes = await gl2.GetBufferParameterAsync<int>(BufferType.ARRAY_BUFFER, BufferParameter.BUFFER_SIZE);
            if (vboBytes != tangentBytes)
                throw new ApplicationException(string.Format("Problem uploading vertex buffer to VBO (tangents). Tried to upload {0} bytes, uploaded {1}.", tangentBytes, vboBytes));

        }

        /// <summary>
        /// Binds the bitangents onto the GL render context and assigns an BiTangentBuffer index to the passed <see cref="IMeshImp" /> instance.
        /// </summary>
        /// <param name="mr">The <see cref="IMeshImp" /> instance.</param>
        /// <param name="bitangents">The BiTangents.</param>
        /// <exception cref="ArgumentException">BiTangents must not be null or empty</exception>
        /// <exception cref="ApplicationException"></exception>
        public async Task SetBiTangents(IMeshImp mr, float3[] bitangents)
        {
            if (bitangents == null || bitangents.Length == 0)
            {
                throw new ArgumentException("BiTangents must not be null or empty");
            }

            int vboBytes;
            var bitangentBytes = bitangents.Length * 3 * sizeof(float);
            if (((MeshImp)mr).BitangentBufferObject == null)
                ((MeshImp)mr).BitangentBufferObject = await gl2.CreateBufferAsync();

            await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, ((MeshImp)mr).BitangentBufferObject);

            var bitangentsFlat = new float[bitangents.Length * 3];
            unsafe
            {
                fixed (float3* pBytes = &bitangents[0])
                {
                    Marshal.Copy((IntPtr)(pBytes), bitangentsFlat, 0, bitangentsFlat.Length);
                }
            }

            await gl2.BufferDataAsync(BufferType.ARRAY_BUFFER, bitangentsFlat, BufferUsageHint.STATIC_DRAW);
            vboBytes = await gl2.GetBufferParameterAsync<int>(BufferType.ARRAY_BUFFER, BufferParameter.BUFFER_SIZE);
            if (vboBytes != bitangentBytes)
                throw new ApplicationException(string.Format("Problem uploading vertex buffer to VBO (bitangents). Tried to upload {0} bytes, uploaded {1}.", bitangentBytes, vboBytes));
        }

        /// <summary>
        /// Binds the normals onto the GL render context and assigns an NormalBuffer index to the passed <see cref="IMeshImp" /> instance.
        /// </summary>
        /// <param name="mr">The <see cref="IMeshImp" /> instance.</param>
        /// <param name="normals">The normals.</param>
        /// <exception cref="ArgumentException">Normals must not be null or empty</exception>
        /// <exception cref="ApplicationException"></exception>
        public async Task SetNormals(IMeshImp mr, float3[] normals)
        {
            if (normals == null || normals.Length == 0)
            {
                throw new ArgumentException("Normals must not be null or empty");
            }

            int vboBytes;
            var normsBytes = normals.Length * 3 * sizeof(float);
            if (((MeshImp)mr).NormalBufferObject == null)
                ((MeshImp)mr).NormalBufferObject = await gl2.CreateBufferAsync();

            await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, ((MeshImp)mr).NormalBufferObject);

            var normalsFlat = new float[normals.Length * 3];
            unsafe
            {
                fixed (float3* pBytes = &normals[0])
                {
                    Marshal.Copy((IntPtr)(pBytes), normalsFlat, 0, normalsFlat.Length);
                }
            }

            await gl2.BufferDataAsync(BufferType.ARRAY_BUFFER, normalsFlat, BufferUsageHint.STATIC_DRAW);

            vboBytes = await gl2.GetBufferParameterAsync<int>(BufferType.ARRAY_BUFFER, BufferParameter.BUFFER_SIZE);
            if (vboBytes != normsBytes)
                throw new ApplicationException(string.Format("Problem uploading normal buffer to VBO (normals). Tried to upload {0} bytes, uploaded {1}.", normsBytes, vboBytes));
        }

        /// <summary>
        /// Binds the bone indices onto the GL render context and assigns an BondeIndexBuffer index to the passed <see cref="IMeshImp" /> instance.
        /// </summary>
        /// <param name="mr">The <see cref="IMeshImp" /> instance.</param>
        /// <param name="boneIndices">The bone indices.</param>
        /// <exception cref="ArgumentException">BoneIndices must not be null or empty</exception>
        /// <exception cref="ApplicationException"></exception>
        public async Task SetBoneIndices(IMeshImp mr, float4[] boneIndices)
        {
            if (boneIndices == null || boneIndices.Length == 0)
            {
                throw new ArgumentException("BoneIndices must not be null or empty");
            }

            int vboBytes;
            var indicesBytes = boneIndices.Length * 4 * sizeof(float);
            if (((MeshImp)mr).BoneIndexBufferObject == null)
                ((MeshImp)mr).BoneIndexBufferObject = gl2.CreateBuffer();

            await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, ((MeshImp)mr).BoneIndexBufferObject);

            var boneIndicesFlat = new float[boneIndices.Length * 4];
            unsafe
            {
                fixed (float4* pBytes = &boneIndices[0])
                {
                    Marshal.Copy((IntPtr)(pBytes), boneIndicesFlat, 0, boneIndicesFlat.Length);
                }
            }

            await gl2.BufferDataAsync(BufferType.ARRAY_BUFFER, boneIndicesFlat, BufferUsageHint.STATIC_DRAW);
            vboBytes = await gl2.GetBufferParameterAsync<int>(BufferType.ARRAY_BUFFER, BufferParameter.BUFFER_SIZE);
            if (vboBytes != indicesBytes)
                throw new ApplicationException(string.Format("Problem uploading bone indices buffer to VBO (bone indices). Tried to upload {0} bytes, uploaded {1}.", indicesBytes, vboBytes));
        }

        /// <summary>
        /// Binds the bone weights onto the GL render context and assigns an BondeWeightBuffer index to the passed <see cref="IMeshImp" /> instance.
        /// </summary>
        /// <param name="mr">The <see cref="IMeshImp" /> instance.</param>
        /// <param name="boneWeights">The bone weights.</param>
        /// <exception cref="ArgumentException">BoneWeights must not be null or empty</exception>
        /// <exception cref="ApplicationException"></exception>
        public async Task SetBoneWeights(IMeshImp mr, float4[] boneWeights)
        {
            if (boneWeights == null || boneWeights.Length == 0)
            {
                throw new ArgumentException("BoneWeights must not be null or empty");
            }

            int vboBytes;
            var weightsBytes = boneWeights.Length * 4 * sizeof(float);
            if (((MeshImp)mr).BoneWeightBufferObject == null)
                ((MeshImp)mr).BoneWeightBufferObject = await gl2.CreateBufferAsync();

            await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, ((MeshImp)mr).BoneWeightBufferObject);

            var boneWeightsFlat = new float[boneWeights.Length * 4];
            unsafe
            {
                fixed (float4* pBytes = &boneWeights[0])
                {
                    Marshal.Copy((IntPtr)(pBytes), boneWeightsFlat, 0, boneWeightsFlat.Length);
                }
            }

            await gl2.BufferDataAsync(BufferType.ARRAY_BUFFER, boneWeightsFlat, BufferUsageHint.STATIC_DRAW);
            vboBytes = await gl2.GetBufferParameterAsync<int>(BufferType.ARRAY_BUFFER, BufferParameter.BUFFER_SIZE);
            if (vboBytes != weightsBytes)
                throw new ApplicationException(string.Format("Problem uploading bone weights buffer to VBO (bone weights). Tried to upload {0} bytes, uploaded {1}.", weightsBytes, vboBytes));

        }

        /// <summary>
        /// Binds the UV coordinates onto the GL render context and assigns an UVBuffer index to the passed <see cref="IMeshImp" /> instance.
        /// </summary>
        /// <param name="mr">The <see cref="IMeshImp" /> instance.</param>
        /// <param name="uvs">The UV's.</param>
        /// <exception cref="ArgumentException">UVs must not be null or empty</exception>
        /// <exception cref="ApplicationException"></exception>
        public async Task SetUVs(IMeshImp mr, float2[] uvs)
        {
            if (uvs == null || uvs.Length == 0)
            {
                throw new ArgumentException("UVs must not be null or empty");
            }

            int vboBytes;
            var uvsBytes = uvs.Length * 2 * sizeof(float);
            if (((MeshImp)mr).UVBufferObject == null)
                ((MeshImp)mr).UVBufferObject = await gl2.CreateBufferAsync();

            await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, ((MeshImp)mr).UVBufferObject);

            var uvsFlat = new float[uvs.Length * 2];
            unsafe
            {
                fixed (float2* pBytes = &uvs[0])
                {
                    Marshal.Copy((IntPtr)(pBytes), uvsFlat, 0, uvsFlat.Length);
                }
            }

            await gl2.BufferDataAsync(BufferType.ARRAY_BUFFER, uvsFlat, BufferUsageHint.STATIC_DRAW);
            vboBytes = await gl2.GetBufferParameterAsync<int>(BufferType.ARRAY_BUFFER, BufferParameter.BUFFER_SIZE);
            if (vboBytes != uvsBytes)
                throw new ApplicationException(string.Format("Problem uploading uv buffer to VBO (uvs). Tried to upload {0} bytes, uploaded {1}.", uvsBytes, vboBytes));

        }

        /// <summary>
        /// Binds the colors onto the GL render context and assigns an ColorBuffer index to the passed <see cref="IMeshImp" /> instance.
        /// </summary>
        /// <param name="mr">The <see cref="IMeshImp" /> instance.</param>
        /// <param name="colors">The colors.</param>
        /// <exception cref="ArgumentException">colors must not be null or empty</exception>
        /// <exception cref="ApplicationException"></exception>
        public async Task SetColors(IMeshImp mr, uint[] colors)
        {
            if (colors == null || colors.Length == 0)
            {
                throw new ArgumentException("colors must not be null or empty");
            }

            int vboBytes;
            var colsBytes = colors.Length * sizeof(uint);
            if (((MeshImp)mr).ColorBufferObject == null)
                ((MeshImp)mr).ColorBufferObject = await gl2.CreateBufferAsync();

            await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, ((MeshImp)mr).ColorBufferObject);
            await gl2.BufferDataAsync(BufferType.ARRAY_BUFFER, colors, BufferUsageHint.STATIC_DRAW);
            vboBytes = await gl2.GetBufferParameterAsync<int>(BufferType.ARRAY_BUFFER, BufferParameter.BUFFER_SIZE);
            if (vboBytes != colsBytes)
                throw new ApplicationException(string.Format("Problem uploading color buffer to VBO (colors). Tried to upload {0} bytes, uploaded {1}.", colsBytes, vboBytes));
        }

        /// <summary>
        /// Binds the triangles onto the GL render context and assigns an ElementBuffer index to the passed <see cref="IMeshImp" /> instance.
        /// </summary>
        /// <param name="mr">The <see cref="IMeshImp" /> instance.</param>
        /// <param name="triangleIndices">The triangle indices.</param>
        /// <exception cref="ArgumentException">triangleIndices must not be null or empty</exception>
        /// <exception cref="ApplicationException"></exception>
        public async Task SetTriangles(IMeshImp mr, ushort[] triangleIndices)
        {
            if (triangleIndices == null || triangleIndices.Length == 0)
            {
                throw new ArgumentException("triangleIndices must not be null or empty");
            }
            ((MeshImp)mr).NElements = triangleIndices.Length;
            int vboBytes;
            var trisBytes = triangleIndices.Length * sizeof(short);

            if (((MeshImp)mr).ElementBufferObject == null)
                ((MeshImp)mr).ElementBufferObject = await gl2.CreateBufferAsync();
            // Upload the index buffer (elements inside the vertex buffer, not color indices as per the IndexPointer function!)
            await gl2.BindBufferAsync(BufferType.ELEMENT_ARRAY_BUFFER, ((MeshImp)mr).ElementBufferObject);
            await gl2.BufferDataAsync(BufferType.ELEMENT_ARRAY_BUFFER, triangleIndices, BufferUsageHint.STATIC_DRAW);
            vboBytes = await gl2.GetBufferParameterAsync<int>(BufferType.ELEMENT_ARRAY_BUFFER, BufferParameter.BUFFER_SIZE);
            if (vboBytes != trisBytes)
                throw new ApplicationException(string.Format("Problem uploading vertex buffer to VBO (offsets). Tried to upload {0} bytes, uploaded {1}.", trisBytes, vboBytes));

        }

        /// <summary>
        /// Deletes the buffer associated with the mesh implementation.
        /// </summary>
        /// <param name="mr">The mesh which buffer respectively GPU memory should be deleted.</param>
        public async Task RemoveVertices(IMeshImp mr)
        {
            await gl2.DeleteBufferAsync(((MeshImp)mr).VertexBufferObject);
            ((MeshImp)mr).InvalidateVertices();
        }

        /// <summary>
        /// Deletes the buffer associated with the mesh implementation.
        /// </summary>
        /// <param name="mr">The mesh which buffer respectively GPU memory should be deleted.</param>
        public async Task RemoveNormals(IMeshImp mr)
        {
            await gl2.DeleteBufferAsync(((MeshImp)mr).NormalBufferObject);
            ((MeshImp)mr).InvalidateNormals();
        }

        /// <summary>
        /// Deletes the buffer associated with the mesh implementation.
        /// </summary>
        /// <param name="mr">The mesh which buffer respectively GPU memory should be deleted.</param>
        public async Task RemoveColors(IMeshImp mr)
        {
            await gl2.DeleteBufferAsync(((MeshImp)mr).ColorBufferObject);
            ((MeshImp)mr).InvalidateColors();
        }

        /// <summary>
        /// Deletes the buffer associated with the mesh implementation.
        /// </summary>
        /// <param name="mr">The mesh which buffer respectively GPU memory should be deleted.</param>
        public async Task RemoveUVs(IMeshImp mr)
        {
            await gl2.DeleteBufferAsync(((MeshImp)mr).UVBufferObject);
            ((MeshImp)mr).InvalidateUVs();
        }

        /// <summary>
        /// Deletes the buffer associated with the mesh implementation.
        /// </summary>
        /// <param name="mr">The mesh which buffer respectively GPU memory should be deleted.</param>
        public async Task RemoveTriangles(IMeshImp mr)
        {
            await gl2.DeleteBufferAsync(((MeshImp)mr).ElementBufferObject);
            ((MeshImp)mr).InvalidateTriangles();
        }

        /// <summary>
        /// Deletes the buffer associated with the mesh implementation.
        /// </summary>
        /// <param name="mr">The mesh which buffer respectively GPU memory should be deleted.</param>
        public async Task RemoveBoneWeights(IMeshImp mr)
        {
            await gl2.DeleteBufferAsync(((MeshImp)mr).BoneWeightBufferObject);
            ((MeshImp)mr).InvalidateBoneWeights();
        }

        /// <summary>
        /// Deletes the buffer associated with the mesh implementation.
        /// </summary>
        /// <param name="mr">The mesh which buffer respectively GPU memory should be deleted.</param>
        public async Task RemoveBoneIndices(IMeshImp mr)
        {
            await gl2.DeleteBufferAsync(((MeshImp)mr).BoneIndexBufferObject);
            ((MeshImp)mr).InvalidateBoneIndices();
        }

        /// <summary>
        /// Deletes the buffer associated with the mesh implementation.
        /// </summary>
        /// <param name="mr">The mesh which buffer respectively GPU memory should be deleted.</param>
        public async Task RemoveTangents(IMeshImp mr)
        {
            await gl2.DeleteBufferAsync(((MeshImp)mr).TangentBufferObject);
            ((MeshImp)mr).InvalidateTangents();
        }

        /// <summary>
        /// Deletes the buffer associated with the mesh implementation.
        /// </summary>
        /// <param name="mr">The mesh which buffer respectively GPU memory should be deleted.</param>
        public async Task RemoveBiTangents(IMeshImp mr)
        {
            await gl2.DeleteBufferAsync(((MeshImp)mr).BitangentBufferObject);
            ((MeshImp)mr).InvalidateBiTangents();
        }
        /// <summary>
        /// Renders the specified <see cref="IMeshImp" />.
        /// </summary>
        /// <param name="mr">The <see cref="IMeshImp" /> instance.</param>
        public async Task Render(IMeshImp mr)
        {
            Console.WriteLine("Render mesh called");
            Console.Out.Flush();

            if (((MeshImp)mr).VertexBufferObject != null)
            {
                await gl2.EnableVertexAttribArrayAsync((uint)AttributeLocations.VertexAttribLocation);
                await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, ((MeshImp)mr).VertexBufferObject);
                await gl2.VertexAttribPointerAsync((uint)AttributeLocations.VertexAttribLocation, 3, DataType.FLOAT, false, 0, 0);
            }
            if (((MeshImp)mr).ColorBufferObject != null)
            {
                await gl2.EnableVertexAttribArrayAsync((uint)AttributeLocations.ColorAttribLocation);
                await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, ((MeshImp)mr).ColorBufferObject);
                await gl2.VertexAttribPointerAsync((uint)AttributeLocations.ColorAttribLocation, 4, DataType.UNSIGNED_BYTE, true, 0, 0);
            }

            if (((MeshImp)mr).UVBufferObject != null)
            {
                await gl2.EnableVertexAttribArrayAsync((uint)AttributeLocations.UvAttribLocation);
                await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, ((MeshImp)mr).UVBufferObject);
                await gl2.VertexAttribPointerAsync((uint)AttributeLocations.UvAttribLocation, 2, DataType.FLOAT, false, 0, 0);
            }
            if (((MeshImp)mr).NormalBufferObject != null)
            {
                await gl2.EnableVertexAttribArrayAsync((uint)AttributeLocations.NormalAttribLocation);
                await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, ((MeshImp)mr).NormalBufferObject);
                await gl2.VertexAttribPointerAsync((uint)AttributeLocations.NormalAttribLocation, 3, DataType.FLOAT, false, 0, 0);
            }
            if (((MeshImp)mr).TangentBufferObject != null)
            {
                await gl2.EnableVertexAttribArrayAsync((uint)AttributeLocations.TangentAttribLocation);
                await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, ((MeshImp)mr).TangentBufferObject);
                await gl2.VertexAttribPointerAsync((uint)AttributeLocations.TangentAttribLocation, 3, DataType.FLOAT, false, 0, 0);
            }
            if (((MeshImp)mr).BitangentBufferObject != null)
            {
                await gl2.EnableVertexAttribArrayAsync((uint)AttributeLocations.BitangentAttribLocation);
                await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, ((MeshImp)mr).BitangentBufferObject);
                await gl2.VertexAttribPointerAsync((uint)AttributeLocations.BitangentAttribLocation, 3, DataType.FLOAT, false, 0, 0);
            }
            if (((MeshImp)mr).BoneIndexBufferObject != null)
            {
                await gl2.EnableVertexAttribArrayAsync((uint)AttributeLocations.BoneIndexAttribLocation);
                await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, ((MeshImp)mr).BoneIndexBufferObject);
                await gl2.VertexAttribPointerAsync((uint)AttributeLocations.BoneIndexAttribLocation, 4, DataType.FLOAT, false, 0, 0);
            }
            if (((MeshImp)mr).BoneWeightBufferObject != null)
            {
                await gl2.EnableVertexAttribArrayAsync((uint)AttributeLocations.BoneWeightAttribLocation);
                await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, ((MeshImp)mr).BoneWeightBufferObject);
                await gl2.VertexAttribPointerAsync((uint)AttributeLocations.BoneWeightAttribLocation, 4, DataType.FLOAT, false, 0, 0);
            }
            if (((MeshImp)mr).ElementBufferObject != null)
            {
                await gl2.BindBufferAsync(BufferType.ELEMENT_ARRAY_BUFFER, ((MeshImp)mr).ElementBufferObject);
                await gl2.DrawElementsAsync(Primitive.TRIANGLES, ((MeshImp)mr).NElements, DataType.UNSIGNED_SHORT, 0);
                //gl2.DrawArrays(gl2.Enums.BeginMode.POINTS, 0, shape.Vertices.Length);
            }
            if (((MeshImp)mr).ElementBufferObject != null)
            {
                await gl2.BindBufferAsync(BufferType.ELEMENT_ARRAY_BUFFER, ((MeshImp)mr).ElementBufferObject);

                switch (((MeshImp)mr).MeshType)
                {
                    case OpenGLPrimitiveType.TRIANGLES:
                    default:
                        await gl2.DrawElementsAsync(Primitive.TRIANGLES, ((MeshImp)mr).NElements, DataType.UNSIGNED_SHORT, 0);
                        break;
                    case OpenGLPrimitiveType.POINT:
                        await gl2.DrawElementsAsync(Primitive.POINTS, ((MeshImp)mr).NElements, DataType.UNSIGNED_SHORT, 0);
                        break;
                    case OpenGLPrimitiveType.LINES:
                        await gl2.DrawElementsAsync(Primitive.LINES, ((MeshImp)mr).NElements, DataType.UNSIGNED_SHORT, 0);
                        break;
                    case OpenGLPrimitiveType.LINE_LOOP:
                        await gl2.DrawElementsAsync(Primitive.LINE_LOOP, ((MeshImp)mr).NElements, DataType.UNSIGNED_SHORT, 0);
                        break;
                    case OpenGLPrimitiveType.LINE_STRIP:
                        await gl2.DrawElementsAsync(Primitive.LINE_STRIP, ((MeshImp)mr).NElements, DataType.UNSIGNED_SHORT, 0);
                        break;
                    case OpenGLPrimitiveType.PATCHES:
                        await gl2.DrawElementsAsync(Primitive.TRIANGLES, ((MeshImp)mr).NElements, DataType.UNSIGNED_SHORT, 0);
                        Diagnostics.Warn("Mesh type set to triangles due to unavailability of PATCHES");
                        break;
                    case OpenGLPrimitiveType.QUAD_STRIP:
                        await gl2.DrawElementsAsync(Primitive.TRIANGLES, ((MeshImp)mr).NElements, DataType.UNSIGNED_SHORT, 0);
                        Diagnostics.Warn("Mesh type set to triangles due to unavailability of QUAD_STRIP");
                        break;
                    case OpenGLPrimitiveType.TRIANGLE_FAN:
                        await gl2.DrawElementsAsync(Primitive.TRIANGLE_FAN, ((MeshImp)mr).NElements, DataType.UNSIGNED_SHORT, 0);
                        break;
                    case OpenGLPrimitiveType.TRIANGLE_STRIP:
                        await gl2.DrawElementsAsync(Primitive.TRIANGLES, ((MeshImp)mr).NElements, DataType.UNSIGNED_SHORT, 0);
                        Diagnostics.Warn("Mesh type set to triangles due to unavailability of TRIANGLE_STRIP");
                        break;
                }
            }


            if (((MeshImp)mr).VertexBufferObject != null)
            {
                await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, null);
                await gl2.DisableVertexAttribArrayAsync((uint)AttributeLocations.VertexAttribLocation);
            }
            if (((MeshImp)mr).ColorBufferObject != null)
            {
                await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, null);
                await gl2.DisableVertexAttribArrayAsync((uint)AttributeLocations.ColorAttribLocation);
            }
            if (((MeshImp)mr).NormalBufferObject != null)
            {
                await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, null);
                await gl2.DisableVertexAttribArrayAsync((uint)AttributeLocations.NormalAttribLocation);
            }
            if (((MeshImp)mr).UVBufferObject != null)
            {
                await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, null);
                await gl2.DisableVertexAttribArrayAsync((uint)AttributeLocations.UvAttribLocation);
            }
            if (((MeshImp)mr).TangentBufferObject != null)
            {
                await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, null);
                await gl2.DisableVertexAttribArrayAsync((uint)AttributeLocations.TangentAttribLocation);
            }
            if (((MeshImp)mr).BitangentBufferObject != null)
            {
                await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, null);
                await gl2.DisableVertexAttribArrayAsync((uint)AttributeLocations.TangentAttribLocation);
            }
        }

        /// <summary>
        /// Gets the content of the buffer.
        /// </summary>
        /// <param name="quad">The Rectangle where the content is draw into.</param>
        /// <param name="texId">The tex identifier.</param>
        public async Task GetBufferContent(Rectangle quad, ITextureHandle texId)
        {
            await gl2.BindTextureAsync(TextureType.TEXTURE_2D, ((TextureHandle)texId).TexHandle);
            await gl2.CopyTexImage2DAsync(Texture2DType.TEXTURE_2D, 0, PixelFormat.RGBA, quad.Left, quad.Top, quad.Width, quad.Height, 0);
        }

        /// <summary>
        /// Creates the mesh implementation.
        /// </summary>
        /// <returns>The <see cref="IMeshImp" /> instance.</returns>
        public IMeshImp CreateMeshImp()
        {
            return CreateMeshImpAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates the mesh implementation.
        /// </summary>
        /// <returns>The <see cref="IMeshImp" /> instance.</returns>
        public async Task<IMeshImp> CreateMeshImpAsync()
        {
            return await Task.FromResult(new MeshImp());
        }

        internal static uint BlendOperationToOgl(BlendOperation bo)
        {
            switch (bo)
            {
                case BlendOperation.Add:
                    return (uint)BlendingEquation.FUNC_ADD;
                case BlendOperation.Subtract:
                    return (uint)BlendingEquation.FUNC_SUBTRACT;
                case BlendOperation.ReverseSubtract:
                    return (uint)BlendingEquation.FUNC_REVERSE_SUBTRACT;
                case BlendOperation.Minimum:
                    throw new NotSupportedException("MIN blending mode not supported in WebGL!");
                case BlendOperation.Maximum:
                    throw new NotSupportedException("MAX blending mode not supported in WebGL!");
                default:
                    throw new ArgumentOutOfRangeException("bo");
            }
        }

        internal static BlendOperation BlendOperationFromOgl(uint bom)
        {
            switch (bom)
            {
                case (uint)BlendingEquation.FUNC_ADD:
                    return BlendOperation.Add;
                case (uint)BlendingEquation.FUNC_SUBTRACT:
                    return BlendOperation.Subtract;
                case (uint)BlendingEquation.FUNC_REVERSE_SUBTRACT:
                    return BlendOperation.ReverseSubtract;
                default:
                    throw new ArgumentOutOfRangeException("bom");
            }
        }

        internal static uint BlendToOgl(Blend blend, bool isForAlpha = false)
        {
            switch (blend)
            {
                case Blend.Zero:
                    return (uint)BlendingMode.ZERO;
                case Blend.One:
                    return (uint)BlendingMode.ONE;
                case Blend.SourceColor:
                    return (uint)BlendingMode.SRC_COLOR;
                case Blend.InverseSourceColor:
                    return (uint)BlendingMode.ONE_MINUS_SRC_COLOR;
                case Blend.SourceAlpha:
                    return (uint)BlendingMode.SRC_ALPHA;
                case Blend.InverseSourceAlpha:
                    return (uint)BlendingMode.ONE_MINUS_SRC_ALPHA;
                case Blend.DestinationAlpha:
                    return (uint)BlendingMode.DST_ALPHA;
                case Blend.InverseDestinationAlpha:
                    return (uint)BlendingMode.ONE_MINUS_DST_ALPHA;
                case Blend.DestinationColor:
                    return (uint)BlendingMode.DST_COLOR;
                case Blend.InverseDestinationColor:
                    return (uint)BlendingMode.ONE_MINUS_DST_COLOR;
                case Blend.BlendFactor:
                    return (uint)((isForAlpha) ? BlendingMode.CONSTANT_ALPHA : BlendingMode.CONSTANT_COLOR);
                case Blend.InverseBlendFactor:
                    return (uint)((isForAlpha) ? BlendingMode.ONE_MINUS_CONSTANT_ALPHA : BlendingMode.ONE_MINUS_CONSTANT_COLOR);
                // Ignored...
                // case Blend.SourceAlphaSaturated:
                //     break;
                //case Blend.Bothsrcalpha:
                //    break;
                //case Blend.BothInverseSourceAlpha:
                //    break;
                //case Blend.SourceColor2:
                //    break;
                //case Blend.InverseSourceColor2:
                //    break;
                default:
                    throw new ArgumentOutOfRangeException("blend");
            }
        }

        internal static Blend BlendFromOgl(uint bf)
        {
            switch (bf)
            {
                case (uint)BlendingMode.ZERO:
                    return Blend.Zero;
                case (uint)BlendingMode.ONE:
                    return Blend.One;
                case (uint)BlendingMode.SRC_COLOR:
                    return Blend.SourceColor;
                case (uint)BlendingMode.ONE_MINUS_SRC_COLOR:
                    return Blend.InverseSourceColor;
                case (uint)BlendingMode.SRC_ALPHA:
                    return Blend.SourceAlpha;
                case (uint)BlendingMode.ONE_MINUS_SRC_ALPHA:
                    return Blend.InverseSourceAlpha;
                case (uint)BlendingMode.DST_ALPHA:
                    return Blend.DestinationAlpha;
                case (uint)BlendingMode.ONE_MINUS_DST_ALPHA:
                    return Blend.InverseDestinationAlpha;
                case (uint)BlendingMode.DST_COLOR:
                    return Blend.DestinationColor;
                case (uint)BlendingMode.ONE_MINUS_DST_COLOR:
                    return Blend.InverseDestinationColor;
                case (uint)BlendingMode.CONSTANT_COLOR:
                case (uint)BlendingMode.CONSTANT_ALPHA:
                    return Blend.BlendFactor;
                case (uint)BlendingMode.ONE_MINUS_CONSTANT_COLOR:
                case (uint)BlendingMode.ONE_MINUS_CONSTANT_ALPHA:
                    return Blend.InverseBlendFactor;
                default:
                    throw new ArgumentOutOfRangeException("blend");
            }
        }

        /// <summary>
        /// Sets the RenderState object onto the current OpenGL based RenderContext.
        /// </summary>
        /// <param name="renderState">State of the render(enum).</param>
        /// <param name="value">The value. See <see cref="RenderState"/> for detailed information. </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// value
        /// or
        /// value
        /// or
        /// value
        /// or
        /// renderState
        /// </exception>
        public async Task SetRenderState(RenderState renderState, uint value)
        {

            await gl2.EnableAsync(EnableCap.SCISSOR_TEST);

            switch (renderState)
            {
                case RenderState.FillMode:
                    {
                        if (value != (uint)FillMode.Solid)
                            throw new NotSupportedException("Line or Point fill mode (glPolygonMode) not supported in WebGL!");
                    }
                    break;
                case RenderState.CullMode:
                    {
                        switch ((Cull)value)
                        {
                            case Cull.None:
                                //gl2.FrontFace(FrontFaceDirection.NONE);
                                await gl2.DisableAsync(EnableCap.CULL_FACE);
                                if (_isCullEnabled)
                                {
                                    _isCullEnabled = false;
                                    await gl2.DisableAsync(EnableCap.CULL_FACE);
                                }
                                //gl2.FrontFace(NONE);
                                break;
                            case Cull.Clockwise:
                                await gl2.FrontFaceAsync(FrontFaceDirection.CW);
                                if (!_isCullEnabled)
                                {
                                    _isCullEnabled = true;
                                    await gl2.EnableAsync(EnableCap.CULL_FACE);
                                }
                                await gl2.FrontFaceAsync(FrontFaceDirection.CW);
                                break;
                            case Cull.Counterclockwise:
                                await gl2.EnableAsync(EnableCap.CULL_FACE);
                                await gl2.FrontFaceAsync(FrontFaceDirection.CCW);
                                if (!_isCullEnabled)
                                {
                                    _isCullEnabled = true;
                                    await gl2.EnableAsync(EnableCap.CULL_FACE);
                                }
                                await gl2.FrontFaceAsync(FrontFaceDirection.CCW);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("value");
                        }
                    }
                    break;
                case RenderState.Clipping:
                    // clipping is always on in OpenGL - This state is simply ignored
                    break;
                case RenderState.ZFunc:
                    {
                        var df = GetDepthCompareFunc((Compare)value);
                        await gl2.DepthFuncAsync((CompareFunction)df);
                    }
                    break;
                case RenderState.ZEnable:
                    if (value == 0)
                        await gl2.DisableAsync(EnableCap.DEPTH_TEST);
                    else
                        await gl2.EnableAsync(EnableCap.DEPTH_TEST);
                    break;
                case RenderState.ZWriteEnable:
                    await gl2.DepthMaskAsync(value != 0);
                    break;
                case RenderState.AlphaBlendEnable:
                    if (value == 0)
                        await gl2.DisableAsync(EnableCap.BLEND);
                    else
                        await gl2.EnableAsync(EnableCap.BLEND);
                    break;
                case RenderState.BlendOperation:
                    _blendEquationRgb = BlendOperationToOgl((BlendOperation)value);
                    await gl2.BlendEquationAsync((BlendingEquation)_blendEquationRgb);
                    break;
                case RenderState.BlendOperationAlpha:
                    _blendEquationAlpha = BlendOperationToOgl((BlendOperation)value);
                    await gl2.BlendEquationSeparateAsync((BlendingEquation)_blendEquationRgb, (BlendingEquation)_blendEquationAlpha);
                    break;
                case RenderState.SourceBlend:
                    {
                        _blendSrcRgb = BlendToOgl((Blend)value);
                        await gl2.BlendFuncSeparateAsync((BlendingMode)_blendSrcRgb, (BlendingMode)_blendDstRgb, (BlendingMode)_blendSrcAlpha, (BlendingMode)_blendDstAlpha);
                    }
                    break;
                case RenderState.DestinationBlend:
                    {
                        _blendDstRgb = BlendToOgl((Blend)value);
                        await gl2.BlendFuncSeparateAsync((BlendingMode)_blendSrcRgb, (BlendingMode)_blendDstRgb, (BlendingMode)_blendSrcAlpha, (BlendingMode)_blendDstAlpha);
                    }
                    break;
                case RenderState.SourceBlendAlpha:
                    {
                        _blendSrcAlpha = BlendToOgl((Blend)value);
                        await gl2.BlendFuncSeparateAsync((BlendingMode)_blendSrcRgb, (BlendingMode)_blendDstRgb, (BlendingMode)_blendSrcAlpha, (BlendingMode)_blendDstAlpha);
                    }
                    break;
                case RenderState.DestinationBlendAlpha:
                    {
                        _blendDstAlpha = BlendToOgl((Blend)value);
                        await gl2.BlendFuncSeparateAsync((BlendingMode)_blendSrcRgb, (BlendingMode)_blendDstRgb, (BlendingMode)_blendSrcAlpha, (BlendingMode)_blendDstAlpha);
                    }
                    break;
                case RenderState.BlendFactor:
                    var blendcolor = ColorUint.Tofloat4((ColorUint)value);
                    await gl2.BlendColorAsync(blendcolor.r, blendcolor.g, blendcolor.b, blendcolor.a);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("renderState");
            }
        }

        /// <summary>
        /// Retrieves the current value for the given RenderState that is applied to the current WebGL based RenderContext.
        /// </summary>
        /// <param name="renderState">The RenderState setting to be retrieved. See <see cref="RenderState"/> for further information.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// pm;Value  + ((PolygonMode)pm) +  not handled
        /// or
        /// depFunc;Value  + ((DepthFunction)depFunc) +  not handled
        /// or
        /// renderState
        /// </exception>
        public uint GetRenderState(RenderState renderState)
        {
            return GetRenderStateAsync(renderState).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the current value for the given RenderState that is applied to the current WebGL based RenderContext.
        /// </summary>
        /// <param name="renderState">The RenderState setting to be retrieved. See <see cref="RenderState"/> for further information.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// pm;Value  + ((PolygonMode)pm) +  not handled
        /// or
        /// depFunc;Value  + ((DepthFunction)depFunc) +  not handled
        /// or
        /// renderState
        /// </exception>
        public async Task<uint> GetRenderStateAsync(RenderState renderState)
        {
            switch (renderState)
            {
                case RenderState.FillMode:
                    {
                        // Only solid polygon fill is supported by WebGL
                        return (uint)FillMode.Solid;
                    }
                case RenderState.CullMode:
                    {
                        uint cullFace;
                        cullFace = await gl2.GetParameterAsync<uint>(Parameter.CULL_FACE_MODE);
                        if (cullFace == 0)
                            return (uint)Cull.None;
                        uint frontFace;
                        frontFace = await gl2.GetParameterAsync<uint>(Parameter.FRONT_FACE);
                        if (frontFace == (uint)Cull.Clockwise)
                            return (uint)Cull.Clockwise;
                        return (uint)Cull.Counterclockwise;
                    }
                case RenderState.Clipping:
                    // clipping is always on in OpenGL - This state is simply ignored
                    return 1; // == true
                case RenderState.ZFunc:
                    {
                        uint depFunc;
                        depFunc = await gl2.GetParameterAsync<uint>(Parameter.DEPTH_FUNC);
                        var ret = depFunc switch
                        {
                            (uint)CompareFunction.NEVER => Compare.Never,
                            (uint)CompareFunction.LESS => Compare.Less,
                            (uint)CompareFunction.EQUAL => Compare.Equal,
                            (uint)CompareFunction.LEQUAL => Compare.LessEqual,
                            (uint)CompareFunction.GREATER => Compare.Greater,
                            (uint)CompareFunction.NOTEQUAL => Compare.NotEqual,
                            (uint)CompareFunction.GEQUAL => Compare.GreaterEqual,
                            (uint)CompareFunction.ALWAYS => Compare.Always,
                            _ => throw new ArgumentOutOfRangeException("depFunc", "Value " + depFunc + " not handled"),
                        };
                        return (uint)ret;
                    }
                case RenderState.ZEnable:
                    {
                        uint depTest;
                        depTest = await gl2.GetParameterAsync<uint>(Parameter.DEPTH_BITS);
                        return depTest;
                    }
                case RenderState.ZWriteEnable:
                    {
                        uint depWriteMask;
                        depWriteMask = await gl2.GetParameterAsync<uint>(Parameter.DEPTH_WRITEMASK);
                        return depWriteMask;
                    }
                //case RenderState.AlphaBlendEnable:
                //    {
                //        //uint blendEnable;
                //        //blendEnable = gl2.GetParameter<uint>(Parameter.BLEND_EQUATION_ALPHA);
                //        //return (uint)(blendEnable);
                //    }
                case RenderState.BlendOperation:
                    {
                        uint rgbMode;
                        rgbMode = await gl2.GetParameterAsync<uint>(Parameter.BLEND_EQUATION_RGB);
                        return (uint)BlendOperationFromOgl(rgbMode);
                    }
                case RenderState.BlendOperationAlpha:
                    {
                        uint alphaMode;
                        alphaMode = await gl2.GetParameterAsync<uint>(Parameter.BLEND_EQUATION_ALPHA);
                        return (uint)BlendOperationFromOgl(alphaMode);
                    }
                case RenderState.SourceBlend:
                    {
                        uint rgbSrc;
                        rgbSrc = await gl2.GetParameterAsync<uint>(Parameter.BLEND_SRC_RBG);
                        return (uint)BlendFromOgl(rgbSrc);
                    }
                case RenderState.DestinationBlend:
                    {
                        uint rgbDst;
                        rgbDst = await gl2.GetParameterAsync<uint>(Parameter.BLEND_DST_RGB);
                        return (uint)BlendFromOgl(rgbDst);
                    }
                case RenderState.SourceBlendAlpha:
                    {
                        uint alphaSrc;
                        alphaSrc = await gl2.GetParameterAsync<uint>(Parameter.BLEND_SRC_ALPHA);
                        return (uint)BlendFromOgl(alphaSrc);
                    }
                case RenderState.DestinationBlendAlpha:
                    {
                        uint alphaDst;
                        alphaDst = await gl2.GetParameterAsync<uint>(Parameter.BLEND_DST_ALPHA);
                        return (uint)BlendFromOgl(alphaDst);
                    }
                case RenderState.BlendFactor:
                    {
                        var col = await gl2.GetParameterAsync<float[]>(Parameter.BLEND_COLOR);
                        var uintCol = new ColorUint(col);
                        return (uint)uintCol.ToRgba();
                    }
                default:
                    throw new ArgumentOutOfRangeException("renderState");
            }
        }

        /// <summary>
        /// Renders into the given texture.
        /// </summary>
        /// <param name="tex">The texture.</param>
        /// <param name="texHandle">The texture handle, associated with the given texture. Should be created by the TextureManager in the RenderContext.</param>
        public async Task SetRenderTarget(IWritableTexture tex, ITextureHandle texHandle)
        {
            if (((TextureHandle)texHandle).FrameBufferHandle == null)
            {
                var fBuffer = await gl2.CreateFramebufferAsync();
                ((TextureHandle)texHandle).FrameBufferHandle = fBuffer;
                await gl2.BindFramebufferAsync(FramebufferType.FRAMEBUFFER, fBuffer);

                await gl2.BindTextureAsync(TextureType.TEXTURE_2D, ((TextureHandle)texHandle).TexHandle);

                if (tex.TextureType != RenderTargetTextureTypes.G_DEPTH)
                {
                    CreateDepthRenderBufferAsync(tex.Width, tex.Height);
                    await gl2.FramebufferTexture2DAsync(FramebufferType.FRAMEBUFFER, FramebufferAttachment.COLOR_ATTACHMENT0, Texture2DType.TEXTURE_2D, ((TextureHandle)texHandle).TexHandle, 0);
                    //gl2.DrawBuffers(new uint[] { COLOR_ATTACHMENT0 });
                }
                else
                {
                    await gl2.FramebufferTexture2DAsync(FramebufferType.FRAMEBUFFER, FramebufferAttachment.DEPTH_ATTACHMENT, Texture2DType.TEXTURE_2D, ((TextureHandle)texHandle).TexHandle, 0);
                    //gl2.DrawBuffers(new uint[] { NONE });
                    //gl2.ReadBuffer(NONE);
                }
            }
            else
            {
                await gl2.BindFramebufferAsync(FramebufferType.FRAMEBUFFER, ((TextureHandle)texHandle).FrameBufferHandle);
            }

            if (await gl2.CheckFramebufferStatusAsync(FramebufferType.FRAMEBUFFER) != FramebufferStatus.FRAMEBUFFER_COMPLETE)
                throw new Exception($"Error creating RenderTarget: {await gl2.GetErrorAsync()}, {await gl2.CheckFramebufferStatusAsync(FramebufferType.FRAMEBUFFER)}; Pixelformat: {tex.PixelFormat}");

            await gl2.ClearAsync(BufferBits.DEPTH_BUFFER_BIT | BufferBits.COLOR_BUFFER_BIT);
        }

        /// <summary>
        /// Renders into the given cube map.
        /// </summary>
        /// <param name="tex">The texture.</param>
        /// <param name="texHandle">The texture handle, associated with the given cube map. Should be created by the TextureManager in the RenderContext.</param>
        public async Task SetRenderTarget(IWritableCubeMap tex, ITextureHandle texHandle)
        {
            if (((TextureHandle)texHandle).FrameBufferHandle == null)
            {
                var fBuffer = await gl2.CreateFramebufferAsync();
                ((TextureHandle)texHandle).FrameBufferHandle = fBuffer;
                await gl2.BindFramebufferAsync(FramebufferType.FRAMEBUFFER, fBuffer);

                await gl2.BindTextureAsync(TextureType.TEXTURE_CUBE_MAP, ((TextureHandle)texHandle).TexHandle);

                if (tex.TextureType != RenderTargetTextureTypes.G_DEPTH)
                {
                    CreateDepthRenderBufferAsync(tex.Width, tex.Height);
                    await gl2.FramebufferTexture2DAsync(FramebufferType.FRAMEBUFFER, FramebufferAttachment.COLOR_ATTACHMENT0, Texture2DType.TEXTURE_CUBE_MAP_NEGATIVE_X, ((TextureHandle)texHandle).TexHandle, 0);
                    //gl2.DrawBuffers(new uint[] { FramebufferAttachment.COLOR_ATTACHMENT0 });

                }
                else
                {
                    await gl2.FramebufferTexture2DAsync(FramebufferType.FRAMEBUFFER, FramebufferAttachment.DEPTH_ATTACHMENT, Texture2DType.TEXTURE_CUBE_MAP_NEGATIVE_X, ((TextureHandle)texHandle).TexHandle, 0);
                    //gl2.DrawBuffers(new uint[] { NONE });
                    //gl2.ReadBuffer(NONE);
                }
            }
            else
            {
                await gl2.BindFramebufferAsync(FramebufferType.FRAMEBUFFER, ((TextureHandle)texHandle).FrameBufferHandle);
            }

            if (await gl2.CheckFramebufferStatusAsync(FramebufferType.FRAMEBUFFER) != FramebufferStatus.FRAMEBUFFER_COMPLETE)
                throw new Exception($"Error creating RenderTarget: {await gl2.GetErrorAsync()}, {await gl2.CheckFramebufferStatusAsync(FramebufferType.FRAMEBUFFER)}; Pixelformat: {tex.PixelFormat}");


            await gl2.ClearAsync(BufferBits.DEPTH_BUFFER_BIT | BufferBits.COLOR_BUFFER_BIT);
        }

        /// <summary>
        /// Renders into the given textures of the RenderTarget.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        /// <param name="texHandles">The texture handles, associated with the given textures. Each handle should be created by the TextureManager in the RenderContext.</param>
        public async Task SetRenderTarget(IRenderTarget renderTarget, ITextureHandle[] texHandles)
        {
            if (renderTarget == null || (renderTarget.RenderTextures.All(x => x == null)))
            {
                await gl2.BindFramebufferAsync(FramebufferType.FRAMEBUFFER, null);
                return;
            }

            WebGLFramebuffer gBuffer;

            if (renderTarget.GBufferHandle == null)
            {
                renderTarget.GBufferHandle = new FrameBufferHandle();
                gBuffer = await CreateFrameBufferAsync(renderTarget, texHandles);
                ((FrameBufferHandle)renderTarget.GBufferHandle).Handle = gBuffer;
            }
            else
            {
                gBuffer = ((FrameBufferHandle)renderTarget.GBufferHandle).Handle;
                await gl2.BindFramebufferAsync(FramebufferType.FRAMEBUFFER, gBuffer);
            }

            if (renderTarget.RenderTextures[(int)RenderTargetTextureTypes.G_DEPTH] == null && !renderTarget.IsDepthOnly)
            {
                WebGLRenderbuffer gDepthRenderbufferHandle;
                if (renderTarget.DepthBufferHandle == null)
                {
                    renderTarget.DepthBufferHandle = new RenderBufferHandle();
                    // Create and attach depth-buffer (render-buffer)
                    gDepthRenderbufferHandle = await CreateDepthRenderBufferAsync((int)renderTarget.TextureResolution, (int)renderTarget.TextureResolution);
                    ((RenderBufferHandle)renderTarget.DepthBufferHandle).Handle = gDepthRenderbufferHandle;
                }
                else
                {
                    gDepthRenderbufferHandle = ((RenderBufferHandle)renderTarget.DepthBufferHandle).Handle;
                    await gl2.BindRenderbufferAsync(RenderbufferType.RENDERBUFFER, gDepthRenderbufferHandle);
                }
            }

            if (await gl2.CheckFramebufferStatusAsync(FramebufferType.FRAMEBUFFER) != FramebufferStatus.FRAMEBUFFER_COMPLETE)
            {
                throw new Exception($"Error creating Framebuffer: {gl2.GetError()}, {gl2.CheckFramebufferStatus(FramebufferType.FRAMEBUFFER)};" +
                    $"DepthBuffer set? {renderTarget.DepthBufferHandle != null}");
            }

            await gl2.ClearAsync(BufferBits.DEPTH_BUFFER_BIT | BufferBits.COLOR_BUFFER_BIT);
        }

        private async Task<WebGLRenderbuffer> CreateDepthRenderBufferAsync(int width, int height)
        {
            await gl2.EnableAsync(EnableCap.DEPTH_TEST);

            var gDepthRenderbuffer = await gl2.CreateRenderbufferAsync();
            await gl2.BindRenderbufferAsync(RenderbufferType.RENDERBUFFER, gDepthRenderbuffer);
            await gl2.RenderbufferStorageAsync(RenderbufferType.RENDERBUFFER, RenderbufferFormat.DEPTH_COMPONENT16, width, height);
            await gl2.FramebufferRenderbufferAsync(FramebufferType.FRAMEBUFFER, FramebufferAttachment.DEPTH_ATTACHMENT, RenderbufferType.RENDERBUFFER, gDepthRenderbuffer);
            return gDepthRenderbuffer;
        }

        private async Task<WebGLFramebuffer> CreateFrameBufferAsync(IRenderTarget renderTarget, ITextureHandle[] texHandles)
        {
            var gBuffer = await gl2.CreateFramebufferAsync();
            await gl2.BindFramebufferAsync(FramebufferType.FRAMEBUFFER, gBuffer);

            var depthCnt = 0;
            var depthTexPos = (int)RenderTargetTextureTypes.G_DEPTH;

            if (!renderTarget.IsDepthOnly)
            {
                var attachments = new List<uint>();

                //Textures
                for (var i = 0; i < texHandles.Length; i++)
                {

                    var texHandle = texHandles[i];
                    if (texHandle == null) continue;

                    if (i == depthTexPos)
                    {
                        await gl2.FramebufferTexture2DAsync(FramebufferType.FRAMEBUFFER, FramebufferAttachment.DEPTH_ATTACHMENT + depthCnt, Texture2DType.TEXTURE_2D, ((TextureHandle)texHandle).TexHandle, 0);
                        depthCnt++;
                    }
                    else
                    {
                        await gl2.FramebufferTexture2DAsync(FramebufferType.FRAMEBUFFER, FramebufferAttachment.COLOR_ATTACHMENT0 + (i - depthCnt), Texture2DType.TEXTURE_2D, ((TextureHandle)texHandle).TexHandle, 0);
                    }

                    attachments.Add((uint)(FramebufferAttachment.COLOR_ATTACHMENT0 + i));

                }
                //gl2.DrawBuffers(attachments.ToArray());
            }
            else //If a frame-buffer only has a depth texture we don't need draw buffers
            {
                var texHandle = texHandles[depthTexPos];

                if (texHandle != null)
                    await gl2.FramebufferTexture2DAsync(FramebufferType.FRAMEBUFFER, FramebufferAttachment.DEPTH_ATTACHMENT, Texture2DType.TEXTURE_2D, ((TextureHandle)texHandle).TexHandle, 0);
                else
                    throw new NullReferenceException("Texture handle is null!");

                //gl2.DrawBuffers(new uint[] { NONE });
                //gl2.ReadBuffer(NONE);
            }

            return gBuffer;
        }

        /// <summary>
        /// Detaches a texture from the frame buffer object, associated with the given render target.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        /// <param name="attachment">Number of the fbo attachment. For example: attachment = 1 will detach the texture currently associated with <see cref="COLOR_ATTACHMENT1"/>.</param>
        /// <param name="isDepthTex">Determines if the texture is a depth texture. In this case the texture currently associated with <see cref="DEPTH_ATTACHMENT"/> will be detached.</param>       
        public void DetachTextureFromFbo(IRenderTarget renderTarget, bool isDepthTex, int attachment = 0)
        {
            ChangeFramebufferTexture2D(renderTarget, attachment, null, isDepthTex); //TODO: check if "null" is the equivalent to the zero texture (handle = 0) in OpenGL Core
        }


        /// <summary>
        /// Attaches a texture to the frame buffer object, associated with the given render target.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        /// <param name="attachment">Number of the fbo attachment. For example: attachment = 1 will attach the texture to <see cref="COLOR_ATTACHMENT1"/>.</param>
        /// <param name="isDepthTex">Determines if the texture is a depth texture. In this case the texture is attached to <see cref="DEPTH_ATTACHMENT"/>.</param>        
        /// <param name="texHandle">The gpu handle of the texture.</param>
        public void AttacheTextureToFbo(IRenderTarget renderTarget, bool isDepthTex, ITextureHandle texHandle, int attachment = 0)
        {
            ChangeFramebufferTexture2D(renderTarget, attachment, ((TextureHandle)texHandle).TexHandle, isDepthTex);
        }

        private async Task ChangeFramebufferTexture2D(IRenderTarget renderTarget, int attachment, WebGLTexture handle, bool isDepth)
        {
            var boundFbo = await gl2.GetParameterAsync<WebGLFramebuffer>(Parameter.FRAMEBUFFER_BINDING);
            var rtFbo = ((FrameBufferHandle)renderTarget.GBufferHandle).Handle;

            var isCurrentFbo = true;

            if (boundFbo != rtFbo)
            {
                isCurrentFbo = false;
                await gl2.BindFramebufferAsync(FramebufferType.FRAMEBUFFER, rtFbo);
            }

            if (!isDepth)
                await gl2.FramebufferTexture2DAsync(FramebufferType.FRAMEBUFFER, FramebufferAttachment.COLOR_ATTACHMENT0 + attachment, Texture2DType.TEXTURE_2D, handle, 0);
            else
                await gl2.FramebufferTexture2DAsync(FramebufferType.FRAMEBUFFER, FramebufferAttachment.DEPTH_ATTACHMENT, Texture2DType.TEXTURE_2D, handle, 0);

            if (await gl2.CheckFramebufferStatusAsync(FramebufferType.FRAMEBUFFER) != FramebufferStatus.FRAMEBUFFER_COMPLETE)
                throw new Exception($"Error creating RenderTarget: {await gl2.GetErrorAsync()}, {await gl2.CheckFramebufferStatusAsync(FramebufferType.FRAMEBUFFER)}");

            if (!isCurrentFbo)
                await gl2.BindFramebufferAsync(FramebufferType.FRAMEBUFFER, boundFbo);
        }

        /// <summary>
        /// Set the Viewport of the rendering output window by x,y position and width,height parameters. 
        /// The Viewport is the portion of the final image window.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public async Task Viewport(int x, int y, int width, int height)
        {
            await gl2.ViewportAsync(x, y, width, height);
        }

        /// <summary>
        /// Enable or disable Color channels to be written to the frame buffer (final image).
        /// Use this function as a color channel filter for the final image.
        /// </summary>
        /// <param name="red">if set to <c>true</c> [red].</param>
        /// <param name="green">if set to <c>true</c> [green].</param>
        /// <param name="blue">if set to <c>true</c> [blue].</param>
        /// <param name="alpha">if set to <c>true</c> [alpha].</param>
        public async Task ColorMask(bool red, bool green, bool blue, bool alpha)
        {
            await gl2.ColorMaskAsync(red, green, blue, alpha);
        }

        /// <summary>
        /// Returns the capabilities of the underlying graphics hardware
        /// </summary>
        /// <param name="capability"></param>
        /// <returns>uint</returns>
        public uint GetHardwareCapabilities(HardwareCapability capability)
        {
            return capability switch
            {
                HardwareCapability.CAN_RENDER_DEFFERED => 0U,
                HardwareCapability.CAN_USE_GEOMETRY_SHADERS => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(capability), capability, null),
            };
        }

        /// <summary> 
        /// Returns a human readable description of the underlying graphics hardware. This implementation reports GL_VENDOR, GL_RENDERER, GL_VERSION and GL_EXTENSIONS.
        /// </summary> 
        /// <returns></returns> 
        public string GetHardwareDescription()
        {
            return "";
        }

        /// <summary>
        /// Draws a Debug Line in 3D Space by using a start and end point (float3).
        /// </summary>
        /// <param name="start">The starting point of the DebugLine.</param>
        /// <param name="end">The endpoint of the DebugLine.</param>
        /// <param name="color">The color of the DebugLine.</param>
        public async Task DebugLine(float3 start, float3 end, float4 color)
        {
            var vertices = new float3[]
            {
                new float3(start.x, start.y, start.z),
                new float3(end.x, end.y, end.z),
            };

            var itemSize = 3;
            var numItems = 2;
            var posBuffer = await gl2.CreateBufferAsync();

            await gl2.EnableVertexAttribArrayAsync((uint)AttributeLocations.VertexAttribLocation);
            await gl2.BindBufferAsync(BufferType.ARRAY_BUFFER, posBuffer);
            await gl2.BufferDataAsync(BufferType.ARRAY_BUFFER, vertices, BufferUsageHint.STATIC_DRAW);
            await gl2.VertexAttribPointerAsync((uint)AttributeLocations.VertexAttribLocation, itemSize, DataType.FLOAT, false, 0, 0);

            await gl2.DrawArraysAsync(Primitive.LINE_STRIP, 0, numItems);
            await gl2.DisableVertexAttribArrayAsync((uint)AttributeLocations.VertexAttribLocation);
        }

        #endregion

        #region Picking related Members

        /// <summary>
        /// Retrieves a sub-image of the given region.
        /// </summary>
        /// <param name="x">The x value of the start of the region.</param>
        /// <param name="y">The y value of the start of the region.</param>
        /// <param name="w">The width to copy.</param>
        /// <param name="h">The height to copy.</param>
        /// <returns>The specified sub-image</returns>
        public IImageData GetPixelColor(int x, int y, int w = 1, int h = 1)
        {
            return GetPixelColorAsync(x, y, w, h).GetAwaiter().GetResult();
        }


        /// <summary>
        /// Retrieves a sub-image of the given region.
        /// </summary>
        /// <param name="x">The x value of the start of the region.</param>
        /// <param name="y">The y value of the start of the region.</param>
        /// <param name="w">The width to copy.</param>
        /// <param name="h">The height to copy.</param>
        /// <returns>The specified sub-image</returns>
        public async Task<IImageData> GetPixelColorAsync(int x, int y, int w = 1, int h = 1)
        {
            var image = Fusee.Base.Core.ImageData.CreateImage(w, h, ColorUint.Black);
            await gl2.ReadPixelsAsync(x, y, w, h, PixelFormat.RGB /* yuk, yuk ??? */, PixelType.UNSIGNED_BYTE, image.PixelData);
            return image;
        }

        /// <summary>
        /// Retrieves the Z-value at the given pixel position.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns>The Z value at (x, y).</returns>
        public float GetPixelDepth(int x, int y)
        {
            return GetPixelDepthAsync(x, y).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the Z-value at the given pixel position.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns>The Z value at (x, y).</returns>
        public async Task<float> GetPixelDepthAsync(int x, int y)
        {
            var depth = new byte[1];

            await gl2.ReadPixelsAsync(x, y, 1, 1, PixelFormat.RGB, PixelType.FLOAT, depth);

            return depth[0];
        }




        public Task SetShaderParamTexture(IShaderParam param, ITextureHandle texId, Common.TextureType texTarget)
        {
            throw new NotImplementedException();
        }

        public Task SetShaderParamTextureArray(IShaderParam param, ITextureHandle[] texIds, Common.TextureType texTarget)
        {
            throw new NotImplementedException();
        }

        public void SetShaderParam(IShaderParam param, float2[] val)
        {
            throw new NotImplementedException();
        }


        public void SetActiveAndBindTexture(IShaderParam param, ITextureHandle texId, Common.TextureType texTarget, out int texUnit)
        {
            throw new NotImplementedException();
        }

        public void SetActiveAndBindTexture(IShaderParam param, ITextureHandle texId, Common.TextureType texTarget)
        {
            throw new NotImplementedException();
        }


        public void SetActiveAndBindTextureArray(IShaderParam param, ITextureHandle[] texIds, Common.TextureType texTarget, out int[] texUnitArray)
        {
            throw new NotImplementedException();
        }

        public void SetActiveAndBindTextureArray(IShaderParam param, ITextureHandle[] texIds, Common.TextureType texTarget)
        {
            throw new NotImplementedException();
        }

        Task IRenderContextImp.EnableDepthClamp()
        {
            throw new NotImplementedException();
        }

        Task IRenderContextImp.DisableDepthClamp()
        {
            throw new NotImplementedException();
        }

        Task IRenderContextImp.DetachTextureFromFbo(IRenderTarget renderTarget, bool isDepthTex, int attachment)
        {
            throw new NotImplementedException();
        }

        Task IRenderContextImp.AttacheTextureToFbo(IRenderTarget renderTarget, bool isDepthTex, ITextureHandle texHandle, int attachment)
        {
            throw new NotImplementedException();
        }

        Task IRenderContextImp.SetShaderParam(IShaderParam param, float2[] val)
        {
            throw new NotImplementedException();
        }

        Task IRenderContextImp.SetActiveAndBindTexture(IShaderParam param, ITextureHandle texId, Common.TextureType texTarget, out int texUnit)
        {
            throw new NotImplementedException();
        }

        Task IRenderContextImp.SetActiveAndBindTexture(IShaderParam param, ITextureHandle texId, Common.TextureType texTarget)
        {
            throw new NotImplementedException();
        }

        Task IRenderContextImp.SetActiveAndBindTextureArray(IShaderParam param, ITextureHandle[] texIds, Common.TextureType texTarget, out int[] texUnitArray)
        {
            throw new NotImplementedException();
        }

        Task IRenderContextImp.SetActiveAndBindTextureArray(IShaderParam param, ITextureHandle[] texIds, Common.TextureType texTarget)
        {
            throw new NotImplementedException();
        }


        #endregion
    }
}