using System;
using System.Collections.Generic;
using System.Linq;
using IPA.Loader;
using HUI.Interfaces;
using HUI.Utilities;
using SDCPlugin = SongDataCore.Plugin;

namespace HUI.Sort.BuiltIn
{
    public class PPSortMode : ISortMode
    {
#pragma warning disable CS0067
        public event Action AvailabilityChanged;
#pragma warning restore CS0067

        public string Name => "PP";
        public bool IsAvailable => PluginManager.GetPluginFromId("SongDataCore") != null;
        public bool DefaultSortByAscending => false;

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
                if (SDCPlugin.Songs.Data.Songs.TryGetValue(BeatmapUtilities.GetCustomLevelHash(customLevel), out var data))
                    return (level, (float)(data?.diffs?.Max(x => x.pp) ?? 0f));
            }
            
            return (level, 0f);
        }
    }
}
