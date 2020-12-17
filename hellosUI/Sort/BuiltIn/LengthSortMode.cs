using System;
using System.Collections.Generic;
using System.Linq;
using HUI.Interfaces;

namespace HUI.Sort.BuiltIn
{
    public class SongLengthSortMode : ISortMode
    {
#pragma warning disable CS0067
        public event Action AvailabilityChanged;
#pragma warning restore CS0067

        public string Name => "Song Length";
        public bool IsAvailable => true;
        public bool DefaultSortByAscending => true;

        public IEnumerable<IPreviewBeatmapLevel> SortSongs(IEnumerable<IPreviewBeatmapLevel> levels, bool ascending)
        {
            if (ascending)
                return levels.OrderBy(x => x.songDuration);
            else
                return levels.OrderByDescending(x => x.songDuration);
        }
    }
}
