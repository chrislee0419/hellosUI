using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using HMUI;
using VRUIControls;
using IPA.Utilities;
using BeatSaberMarkupLanguage.Attributes;
using HUI.HarmonyPatches;
using HUI.UI.Components;
using HUI.UI.Settings;
using HUI.DataFlow;
using HUI.Utilities;
using Random = System.Random;

namespace HUI.UI.Screens
{
    public class ScrollerScreenManager : ModifiableScreenManagerBase
    {
        public override string ScreenName => "Level Scroller";
        protected override string AssociatedBSMLResource => "HUI.UI.Views.Screens.ScrollerScreenView.bsml";

        private bool _upButtonInteractable = false;
        [UIValue("up-button-interactable")]
        public bool UpButtonInteractable
        {
            get => _upButtonInteractable;
            set
            {
                if (_upButtonInteractable == value)
                    return;

                _upButtonInteractable = value;
                NotifyPropertyChanged();
            }
        }

        private bool _randomButtonInteractable = false;
        [UIValue("random-button-interactable")]
        public bool RandomButtonInteractable
        {
            get => _randomButtonInteractable;
            set
            {
                if (_randomButtonInteractable == value)
                    return;

                _randomButtonInteractable = value;
                NotifyPropertyChanged();
            }
        }

        private bool _downButtonInteractable = false;
        [UIValue("down-button-interactable")]
        public bool DownButtonInteractable
        {
            get => _downButtonInteractable;
            set
            {
                if (_downButtonInteractable == value)
                    return;

                _downButtonInteractable = value;
                NotifyPropertyChanged();
            }
        }

#pragma warning disable CS0649
        [UIObject("settings-button")]
        private GameObject _settingsButton;
        [UIObject("up-button")]
        private GameObject _upButton;
        [UIObject("down-button")]
        private GameObject _downButton;
        [UIObject("random-button")]
        private GameObject _randomButton;
#pragma warning restore CS0649

        private TableView _levelsTableView;
        private ScrollView _scrollView;

        private LevelCollectionDataFlowManager _levelCollectionDataFlowManager;
        private SettingsModalDispatcher _settingsModalDispatcher;

        private Random _rng = new Random();

        private static readonly FieldAccessor<ScrollView, float>.Accessor DestinationPosAccessor = FieldAccessor<ScrollView, float>.GetAccessor("_destinationPos");
        private static readonly FieldAccessor<LevelCollectionTableView, bool>.Accessor ShowLevelPackHeaderAccessor = FieldAccessor<LevelCollectionTableView, bool>.GetAccessor("_showLevelPackHeader");

        public ScrollerScreenManager(
            MainMenuViewController mainMenuVC,
            SoloFreePlayFlowCoordinator soloFC,
            PartyFreePlayFlowCoordinator partyFC,
            LevelCollectionNavigationController levelCollectionNC,
            PhysicsRaycasterWithCache physicsRaycaster,
            LevelCollectionViewController levelCollectionViewController,
            LevelCollectionDataFlowManager levelCollectionDataFlowManager,
            SettingsModalDispatcher settingsModalDispatcher)
            : base(mainMenuVC, soloFC, partyFC, levelCollectionNC, physicsRaycaster, new Vector2(9f, 58f), new Vector3(-2.35f, 1.2f, 3.6f), Quaternion.Euler(0f, 325f, 0f))
        {
            _levelCollectionDataFlowManager = levelCollectionDataFlowManager;
            _settingsModalDispatcher = settingsModalDispatcher;

            this._screen.name = "HUISongScrollerScreen";

            this._animationHandler.UsePointerAnimations = false;

            Transform imageTransform = _upButton.transform.Find("Content/Icon");
            imageTransform.Rotate(0f, 0f, 180f, Space.Self);

            // destroy ContentSizeFitter so the anchors are used
            Object.Destroy(_settingsButton.GetComponent<ContentSizeFitter>());
            Object.Destroy(_upButton.GetComponent<ContentSizeFitter>());
            Object.Destroy(_downButton.GetComponent<ContentSizeFitter>());
            Object.Destroy(_randomButton.GetComponent<ContentSizeFitter>());

            // remove underline
            Object.Destroy(_settingsButton.transform.Find("Underline").gameObject);
            Object.Destroy(_upButton.transform.Find("Underline").gameObject);
            Object.Destroy(_downButton.transform.Find("Underline").gameObject);
            Object.Destroy(_randomButton.transform.Find("Underline").gameObject);

            // remove skew
            _settingsButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);
            _upButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);
            _downButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);
            _randomButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);

            // add custom animations
            Object.Destroy(_settingsButton.GetComponent<ButtonStaticAnimations>());
            Object.Destroy(_upButton.GetComponent<ButtonStaticAnimations>());
            Object.Destroy(_downButton.GetComponent<ButtonStaticAnimations>());
            Object.Destroy(_randomButton.GetComponent<ButtonStaticAnimations>());

            Color settingsButtonHighlightedColour = new Color(0.4f, 1f, 0.15f);
            Color pageButtonHighlightedColour = new Color(1f, 0.375f, 0f);
            Color randomButtonHighlighedColour = new Color(0.145f, 0.443f, 1f);

            var btnAnims = _settingsButton.AddComponent<CustomIconButtonAnimations>();
            btnAnims.HighlightedBGColour = settingsButtonHighlightedColour;
            btnAnims.PressedBGColour = settingsButtonHighlightedColour;

            btnAnims = _upButton.AddComponent<CustomIconButtonAnimations>();
            btnAnims.HighlightedBGColour = pageButtonHighlightedColour;
            btnAnims.PressedBGColour = pageButtonHighlightedColour;

            btnAnims = _downButton.AddComponent<CustomIconButtonAnimations>();
            btnAnims.HighlightedBGColour = pageButtonHighlightedColour;
            btnAnims.PressedBGColour = pageButtonHighlightedColour;

            btnAnims = _randomButton.AddComponent<CustomIconButtonAnimations>();
            btnAnims.HighlightedBGColour = randomButtonHighlighedColour;
            btnAnims.PressedBGColour = randomButtonHighlighedColour;

            var levelCollectionTableView = FieldAccessor<LevelCollectionViewController, LevelCollectionTableView>.Get(ref levelCollectionViewController, "_levelCollectionTableView");

            _levelsTableView = FieldAccessor<LevelCollectionTableView, TableView>.Get(ref levelCollectionTableView, "_tableView");
        }

        public override void Initialize()
        {
            base.Initialize();

            _levelCollectionDataFlowManager.LevelCollectionApplied += OnLevelCollectionApplied;
            _levelCollectionDataFlowManager.EmptyLevelCollectionApplied += OnEmptyLevelCollectionApplied;
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_levelCollectionDataFlowManager != null)
            {
                _levelCollectionDataFlowManager.LevelCollectionApplied -= OnLevelCollectionApplied;
                _levelCollectionDataFlowManager.EmptyLevelCollectionApplied -= OnEmptyLevelCollectionApplied;
            }

            if (_scrollView != null)
                ScrollViewRefreshButtonsPatch.RemoveHook(_scrollView, RefreshPageButtons);
        }

        protected override void OnSinglePlayerLevelSelectionStarting(bool isSolo)
        {
            base.OnSinglePlayerLevelSelectionStarting(isSolo);

            if (_scrollView == null)
            {
                _scrollView = FieldAccessor<TableView, ScrollView>.Get(ref _levelsTableView, "_scrollView");
                ScrollViewRefreshButtonsPatch.InstallHook(_scrollView, RefreshPageButtons);
            }
        }

        public void RefreshPageButtons(bool pageUpInteractable, bool pageDownInteractable)
        {
            UpButtonInteractable = pageUpInteractable;
            DownButtonInteractable = pageDownInteractable;
        }

        private void OnLevelCollectionApplied(IEnumerable<IPreviewBeatmapLevel> levels)
        {
            RandomButtonInteractable = levels.Count() > 0;
        }

        private void OnEmptyLevelCollectionApplied()
        {
            UpButtonInteractable = false;
            DownButtonInteractable = false;
            RandomButtonInteractable = false;
        }

        [UIAction("settings-button-clicked")]
        private void OnSettingsButtonClicked() => _settingsModalDispatcher.ToggleModalVisibility();

        [UIAction("up-button-clicked")]
        private void OnUpButtonClicked()
        {
            float numOfVisibleCells = Mathf.Ceil(_levelsTableView.viewportTransform.rect.height / _levelsTableView.cellSize);
            float newDestinationPos = DestinationPosAccessor(ref _scrollView) - Mathf.Max(1f, numOfVisibleCells - 1f) * _levelsTableView.cellSize * PluginConfig.Instance.FastScrollSpeed;

            // note: clamping is done during the set, so we don't have to do it
            _scrollView.SetDestinationPos(newDestinationPos);
            _scrollView.RefreshButtons();
            _scrollView.enabled = true;
        }

        [UIAction("down-button-clicked")]
        private void OnDownButtonClicked()
        {
            float maxPosition = _levelsTableView.numberOfCells * _levelsTableView.cellSize - _levelsTableView.viewportTransform.rect.height;
            float numOfVisibleCells = Mathf.Ceil(_levelsTableView.viewportTransform.rect.height / _levelsTableView.cellSize);
            float newDestinationPos = DestinationPosAccessor(ref _scrollView) + Mathf.Max(1f, numOfVisibleCells - 1f) * _levelsTableView.cellSize * PluginConfig.Instance.FastScrollSpeed;

            _scrollView.SetDestinationPos(newDestinationPos);
            _scrollView.RefreshButtons();
            _scrollView.enabled = true;
        }

        [UIAction("random-button-clicked")]
        private void OnRandomButtonClicked()
        {
            if (_levelsTableView.numberOfCells == 0)
                return;

            int index = _rng.Next(_levelsTableView.numberOfCells);

            // if we get the level pack header, try getting 
            var levelCollectionTableView = _levelsTableView.dataSource as LevelCollectionTableView;
            if (index == 0 && ShowLevelPackHeaderAccessor.Invoke(ref levelCollectionTableView))
                index = _rng.Next(_levelsTableView.numberOfCells);

            _levelsTableView.ScrollToCellWithIdx(index, TableView.ScrollPositionType.Beginning, false);
        }
    }
}
