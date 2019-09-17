﻿namespace Fusee.Engine.Common
{
    public interface IRenderTarget
    {
        IBufferHandle GBufferHandle { get; set; }
        IBufferHandle DepthBufferHandle { get; set; }

        IWritableTexture[] RenderTextures { get; }

        TexRes TextureResolution { get; }
    }
}
