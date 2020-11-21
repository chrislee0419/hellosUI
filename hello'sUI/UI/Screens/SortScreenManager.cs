using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRUIControls;
using HMUI;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BS_Utils.Utilities;
using HUI.UI.Components;
using HUI.Interfaces;
using HUI.Utilities;
using Object = UnityEngine.Object;

namespace HUI.UI.Screens
{
    public class SortScreenManager : ScreenManagerBase
    {
        public event Action SortDirectionChanged;
        public event Action SortCancelled;
        public event Action<int> SortModeListCellSelected;

        protected override string AssociatedBSMLResource => "HUI.UI.Views.Screens.SortScreenView.bsml";

        // necessary to prevent buttons/list from being interactable while hidden
        private bool _listBGActive = false;
        [UIValue("list-bg-active")]
        public bool ListBGActive
        {
            get => _listBGActive;
            set
            {
                if (_listBGActive == value)
                    return;

                _listBGActive = value;
                NotifyPropertyChanged();
            }
        }

        private string _sortText = "Default";
        [UIValue("sort-text")]
        public string SortText
        {
            get => _sortText;
            set
            {
                if (_sortText == value)
                    return;

                _sortText = value;
                NotifyPropertyChanged();
            }
        }

        private bool _ascending = true;
        public bool SortAscending
        {
            get => _ascending;
            set
            {
                if (_ascending == value)
                    return;

                if (value)
                {
                    _sortIcon.sprite = AscendingIconSprite;
                    _sortDirectionButtonAnimations.NormalBGColour = AscendingIconColour;
                    _sortDirectionButtonAnimations.HighlightedBGColour = AscendingIconColour;
                    _sortDirectionButtonAnimations.PressedBGColour = AscendingIconColour;
                }
                else
                {
                    _sortIcon.sprite = DescendingIconSprite;
                    _sortDirectionButtonAnimations.NormalBGColour = DescendingIconColour;
                    _sortDirectionButtonAnimations.HighlightedBGColour = DescendingIconColour;
                    _sortDirectionButtonAnimations.PressedBGColour = DescendingIconColour;
                }

                _ascending = value;
            }
        }

#pragma warning disable CS0649
        [UIComponent("sort-direction-button")]
        private Button _sortDirectionButton;
        [UIComponent("cancel-sort-button")]
        private Button _cancelButton;
        [UIComponent("sort-button")]
        private Button _sortButton;

        [UIComponent("sort-mode-list")]
        private CustomListTableData _sortModeList;

        [UIObject("page-up-button")]
        private GameObject _pageUpButton;
        [UIObject("page-down-button")]
        private GameObject _pageDownButton;
#pragma warning restore CS0649

        private ImageView _sortIcon;
        private CustomIconButtonAnimations _sortDirectionButtonAnimations;

        private static readonly Vector2 DefaultSize = new Vector2(52f, 10f);
        private static readonly Vector2 ExpandedSize = new Vector2(52f, 54f);

        private static Sprite AscendingIconSprite;
        private static Sprite DescendingIconSprite;
        private static readonly Color AscendingIconColour = new Color(0.3f, 0.8f, 0.15f, 0.5f);
        private static readonly Color DescendingIconColour = new Color(0.4f, 0.36f, 0.8f, 0.5f);

        public SortScreenManager(
            MainMenuViewController mainMenuVC,
            SoloFreePlayFlowCoordinator soloFC,
            PartyFreePlayFlowCoordinator partyFC,
            LevelCollectionNavigationController levelCollectionNC,
            PhysicsRaycasterWithCache physicsRaycaster)
            : base(mainMenuVC, soloFC, partyFC, levelCollectionNC, physicsRaycaster, DefaultSize, new Vector3(-1.1f, 0.5f, 2.3f), Quaternion.Euler(65f, 345f, 0f))
        {
            this._screen.name = "HUISortScreen";

            this._animationHandler.ExpandedSize = ExpandedSize;
            this._animationHandler.UsePointerAnimations = false;
            this._animationHandler.AnimationFinished += OnAnimationHandlerAnimationFinished;

            // BSMLList needs a VRGraphicRaycaster, so we need to fix the PhysicsRaycaster,
            // just like what is done for FloatingScreen in ScreenManagerBase
            _sortModeList.gameObject.FixRaycaster(physicsRaycaster);

            // move pivot so the screen expands downwards
            var rt = this._screen.transform as RectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);

            var curvedCanvasSettings = _screen.GetComponent<CurvedCanvasSettings>();
            curvedCanvasSettings.SetRadius(0);

            // icons
            if (AscendingIconSprite == null)
                AscendingIconSprite = UIUtilities.LoadSpriteFromResources("HUI.Assets.ascending.png");
            if (DescendingIconSprite == null)
                DescendingIconSprite = UIUtilities.LoadSpriteFromResources("HUI.Assets.descending.png");

            _sortIcon = _sortDirectionButton.transform.Find("Content/Icon").GetComponent<ImageView>();
            _sortIcon.sprite = AscendingIconSprite;

            var icon = _cancelButton.transform.Find("Content/Icon").GetComponent<ImageView>();
            icon.sprite = UIUtilities.LoadSpriteFromResources("HUI.Assets.refresh.png");

            Sprite chevronSprite = UIUtilities.LoadSpriteFromResources("HUI.Assets.chevron.png");
            icon = _pageUpButton.transform.Find("Content/Icon").GetComponent<ImageView>();
            icon.sprite = chevronSprite;
            icon.rectTransform.Rotate(0f, 0f, 180f, Space.Self);

            icon = _pageDownButton.transform.Find("Content/Icon").GetComponent<ImageView>();
            icon.sprite = chevronSprite;

            Object.Destroy(_sortDirectionButton.GetComponent<ContentSizeFitter>());
            Object.Destroy(_cancelButton.GetComponent<ContentSizeFitter>());
            Object.Destroy(_sortButton.GetComponent<ContentSizeFitter>());
            Object.Destroy(_pageUpButton.GetComponent<ContentSizeFitter>());
            Object.Destroy(_pageDownButton.GetComponent<ContentSizeFitter>());

            Object.Destroy(_pageUpButton.transform.Find("Underline").gameObject);
            Object.Destroy(_pageDownButton.transform.Find("Underline").gameObject);

            // remove skew
            _sortDirectionButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);
            _sortDirectionButton.transform.Find("Underline").GetComponent<ImageView>().SetSkew(0f);

            _cancelButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);
            _cancelButton.transform.Find("Underline").GetComponent<ImageView>().SetSkew(0f);

            _sortButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);
            _sortButton.transform.Find("Underline").GetComponent<ImageView>().SetSkew(0f);

            _pageUpButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);
            _pageDownButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);

            // custom animations
            Object.Destroy(_sortDirectionButton.GetComponent<ButtonStaticAnimations>());
            Object.Destroy(_cancelButton.GetComponent<ButtonStaticAnimations>());
            Object.Destroy(_sortButton.GetComponent<ButtonStaticAnimations>());
            Object.Destroy(_pageUpButton.GetComponent<ButtonStaticAnimations>());
            Object.Destroy(_pageDownButton.GetComponent<ButtonStaticAnimations>());

            _sortDirectionButtonAnimations = _sortDirectionButton.gameObject.AddComponent<CustomIconButtonAnimations>();
            _sortDirectionButtonAnimations.NormalIconColour = Color.white;
            _sortDirectionButtonAnimations.NormalBGColour = AscendingIconColour;
            _sortDirectionButtonAnimations.HighlightedBGColour = AscendingIconColour;
            _sortDirectionButtonAnimations.PressedBGColour = AscendingIconColour;
            _sortDirectionButtonAnimations.HighlightedLocalScale = new Vector3(1.2f, 1.2f, 1.2f);

            var iconBtnAnims = _cancelButton.gameObject.AddComponent<CustomIconButtonAnimations>();
            iconBtnAnims.NormalIconColour = Color.white;
            iconBtnAnims.HighlightedBGColour = Color.red;
            iconBtnAnims.PressedBGColour = Color.red;
            iconBtnAnims.HighlightedLocalScale = new Vector3(1.2f, 1.2f, 1.2f);

            iconBtnAnims = _pageUpButton.gameObject.AddComponent<CustomIconButtonAnimations>();
            iconBtnAnims.HighlightedBGColour = new Color(1f, 0.375f, 0f);
            iconBtnAnims.PressedBGColour = new Color(1f, 0.375f, 0f);
            iconBtnAnims.HighlightedLocalScale = new Vector3(1.2f, 1.2f, 1.2f);

            iconBtnAnims = _pageDownButton.gameObject.AddComponent<CustomIconButtonAnimations>();
            iconBtnAnims.HighlightedBGColour = new Color(1f, 0.375f, 0f);
            iconBtnAnims.PressedBGColour = new Color(1f, 0.375f, 0f);
            iconBtnAnims.HighlightedLocalScale = new Vector3(1.2f, 1.2f, 1.2f);

            Color sortButtonBGColour = new Color(0.145f, 0.443f, 1f);
            var btnAnims = _sortButton.gameObject.AddComponent<CustomButtonAnimations>();
            btnAnims.HighlightedBGColour = sortButtonBGColour;
            btnAnims.PressedBGColour = sortButtonBGColour;

            // change icon size in buttons by changing the RectOffset
            var slg = _sortDirectionButton.transform.Find("Content").GetComponent<StackLayoutGroup>();
            slg.padding = new RectOffset(0, 0, 2, 2);

            slg = _cancelButton.transform.Find("Content").GetComponent<StackLayoutGroup>();
            slg.padding = new RectOffset(0, 0, 2, 2);

            slg = _pageUpButton.transform.Find("Content").GetComponent<StackLayoutGroup>();
            slg.padding = new RectOffset(0, 0, 1, 1);

            slg = _pageDownButton.transform.Find("Content").GetComponent<StackLayoutGroup>();
            slg.padding = new RectOffset(0, 0, 1, 1);
        }

        private void OnAnimationHandlerAnimationFinished(AnimationType animationType)
        {
            if (animationType == AnimationType.Contract)
            {
                _animationHandler.UsePointerAnimations = false;

                ListBGActive = false;
            }
        }

        public void RefreshSortModeList(IEnumerable<ISortMode> sortModes)
        {
            _sortModeList.data.Clear();

            foreach (ISortMode sortMode in sortModes)
                _sortModeList.data.Add(new CustomListTableData.CustomCellInfo(sortMode.Name.EscapeTextMeshProTags()));

            _sortModeList.tableView.ReloadData();
        }

        public void SelectSortMode(int index, bool fireCallback = false)
        {
            _sortModeList.tableView.SelectCellWithIdx(index, fireCallback);
        }

        [UIAction("sort-direction-button-clicked")]
        private void OnSortDirectionButtonClicked()
        {
            _animationHandler.PlayContractAnimation(true);

            this.CallAndHandleAction(SortDirectionChanged, nameof(SortDirectionChanged));
        }

        [UIAction("sort-button-clicked")]
        private void OnSortButtonClicked()
        {
            // use the UsePointerAnimations bool to check whether the FloatingScreen is expanded
            if (_animationHandler.UsePointerAnimations)
            {
                _animationHandler.PlayContractAnimation(true);
            }
            else
            {
                _animationHandler.PlayExpandAnimation();
                _animationHandler.UsePointerAnimations = true;
                ListBGActive = true;
            }
        }

        [UIAction("cancel-sort-button-clicked")]
        private void OnCancelSortButtonClicked()
        {
            _animationHandler.PlayContractAnimation(true);

            this.CallAndHandleAction(SortCancelled, nameof(SortCancelled));
        }

        [UIAction("sort-mode-list-cell-selected")]
        private void OnSortModeListCellSelected(TableView tableView, int index)
        {
            this.CallAndHandleAction(SortModeListCellSelected, nameof(SortModeListCellSelected), index);
        }
    }
}
