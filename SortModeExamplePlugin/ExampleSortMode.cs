using System;
using System.Collections.Generic;
using System.Linq;
using HUI.Interfaces;
using HUI.Utilities;

namespace SortModeExamplePlugin
{
    public class ExampleSortMode : ISortMode
    {
        public event Action AvailabilityChanged;

        public string Name => "Example Sort";

        private bool _isAvailable = true;
        public bool IsAvailable
        {
            get => _isAvailable;
            set
            {
                if (_isAvailable == value)
                    return;

                _isAvailable = value;
                Plugin.Log.Info($"Example sort mode availability set to '{_isAvailable}'");
                this.CallAndHandleAction(AvailabilityChanged, nameof(AvailabilityChanged));
            }
        }
        public bool DefaultSortByAscending => true;

        public IEnumerable<IPreviewBeatmapLevel> SortSongs(IEnumerable<IPreviewBeatmapLevel> levels, bool ascending)
        {
            if (ascending)
                return levels.OrderBy(x => x.songAuthorName);
            else
                return levels.OrderByDescending(x => x.songAuthorName);
        }
    }
}
