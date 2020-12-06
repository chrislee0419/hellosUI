using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using HMUI;
using VRUIControls;
using IPA.Utilities;
using BeatSaberMarkupLanguage.Attributes;
using BS_Utils.Utilities;
using HUI.UI.Components;
using HUI.DataFlow;
using HUI.Utilities;
using Random = System.Random;

namespace HUI.UI.Screens
{
    public class ScrollerScreenManager : ScreenManagerBase
    {
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

        private Button _originalUpButton;
        private Button _originalDownButton;
        private UnityAction _buttonListener;
        private TableView _levelsTableView;
        private TableViewScroller _scroller;

        private LevelCollectionDataFlowManager _levelCollectionDataFlowManager;
        private SettingsModalManager _settingsModalManager;

        private Random _rng = new Random();

        private static readonly FieldAccessor<TableViewScroller, float>.Accessor TargetPositionAccessor = FieldAccessor<TableViewScroller, float>.GetAccessor("_targetPosition");
        private static readonly FieldAccessor<LevelCollectionTableView, bool>.Accessor ShowLevelPackHeaderAccessor = FieldAccessor<LevelCollectionTableView, bool>.GetAccessor("_showLevelPackHeader");

        public ScrollerScreenManager(
            MainMenuViewController mainMenuVC,
            SoloFreePlayFlowCoordinator soloFC,
            PartyFreePlayFlowCoordinator partyFC,
            LevelCollectionNavigationController levelCollectionNC,
            PhysicsRaycasterWithCache physicsRaycaster,
            LevelCollectionViewController levelCollectionViewController,
            LevelCollectionDataFlowManager levelCollectionDataFlowManager,
            SettingsModalManager settingsModalManager)
            : base(mainMenuVC, soloFC, partyFC, levelCollectionNC, physicsRaycaster, new Vector2(7f, 58f), new Vector3(-1.525f, 1.3f, 2.075f), Quaternion.Euler(0f, 330f, 0f))
        {
            _levelCollectionDataFlowManager = levelCollectionDataFlowManager;
            _settingsModalManager = settingsModalManager;

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
            _originalUpButton = FieldAccessor<TableView, Button>.Get(ref _levelsTableView, "_pageUpButton");
            _originalDownButton = FieldAccessor<TableView, Button>.Get(ref _levelsTableView, "_pageDownButton");
        }

        public override void Initialize()
        {
            base.Initialize();

            // page buttons need to be refreshed on the end of the frame, since it isn't guaranteed that
            // the game's callback to change the targetPosition would happen before the refresh occurs
            _buttonListener = new UnityAction(() => CoroutineUtilities.StartDelayedAction(RefreshPageButtons));

            _originalUpButton.onClick.AddListener(_buttonListener);
            _originalDownButton.onClick.AddListener(_buttonListener);

            _levelCollectionDataFlowManager.LevelCollectionApplied += OnLevelCollectionApplied;
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_buttonListener != null)
            {
                if (_originalUpButton?.onClick != null)
                    _originalUpButton.onClick.RemoveListener(_buttonListener);
                if (_originalDownButton?.onClick != null)
                    _originalDownButton.onClick.RemoveListener(_buttonListener);

                _buttonListener = null;
            }

            if (_levelCollectionDataFlowManager != null)
                _levelCollectionDataFlowManager.LevelCollectionApplied -= OnLevelCollectionApplied;

            if (_scroller != null)
                _scroller.positionDidChangeEvent -= OnTableViewPositionChanged;
        }

        protected override void OnSinglePlayerLevelSelectionStarting(bool isSolo)
        {
            base.OnSinglePlayerLevelSelectionStarting(isSolo);

            CoroutineUtilities.StartDelayedAction(delegate ()
            {
                _scroller = FieldAccessor<TableView, TableViewScroller>.Get(ref _levelsTableView, "scroller");
                _scroller.positionDidChangeEvent += OnTableViewPositionChanged;

                RefreshPageButtons();
            });
        }

        public void RefreshPageButtons()
        {
            DownButtonInteractable = _scroller.targetPosition < _scroller.scrollableSize - 0.01f;
            UpButtonInteractable = _scroller.targetPosition > 0.01f;
        }

        private void OnLevelCollectionApplied(IEnumerable<IPreviewBeatmapLevel> levels)
        {
            RefreshPageButtons();

            RandomButtonInteractable = levels.Count() > 0;
        }

        private void OnTableViewPositionChanged(TableViewScroller scroller, float position) => RefreshPageButtons();

        [UIAction("settings-button-clicked")]
        private void OnSettingsButtonClicked()
        {
            if (_settingsModalManager.IsVisible)
                _settingsModalManager.HideModal();
            else
                _settingsModalManager.ShowModal();
        }

        [UIAction("up-button-clicked")]
        private void OnUpButtonClicked()
        {
            float numOfVisibleCells = Mathf.Ceil(_levelsTableView.viewportTransform.rect.height / _levelsTableView.cellSize);
            float newTargetPosition = _scroller.targetPosition - Mathf.Max(1f, numOfVisibleCells - 1f) * _levelsTableView.cellSize * PluginConfig.Instance.FastScrollSpeed;
            if (newTargetPosition < 0f)
                newTargetPosition = 0f;

            TargetPositionAccessor.Invoke(ref _scroller) = newTargetPosition;
            _scroller.enabled = true;

            RefreshPageButtons();
            _scroller.RefreshScrollBar();
        }

        [UIAction("down-button-clicked")]
        private void OnDownButtonClicked()
        {
            float maxPosition = _levelsTableView.numberOfCells * _levelsTableView.cellSize - _levelsTableView.viewportTransform.rect.height;
            float numOfVisibleCells = Mathf.Ceil(_levelsTableView.viewportTransform.rect.height / _levelsTableView.cellSize);
            float newTargetPosition = _scroller.targetPosition + Mathf.Max(1f, numOfVisibleCells - 1f) * _levelsTableView.cellSize * PluginConfig.Instance.FastScrollSpeed;
            if (newTargetPosition > maxPosition)
                newTargetPosition = maxPosition;

            TargetPositionAccessor.Invoke(ref _scroller) = newTargetPosition;
            _scroller.enabled = true;

            RefreshPageButtons();
            _scroller.RefreshScrollBar();
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

            _levelsTableView.ScrollToCellWithIdx(index, TableViewScroller.ScrollPositionType.Beginning, false);
            RefreshPageButtons();
        }
    }
}
