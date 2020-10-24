﻿using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using HMUI;
using VRUIControls;
using IPA.Utilities;
using BeatSaberMarkupLanguage.Attributes;
using BS_Utils.Utilities;
using HUI.UI.Components;
using HUI.Utilities;
using Random = System.Random;

namespace HUI.UI.Screens
{
    public class ScrollerScreenManager : ScreenManagerBase
    {
        protected override string AssociatedBSMLResource => "HUI.UI.Views.ScrollerScreenView.bsml";

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

        private Random _rng = new Random();

        private static readonly FieldAccessor<TableViewScroller, float>.Accessor TargetPositionAccessor = FieldAccessor<TableViewScroller, float>.GetAccessor("_targetPosition");
        private static readonly FieldAccessor<LevelCollectionTableView, bool>.Accessor ShowLevelPackHeaderAccessor = FieldAccessor<LevelCollectionTableView, bool>.GetAccessor("_showLevelPackHeader");

        public ScrollerScreenManager(
            MainMenuViewController mainMenuVC,
            SoloFreePlayFlowCoordinator soloFC,
            PartyFreePlayFlowCoordinator partyFC,
            PhysicsRaycasterWithCache physicsRaycaster,
            LevelCollectionViewController levelCollectionViewController)
            : base(mainMenuVC, soloFC, partyFC, physicsRaycaster, new Vector2(7f, 50f), new Vector3(-1.5f, 1.2f, 2.05f), Quaternion.Euler(0f, 330f, 0f))
        {
            this._screen.name = "HUISongUIScreen";

            this._animationHandler.UsePointerAnimations = false;

            // BSML seems to not like it when the default namespace is different from the name of the assembly,
            // so load images here using BS Utils
            Sprite chevronSprite = UIUtilities.LoadSpriteFromResources("HUI.Assets.doublechevron.png");

            Image buttonImage = _upButton.GetComponentsInChildren<Image>().First(x => x.name == "Icon");
            buttonImage.rectTransform.Rotate(0f, 0f, 180f, Space.Self);
            buttonImage.sprite = chevronSprite;

            buttonImage = _downButton.GetComponentsInChildren<Image>().First(x => x.name == "Icon");
            buttonImage.sprite = chevronSprite;

            buttonImage = _randomButton.GetComponentsInChildren<Image>().First(x => x.name == "Icon");
            buttonImage.sprite = UIUtilities.LoadSpriteFromResources("HUI.Assets.shuffle.png");

            // destroy ContentSizeFitter so the anchors are used
            Object.Destroy(_upButton.GetComponent<ContentSizeFitter>());
            Object.Destroy(_downButton.GetComponent<ContentSizeFitter>());
            Object.Destroy(_randomButton.GetComponent<ContentSizeFitter>());

            // remove underline
            Object.Destroy(_upButton.GetComponentsInChildren<ImageView>().First(x => x.name == "Underline"));
            Object.Destroy(_downButton.GetComponentsInChildren<ImageView>().First(x => x.name == "Underline"));
            Object.Destroy(_randomButton.GetComponentsInChildren<ImageView>().First(x => x.name == "Underline"));

            // remove skew
            var skewAccessor = FieldAccessor<ImageView, float>.GetAccessor("_skew");

            ImageView bg = _upButton.GetComponentsInChildren<ImageView>().First(x => x.name == "BG");
            skewAccessor.Invoke(ref bg) = 0f;
            bg.SetVerticesDirty();
            bg = _downButton.GetComponentsInChildren<ImageView>().First(x => x.name == "BG");
            skewAccessor.Invoke(ref bg) = 0f;
            bg.SetVerticesDirty();
            bg = _randomButton.GetComponentsInChildren<ImageView>().First(x => x.name == "BG");
            skewAccessor.Invoke(ref bg) = 0f;
            bg.SetVerticesDirty();

            // add custom animations
            Object.Destroy(_upButton.GetComponent<ButtonStaticAnimations>());
            Object.Destroy(_downButton.GetComponent<ButtonStaticAnimations>());
            Object.Destroy(_randomButton.GetComponent<ButtonStaticAnimations>());

            Color pageButtonHighlightedColour = new Color(1f, 0.375f, 0f);
            Color randomButtonHighlighedColour = new Color(0.145f, 0.443f, 1f);

            var btnAnims = _upButton.AddComponent<CustomIconButtonAnimations>();
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

            _buttonListener = new UnityAction(RefreshPageButtons);

            _originalUpButton.onClick.AddListener(_buttonListener);
            _originalDownButton.onClick.AddListener(_buttonListener);
        }

        public override void Dispose()
        {
            base.Dispose();

            _originalUpButton.onClick.RemoveListener(_buttonListener);
            _originalDownButton.onClick.RemoveListener(_buttonListener);

            _buttonListener = null;
        }

        protected override void OnSinglePlayerLevelSelectionStarting()
        {
            if (_scroller == null)
            {
                _screen.StartCoroutine(CoroutineUtilities.DelayedActionCoroutine(RefreshPageButtons));
                return;
            }

            RefreshPageButtons();
        }

        public void RefreshPageButtons()
        {
            if (ScrollerExists())
            {
                DownButtonInteractable = _scroller.targetPosition < _scroller.scrollableSize - 0.01f;
                UpButtonInteractable = _scroller.targetPosition > 0.01f;
            }
        }

        private bool ScrollerExists()
        {
            // the scroller is lazy initialized
            if (_scroller == null)
            {
                _scroller = FieldAccessor<TableView, TableViewScroller>.Get(ref _levelsTableView, "scroller");
                if (_scroller == null)
                    return false;
            }

            return true;
        }

        [UIAction("up-button-clicked")]
        private void OnUpButtonClicked()
        {
            float numOfVisibleCells = Mathf.Ceil(_levelsTableView.viewportTransform.rect.height / _levelsTableView.cellSize);
            float newTargetPosition = _scroller.targetPosition - Mathf.Max(1f, numOfVisibleCells - 1f) * _levelsTableView.cellSize * PluginConfig.Instance.FastScrollSpeed;
            if (newTargetPosition < 0f)
                newTargetPosition = 0f;

            if (ScrollerExists())
            {
                TargetPositionAccessor.Invoke(ref _scroller) = newTargetPosition;
                _scroller.enabled = true;

                RefreshPageButtons();
                _scroller.InvokeMethod("RefreshScrollBar");
            }
        }

        [UIAction("down-button-clicked")]
        private void OnDownButtonClicked()
        {
            float maxPosition = _levelsTableView.numberOfCells * _levelsTableView.cellSize - _levelsTableView.viewportTransform.rect.height;
            float numOfVisibleCells = Mathf.Ceil(_levelsTableView.viewportTransform.rect.height / _levelsTableView.cellSize);
            float newTargetPosition = _scroller.targetPosition + Mathf.Max(1f, numOfVisibleCells - 1f) * _levelsTableView.cellSize * PluginConfig.Instance.FastScrollSpeed;
            if (newTargetPosition > maxPosition)
                newTargetPosition = maxPosition;

            if (ScrollerExists())
            {
                TargetPositionAccessor.Invoke(ref _scroller) = newTargetPosition;
                _scroller.enabled = true;

                RefreshPageButtons();
                _scroller.InvokeMethod("RefreshScrollBar");
            }
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
