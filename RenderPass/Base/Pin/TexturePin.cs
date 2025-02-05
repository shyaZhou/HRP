using UnityEngine;
using UnityEngine.Rendering;
using HypnosRenderPipeline.Tools;
using Unity.Mathematics;

namespace HypnosRenderPipeline.RenderPass
{

    public enum SizeCastMode { ResizeToInput = 0, Fixed };
    public enum ColorCastMode { FitToInput = 0, Fixed };
    public enum SizeScale { Full = 0, Half = 1, Quater = 2, Eighth = 3, Custom = 4, Double = -1, Quadra = -2 };
    public struct TexturePinDesc
    {
        public RenderTextureDescriptor basicDesc;
        public SizeCastMode sizeMode;
        public ColorCastMode colorMode;
        public SizeScale sizeScale;

        public TexturePinDesc(RenderTextureDescriptor descriptor, SizeCastMode sizeCastMode = SizeCastMode.ResizeToInput, ColorCastMode colorCastMode = ColorCastMode.FitToInput, SizeScale sizeScale = SizeScale.Full)
        {
            basicDesc = descriptor;
            sizeMode = sizeCastMode;
            colorMode = colorCastMode;
            this.sizeScale = sizeScale;
        }
    }

    public class TexturePin : BaseNodePin<TexturePinDesc, int>
    {
        TexturePin srcPin;
        public TexturePin(TexturePinDesc desc, TexturePin srcPin = null)
        {
            this.desc = desc;
            this.srcPin = srcPin;
        }
        public TexturePin(RenderTextureDescriptor descriptor, SizeCastMode sizeCastMode = SizeCastMode.ResizeToInput, ColorCastMode colorCastMode = ColorCastMode.FitToInput, SizeScale sizeScale = SizeScale.Full, TexturePin srcPin = null)
        {
            desc = new TexturePinDesc(descriptor, sizeCastMode, colorCastMode, sizeScale);
            this.srcPin = srcPin;
        }

        public static implicit operator RenderTargetIdentifier(TexturePin self)
        {
            return self.handle;
        }

        public override void AllocateResourcces(RenderContext context, int id)
        {
            int2 wh;
            if (srcPin != null)
                wh = new int2(srcPin.desc.basicDesc.width, srcPin.desc.basicDesc.height);
            else
                wh = new int2(context.camera.pixelWidth, context.camera.pixelHeight);

            if (desc.sizeScale != SizeScale.Custom)
            {
                int bits = (int)desc.sizeScale;
                desc.basicDesc.width = (wh.x >> bits) | (wh.x << -bits);
                desc.basicDesc.height = (wh.y >> bits) | (wh.y << -bits);
            }

            if (srcPin != null && desc.colorMode != ColorCastMode.Fixed)
                desc.basicDesc.colorFormat = srcPin.desc.basicDesc.colorFormat;

            context.commandBuffer.GetTemporaryRT(id, desc.basicDesc);
            handle = id;
        }
        public override void ReleaseResourcces(RenderContext context)
        {
            context.commandBuffer.ReleaseTemporaryRT(handle);
        }

        public override bool Compare(BaseNodePin<TexturePinDesc, int> pin)
        {
            var desc2 = pin.desc;

            if (desc.sizeMode == SizeCastMode.Fixed)
            {
                if (desc.sizeScale != SizeScale.Custom)
                {
                    if (desc.sizeScale != desc2.sizeScale) return false;
                }
                else if (desc.basicDesc.width != desc2.basicDesc.width || desc.basicDesc.height != desc2.basicDesc.height) return false;
            }

            if (desc.colorMode != ColorCastMode.FitToInput)
            {
                if (desc.basicDesc.colorFormat != desc2.basicDesc.colorFormat) return false;
            }

            if (desc.basicDesc.dimension != desc2.basicDesc.dimension
                || (desc.basicDesc.enableRandomWrite && !desc2.basicDesc.enableRandomWrite)
                || desc.basicDesc.volumeDepth != desc2.basicDesc.volumeDepth
                || (desc.basicDesc.depthBufferBits > desc2.basicDesc.depthBufferBits && desc.colorMode != ColorCastMode.FitToInput))
                return false;

            return true;
        }
        public override void Move(BaseNodePin<TexturePinDesc, int> pin)
        {
            desc.basicDesc = pin.desc.basicDesc;
            handle = pin.handle;
            name = pin.name;
        }

        public override bool CanCastFrom(BaseNodePin<TexturePinDesc, int> pin)
        {
            var desc2 = pin.desc;
            if (desc.basicDesc.dimension != desc2.basicDesc.dimension
                || desc.basicDesc.volumeDepth != desc2.basicDesc.volumeDepth
                || (desc.basicDesc.colorFormat != desc2.basicDesc.colorFormat && (desc.basicDesc.colorFormat == RenderTextureFormat.Depth || desc2.basicDesc.colorFormat == RenderTextureFormat.Depth)))
                return false;

            return true;
        }

        public override void CastFrom(RenderContext renderContext, BaseNodePin<TexturePinDesc, int> pin)
        {
            var from = pin.handle;

            var texDesc = (pin as TexturePin).desc.basicDesc;

            if (texDesc.colorFormat == RenderTextureFormat.Depth)
            {
                renderContext.commandBuffer.BlitDepth(from, handle);
            }
            else if (texDesc.dimension == TextureDimension.Cube)
            {
                renderContext.commandBuffer.BlitSkybox(from, handle, null, 0);
            }
            else
            {
                renderContext.commandBuffer.Blit(from, handle);
            }
        }
    }
}