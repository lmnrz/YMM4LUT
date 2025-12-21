using System;
using System.Runtime.InteropServices;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace YMM4LUT
{
    internal class LUT_CustomEffect : D2D1CustomShaderEffectBase
    {
        readonly EffectImpl effectImpl;

        public float Value
        {
            set => SetValue((int)EffectImpl.Properties.Value, value);
            get => GetFloatValue((int)EffectImpl.Properties.Value);
        }

        public LUT_CustomEffect(IGraphicsDevicesAndContext devices) : base(Create<EffectImpl>(devices))
        {
            effectImpl = EffectImpl.LastCreatedInstance!;
        }

        public void SetLUTTexture(LUTData data) => effectImpl.SetLUTData(data);

        [CustomEffect(1)]
        class EffectImpl : D2D1CustomShaderEffectImplBase<EffectImpl>
        {
            ConstantBuffer constantBuffer;
            ID2D1EffectContext? effectContext;
            ID2D1ResourceTexture? lutResourceTexture;
            LUTData? lutData;
            bool lutDataPending;

            public static EffectImpl? LastCreatedInstance { get; private set; }

            [CustomEffectProperty(PropertyType.Float, (int)Properties.Value)]
            public float Value
            {
                get => constantBuffer.Value;
                set { constantBuffer.Value = value; UpdateConstants(); }
            }

            public EffectImpl() : base(LUT_ShaderResourceLoader.GetShaderResource("PixelShader.cso"))
            {
                constantBuffer.Value = 1.0f;
                constantBuffer.LutSize = 33.0f;
                constantBuffer.Is3D = 1.0f;
                LastCreatedInstance = this;
            }

            public void SetLUTData(LUTData data)
            {
                lutData = data;
                lutDataPending = true;
                constantBuffer.LutSize = (float)data.Size;
                constantBuffer.Is3D = data.Is3D ? 1.0f : 0.0f;
                UpdateConstants();
                if (effectContext != null)
                    CreateLUTResourceTexture();
            }

            public override void Initialize(ID2D1EffectContext effectContext, ID2D1TransformGraph transformGraph)
            {
                this.effectContext = effectContext;
                base.Initialize(effectContext, transformGraph);
            }

            public override void SetDrawInfo(ID2D1DrawInfo drawInfo)
            {
                base.SetDrawInfo(drawInfo);
                
                if (lutDataPending && lutData != null && effectContext != null)
                {
                    CreateLUTResourceTexture();
                    lutDataPending = false;
                }
                else if (lutResourceTexture != null)
                    UpdateResourceTexture();
            }

            private void CreateLUTResourceTexture()
            {
                if (lutData == null || effectContext == null) return;

                lutResourceTexture?.Dispose();

                unsafe
                {
                    // 1D LUTの場合も (Size, 1, 1) の 3Dテクスチャとして作成し、シェーダー側の互換性を保つ
                    uint width = (uint)lutData.Size;
                    uint height = (uint)(lutData.Is3D ? lutData.Size : 1);
                    uint depth = (uint)(lutData.Is3D ? lutData.Size : 1);

                    uint[] extents = [width, height, depth];
                    fixed (uint* extentsPtr = extents)
                    {
                        ExtendMode[] extendModes = [ExtendMode.Clamp, ExtendMode.Clamp, ExtendMode.Clamp];
                        fixed (ExtendMode* extendModesPtr = extendModes)
                        {
                            var props = new ResourceTextureProperties
                            {
                                Extents = (IntPtr)extentsPtr,
                                Dimensions = 3,
                                BufferPrecision = (BufferPrecision)5, // Precision32Float
                                ChannelDepth = (ChannelDepth)4,       // FourChannels
                                Filter = Filter.MinMagMipLinear,
                                ExtendModes = (IntPtr)extendModesPtr
                            };

                            // ストライド計算 (RGBA float = 16 bytes)
                            int[] strides = [
                                (int)width * 16,
                                (int)(width * height * 16)
                            ];

                            lutResourceTexture = effectContext.CreateResourceTexture(
                                null,
                                ref props,
                                lutData.Data,
                                strides,
                                lutData.Data.Length
                            );
                            UpdateResourceTexture();
                        }
                    }
                }
            }

            protected override void UpdateConstants()
            {
                if (drawInformation == null) return;
                
                var buffer = constantBuffer;
                if (lutData == null) buffer.Value = 0.0f;
                
                drawInformation.SetPixelShaderConstantBuffer(buffer);
            }

            private void UpdateResourceTexture()
            {
                if (lutResourceTexture != null && drawInformation != null)
                    drawInformation.SetResourceTexture(1, lutResourceTexture);
            }

            [StructLayout(LayoutKind.Sequential)]
            struct ConstantBuffer
            {
                public float Value;
                public float LutSize;
                public float Is3D;
                public float Padding;
            }
            public enum Properties { Value = 0 }
        }
    }
}