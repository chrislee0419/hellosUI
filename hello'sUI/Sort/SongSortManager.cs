using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;
using IPA.Utilities;
using SongCore;
using HUI.Sort.BuiltIn;
using HUI.Utilities;

namespace HUI.Sort
{
    public class SongSortManager : SinglePlayerManagerBase
    {
        public event Action SortModeAvailabilityChanged;

        // expose instance to mods that don't use SiraUtil
        public static SongSortManager Instance { get; private set; }

        public IReadOnlyList<ISortMode> SortModes { get; private set; }
        public ISortMode CurrentSortMode { get; private set; }
        public bool SortAscending { get; private set; }

        private DefaultSortMode _defaultSortMode;
        private List<ISortMode> _builtInSortModes;
        private List<ISortMode> _externalSortModes;

        private IAnnotatedBeatmapLevelCollection _originalLevelCollection;
        private SortedBeatmapLevelPack _sortedLevelPack = new SortedBeatmapLevelPack();

        private LevelFilteringNavigationController _levelFilteringNavigationController;
        private LevelCollectionNavigationController _levelCollectionNavigationController;
        private LevelSelectionNavigationController _levelSelectionNavigationController;

        private static readonly FieldAccessor<LevelCollectionNavigationController, bool>.Accessor ShowPlayerStatsAccessor = FieldAccessor<LevelCollectionNavigationController, bool>.GetAccessor("_showPlayerStatsInDetailView");
        private static readonly FieldAccessor<LevelCollectionNavigationController, bool>.Accessor ShowPracticeButtonAccessor = FieldAccessor<LevelCollectionNavigationController, bool>.GetAccessor("_showPracticeButtonInDetailView");
        private static readonly FieldAccessor<LevelCollectionNavigationController, string>.Accessor ActionButtonTextAccessor = FieldAccessor<LevelCollectionNavigationController, string>.GetAccessor("_actionButtonTextInDetailView");
        private static readonly FieldAccessor<LevelSelectionNavigationController, BeatmapDifficultyMask>.Accessor AllowedBeatmapDifficultyMaskAccessor = FieldAccessor<LevelSelectionNavigationController, BeatmapDifficultyMask>.GetAccessor("_allowedBeatmapDifficultyMask");
        private static readonly FieldAccessor<LevelSelectionNavigationController, BeatmapCharacteristicSO[]>.Accessor NotAllowedCharacteristicsAccessor = FieldAccessor<LevelSelectionNavigationController, BeatmapCharacteristicSO[]>.GetAccessor("_notAllowedCharacteristics");

        [Inject]
        public SongSortManager(
            MainMenuViewController mainMenuVC,
            SoloFreePlayFlowCoordinator soloFC,
            PartyFreePlayFlowCoordinator partyFC,
            LevelFilteringNavigationController levelFilteringNavigationController,
            LevelCollectionNavigationController levelCollectionNavigationController,
            LevelSelectionNavigationController levelSelectionNavigationController,
            DefaultSortMode defaultSort,
            NewestSortMode newestSort,
            PlayCountSortMode playCountSort,
            List<ISortMode> externalSortModes)
            : base(mainMenuVC, soloFC, partyFC)
        {
            _levelFilteringNavigationController = levelFilteringNavigationController;
            _levelCollectionNavigationController = levelCollectionNavigationController;
            _levelSelectionNavigationController = levelSelectionNavigationController;

            _defaultSortMode = defaultSort;
            _builtInSortModes = new List<ISortMode> { defaultSort, newestSort, playCountSort };
            _externalSortModes = externalSortModes;

            RefreshSortModeAvailablity();

            Instance = this;
        }

        public void RefreshSortModeAvailablity()
        {
            SortModes = _builtInSortModes
                .Concat(_externalSortModes.OrderBy(x => x.Name))
                .OrderByDescending(x => x.IsAvailable)
                .ToList()
                .AsReadOnly();

            ApplyDefaultSort();

            try
            {
                SortModeAvailabilityChanged?.Invoke();
            }
            catch (Exception e)
            {
                Plugin.Log.Warn($"Unexpected exception occurred in {nameof(SongSortManager)}:{nameof(SortModeAvailabilityChanged)} event");
                Plugin.Log.Debug(e);
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            OnSinglePlayerLevelSelectionFinished();
        }

        protected override void OnSinglePlayerLevelSelectionStarting()
        {
            _levelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent += OnAnnotatedBeatmapLevelCollectionSelected;
            Loader.OnLevelPacksRefreshed += OnLevelPacksRefreshed;

            // apply sort mode on next frame
            ApplySortedLevelPackDelayed();
        }

        protected override void OnSinglePlayerLevelSelectionFinished()
        {
            _levelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent -= OnAnnotatedBeatmapLevelCollectionSelected;
            Loader.OnLevelPacksRefreshed -= OnLevelPacksRefreshed;
        }

        private void OnAnnotatedBeatmapLevelCollectionSelected(LevelFilteringNavigationController unused, IAnnotatedBeatmapLevelCollection levelCollection, GameObject unused2, BeatmapCharacteristicSO unused3)
        {
            _originalLevelCollection = levelCollection;
            Plugin.Log.Debug($"SongSortManager storing level collection \"{levelCollection.collectionName}\"");

            // reapply sort mode on next frame
            ApplySortedLevelPackDelayed();
        }

        private void OnLevelPacksRefreshed() => ApplySortedLevelPackDelayed();

        private void ApplySortedLevelPackDelayed(int framesToWait = 1, bool waitForEndOfFrame = true)
        {
            if (!(CurrentSortMode is DefaultSortMode) || !SortAscending)
                CoroutineUtilities.StartDelayedAction(ApplySortedLevelPack, framesToWait, waitForEndOfFrame);
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

                ApplySortedLevelPack();
            }
        }

        internal void ApplyDefaultSort()
        {
            CurrentSortMode = _defaultSortMode;
            SortAscending = true;

            if (_levelCollectionNavigationController.isActiveAndEnabled)
            {
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

        private void ApplySortedLevelPack()
        {
            var sortedLevels = CurrentSortMode.SortSongs(_originalLevelCollection.beatmapLevelCollection.beatmapLevels, SortAscending);

            _levelCollectionNavigationController.SetDataForPack(
                _sortedLevelPack.SetupFromLevels(_originalLevelCollection, sortedLevels),
                true,
                ShowPlayerStatsAccessor(ref _levelCollectionNavigationController),
                ShowPracticeButtonAccessor(ref _levelCollectionNavigationController),
                ActionButtonTextAccessor(ref _levelCollectionNavigationController));
        }
    }
}
