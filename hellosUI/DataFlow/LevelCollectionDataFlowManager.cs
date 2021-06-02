using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using HMUI;
using IPA.Utilities;
using SongCore;
using HUI.Interfaces;
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
        private TableView _levelsTableView;

        private SongSortManager _sortManager;
        private List<ILevelCollectionModifier> _externalModifiers;

        private bool _levelCollectionRefreshing = false;
        private bool _selectLastLevel = false;
        private int _scrollToIndexAfterDeletion = -1;
        private string _customLevelPackIdToShowAfterDeletion = null;

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
            SongSortManager sortManager,
            List<ILevelCollectionModifier> modifiers)
            : base(mainMenuVC, soloFC, partyFC)
        {
            _beatmapLevelsModel = beatmapLevelsModel;
            _levelFilteringNavigationController = levelFilteringNavigationController;
            _levelCollectionNavigationController = levelCollectionNavigationController;
            _levelSelectionNavigationController = levelSelectionNavigationController;
            _levelCollectionViewController = levelCollectionViewController;

            var levelCollectionTableView = FieldAccessor<LevelCollectionViewController, LevelCollectionTableView>.Get(ref _levelCollectionViewController, "_levelCollectionTableView");
            _levelsTableView = FieldAccessor<LevelCollectionTableView, TableView>.Get(ref levelCollectionTableView, "_tableView");

            _sortManager = sortManager;

            // since the search and sort managers are bound to the ILevelCollectionModifier interface as wel,
            // remove them from the list when assigning to _externalModifiers
            _externalModifiers = new List<ILevelCollectionModifier>(modifiers);
            _externalModifiers.Remove(_sortManager);
        }

        public override void Initialize()
        {
            base.Initialize();

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
            Loader.DeletingSong -= OnDeletingSong;

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
            Loader.DeletingSong += OnDeletingSong;

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

        private void OnLevelCollectionRefreshRequested(bool selectLastLevel)
        {
            if (_originalLevelCollection == null)
                return;

            _selectLastLevel = selectLastLevel;
            ApplyCustomLevelCollectionDelayed();
        }

        private void OnLevelPacksRefreshed() => ApplyCustomLevelCollectionDelayed();

        private void OnDeletingSong()
        {
            Plugin.Log.DebugOnly("Song is going to be deleted, finding index to scroll to after deletion");

            if (_levelsTableView.visibleCells.Count() > 1)
                _scrollToIndexAfterDeletion = _levelsTableView.GetVisibleCellsIdRange().Item1;

            _customLevelPackIdToShowAfterDeletion = PluginConfig.Instance.LastLevelPackID;
        }

        private void ApplyCustomLevelCollectionDelayed(int framesToWait = 1, bool waitForEndOfFrame = true)
        {
            if (_levelCollectionRefreshing)
                return;

            Plugin.Log.DebugOnly("Delaying application of custom level collection");

            _levelCollectionRefreshing = true;
            CoroutineUtilities.StartDelayedAction(ApplyCustomLevelCollection, framesToWait, waitForEndOfFrame);
        }

        private void ApplyCustomLevelCollection()
        {
            _levelCollectionRefreshing = false;

            IEnumerable<IPreviewBeatmapLevel> levelCollection = _originalLevelCollection.beatmapLevelCollection.beatmapLevels;
            IEnumerable<IPreviewBeatmapLevel> modifiedLevelCollection;

            bool hasChanges = false;
            if (levelCollection.Count() > 0)
            {
                foreach (var modifier in _externalModifiers.OrderByDescending(x => x.Priority))
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
                Plugin.Log.DebugOnly($"Applying modified IAnnotatedBeatmapLevelCollection to song list");

                _levelCollectionNavigationController.SetDataForPack(
                    _customLevelPack.SetupFromLevels(_originalLevelCollection, levelCollection),
                    true,
                    ShowPlayerStatsAccessor(ref _levelCollectionNavigationController),
                    ShowPracticeButtonAccessor(ref _levelCollectionNavigationController),
                    ActionButtonTextAccessor(ref _levelCollectionNavigationController));
            }
            else
            {
                Plugin.Log.DebugOnly($"Applying unmodified IAnnotatedBeatmapLevelCollection to song list");

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
            else if (_scrollToIndexAfterDeletion >= 0)
            {
                // when deleting a song from a level pack that isn't "Custom Levels" (ex: different folder or in a playlist)
                // the game selects the "Custom Levels" pack, instead of the originally selected pack
                // as such, we reselect the actual original pack here, after the game has selected the "Custom Levels" pack
                // NOTE: this returns early (clean up can be handled by the subsequent call of this function)
                if (PluginConfig.Instance.LastLevelCategory == SelectLevelCategoryViewController.LevelCategory.CustomSongs &&
                    !string.IsNullOrEmpty(_customLevelPackIdToShowAfterDeletion) &&
                    _customLevelPackIdToShowAfterDeletion != PluginConfig.Instance.LastLevelPackID)
                {
                    Plugin.Log.Debug($"Wrong level pack (id = {PluginConfig.Instance.LastLevelPackID}) selected after song deletion, reselecting original custom level pack (id = {_customLevelPackIdToShowAfterDeletion})");

                    CoroutineUtilities.StartDelayedAction(delegate ()
                    {
                        FieldAccessor<LevelFilteringNavigationController, string>
                            .Set(ref _levelFilteringNavigationController, "_levelPackIdToBeSelectedAfterPresent", _customLevelPackIdToShowAfterDeletion);

                        _levelFilteringNavigationController.UpdateSecondChildControllerContent(SelectLevelCategoryViewController.LevelCategory.CustomSongs);
                    });

                    return;
                }

                Plugin.Log.DebugOnly($"Scrolling to index = {_scrollToIndexAfterDeletion} after deleting song");

                // scroll back to where we were before deleting the song
                _levelsTableView.ScrollToCellWithIdx(_scrollToIndexAfterDeletion, TableView.ScrollPositionType.Beginning, false);
            }
            else if (_selectLastLevel && !string.IsNullOrEmpty(PluginConfig.Instance.LastLevelID))
            {
                Plugin.Log.DebugOnly($"Reselecting level \"{PluginConfig.Instance.LastLevelID}\" after modifying song list");

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
            _scrollToIndexAfterDeletion = -1;
            _customLevelPackIdToShowAfterDeletion = null;

            this.CallAndHandleAction(LevelCollectionApplied, nameof(LevelCollectionApplied), levelCollection);
        }
    }
}
