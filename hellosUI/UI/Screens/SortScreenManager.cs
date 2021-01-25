using System;
using UnityEngine;
using UnityEngine.UI;
using VRUIControls;
using HMUI;
using BeatSaberMarkupLanguage.Attributes;
using BS_Utils.Utilities;
using HUI.UI.Components;
using HUI.Utilities;
using Object = UnityEngine.Object;

namespace HUI.UI.Screens
{
    public class SortScreenManager : ModifiableScreenManagerBase
    {
        public event Action SortButtonPressed;
        public event Action SortDirectionChanged;
        public event Action SortCancelled;

        public override string ScreenName => "Sort Widget";
        protected override string AssociatedBSMLResource => "HUI.UI.Views.Screens.SortScreenView.bsml";

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

        public bool SortAscending
        {
            set
            {
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
            }
        }

#pragma warning disable CS0649
        [UIComponent("sort-direction-button")]
        private Button _sortDirectionButton;
        [UIComponent("cancel-sort-button")]
        private Button _cancelButton;
        [UIComponent("sort-button")]
        private Button _sortButton;
#pragma warning restore CS0649

        private ImageView _sortIcon;
        private CustomIconButtonAnimations _sortDirectionButtonAnimations;

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
            : base(mainMenuVC, soloFC, partyFC, levelCollectionNC, physicsRaycaster, new Vector2(52f, 10f), new Vector3(-1.5f, 0.15f, 2.9f), Quaternion.Euler(65f, 345f, 0f))
        {
            this._screen.name = "HUISortScreen";

            this._animationHandler.UsePointerAnimations = false;

            // icons
            if (AscendingIconSprite == null)
                AscendingIconSprite = UIUtilities.LoadSpriteFromResources("HUI.Assets.ascending.png");
            if (DescendingIconSprite == null)
                DescendingIconSprite = UIUtilities.LoadSpriteFromResources("HUI.Assets.descending.png");

            _sortIcon = _sortDirectionButton.transform.Find("Content/Icon").GetComponent<ImageView>();

            Object.Destroy(_sortDirectionButton.GetComponent<ContentSizeFitter>());
            Object.Destroy(_cancelButton.GetComponent<ContentSizeFitter>());
            Object.Destroy(_sortButton.GetComponent<ContentSizeFitter>());

            // remove skew
            _sortDirectionButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);
            _sortDirectionButton.transform.Find("Underline").GetComponent<ImageView>().SetSkew(0f);

            _cancelButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);
            _cancelButton.transform.Find("Underline").GetComponent<ImageView>().SetSkew(0f);

            _sortButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);
            _sortButton.transform.Find("Underline").GetComponent<ImageView>().SetSkew(0f);

            // custom animations
            Object.Destroy(_sortDirectionButton.GetComponent<ButtonStaticAnimations>());
            Object.Destroy(_cancelButton.GetComponent<ButtonStaticAnimations>());
            Object.Destroy(_sortButton.GetComponent<ButtonStaticAnimations>());

            _sortDirectionButtonAnimations = _sortDirectionButton.gameObject.AddComponent<CustomIconButtonAnimations>();
            _sortDirectionButtonAnimations.NormalIconColour = Color.white;
            _sortDirectionButtonAnimations.HighlightedLocalScale = new Vector3(1.2f, 1.2f, 1.2f);

            var iconBtnAnims = _cancelButton.gameObject.AddComponent<CustomIconButtonAnimations>();
            iconBtnAnims.NormalIconColour = Color.white;
            iconBtnAnims.HighlightedBGColour = Color.red;
            iconBtnAnims.PressedBGColour = Color.red;
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

            SortAscending = true;
        }

        [UIAction("sort-direction-button-clicked")]
        private void OnSortDirectionButtonClicked()
        {
            this.CallAndHandleAction(SortDirectionChanged, nameof(SortDirectionChanged));
        }

        [UIAction("sort-button-clicked")]
        private void OnSortButtonClicked()
        {
            this.CallAndHandleAction(SortButtonPressed, nameof(SortButtonPressed));
        }

        [UIAction("cancel-sort-button-clicked")]
        private void OnCancelSortButtonClicked()
        {
            this.CallAndHandleAction(SortCancelled, nameof(SortCancelled));
        }
    }
}
