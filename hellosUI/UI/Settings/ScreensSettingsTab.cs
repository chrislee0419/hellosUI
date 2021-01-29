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
        public bool BackgroundSettingsInteractable => _selectedScreen != null && _selectedScreen.Background != null;
        [UIValue("screen-interactable")]
        public bool ScreenSettingsInteractable => _selectedScreen != null && _selectedScreen.Screen != null;

        [UIValue("bg-colour-value")]
        public Color BackgroundColour
        {
            get => PluginConfig.Instance.Screens.ScreenBackgroundColour;
            set => PluginConfig.Instance.Screens.ScreenBackgroundColour = value;
        }
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
            get => IsScreenSelected() ? _selectedScreen.Screen.ShowHandle : false;
            set
            {
                if (IsScreenSelected())
                {
                    _selectedScreen.Screen.ShowHandle = value;

                    if (!value)
                    {
                        string id = _selectedScreen.GetIdentifier();

                        PluginConfig.Instance.Screens.ScreenPositions[id] = _selectedScreen.Screen.ScreenPosition;
                        PluginConfig.Instance.Screens.ScreenRotations[id] = _selectedScreen.Screen.ScreenRotation;
                    }
                }
            }
        }

#pragma warning disable CS0649
        [UIObject("list-up-button")]
        private GameObject _listUpButton;
        [UIObject("list-down-button")]
        private GameObject _listDownButton;

        [UIValue("data")]
        private List<CustomListTableData.CustomCellInfo> _screenCells;
#pragma warning restore CS0649

        private List<IModifiableScreen> _screens;
        private IModifiableScreen _selectedScreen;

        [UIValue("bg-opacity-options")]
        private static readonly List<object> BackgroundOpacityOptions = Enum.GetValues(typeof(BackgroundOpacity))
            .Cast<BackgroundOpacity>()
            .Select(x => (object)x)
            .ToList();

        public ScreensSettingsTab(List<IModifiableScreen> screens)
        {
            _screens = screens;

            _screenCells = new List<CustomListTableData.CustomCellInfo>(screens.Count);
            foreach (var screen in screens)
                _screenCells.Add(new CustomListTableData.CustomCellInfo(screen.ScreenName));
        }

        public void Initialize()
        {
            // set saved background styles/positions/rotations for all screens
            var screenOpacities = PluginConfig.Instance.Screens.ScreenOpacities;
            var screenPositions = PluginConfig.Instance.Screens.ScreenPositions;
            var screenRotations = PluginConfig.Instance.Screens.ScreenRotations;
            foreach (var screen in _screens)
            {
                string id = screen.GetIdentifier();

                if (screenOpacities.TryGetValue(id, out BackgroundOpacity value))
                    SetBackgroundColour(screen, value, false);
                else
                    SetBackgroundColour(screen, BackgroundOpacity.Transparent, false);

                if (screen.Screen != null)
                {
                    if (screenPositions.TryGetValue(id, out Vector3 posValue))
                        screen.Screen.ScreenPosition = posValue;
                    if (screenRotations.TryGetValue(id, out Quaternion quatValue))
                        screen.Screen.ScreenRotation = quatValue;
                }

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
        }

        public override void OnTabHidden()
        {
            if (_selectedScreen != null)
                _selectedScreen.Screen.ShowHandle = false;
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

        private void SetBackgroundColour(IModifiableScreen screen, BackgroundOpacity bgOpacity, bool save = true, Color? overrideColour = null)
        {
            if (screen.Background == null)
                return;

            string id = screen.GetIdentifier();
            Color bgColour = overrideColour ?? PluginConfig.Instance.Screens.ScreenBackgroundColour;
            switch (bgOpacity)
            {
                case BackgroundOpacity.Transparent:
                    screen.Background.color = Color.clear;
                    if (save)
                        PluginConfig.Instance.Screens.ScreenOpacities.Remove(id);
                    break;

                case BackgroundOpacity.Translucent:
                    screen.Background.color = new Color(bgColour.r, bgColour.g, bgColour.b, 0.25f);
                    if (save)
                        PluginConfig.Instance.Screens.ScreenOpacities[id] = bgOpacity;
                    break;

                case BackgroundOpacity.Opaque:
                    screen.Background.color = bgColour;
                    if (save)
                        PluginConfig.Instance.Screens.ScreenOpacities[id] = bgOpacity;
                    break;
            }
        }

        private void RefreshValues() => base.OnPluginConfigReloaded();

        [UIAction("bg-colour-changed")]
        private void OnBGColourChanged(Color value)
        {
            // set background colour for all non-transparent screens
            var screenOpacities = PluginConfig.Instance.Screens.ScreenOpacities;
            foreach (var screen in _screens)
            {
                string id = screen.GetIdentifier();

                if (screenOpacities.TryGetValue(id, out BackgroundOpacity opacity))
                    SetBackgroundColour(screen, opacity, false, value);
            }
        }

        [UIAction("cell-selected")]
        private void OnCellSelected(TableView tableView, int index)
        {
            EnableMovement = false;

            _selectedScreen = _screens[index];

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

        public enum BackgroundOpacity
        {
            Transparent,
            Translucent,
            Opaque
        }
    }
}
