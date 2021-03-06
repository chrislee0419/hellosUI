using System;
using System.Collections.Generic;
using System.Linq;
using HUI.Interfaces;

namespace HUI.Sort.BuiltIn
{
    public class DefaultSortMode : ISortMode
    {
#pragma warning disable CS0067
        public event Action AvailabilityChanged;
#pragma warning restore CS0067

        public string Name => "Default";
        public bool IsAvailable => true;

        // assuming default sort is equivalent to alphabetical order
        public bool DefaultSortByAscending => true;

        public IEnumerable<IPreviewBeatmapLevel> SortSongs(IEnumerable<IPreviewBeatmapLevel> levels, bool ascending)
        {
            if (ascending)
                return levels;
            else
                return levels.Reverse();
        }
    }
}
