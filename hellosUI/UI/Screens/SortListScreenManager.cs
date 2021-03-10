using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HMUI;
using VRUIControls;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HUI.Interfaces;
using HUI.UI.Components;
using HUI.UI.Settings;
using HUI.Utilities;
using Object = UnityEngine.Object;

namespace HUI.UI.Screens
{
    public class SortListScreenManager : ModifiableScreenManagerBase
    {
        public event Action<int> SortModeListCellSelected;

        public override string ScreenName => "Sort List";
        protected override string AssociatedBSMLResource => "HUI.UI.Views.Screens.SortListScreenView.bsml";
        protected override bool ShowScreenOnSinglePlayerLevelSelectionStarting => false;
        protected override ScreensSettingsTab.BackgroundOpacity DefaultBGOpacity => ScreensSettingsTab.BackgroundOpacity.Translucent;

        public bool IsVisible => this._screen.gameObject.activeSelf;

#pragma warning disable CS0649
        [UIComponent("sort-mode-list")]
        private CustomListTableData _sortModeList;

        [UIObject("page-up-button")]
        private GameObject _pageUpButton;
        [UIObject("page-down-button")]
        private GameObject _pageDownButton;
#pragma warning restore CS0649

        private Coroutine _concealDelayCoroutine;

        private static readonly WaitForSeconds ConcealDelaySeconds = new WaitForSeconds(1f);

        public SortListScreenManager(
            MainMenuViewController mainMenuVC,
            SoloFreePlayFlowCoordinator soloFC,
            PartyFreePlayFlowCoordinator partyFC,
            LevelCollectionNavigationController levelCollectionNC,
            PhysicsRaycasterWithCache physicsRaycaster)
            : base(mainMenuVC, soloFC, partyFC, levelCollectionNC, physicsRaycaster, new Vector2(50f, 56f), new Vector3(-0.975f, 0.15f, 1.6f), Quaternion.Euler(65f, 345f, 0f))
        {
            this._screen.name = "HUISortListScreen";

            this._animationHandler.LocalScale = 0.025f;
            this._animationHandler.UsePointerAnimations = false;
            this._animationHandler.PointerEntered += OnPointerEntered;
            this._animationHandler.PointerExited += OnPointerExited;

            // BSMLList needs a VRGraphicRaycaster, so we need to fix the PhysicsRaycaster,
            // just like what is done for FloatingScreen in ScreenManagerBase
            _sortModeList.gameObject.FixRaycaster(physicsRaycaster);

            var icon = _pageUpButton.transform.Find("Content/Icon").GetComponent<ImageView>();
            icon.rectTransform.Rotate(0f, 0f, 180f, Space.Self);

            Object.Destroy(_pageUpButton.GetComponent<ContentSizeFitter>());
            Object.Destroy(_pageDownButton.GetComponent<ContentSizeFitter>());

            Object.Destroy(_pageUpButton.transform.Find("Underline").gameObject);
            Object.Destroy(_pageDownButton.transform.Find("Underline").gameObject);

            _pageUpButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);
            _pageDownButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);

            // custom animations
            Object.Destroy(_pageUpButton.GetComponent<ButtonStaticAnimations>());
            Object.Destroy(_pageDownButton.GetComponent<ButtonStaticAnimations>());

            Color highlightedColour = new Color(1f, 0.375f, 0f);

            var iconBtnAnims = _pageUpButton.gameObject.AddComponent<CustomIconButtonAnimations>();
            iconBtnAnims.HighlightedBGColour = highlightedColour;
            iconBtnAnims.PressedBGColour = highlightedColour;
            iconBtnAnims.HighlightedLocalScale = new Vector3(1.2f, 1.2f, 1.2f);

            iconBtnAnims = _pageDownButton.gameObject.AddComponent<CustomIconButtonAnimations>();
            iconBtnAnims.HighlightedBGColour = highlightedColour;
            iconBtnAnims.PressedBGColour = highlightedColour;
            iconBtnAnims.HighlightedLocalScale = new Vector3(1.2f, 1.2f, 1.2f);

            // change icon size in buttons by changing the RectOffset
            var slg = _pageUpButton.transform.Find("Content").GetComponent<StackLayoutGroup>();
            slg.padding = new RectOffset(0, 0, 1, 1);

            slg = _pageDownButton.transform.Find("Content").GetComponent<StackLayoutGroup>();
            slg.padding = new RectOffset(0, 0, 1, 1);
        }

        protected override void OnLevelCollectionNavigationControllerActivated(bool firstActivation, bool addToHierarchy, bool screenSystemEnabling)
        {
            // do not show screen during activation
        }

        public void ShowScreen() => this._animationHandler.PlayRevealAnimation();

        public void HideScreen() => this._animationHandler.PlayConcealAnimation();

        public void RefreshSortModeList(IEnumerable<ISortMode> sortModes)
        {
            _sortModeList.data.Clear();

            foreach (ISortMode sortMode in sortModes)
            {
                string name = sortMode.Name.EscapeTextMeshProTags();
                if (!sortMode.IsAvailable)
                    name = $"<color=#FF5555>{name} <size=60%>(!)</size></color>";

                _sortModeList.data.Add(new CustomListTableData.CustomCellInfo(name));
            }

            _sortModeList.tableView.ReloadData();
        }

        public void SelectSortMode(int index, bool fireCallback = false)
        {
            _sortModeList.tableView.SelectCellWithIdx(index, fireCallback);
        }

        private void OnPointerEntered()
        {
            if (_concealDelayCoroutine != null)
            {
                this._animationHandler.StopCoroutine(_concealDelayCoroutine);
                _concealDelayCoroutine = null;
            }
        }

        private void OnPointerExited()
        {
            if (this._screen.ShowHandle)
                return;
            else if (_concealDelayCoroutine != null)
                this._animationHandler.StopCoroutine(_concealDelayCoroutine);

            _concealDelayCoroutine = this._animationHandler.StartCoroutine(ConcealDelayCoroutine());
        }

        private IEnumerator ConcealDelayCoroutine()
        {
            yield return ConcealDelaySeconds;
            this._animationHandler.PlayConcealAnimation();
        }

        [UIAction("sort-mode-list-cell-selected")]
        private void OnSortModeListCellSelected(TableView tableView, int index)
        {
            // note: sort name text and ascending/descending icon will be changed by the song sort manager
            this.CallAndHandleAction(SortModeListCellSelected, nameof(SortModeListCellSelected), index);
        }
    }
}
