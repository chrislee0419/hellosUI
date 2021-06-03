using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;
using HMUI;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HUI.Interfaces;
using HUI.UI.Components;
using HUI.Utilities;

namespace HUI.UI.Settings
{
    public class ScreensSettingsTab : SettingsModalManager.SettingsModalTabBase, IInitializable
    {
        public override string TabName => "Screens";
        protected override string AssociatedBSMLResource => "HUI.UI.Views.Settings.ScreensSettingsView.bsml";

        [UIValue("bg-interactable")]
        public bool BackgroundSettingsInteractable => _selectedScreen != null;
        [UIValue("screen-interactable")]
        public bool ScreenSettingsInteractable => _selectedScreen != null;
        [UIValue("bg-opacity-value")]
        public object BGOpacity
        {
            get
            {
                if (IsScreenSelected() && PluginConfig.Instance.Screens.ScreenOpacities.TryGetValue(_selectedScreen.GetIdentifier(), out var value))
                    return value;
                else
                    return BackgroundOpacity.Transparent;
            }
            set
            {
                if (IsScreenSelected())
                    SetBackgroundColour(_selectedScreen, (BackgroundOpacity)value);
            }
        }
        [UIValue("enable-movement-value")]
        public bool EnableMovement
        {
            get => IsScreenSelected() ? _selectedScreen.AllowMovement : false;
            set
            {
                if (IsScreenSelected())
                {
                    _selectedScreen.AllowMovement = value;

                    if (!value)
                        _selectedScreen.SavePosition();
                }
            }
        }

        [UIValue("screen-list-data")]
        public List<object> ScreenListData => _screens.Select(x => (object)new ScreenListCell(x)).ToList();

        private Color BackgroundColour
        {
            get => _bgRGBController.color;
            set
            {
                _bgRGBController.color = value;
                _bgImageView.color = value;

                // apparently there's some issues with assigning a colour with the same hue to the HSV controller
                // https://github.com/monkeymanboy/BeatSaberMarkupLanguage/blob/47e00b7197c3b1486481fe4bdf91dce727735273/BeatSaberMarkupLanguage/Components/ModalColorPicker.cs#L34
                if (_bgHSVController.color != value)
                    _bgHSVController.color = value;
            }
        }

#pragma warning disable CS0649
        [UIObject("list-up-button")]
        private GameObject _listUpButton;
        [UIObject("list-down-button")]
        private GameObject _listDownButton;
        [UIObject("bg-colour-tab")]
        private GameObject _bgColourTab;
        [UIObject("bg-colour-preview-image-bg")]
        private GameObject _bgColourPreviewImageBackground;
#pragma warning restore CS0649

        private List<IModifiableScreen> _screens;
        private IModifiableScreen _selectedScreen;

        private RGBPanelController _bgRGBController;
        private HSVPanelController _bgHSVController;
        private ImageView _bgImageView;

        [UIValue("bg-opacity-options")]
        private static readonly List<object> BackgroundOpacityOptions = Enum.GetValues(typeof(BackgroundOpacity))
            .Cast<BackgroundOpacity>()
            .Select(x => (object)x)
            .ToList();

        public ScreensSettingsTab(List<IModifiableScreen> screens)
        {
            _screens = screens;
        }

        public void Initialize()
        {
            // set saved background styles/positions/rotations for all screens
            var screenOpacities = PluginConfig.Instance.Screens.ScreenOpacities;
            foreach (var screen in _screens)
            {
                string id = screen.GetIdentifier();

                if (screenOpacities.TryGetValue(id, out BackgroundOpacity value))
                    SetBackgroundColour(screen, value, false);
                else
                    SetBackgroundColour(screen, BackgroundOpacity.Transparent, false);

                screen.LoadPosition();
            }
        }

        public override void SetupView()
        {
            base.SetupView();

            GameObject.Destroy(_listUpButton.transform.Find("Underline").gameObject);
            GameObject.Destroy(_listDownButton.transform.Find("Underline").gameObject);

            // remove skew
            _listUpButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);
            _listDownButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);

            // reduce padding
            var offset = new RectOffset(0, 0, 4, 4);
            _listUpButton.transform.Find("Content").GetComponent<StackLayoutGroup>().padding = offset;
            _listDownButton.transform.Find("Content").GetComponent<StackLayoutGroup>().padding = offset;

            // rotate image
            _listUpButton.GetComponent<ButtonIconImage>().image.rectTransform.Rotate(0f, 0f, 180f, Space.Self);

            // custom button animations
            GameObject.Destroy(_listUpButton.GetComponent<ButtonStaticAnimations>());
            GameObject.Destroy(_listDownButton.GetComponent<ButtonStaticAnimations>());

            Color ListMoveColour = new Color(0.145f, 0.443f, 1f);

            var btnAnims = _listUpButton.AddComponent<CustomIconButtonAnimations>();
            btnAnims.HighlightedBGColour = ListMoveColour;
            btnAnims.PressedBGColour = ListMoveColour;

            btnAnims = _listDownButton.AddComponent<CustomIconButtonAnimations>();
            btnAnims.HighlightedBGColour = ListMoveColour;
            btnAnims.PressedBGColour = ListMoveColour;

            // create background colour picker
            // adapted from: https://github.com/monkeymanboy/BeatSaberMarkupLanguage/blob/47e00b7197c3b1486481fe4bdf91dce727735273/BeatSaberMarkupLanguage/Components/ModalColorPicker.cs
            _bgRGBController = GameObject.Instantiate(
                Resources.FindObjectsOfTypeAll<RGBPanelController>().First(x => x.name == "RGBColorPicker"),
                _bgColourTab.transform,
                false);
            _bgRGBController.name = "BackgroundRGBColourPicker";
            _bgRGBController.colorDidChangeEvent += OnBackgroundColourChanged;

            var rt = _bgRGBController.gameObject.transform as RectTransform;
            rt.anchoredPosition = Vector2.zero;
            rt.anchorMin = new Vector2(0f, 0.25f);
            rt.anchorMax = new Vector2(0f, 0.25f);
            rt.localScale = new Vector3(0.75f, 0.75f, 0.75f);

            _bgHSVController = GameObject.Instantiate(
                Resources.FindObjectsOfTypeAll<HSVPanelController>().First(x => x.name == "HSVColorPicker"),
                _bgColourTab.transform,
                false);
            _bgHSVController.name = "BackgroundHSVColourPicker";
            _bgHSVController.colorDidChangeEvent += OnBackgroundColourChanged;

            GameObject.Destroy(_bgHSVController.transform.Find("ColorPickerButtonPrimary").gameObject);

            rt = _bgHSVController.gameObject.transform as RectTransform;
            rt.anchoredPosition = new Vector2(0f, 3f);
            rt.anchorMin = new Vector2(0.75f, 0.5f);
            rt.anchorMax = new Vector2(0.75f, 0.5f);
            rt.localScale = new Vector3(0.75f, 0.75f, 0.75f);

            _bgImageView = GameObject.Instantiate(
                Resources.FindObjectsOfTypeAll<ImageView>().First(x => x.name == "SaberColorA" && x.transform.parent?.name == "ColorSchemeView"),
                _bgColourPreviewImageBackground.transform,
                false);
            _bgImageView.name = "BackgroundCurrentColour";

            rt = _bgImageView.rectTransform;
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-4f, 0);
            rt.sizeDelta = new Vector2(6f, 6f);

            BackgroundColour = PluginConfig.Instance.Screens.ScreenBackgroundColour;
        }

        public override void OnTabHidden()
        {
            if (_selectedScreen != null)
                _selectedScreen.AllowMovement = false;
        }

        private bool IsScreenSelected()
        {
            if (_selectedScreen == null)
            {
                NotifyPropertyChanged(nameof(BackgroundSettingsInteractable));
                NotifyPropertyChanged(nameof(ScreenSettingsInteractable));
                return false;
            }
            else
            {
                return true;
            }
        }

        private void OnBackgroundColourChanged(Color colour, ColorChangeUIEventType eventType)
        {
            BackgroundColour = colour;
        }

        private void SetBackgroundColour(IModifiableScreen screen, BackgroundOpacity bgOpacity, bool save = true)
        {
            string id = screen.GetIdentifier();
            Color bgColour = PluginConfig.Instance.Screens.ScreenBackgroundColour;
            switch (bgOpacity)
            {
                case BackgroundOpacity.Transparent:
                    screen.BackgroundColor = Color.clear;
                    if (save)
                        PluginConfig.Instance.Screens.ScreenOpacities.Remove(id);
                    break;

                case BackgroundOpacity.Translucent:
                    screen.BackgroundColor = new Color(bgColour.r, bgColour.g, bgColour.b, 0.25f);
                    if (save)
                        PluginConfig.Instance.Screens.ScreenOpacities[id] = bgOpacity;
                    break;

                case BackgroundOpacity.Opaque:
                    screen.BackgroundColor = bgColour;
                    if (save)
                        PluginConfig.Instance.Screens.ScreenOpacities[id] = bgOpacity;
                    break;
            }
        }

        private void RefreshValues() => base.OnPluginConfigReloaded();

        [UIAction("cell-selected")]
        private void OnCellSelected(TableView tableView, ScreenListCell screenListCell)
        {
            EnableMovement = false;

            _selectedScreen = screenListCell.Screen;

            NotifyPropertyChanged(nameof(BackgroundSettingsInteractable));
            NotifyPropertyChanged(nameof(ScreenSettingsInteractable));
            NotifyPropertyChanged(nameof(BGOpacity));
            NotifyPropertyChanged(nameof(EnableMovement));

            RefreshValues();
        }

        [UIAction("bg-opacity-changed")]
        private void OnBGOpacityValueChanged(object value)
        {
            if (IsScreenSelected())
                SetBackgroundColour(_selectedScreen, (BackgroundOpacity)value);
        }

        [UIAction("reset-button-clicked")]
        private void OnResetButtonClicked()
        {
            if (IsScreenSelected())
                _selectedScreen.ResetPosition();
        }

        [UIAction("bg-colour-apply-button-clicked")]
        private void OnBackgroundColourApplyButtonClicked()
        {
            PluginConfig.Instance.Screens.ScreenBackgroundColour = BackgroundColour;

            // set background colour for all non-transparent screens
            var screenOpacities = PluginConfig.Instance.Screens.ScreenOpacities;
            foreach (var screen in _screens)
            {
                string id = screen.GetIdentifier();

                if (screenOpacities.TryGetValue(id, out BackgroundOpacity opacity))
                    SetBackgroundColour(screen, opacity, false);
            }
        }

        [UIAction("bg-colour-reset-button-clicked")]
        private void OnBackgroundColourResetButtonClicked()
        {
            BackgroundColour = PluginConfig.Instance.Screens.ScreenBackgroundColour;
        }

        public enum BackgroundOpacity
        {
            Transparent,
            Translucent,
            Opaque
        }

        private class ScreenListCell
        {
            [UIValue("name-text")]
            public string NameText => Screen.ScreenName.EscapeTextMeshProTags();

            public IModifiableScreen Screen { get; private set; }

            public ScreenListCell(IModifiableScreen screen)
            {
                Screen = screen;
            }
        }
    }
}
