using Fusee.Base.Common;
using Fusee.Base.Core;
using Fusee.Base.Imp.Desktop;
using Fusee.Engine.Core;
using Fusee.Engine.Core.Scene;
using Fusee.Serialization;
using System.IO;
using System.Reflection;
using System.Threading;
using Path = Fusee.Base.Common.Path;

namespace Fusee.Examples.Simple.Lib
{
    public static class SimpleLib
    {
        public delegate void ExecFusAppDelegate();

        public static bool IsAppInitialized { get; set; } = false;

        private static Thread _fusThread;

        public static void AbortFusThread()
        {
            if (_fusThread != null)
            {
                _fusThread.Abort();
            }
        }

        public static async void ExecFusAppAsync()
        {
            Core.Simple app = null;

            _fusThread = new Thread(() =>
            {
                InitAndRunApp();
            });

            _fusThread.Start();

            SpinWait.SpinUntil(() => app != null && app.IsInitialized);
            //Closed += (s, e) => app?.CloseGameWindow();
            IsAppInitialized = true;

        }

        public static void ExecFusApp()
        {
            InitAndRunApp();
            IsAppInitialized = true;
        }

        private static void InitAndRunApp()
        {
            // Inject Fusee.Engine.Base InjectMe dependencies
            IO.IOImp = new Fusee.Base.Imp.Desktop.IOImp();

            var fap = new Fusee.Base.Imp.Desktop.FileAssetProvider("Assets");
            fap.RegisterTypeHandler(
                new AssetHandler
                {
                    ReturnedType = typeof(Font),
                    Decoder = (string id, object storage) =>
                    {
                        if (!Path.GetExtension(id).Contains("ttf", System.StringComparison.OrdinalIgnoreCase)) return null;
                        return new Font { _fontImp = new FontImp((Stream)storage) };
                    },
                    Checker = id => Path.GetExtension(id).Contains("ttf", System.StringComparison.OrdinalIgnoreCase)
                });
            fap.RegisterTypeHandler(
                new AssetHandler
                {
                    ReturnedType = typeof(SceneContainer),
                    Decoder = (string id, object storage) =>
                    {
                        if (!Path.GetExtension(id).Contains("fus", System.StringComparison.OrdinalIgnoreCase)) return null;
                        return FusSceneConverter.ConvertFrom(ProtoBuf.Serializer.Deserialize<FusFile>((Stream)storage));
                    },
                    Checker = id => Path.GetExtension(id).Contains("fus", System.StringComparison.OrdinalIgnoreCase)
                });

            AssetStorage.RegisterProvider(fap);

            var app = new Core.Simple();

            // Inject Fusee.Engine InjectMe dependencies (hard coded)
            System.Drawing.Icon appIcon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            app.CanvasImplementor = new Fusee.Engine.Imp.Graphics.Desktop.RenderCanvasImp(appIcon);
            app.ContextImplementor = new Fusee.Engine.Imp.Graphics.Desktop.RenderContextImp(app.CanvasImplementor);
            Input.AddDriverImp(new Fusee.Engine.Imp.Graphics.Desktop.RenderCanvasInputDriverImp(app.CanvasImplementor));
            Input.AddDriverImp(new Fusee.Engine.Imp.Graphics.Desktop.WindowsTouchInputDriverImp(app.CanvasImplementor));

            // Start the app
            app.Run();
        }
    }
}
