﻿using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace HypnosRenderPipeline.RenderPass
{
    public class SunAtmo : BaseRenderPass
    {
        // parameters
        public Vector2Int TLutResolution = new Vector2Int(128, 128);

        public Vector2Int SkyLutResolution = new Vector2Int(200, 256);

        public Vector2Int MSLutResolution = new Vector2Int(32, 32);

        public Vector3Int VolumeResolution = new Vector3Int(32, 32, 32);

        public float VolumeMaxDepth = 32000;

        // pins
        [NodePin(PinType.In)]
        public LightListPin sunLight = new LightListPin();
        [NodePin(PinType.InOut, true)]
        public TexturePin target = new TexturePin(new RenderTextureDescriptor(1, 1));

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

        RenderTexture t_table = null;
        RenderTexture sky_table = null;
        RenderTexture multiScatter_table = null;
        RenderTexture volumeScatter_table = null;

        HRPAtmo atmo;

        static MaterialWithName lightMat = new MaterialWithName("Hidden/DeferredLighting");

        int hash;
        public SunAtmo() { hash = GetHashCode(); }

        public override void Excute(RenderContext context)
        {
            var cb = context.commandBuffer;

            var sun = sunLight.handle.sunLight;
            Color lum;
            Vector3 dir;
            if (sun == null)
            {
                atmo = null;
                lum = Color.white * math.pow(10, 4.6f);
                dir = Vector3.up;
            }
            else
            {
                atmo = sun.atmoPreset;
                lum = sun.color * sun.radiance * math.pow(10, 4.6f);
                dir = -sun.direction;
            }
            if (atmo != null)
            {
                bool LutSizeChanged = InitLut();

                atmo.GenerateLut(hash, cb, t_table, multiScatter_table, lum, dir, LutSizeChanged);

                atmo.GenerateVolumeSkyTexture(cb, volumeScatter_table, sky_table, VolumeMaxDepth);

                int tempColor = Shader.PropertyToID("TempColor");
                cb.GetTemporaryRT(tempColor, target.desc.basicDesc);
                cb.SetGlobalVector("_SunColor", sun.color * sun.radiance);
                cb.Blit(target, tempColor, lightMat, 4);

                atmo.RenderToRT(cb, tempColor, depth, target);

                cb.ReleaseTemporaryRT(tempColor);
            }


            //context.commandBuffer.SetGlobalTexture("_DepthTex", depth.handle);
            //context.commandBuffer.SetGlobalTexture("_BaseColorTex", baseColor_roughness);
            //context.commandBuffer.SetGlobalTexture("_NormalTex", normal_metallic);
            //context.commandBuffer.SetGlobalTexture("_EmissionTex", emission);
            //if (ao.connected)
            //    context.commandBuffer.SetGlobalTexture("_AOTex", ao);
            //else
            //    context.commandBuffer.SetGlobalTexture("_AOTex", Texture2D.whiteTexture);

        }

        bool TestRTChange(ref RenderTexture rt, RenderTextureFormat format, Vector2Int wh)
        {
            if (rt == null || wh.x != rt.width || wh.y != rt.height)
            {
                if (rt != null) rt.Release();
                rt = new RenderTexture(wh.x, wh.y, 0, format, RenderTextureReadWrite.Linear);
                rt.wrapMode = TextureWrapMode.Clamp;
                rt.Create();
                return true;
            }
            return false;
        }
        bool TestRTChange(ref RenderTexture rt, RenderTextureFormat format, Vector3Int whd)
        {
            if (rt == null || whd.x != rt.width || whd.y != rt.height || whd.z != rt.volumeDepth)
            {
                if (rt != null) rt.Release();
                rt = new RenderTexture(whd.x, whd.y, 0, format, RenderTextureReadWrite.Linear);
                rt.dimension = TextureDimension.Tex3D;
                rt.volumeDepth = whd.z;
                rt.enableRandomWrite = true;
                rt.wrapMode = TextureWrapMode.Clamp;
                rt.Create();
                return true;
            }
            return false;
        }

        bool InitLut()
        {
            bool regenerate = false;
            TLutResolution.y = TLutResolution.y / 2 * 2 + 1;
            regenerate |= TestRTChange(ref t_table, RenderTextureFormat.ARGBFloat, TLutResolution);
            regenerate |= TestRTChange(ref multiScatter_table, RenderTextureFormat.ARGBFloat, MSLutResolution);

            if (TestRTChange(ref sky_table, RenderTextureFormat.ARGBFloat, SkyLutResolution))
            {
                sky_table.wrapModeU = TextureWrapMode.Repeat;
                sky_table.wrapModeV = TextureWrapMode.Clamp;
            }

            TestRTChange(ref volumeScatter_table, RenderTextureFormat.RGB111110Float, VolumeResolution);

            return regenerate;
        }
    }
}