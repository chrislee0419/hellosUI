using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;
using IPA.Loader;
using HUI.Interfaces;
using HUI.Utilities;
using SDCPlugin = SongDataCore.Plugin;

namespace HUI.Sort.BuiltIn
{
    public class StarRatingSortMode : ISortMode, IInitializable, IDisposable
    {
#pragma warning disable CS0067
        public event Action AvailabilityChanged;
#pragma warning restore CS0067

        public string Name => "Star Rating";
        public bool IsAvailable => PluginManager.GetPluginFromId("SongDataCore") != null;
        public bool DefaultSortByAscending => false;

        private bool PluginLoaded => PluginManager.GetPluginFromId("SongDataCore") != null;
        private bool IsReady => SDCPlugin.Songs?.IsDataAvailable() ?? false;

        public void Initialize()
        {
            if (PluginLoaded)
                CoroutineUtilities.Start(TryInstallEvents());
        }

        public void Dispose()
        {
            if (PluginLoaded)
                RemoveEvents();
        }

        private IEnumerator TryInstallEvents()
        {
            var wait = new WaitForSeconds(0.1f);
            int tries = 0;

            while (SDCPlugin.Songs == null && tries++ < 10)
                yield return wait;

            if (SDCPlugin.Songs != null)
                SDCPlugin.Songs.OnDataFinishedProcessing += OnDataFinishedProcessing;
            else
                Plugin.Log.Warn("Star Rating sort mode failed to install listener to SongDataCore, sort mode may not work correctly");
        }

        private void RemoveEvents()
        {
            SDCPlugin.Songs.OnDataFinishedProcessing -= OnDataFinishedProcessing;
        }

        public IEnumerable<IPreviewBeatmapLevel> SortSongs(IEnumerable<IPreviewBeatmapLevel> levels, bool ascending)
        {
            if (!IsAvailable)
                return levels;

            var levelsWithMaxPP = levels.Select(level => GetMaxStarRatingForSong(level));

            if (ascending)
                return levelsWithMaxPP.OrderBy(x => x.star).Select(x => x.level);
            else
                return levelsWithMaxPP.OrderByDescending(x => x.star).Select(x => x.level);
        }

        private (IPreviewBeatmapLevel level, float star) GetMaxStarRatingForSong(IPreviewBeatmapLevel level)
        {
            if (level is CustomPreviewBeatmapLevel customLevel)
            {
                if (SDCPlugin.Songs.Data.Songs.TryGetValue(BeatmapUtilities.GetCustomLevelHash(customLevel), out var data))
                    return (level, (float)(data?.diffs?.Max(x => x.star) ?? 0f));
            }
            
            return (level, 0f);
        }

        private void OnDataFinishedProcessing() => this.CallAndHandleAction(AvailabilityChanged, nameof(AvailabilityChanged));
    }
}
