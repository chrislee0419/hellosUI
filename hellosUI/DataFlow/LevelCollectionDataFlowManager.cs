using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using HMUI;
using IPA.Utilities;
using SongCore;
using HUI.Interfaces;
using HUI.Search;
using HUI.Sort;
using HUI.Utilities;

namespace HUI.DataFlow
{
    public class LevelCollectionDataFlowManager : SinglePlayerManagerBase
    {
        public event Action<IEnumerable<IPreviewBeatmapLevel>> LevelCollectionApplied;
        public event Action EmptyLevelCollectionApplied;

        private IAnnotatedBeatmapLevelCollection _originalLevelCollection;
        private CustomBeatmapLevelPack _customLevelPack = new CustomBeatmapLevelPack();

        private BeatmapLevelsModel _beatmapLevelsModel;
        private LevelFilteringNavigationController _levelFilteringNavigationController;
        private LevelCollectionNavigationController _levelCollectionNavigationController;
        private LevelSelectionNavigationController _levelSelectionNavigationController;
        private LevelCollectionViewController _levelCollectionViewController;

        private SearchManager _searchManager;
        private SongSortManager _sortManager;
        private List<ILevelCollectionModifier> _externalModifiers;

        private bool _levelCollectionRefreshing = false;
        private bool _selectLastLevel = false;

        private static readonly FieldAccessor<LevelCollectionNavigationController, bool>.Accessor ShowPlayerStatsAccessor = FieldAccessor<LevelCollectionNavigationController, bool>.GetAccessor("_showPlayerStatsInDetailView");
        private static readonly FieldAccessor<LevelCollectionNavigationController, bool>.Accessor ShowPracticeButtonAccessor = FieldAccessor<LevelCollectionNavigationController, bool>.GetAccessor("_showPracticeButtonInDetailView");
        private static readonly FieldAccessor<LevelCollectionNavigationController, string>.Accessor ActionButtonTextAccessor = FieldAccessor<LevelCollectionNavigationController, string>.GetAccessor("_actionButtonTextInDetailView");
        private static readonly FieldAccessor<LevelSelectionNavigationController, BeatmapDifficultyMask>.Accessor AllowedBeatmapDifficultyMaskAccessor = FieldAccessor<LevelSelectionNavigationController, BeatmapDifficultyMask>.GetAccessor("_allowedBeatmapDifficultyMask");
        private static readonly FieldAccessor<LevelSelectionNavigationController, BeatmapCharacteristicSO[]>.Accessor NotAllowedCharacteristicsAccessor = FieldAccessor<LevelSelectionNavigationController, BeatmapCharacteristicSO[]>.GetAccessor("_notAllowedCharacteristics");
        private static readonly FieldAccessor<LevelCollectionViewController, LevelCollectionTableView>.Accessor LevelCollectionTableViewAccessor = FieldAccessor<LevelCollectionViewController, LevelCollectionTableView>.GetAccessor("_levelCollectionTableView");
        private static readonly FieldAccessor<LevelCollectionTableView, int>.Accessor SelectedRowAccessor = FieldAccessor<LevelCollectionTableView, int>.GetAccessor("_selectedRow");
        private static readonly FieldAccessor<LevelCollectionTableView, bool>.Accessor ShowLevelPackHeaderAccessor = FieldAccessor<LevelCollectionTableView, bool>.GetAccessor("_showLevelPackHeader");
        private static readonly FieldAccessor<LevelCollectionTableView, TableView>.Accessor TableViewAccessor = FieldAccessor<LevelCollectionTableView, TableView>.GetAccessor("_tableView");

        public LevelCollectionDataFlowManager(
            MainMenuViewController mainMenuVC,
            SoloFreePlayFlowCoordinator soloFC,
            PartyFreePlayFlowCoordinator partyFC,
            BeatmapLevelsModel beatmapLevelsModel,
            LevelFilteringNavigationController levelFilteringNavigationController,
            LevelCollectionNavigationController levelCollectionNavigationController,
            LevelSelectionNavigationController levelSelectionNavigationController,
            LevelCollectionViewController levelCollectionViewController,
            SearchManager searchManager,
            SongSortManager sortManager,
            List<ILevelCollectionModifier> modifiers)
            : base(mainMenuVC, soloFC, partyFC)
        {
            _beatmapLevelsModel = beatmapLevelsModel;
            _levelFilteringNavigationController = levelFilteringNavigationController;
            _levelCollectionNavigationController = levelCollectionNavigationController;
            _levelSelectionNavigationController = levelSelectionNavigationController;
            _levelCollectionViewController = levelCollectionViewController;

            _searchManager = searchManager;
            _sortManager = sortManager;

            // since the search and sort managers are bound to the ILevelCollectionModifier interface as wel,
            // remove them from the list when assigning to _externalModifiers
            _externalModifiers = new List<ILevelCollectionModifier>(modifiers);
            _externalModifiers.Remove(_searchManager);
            _externalModifiers.Remove(_sortManager);
        }

        public override void Initialize()
        {
            base.Initialize();

            _searchManager.LevelCollectionRefreshRequested += OnLevelCollectionRefreshRequested;
            _sortManager.LevelCollectionRefreshRequested += OnLevelCollectionRefreshRequested;

            foreach (var modifier in _externalModifiers)
                modifier.LevelCollectionRefreshRequested += OnLevelCollectionRefreshRequested;
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_levelFilteringNavigationController != null)
                _levelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent -= OnAnnotatedBeatmapLevelCollectionSelected;

            Loader.OnLevelPacksRefreshed -= OnLevelPacksRefreshed;

            if (_searchManager != null)
                _searchManager.LevelCollectionRefreshRequested -= OnLevelCollectionRefreshRequested;

            if (_sortManager != null)
                _sortManager.LevelCollectionRefreshRequested -= OnLevelCollectionRefreshRequested;

            if (_externalModifiers != null)
            {
                foreach (var modifier in _externalModifiers)
                {
                    if (modifier != null)
                        modifier.LevelCollectionRefreshRequested -= OnLevelCollectionRefreshRequested;
                }
            }
        }

        protected override void OnSinglePlayerLevelSelectionStarting(bool isSolo)
        {
            _levelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent += OnAnnotatedBeatmapLevelCollectionSelected;
            _levelCollectionViewController.didSelectLevelEvent += OnLevelSelected;
            Loader.OnLevelPacksRefreshed += OnLevelPacksRefreshed;

            if (!string.IsNullOrEmpty(PluginConfig.Instance.LastLevelPackID))
            {
                SinglePlayerLevelSelectionFlowCoordinator levelSelectionFlowCoordinator;
                if (isSolo)
                    levelSelectionFlowCoordinator = _soloFC;
                else
                    levelSelectionFlowCoordinator = _partyFC;

                IBeatmapLevelPack lastLevelPack = _beatmapLevelsModel.GetLevelPack(PluginConfig.Instance.LastLevelPackID);
                if (lastLevelPack != null)
                {
                    var startState = new LevelSelectionFlowCoordinator.State(lastLevelPack);
                    FieldAccessor<LevelSelectionFlowCoordinator.State, SelectLevelCategoryViewController.LevelCategory?>
                        .Set(ref startState, "levelCategory", PluginConfig.Instance.LastLevelCategory);

                    // note: startState is set to null after fc activation, so we don't have to do any clean up there
                    levelSelectionFlowCoordinator.Setup(startState);
                }

                _selectLastLevel = true;
            }
            else
            {
                PluginConfig.Instance.LastLevelCategory = SelectLevelCategoryViewController.LevelCategory.None;
                PluginConfig.Instance.LastLevelPackID = null;
                PluginConfig.Instance.LastLevelID = null;
            }
        }

        protected override void OnSinglePlayerLevelSelectionFinished()
        {
            _levelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent -= OnAnnotatedBeatmapLevelCollectionSelected;
            _levelCollectionViewController.didSelectLevelEvent -= OnLevelSelected;
            Loader.OnLevelPacksRefreshed -= OnLevelPacksRefreshed;
        }

        private void OnAnnotatedBeatmapLevelCollectionSelected(LevelFilteringNavigationController controller, IAnnotatedBeatmapLevelCollection levelCollection, GameObject noDataInfoPrefab, BeatmapCharacteristicSO preferredBeatmapCharacteristic)
        {
            _originalLevelCollection = levelCollection;

            if (levelCollection is IBeatmapLevelPack levelPack)
            {
                PluginConfig.Instance.LastLevelCategory = _levelFilteringNavigationController.selectedLevelCategory;
                PluginConfig.Instance.LastLevelPackID = levelPack.packID;
            }
            else
            {
                PluginConfig.Instance.LastLevelCategory = SelectLevelCategoryViewController.LevelCategory.None;
                PluginConfig.Instance.LastLevelPackID = null;
            }

            ApplyCustomLevelCollectionDelayed();

            Plugin.Log.Debug($"{nameof(LevelCollectionDataFlowManager)} storing level collection named \"{levelCollection.collectionName}\"");

            _searchManager.OnLevelCollectionSelected(levelCollection);
            _sortManager.OnLevelCollectionSelected(levelCollection);
            foreach (var externalModifier in _externalModifiers)
                externalModifier.OnLevelCollectionSelected(levelCollection);
        }

        private void OnLevelSelected(LevelCollectionViewController viewController, IPreviewBeatmapLevel level)
        {
            StringBuilder sb = new StringBuilder(level.levelID)
                .Append(PluginConfig.LastLevelIDSeparator)
                .Append(level.songName)
                .Append(PluginConfig.LastLevelIDSeparator)
                .Append(level.levelAuthorName);

            PluginConfig.Instance.LastLevelID = sb.ToString();
        }

        private void OnLevelCollectionRefreshRequested()
        {
            if (_originalLevelCollection == null)
                return;

            ApplyCustomLevelCollectionDelayed();
        }

        private void OnLevelPacksRefreshed() => ApplyCustomLevelCollectionDelayed();

        private void ApplyCustomLevelCollectionDelayed(int framesToWait = 1, bool waitForEndOfFrame = true)
        {
            if (_levelCollectionRefreshing)
                return;

            _levelCollectionRefreshing = true;
            CoroutineUtilities.StartDelayedAction(ApplyCustomLevelCollection, framesToWait, waitForEndOfFrame);
        }

        private void ApplyCustomLevelCollection()
        {
            _levelCollectionRefreshing = false;

            IEnumerable<IPreviewBeatmapLevel> levelCollection = _originalLevelCollection.beatmapLevelCollection.beatmapLevels;
            IEnumerable<IPreviewBeatmapLevel> modifiedLevelCollection;

            // check search manager first, since an ongoing search will set an empty level collection
            // and request a refresh when it is finished
            bool hasChanges = _searchManager.ApplyModifications(levelCollection, out modifiedLevelCollection);
            if (hasChanges)
                levelCollection = modifiedLevelCollection;

            if (levelCollection.Count() > 0)
            {
                foreach (var modifier in _externalModifiers)
                {
                    if (modifier.ApplyModifications(levelCollection, out modifiedLevelCollection))
                    {
                        levelCollection = modifiedLevelCollection;
                        hasChanges = true;

                        if (levelCollection.Count() == 0)
                            break;
                    }
                }

                if (_sortManager.ApplyModifications(levelCollection, out modifiedLevelCollection))
                {
                    levelCollection = modifiedLevelCollection;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
#if DEBUG
                Plugin.Log.Debug($"Applying modified IAnnotatedBeatmapLevelCollection to song list");
#endif
                _levelCollectionNavigationController.SetDataForPack(
                    _customLevelPack.SetupFromLevels(_originalLevelCollection, levelCollection),
                    true,
                    ShowPlayerStatsAccessor(ref _levelCollectionNavigationController),
                    ShowPracticeButtonAccessor(ref _levelCollectionNavigationController),
                    ActionButtonTextAccessor(ref _levelCollectionNavigationController));
            }
            else
            {
#if DEBUG
                Plugin.Log.Debug($"Applying unmodified IAnnotatedBeatmapLevelCollection to song list");
#endif
                _levelCollectionNavigationController.SetData(
                    _originalLevelCollection,
                    true,
                    ShowPlayerStatsAccessor(ref _levelCollectionNavigationController),
                    ShowPracticeButtonAccessor(ref _levelCollectionNavigationController),
                    ActionButtonTextAccessor(ref _levelCollectionNavigationController),
                    null,
                    AllowedBeatmapDifficultyMaskAccessor(ref _levelSelectionNavigationController),
                    NotAllowedCharacteristicsAccessor(ref _levelSelectionNavigationController));

                levelCollection = _originalLevelCollection.beatmapLevelCollection.beatmapLevels;
            }


            // when the list of levels is empty, the game just deactivates the levels TableView (doesn't clear the list either)
            // which means it doesn't update the interactable state of this mod's fast page up/down buttons
            // hence, we do update the interactable state here
            if (!levelCollection.Any())
            {
                this.CallAndHandleAction(EmptyLevelCollectionApplied, nameof(EmptyLevelCollectionApplied));
            }
            else if (_selectLastLevel && !string.IsNullOrEmpty(PluginConfig.Instance.LastLevelID))
            {
                string[] lastLevelData = PluginConfig.Instance.LastLevelID.Split(PluginConfig.LastLevelIDSeparatorArray, StringSplitOptions.None);
                if (lastLevelData.Length == 3)
                {
                    string levelID = lastLevelData[0];

                    IPreviewBeatmapLevel lastLevel = levelCollection.FirstOrDefault(x => x.levelID == levelID);

                    // couldn't find the level by its id
                    // it could be a WIP level, so look again, but now with the song title and level author
                    if (lastLevel == null)
                    {
                        string songName = lastLevelData[1];
                        string levelAuthor = lastLevelData[2];

                        lastLevel = levelCollection.FirstOrDefault(x => x.songName == songName && x.levelAuthorName == levelAuthor);
                    }

                    if (lastLevel != null)
                        _levelCollectionNavigationController.SelectLevel(lastLevel);
                }
            }

            _selectLastLevel = false;

            this.CallAndHandleAction(LevelCollectionApplied, nameof(LevelCollectionApplied), levelCollection);
        }
    }
}
