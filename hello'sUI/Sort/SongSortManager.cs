using System;
using System.Collections.Generic;
using System.Linq;
using Zenject;
using HUI.Interfaces;
using HUI.Sort.BuiltIn;
using HUI.UI.Screens;
using HUI.Utilities;

namespace HUI.Sort
{
    public class SongSortManager : IInitializable, IDisposable, ILevelCollectionModifier
    {
        public event Action LevelCollectionRefreshRequested;

        // expose instance to mods that don't use SiraUtil
        public static SongSortManager Instance { get; private set; }

        public IReadOnlyList<ISortMode> SortModes { get; private set; }
        public ISortMode CurrentSortMode { get; private set; }
        public bool SortAscending { get; private set; }

        private bool IsDefaultSort => CurrentSortMode == _defaultSortMode && SortAscending == _defaultSortMode.DefaultSortByAscending;

        private SortScreenManager _sortScreenManager;

        private DefaultSortMode _defaultSortMode;
        private ISortMode[] _builtInSortModes;
        private List<ISortMode> _externalSortModes;

        [Inject]
        public SongSortManager(
            SortScreenManager sortScreenManager,
            PlayCountSortMode playCountSort,
            List<ISortMode> externalSortModes)
        {
            _sortScreenManager = sortScreenManager;

            _defaultSortMode = new DefaultSortMode();
            _builtInSortModes = new ISortMode[]
            {
                _defaultSortMode,
                new NewestSortMode(),
                playCountSort,
                new SongLengthSortMode(),
                new PPSortMode(),
                new StarRatingSortMode()
            };
            _externalSortModes = externalSortModes;
        }

        public void Initialize()
        {
            RefreshSortModeAvailability();

            _sortScreenManager.SortDirectionChanged += OnSortDirectionChanged;
            _sortScreenManager.SortCancelled += OnSortCancelled;
            _sortScreenManager.SortModeListCellSelected += OnSortModeListCellSelected;

            foreach (var sortMode in SortModes)
                sortMode.AvailabilityChanged += RefreshSortModeAvailability;
        }

        public void Dispose()
        {
            if (_sortScreenManager != null)
            {
                _sortScreenManager.SortDirectionChanged -= OnSortDirectionChanged;
                _sortScreenManager.SortCancelled -= OnSortCancelled;
                _sortScreenManager.SortModeListCellSelected -= OnSortModeListCellSelected;
            }

            if (SortModes != null)
            {
                foreach (var sortMode in SortModes)
                {
                    if (sortMode != null)
                        sortMode.AvailabilityChanged -= RefreshSortModeAvailability;
                }
            }
        }

        public void OnLevelCollectionSelected(IAnnotatedBeatmapLevelCollection annotatedBeatmaplevelCollection)
        {
            // no action needed
        }

        public bool ApplyModifications(IEnumerable<IPreviewBeatmapLevel> levelCollection, out IEnumerable<IPreviewBeatmapLevel> modifiedLevelCollection)
        {
            if (IsDefaultSort)
            {
                modifiedLevelCollection = levelCollection;
                return false;
            }
            else
            {
                modifiedLevelCollection = CurrentSortMode.SortSongs(levelCollection, SortAscending);
                return true;
            }
        }

        private void RefreshSortModeAvailability()
        {
            Plugin.Log.Info("Refreshing sort mode availability");

            SortModes = _builtInSortModes
                .Concat(_externalSortModes.OrderBy(x => x.Name))
                .OrderByDescending(x => x.IsAvailable)
                .ToList()
                .AsReadOnly();

            _sortScreenManager.RefreshSortModeList(SortModes);

            ApplyDefaultSort();
        }

        private void OnSortDirectionChanged() => ApplySortMode(CurrentSortMode, !SortAscending);

        private void OnSortCancelled() => ApplyDefaultSort();

        private void OnSortModeListCellSelected(int index)
        {
            ISortMode newSortMode = SortModes[index];

            if (newSortMode.IsAvailable)
            {
                bool ascending = newSortMode.DefaultSortByAscending;
                if (newSortMode == CurrentSortMode)
                    ascending = !SortAscending;

                ApplySortMode(newSortMode, ascending);
            }
            else
            {
                // reselect the current sort mode if the user clicked on an unavailable sort mode
                index = SortModes.IndexOf(CurrentSortMode);
                _sortScreenManager.SelectSortMode(index);
            }
        }

        internal void ApplySortMode(ISortMode sortMode, bool ascending)
        {
            if (sortMode is DefaultSortMode && ascending)
            {
                ApplyDefaultSort();
            }
            else
            {
                CurrentSortMode = sortMode;
                SortAscending = ascending;

                _sortScreenManager.SelectSortMode(SortModes.IndexOf(CurrentSortMode));
                _sortScreenManager.SortText = CurrentSortMode.Name.EscapeTextMeshProTags();
                _sortScreenManager.SortAscending = SortAscending;

                RequestLevelCollectionRefresh();
            }
        }

        internal void ApplyDefaultSort()
        {
            CurrentSortMode = _defaultSortMode;
            SortAscending = true;

            _sortScreenManager.SelectSortMode(0);
            _sortScreenManager.SortText = _defaultSortMode.Name;
            _sortScreenManager.SortAscending = _defaultSortMode.DefaultSortByAscending;

            RequestLevelCollectionRefresh();
        }

        private void RequestLevelCollectionRefresh() => this.CallAndHandleAction(LevelCollectionRefreshRequested, nameof(LevelCollectionRefreshRequested));
    }
}
