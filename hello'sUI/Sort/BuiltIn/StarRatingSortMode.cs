﻿using System.Collections.Generic;
using System.Linq;
using IPA.Loader;
using HUI.Utilities;
using SDCPlugin = SongDataCore.Plugin;

namespace HUI.Sort.BuiltIn
{
    public class StarRatingSortMode : ISortMode
    {
        public string Name => "Star Rating";
        public bool IsAvailable => PluginManager.GetPluginFromId("SongDataCore") != null;
        public bool DefaultSortByAscending => false;

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
    }
}
