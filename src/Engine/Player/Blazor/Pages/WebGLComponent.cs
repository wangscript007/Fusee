using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Fusee.Base.Common;
using Fusee.Base.Core;
using Fusee.Base.Imp.WebAsm;
using Fusee.Engine.Core;
using Fusee.Engine.Imp.Graphics.WebAsm;
using Fusee.Math.Core;
using Fusee.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Fusee.Engine.Player.Blazor.Pages
{
    public class WebGLComponent : FusCanvas
    {
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            Console.WriteLine("OnAfterRenderAsync called");
            // This method takes care of everything

            Base.Imp.WebAsm.WebAsmProgram.Start(new Main(this));
        }
    }

    public class Main : WebAsmBase
    {
        private RenderCanvasImp _canvasImp;
        private Core.Player _app;

        public Main(FusCanvas canvas)
        {
            Canvas = canvas;
        }

        public async override void Run()
        {
            gl = await Canvas.CreateWebGLAsync();
            Console.WriteLine($"Init Canvas gl is {gl}");

            base.Run();

            Console.WriteLine("Main Run() called");

            // disable the debug output as the console output and debug output are the same for web
            // this prevents that every message is printed twice!
            //Diagnostics.SetMinDebugOutputLoggingSeverityLevel(Diagnostics.SeverityLevel.NONE);

            // Inject Fusee.Engine.Base InjectMe dependencies
            IO.IOImp = new Fusee.Base.Imp.WebAsm.IOImp();


            var fap = new Fusee.Base.Imp.WebAsm.AssetProvider();
            fap.RegisterTypeHandler(
                new AssetHandler
                {
                    ReturnedType = typeof(Font),
                    DecoderAsync = async (string id, object storage) =>
                    {
                        if (System.IO.Path.GetExtension(id).IndexOf("ttf", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            var font = new Font
                            {
                                _fontImp = await Task.Factory.StartNew(() => new FontImp((Stream)storage)).ConfigureAwait(false)
                            };

                            return font;
                        }

                        return null;
                    },
                    Checker = (string id) =>
                    {
                        return System.IO.Path.GetExtension(id).IndexOf("ttf", System.StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                });

            fap.RegisterTypeHandler(
                new AssetHandler
                {
                    ReturnedType = typeof(SceneContainer),
                    DecoderAsync = async (string id, object storage) =>
                    {
                        if (System.IO.Path.GetExtension(id).IndexOf("fus", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            //var storageStream = (Stream)storage;
                            //return await Task.Factory.StartNew(() => Serializer.DeserializeSceneContainer((Stream)storage)).ConfigureAwait(false);
                        }
                        // always return something
                        return new ConvertSceneGraph().Convert(new SceneContainer
                        {
                            Children = new List<SceneNodeContainer>
                            {
                                new SceneNodeContainer
                                {
                                    Components = new List<SceneComponentContainer>
                                    {
                                        new TransformComponent
                                        {
                                            Scale = float3.One * 50
                                        },
                                        new MaterialComponent() // TODO: MaterialComponent is broken, shader is missing, figure out why!
                                        {
                                            Diffuse = new MatChannelContainer
                                            {
                                                Color = new float4(0.5f, 0.3f, 0.8f, 1)
                                            }
                                        },
                                        new Cube()
                                    }
                                }
                            }
                        });
                    },
                    Checker = (string id) =>
                    {
                        return System.IO.Path.GetExtension(id).IndexOf("fus", System.StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                });

            // Image handler
            fap.RegisterTypeHandler(new AssetHandler
            {
                ReturnedType = typeof(Base.Core.ImageData),
                DecoderAsync = async (string id, object storage) =>
                {
                    var ext = System.IO.Path.GetExtension(id).ToLower();
                    switch (ext)
                    {
                        //case ".jpg": // not possible YET!
                        // case ".jpeg":
                        case ".png":
                        case ".bmp":
                            return null;
                    }
                    return null;
                },
                Checker = (string id) =>
                {
                    var ext = System.IO.Path.GetExtension(id).ToLower();
                    switch (ext)
                    {
                        case ".png":
                        case ".bmp":
                            return true;
                    }
                    return false;
                }
            });

            AssetStorage.RegisterProvider(fap);

            Console.WriteLine("_app created()");


            _app = new Core.Player();

            // Inject Fusee.Engine InjectMe dependencies (hard coded)
            _canvasImp = new RenderCanvasImp(Canvas, gl, canvasWidth, canvasHeight);
            _app.CanvasImplementor = _canvasImp;
            _app.ContextImplementor = new RenderContextImp(_app.CanvasImplementor);
            Input.AddDriverImp(new RenderCanvasInputDriverImp(_app.CanvasImplementor));

            Console.WriteLine("App run");

            // Start the app
            _app.Run();
        }

        public override void Update(double elapsedMilliseconds)
        {
            if (_canvasImp != null)
                _canvasImp.DeltaTime = (float)(elapsedMilliseconds / 1000.0);
        }

        public override void Draw()
        {
            _canvasImp?.DoRender();
        }

        public override void Resize(int width, int height)
        {
            base.Resize(width, height);
            _canvasImp.DoResize(width, height);
        }
    }
}
