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

            if (currentLutPath != item.LUTPath && !string.IsNullOrEmpty(item.LUTPath) && File.Exists(item.LUTPath))
            {
                var lutData = CubeParser.Parse(item.LUTPath);
                if (lutData != null)
                {
                    effect.SetLUTTexture(lutData);
                    currentLutPath = item.LUTPath;
                }
            }

            var frame = effectDescription.ItemPosition.Frame;
            var value = item.Value.GetValue(frame, effectDescription.ItemDuration.Frame, effectDescription.FPS) / 100.0;

            if (isFirst || this.value != value) effect.Value = (float)value;
            isFirst = false; this.value = value;

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