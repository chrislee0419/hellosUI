using System;
using System.Collections.Generic;
using System.Linq;
using Zenject;
using IPA.Loader;
using HUI.Interfaces;
using HUI.Utilities;
using SDCPlugin = SongDataCore.Plugin;

namespace HUI.Sort.BuiltIn
{
    public class PPSortMode : ISortMode, IInitializable, IDisposable
    {
#pragma warning disable CS0067
        public event Action AvailabilityChanged;
#pragma warning restore CS0067

        public string Name => "PP";
        public bool IsAvailable => PluginLoaded && IsReady;
        public bool DefaultSortByAscending => false;

        private bool PluginLoaded => PluginManager.GetPluginFromId("SongDataCore") != null;
        private bool IsReady => SDCPlugin.Songs?.IsDataAvailable() ?? false;

        public void Initialize()
        {
            if (PluginLoaded)
            {
                // InstallEvents needs to run after SongDataCore creates the Songs object, hence the wait here
                CoroutineUtilities.StartDelayedAction(InstallEvents, 1, false);
            }
        }

        public void Dispose()
        {
            if (PluginLoaded)
                RemoveEvents();
        }

        private void InstallEvents()
        {
            SDCPlugin.Songs.OnDataFinishedProcessing += OnDataFinishedProcessing;
        }

        private void RemoveEvents()
        {
            SDCPlugin.Songs.OnDataFinishedProcessing -= OnDataFinishedProcessing;
        }

        public IEnumerable<IPreviewBeatmapLevel> SortSongs(IEnumerable<IPreviewBeatmapLevel> levels, bool ascending)
        {
            if (!IsAvailable)
                return levels;

            var levelsWithMaxPP = levels.Select(level => GetMaxPPForSong(level));

            if (ascending)
                return levelsWithMaxPP.OrderBy(x => x.pp).Select(x => x.level);
            else
                return levelsWithMaxPP.OrderByDescending(x => x.pp).Select(x => x.level);
        }

        private (IPreviewBeatmapLevel level, float pp) GetMaxPPForSong(IPreviewBeatmapLevel level)
        {
            if (level is CustomPreviewBeatmapLevel customLevel)
            {
                if (SDCPlugin.Songs.Data.Songs.TryGetValue(BeatmapUtilities.GetCustomLevelHash(customLevel), out var data) &&
                    data != null &&
                    data.diffs != null)
                    return (level, (float)data.diffs.Max(x => x.pp));
            }
            
            return (level, 0f);
        }

        private void OnDataFinishedProcessing() => this.CallAndHandleAction(AvailabilityChanged, nameof(AvailabilityChanged));
    }
}
