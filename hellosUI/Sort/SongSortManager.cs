using System;
using System.Collections.Generic;
using System.Linq;
using Zenject;
using HUI.Interfaces;
using HUI.Sort.BuiltIn;
using HUI.UI.Screens;
using HUI.UI.Settings;
using HUI.Utilities;

namespace HUI.Sort
{
    public class SongSortManager : IInitializable, IDisposable, ILevelCollectionModifier
    {
        public event Action<bool> LevelCollectionRefreshRequested;

        public int Priority => -10;

        public IReadOnlyList<ISortMode> SortModes { get; private set; }
        public ISortMode CurrentSortMode { get; private set; }
        public bool SortAscending { get; private set; }

        private bool IsDefaultSort => CurrentSortMode == _defaultSortMode && SortAscending == _defaultSortMode.DefaultSortByAscending;

        private SortScreenManager _sortScreenManager;
        private SortListScreenManager _sortListScreenManager;
        private SortSettingsTab _sortSettingsTab;

        private DefaultSortMode _defaultSortMode;
        private ISortMode[] _builtInSortModes;
        private List<ISortMode> _externalSortModes;

        private bool _isBaseGamePlaylist = false;

        [Inject]
        public SongSortManager(
            SortScreenManager sortScreenManager,
            SortListScreenManager sortListScreenManager,
            SortSettingsTab sortSettingsTab,
            PlayCountSortMode playCountSort,
            PPSortMode ppSort,
            List<ISortMode> externalSortModes)
        {
            _sortScreenManager = sortScreenManager;
            _sortListScreenManager = sortListScreenManager;
            _sortSettingsTab = sortSettingsTab;

            _defaultSortMode = new DefaultSortMode();
            _builtInSortModes = new ISortMode[]
            {
                _defaultSortMode,
                new NewestSortMode(),
                playCountSort,
                new SongLengthSortMode(),
                ppSort,
                new StarRatingSortMode()
            };
            _externalSortModes = externalSortModes;
        }

        public void Initialize()
        {
            RefreshSortModeList();

            // try to reselect the last sort mode used
            if (!string.IsNullOrEmpty(PluginConfig.Instance.Sort.LastSortModeID))
            {
                foreach (var sortMode in SortModes)
                {
                    if (sortMode.GetIdentifier() == PluginConfig.Instance.Sort.LastSortModeID)
                    {
                        if (!sortMode.IsAvailable)
                            break;

                        ApplySortMode(sortMode, PluginConfig.Instance.Sort.LastSortModeIsAscending);
                        break;
                    }
                }
            }

            if (CurrentSortMode == null)
                ApplyDefaultSort();

            _sortScreenManager.SortButtonPressed += OnSortButtonPressed;
            _sortScreenManager.SortDirectionChanged += OnSortDirectionChanged;
            _sortScreenManager.SortCancelled += OnSortCancelled;

            _sortListScreenManager.SortModeListCellSelected += OnSortModeListCellSelected;

            _sortSettingsTab.SortModeListSettingChanged += OnSortModeListChanged;

            foreach (var sortMode in _builtInSortModes.Concat(_externalSortModes))
                sortMode.AvailabilityChanged += OnSortModeListChanged;
        }

        public void Dispose()
        {
            if (_sortScreenManager != null)
            {
                _sortScreenManager.SortButtonPressed -= OnSortButtonPressed;
                _sortScreenManager.SortDirectionChanged -= OnSortDirectionChanged;
                _sortScreenManager.SortCancelled -= OnSortCancelled;
            }

            if (_sortListScreenManager != null)
                _sortListScreenManager.SortModeListCellSelected -= OnSortModeListCellSelected;

            if (_sortSettingsTab != null)
                _sortSettingsTab.SortModeListSettingChanged += OnSortModeListChanged;

            IEnumerable<ISortMode> sortModes = Enumerable.Empty<ISortMode>();
            if (_builtInSortModes != null)
                sortModes = sortModes.Concat(_builtInSortModes);
            if (_externalSortModes != null)
                sortModes = sortModes.Concat(_externalSortModes);

            foreach (var sortMode in sortModes)
            {
                if (sortMode != null)
                    sortMode.AvailabilityChanged -= OnSortModeListChanged;
            }
        }

        public void OnLevelCollectionSelected(IAnnotatedBeatmapLevelCollection annotatedBeatmaplevelCollection)
        {
            _isBaseGamePlaylist = annotatedBeatmaplevelCollection.collectionName == null;
        }

        public bool ApplyModifications(IEnumerable<IPreviewBeatmapLevel> levelCollection, out IEnumerable<IPreviewBeatmapLevel> modifiedLevelCollection)
        {
            // reversed default sort for all songs and favorites playlists has to be partially done here,
            // since the songs aren't actually in the expected order
            // (expects alphabetical order, but the sorting was done in
            // LevelCollectionTableView -> AlphabetScrollbarInfoBeatmapLevelHelper.CreateData)
            if (CurrentSortMode == _defaultSortMode && !SortAscending && _isBaseGamePlaylist)
                levelCollection = levelCollection.OrderBy(x => x.songName.ToUpperInvariant());

            modifiedLevelCollection = CurrentSortMode.SortSongs(levelCollection, SortAscending);
            return !IsDefaultSort;
        }

        private void RefreshSortModeList()
        {
            Plugin.Log.Debug("Refreshing sort mode list");

            var sortSettings = PluginConfig.Instance.Sort;

            // do not allow default sort mode to be hidden
            sortSettings.HiddenSortModes.Remove(_defaultSortMode.GetIdentifier());

            IEnumerable<ISortMode> sortModeOrdering = _builtInSortModes.Concat(_externalSortModes.OrderBy(x => x.Name));
            List<string> savedOrdering = sortSettings.SortModeOrdering;
            if (savedOrdering.Count == 0)
            {
                savedOrdering = sortModeOrdering.Select(x => x.GetIdentifier()).ToList();
                sortSettings.SortModeOrdering = savedOrdering;
            }
            else
            {
                Dictionary<string, int> savedOrderMap = new Dictionary<string, int>(sortModeOrdering.Count());
                for (int i = 0; i < savedOrdering.Count; ++i)
                    savedOrderMap.Add(savedOrdering[i], i);

                // some default order is needed to position newly added sort modes
                Dictionary<string, int> defaultOrderMap = new Dictionary<string, int>(sortModeOrdering.Count());
                int j = 0;
                foreach (var sortMode in sortModeOrdering)
                    defaultOrderMap.Add(sortMode.GetIdentifier(), j++);

                sortModeOrdering = sortModeOrdering.OrderBy(delegate (ISortMode sortMode)
                {
                    string id = sortMode.GetIdentifier();
                    if (savedOrderMap.ContainsKey(id))
                        return savedOrderMap[id];
                    else
                        return defaultOrderMap[id];
                });

                if (savedOrdering.Count != sortModeOrdering.Count())
                    sortSettings.SortModeOrdering = sortModeOrdering.Select(x => x.GetIdentifier()).ToList();
            }

            IEnumerable<ISortMode> unhiddenSortModes = sortModeOrdering.Where(x => !sortSettings.HiddenSortModes.Contains(x.GetIdentifier()));
            if (sortSettings.HideUnavailable)
                unhiddenSortModes = unhiddenSortModes.Where(x => x.IsAvailable);
            else
                unhiddenSortModes = unhiddenSortModes.OrderByDescending(x => x.IsAvailable);

            SortModes = unhiddenSortModes.ToList().AsReadOnly();

            _sortListScreenManager.RefreshSortModeList(SortModes);
            _sortSettingsTab.RefreshSortModeList(sortModeOrdering);
        }

        private void OnSortButtonPressed()
        {
            if (_sortListScreenManager.IsVisible)
                _sortListScreenManager.HideScreen();
            else
                _sortListScreenManager.ShowScreen();
        }

        private void OnSortModeListChanged()
        {
            RefreshSortModeList();

            // reset to default if the current sort mode is unavailable
            if (!CurrentSortMode.IsAvailable)
                ApplyDefaultSort();
        }

        private void OnSortDirectionChanged()
        {
            ApplySortMode(CurrentSortMode, !SortAscending);

            _sortListScreenManager.HideScreen();
        }

        private void OnSortCancelled()
        {
            ApplyDefaultSort();

            _sortListScreenManager.HideScreen();
        }

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
                _sortListScreenManager.SelectSortMode(index);
            }
        }

        internal void ApplySortMode(ISortMode sortMode, bool ascending)
        {
            CurrentSortMode = sortMode;
            SortAscending = ascending;

            PluginConfig.Instance.Sort.LastSortModeID = sortMode.GetIdentifier();
            PluginConfig.Instance.Sort.LastSortModeIsAscending = ascending;

            _sortListScreenManager.SelectSortMode(SortModes.IndexOf(sortMode));
            _sortScreenManager.SortText = sortMode.Name.EscapeTextMeshProTags();
            _sortScreenManager.SortAscending = ascending;

            RequestLevelCollectionRefresh();
        }

        internal void ApplyDefaultSort() => ApplySortMode(_defaultSortMode, _defaultSortMode.DefaultSortByAscending);

        private void RequestLevelCollectionRefresh() => this.CallAndHandleAction(LevelCollectionRefreshRequested, nameof(LevelCollectionRefreshRequested), false);
    }
}
