using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;
using YukkuriMovieMaker.Settings;

namespace YMM4LUT
{
    [VideoEffect("LUT", ["加工"], [])]
    internal class LUT_VideoEffect : VideoEffectBase
    {
        public override string Label => "LUT";

        [Display(Name = "LUTファイル", Description = ".cubeファイルを選択してください")]
        [FileSelector(FileGroupType.None)]
        public string LUTPath { get => lutPath; set => Set(ref lutPath, value); }
        private string lutPath = "";

        [Display(Name = "強度", Description = "エフェクトの強度")]
        [AnimationSlider("F0", "%", 0, 100)]
        public Animation Value { get; set; } = new Animation(100, 0, 100);

        public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription)
        {
            return [];
        }

        public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices)
        {
            return new LUT_VideoEffectProcessor(devices, this);
        }

        protected override IEnumerable<IAnimatable> GetAnimatables() => [Value];
    }
}