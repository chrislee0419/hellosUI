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

            Instance = this;
        }

        public void Initialize()
        {
            RefreshSortModeAvailablity();

            _sortScreenManager.SortDirectionChanged += OnSortDirectionChanged;
            _sortScreenManager.SortCancelled += OnSortCancelled;
            _sortScreenManager.SortModeListCellSelected += OnSortModeListCellSelected;
        }

        public void Dispose()
        {
            if (_sortScreenManager != null)
            {
                _sortScreenManager.SortDirectionChanged -= OnSortDirectionChanged;
                _sortScreenManager.SortCancelled -= OnSortCancelled;
                _sortScreenManager.SortModeListCellSelected -= OnSortModeListCellSelected;
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

        public void RefreshSortModeAvailablity()
        {
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
            bool ascending = newSortMode.DefaultSortByAscending;

            if (newSortMode == CurrentSortMode)
                ascending = !SortAscending;

            ApplySortMode(newSortMode, ascending);
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

        private void RequestLevelCollectionRefresh()
        {
            try
            {
                LevelCollectionRefreshRequested?.Invoke();
            }
            catch (Exception e)
            {
                Plugin.Log.Warn($"Unexpected exception occurred in {nameof(SongSortManager)}:{nameof(LevelCollectionRefreshRequested)} event");
                Plugin.Log.Debug(e);
            }
        }
    }
}
