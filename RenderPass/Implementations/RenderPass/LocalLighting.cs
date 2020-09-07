﻿using UnityEngine;

namespace HypnosRenderPipeline.RenderPass
{
    public class LocalLighting : BaseRenderPass
    {

        [NodePin(PinType.In, true)]
        public BufferPin<LightStructGPU> lightBuffer = new BufferPin<LightStructGPU>(1);

        [NodePin(PinType.In, true)]
        public BufferPin<uint> tiledLights = new BufferPin<uint>(1);

        [NodePin(PinType.In, true)]
        public TexturePin depth = new TexturePin(new RenderTextureDescriptor(1, 1, RenderTextureFormat.Depth, 24), colorCastMode: ColorCastMode.Fixed);

        [NodePin(PinType.In, true)]
        public TexturePin baseColor_roughness = new TexturePin(new RenderTextureDescriptor(1, 1, RenderTextureFormat.ARGB32, 0));

        [NodePin(PinType.In, true)]
        public TexturePin normal_metallic = new TexturePin(new RenderTextureDescriptor(1, 1, RenderTextureFormat.ARGB32, 0));

        [NodePin(PinType.In, true)]
        public TexturePin emission = new TexturePin(new RenderTextureDescriptor(1, 1, RenderTextureFormat.ARGB32, 0));

        [NodePin(PinType.In)]
        public TexturePin ao = new TexturePin(new RenderTextureDescriptor(1, 1, RenderTextureFormat.ARGB32, 0));

        [NodePin(PinType.Out)]
        public TexturePin lightingResult = new TexturePin(new RenderTextureDescriptor(1, 1, RenderTextureFormat.DefaultHDR, 0),
                                                                SizeCastMode.Fixed,
                                                                ColorCastMode.Fixed,
                                                                SizeScale.Full);
#if UNITY_EDITOR
        public bool debugTiledLight = false;
#endif

        static MaterialWithName lightingMat = new MaterialWithName("Hidden/DeferredLighting");

        public override void Excute(RenderContext context)
        {           
            var cam = context.RenderCamera;

            context.CmdBuffer.SetGlobalTexture("_DepthTex", depth.handle);
            context.CmdBuffer.SetGlobalTexture("_BaseColorTex", baseColor_roughness);
            context.CmdBuffer.SetGlobalTexture("_NormalTex", normal_metallic);
            context.CmdBuffer.SetGlobalTexture("_EmissionTex", emission);
            if (ao.connected)
                context.CmdBuffer.SetGlobalTexture("_AOTex", ao);
            else
                context.CmdBuffer.SetGlobalTexture("_AOTex", Texture2D.whiteTexture);

#if UNITY_EDITOR
            context.CmdBuffer.SetGlobalInt("_DebugTiledLight", debugTiledLight ? 1 : 0);
#endif

            context.CmdBuffer.Blit(null, lightingResult, lightingMat, 0);
            context.Context.ExecuteCommandBuffer(context.CmdBuffer);
            context.CmdBuffer.Clear();
        }
    }
}