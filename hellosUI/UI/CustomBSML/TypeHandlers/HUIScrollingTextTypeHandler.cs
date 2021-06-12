using System;
using System.Collections.Generic;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.TypeHandlers;
using HUI.UI.CustomBSML.Components;

namespace HUI.UI.CustomBSML.TypeHandlers
{
    [ComponentHandler(typeof(HUIScrollingText))]
    public class HUIScrollingTextTypeHandler : TypeHandler<HUIScrollingText>
    {
        public override Dictionary<string, string[]> Props => new Dictionary<string, string[]>()
        {
            { "movementType", new[]{ "movement-type" } },
            { "animationType", new[]{ "animation-type" } },
            { "textWidthRatioThreshold", new[]{ "text-width-ratio-threshold" } },
            { "pauseDuration", new[]{ "pause-duration" } },
            { "scrollDuration", new[]{ "scroll-duration" } },
            { "scrollSpeed", new[]{ "scroll-speed" } },
            { "alwaysScroll", new[]{ "always-scroll" } },
        };

        public override Dictionary<string, Action<HUIScrollingText, string>> Setters => new Dictionary<string, Action<HUIScrollingText, string>>()
        {
            { "movementType", new Action<HUIScrollingText, string>((scrollingText, value) => scrollingText.movementType = (HUIScrollingText.ScrollMovementType)Enum.Parse(typeof(HUIScrollingText.ScrollMovementType), value)) },
            { "animationType", new Action<HUIScrollingText, string>((scrollingText, value) => scrollingText.animationType = (HUIScrollingText.ScrollAnimationType)Enum.Parse(typeof(HUIScrollingText.ScrollAnimationType), value)) },
            { "textWidthRatioThreshold", new Action<HUIScrollingText, string>((scrollingText, value) => scrollingText.textWidthRatioThreshold = Parse.Float(value)) },
            { "pauseDuration", new Action<HUIScrollingText, string>((scrollingText, value) => scrollingText.pauseDuration = Parse.Float(value)) },
            { "scrollDuration", new Action<HUIScrollingText, string>((scrollingText, value) => scrollingText.scrollDuration = Parse.Float(value)) },
            { "scrollSpeed", new Action<HUIScrollingText, string>((scrollingText, value) => scrollingText.scrollSpeed = Parse.Float(value)) },
            { "alwaysScroll", new Action<HUIScrollingText, string>((scrollingText, value) => scrollingText.alwaysScroll = Parse.Bool(value)) },
        };
    }
}
