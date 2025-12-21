using System;
using System.IO;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace YMM4LUT
{
    internal class LUT_VideoEffectProcessor : IVideoEffectProcessor
    {
        readonly LUT_VideoEffect item;
        string? currentLutPath;
        bool isFirst = true;
        double value;

        readonly LUT_CustomEffect? effect;
        readonly ID2D1Image? output;
        ID2D1Image? input;

        public ID2D1Image Output => output ?? input ?? throw new NullReferenceException();

        public LUT_VideoEffectProcessor(IGraphicsDevicesAndContext devices, LUT_VideoEffect item)
        {
            this.item = item;
            effect = new LUT_CustomEffect(devices);
            if (!effect.IsEnabled)
            {
                effect.Dispose();
                effect = null;
            }
            else
            {
                output = effect.Output;
            }
        }

        public void SetInput(ID2D1Image? input) { this.input = input; effect?.SetInput(0, input, true); }
        public void ClearInput() { effect?.SetInput(0, null, true); }

        public DrawDescription Update(EffectDescription effectDescription)
        {
            if (effect is null) return effectDescription.DrawDescription;

            // パスが変わった時のみ処理を行う (File.Existsは重いため最小限にする)
            if (currentLutPath != item.LUTPath)
            {
                currentLutPath = item.LUTPath;
                if (!string.IsNullOrEmpty(currentLutPath) && File.Exists(currentLutPath))
                {
                    var lutData = CubeParser.Parse(currentLutPath);
                    if (lutData != null)
                    {
                        effect.SetLUTTexture(lutData);
                    }
                }
            }

            var frame = effectDescription.ItemPosition.Frame;
            var value = item.Value.GetValue(frame, effectDescription.ItemDuration.Frame, effectDescription.FPS) / 100.0;

            if (isFirst || this.value != value)
            {
                effect.Value = (float)value;
                isFirst = false;
                this.value = value;
            }

            return effectDescription.DrawDescription;
        }

        public void Dispose()
        {
            output?.Dispose();
            effect?.SetInput(0, null, true);
            effect?.Dispose();
        }
    }
}