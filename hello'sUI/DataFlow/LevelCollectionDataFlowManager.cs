using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
        private IAnnotatedBeatmapLevelCollection _originalLevelCollection;
        private CustomBeatmapLevelPack _customLevelPack = new CustomBeatmapLevelPack();

        private LevelFilteringNavigationController _levelFilteringNavigationController;
        private LevelCollectionNavigationController _levelCollectionNavigationController;
        private LevelSelectionNavigationController _levelSelectionNavigationController;

        private SearchManager _searchManager;
        private SongSortManager _sortManager;
        private List<ILevelCollectionModifier> _externalModifiers;

        private bool _levelCollectionRefreshing = false;

        private static readonly FieldAccessor<LevelCollectionNavigationController, bool>.Accessor ShowPlayerStatsAccessor = FieldAccessor<LevelCollectionNavigationController, bool>.GetAccessor("_showPlayerStatsInDetailView");
        private static readonly FieldAccessor<LevelCollectionNavigationController, bool>.Accessor ShowPracticeButtonAccessor = FieldAccessor<LevelCollectionNavigationController, bool>.GetAccessor("_showPracticeButtonInDetailView");
        private static readonly FieldAccessor<LevelCollectionNavigationController, string>.Accessor ActionButtonTextAccessor = FieldAccessor<LevelCollectionNavigationController, string>.GetAccessor("_actionButtonTextInDetailView");
        private static readonly FieldAccessor<LevelSelectionNavigationController, BeatmapDifficultyMask>.Accessor AllowedBeatmapDifficultyMaskAccessor = FieldAccessor<LevelSelectionNavigationController, BeatmapDifficultyMask>.GetAccessor("_allowedBeatmapDifficultyMask");
        private static readonly FieldAccessor<LevelSelectionNavigationController, BeatmapCharacteristicSO[]>.Accessor NotAllowedCharacteristicsAccessor = FieldAccessor<LevelSelectionNavigationController, BeatmapCharacteristicSO[]>.GetAccessor("_notAllowedCharacteristics");

        public LevelCollectionDataFlowManager(
            MainMenuViewController mainMenuVC,
            SoloFreePlayFlowCoordinator soloFC,
            PartyFreePlayFlowCoordinator partyFC,
            LevelFilteringNavigationController levelFilteringNavigationController,
            LevelCollectionNavigationController levelCollectionNavigationController,
            LevelSelectionNavigationController levelSelectionNavigationController,
            SearchManager searchManager,
            SongSortManager sortManager,
            List<ILevelCollectionModifier> modifiers)
            : base(mainMenuVC, soloFC, partyFC)
        {
            _levelFilteringNavigationController = levelFilteringNavigationController;
            _levelCollectionNavigationController = levelCollectionNavigationController;
            _levelSelectionNavigationController = levelSelectionNavigationController;

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

        protected override void OnSinglePlayerLevelSelectionStarting()
        {
            _levelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent += OnAnnotatedBeatmapLevelCollectionSelected;
            Loader.OnLevelPacksRefreshed += OnLevelPacksRefreshed;
        }

        protected override void OnSinglePlayerLevelSelectionFinished()
        {
            _levelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent -= OnAnnotatedBeatmapLevelCollectionSelected;
            Loader.OnLevelPacksRefreshed -= OnLevelPacksRefreshed;
        }

        private void OnAnnotatedBeatmapLevelCollectionSelected(LevelFilteringNavigationController controller, IAnnotatedBeatmapLevelCollection levelCollection, GameObject noDataInfoPrefab, BeatmapCharacteristicSO preferredBeatmapCharacteristic)
        {
            _originalLevelCollection = levelCollection;

            Plugin.Log.Debug($"{nameof(LevelCollectionDataFlowManager)} storing level collection named \"{levelCollection.collectionName}\"");

            _searchManager.OnLevelCollectionSelected(levelCollection);
            _sortManager.OnLevelCollectionSelected(levelCollection);
            foreach (var externalModifier in _externalModifiers)
                externalModifier.OnLevelCollectionSelected(levelCollection);

            ApplyCustomLevelCollectionDelayed();
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
            Plugin.Log.Notice("Applying custom level collection");

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
            }
        }
    }
}
