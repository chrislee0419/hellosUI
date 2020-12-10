using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HMUI;
using VRUIControls;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.FloatingScreen;
using HUI.Search;
using HUI.UI.Components;
using HUI.UI.Settings;
using HUI.Utilities;
using static HUI.Search.WordPredictionEngine;
using Object = UnityEngine.Object;

namespace HUI.UI.Screens
{
    public class SearchKeyboardScreenManager : ModifiableScreenManagerBase
    {
        public event Action<char> KeyPressed;
        public event Action DeleteButtonPressed;
        public event Action ClearButtonPressed;
        public event Action<SuggestedWord> PredictionButtonPressed;
        public event Action SearchOptionChanged;

        public override string ScreenName => "Search Keyboard";
        public override Graphic Background => throw new NotImplementedException();
        protected override string AssociatedBSMLResource => "HUI.UI.Views.Screens.SearchKeyboardScreenView.bsml";
        protected override bool ShowScreenOnSinglePlayerLevelSelectionStarting => false;

        public bool IsVisible => _screen.gameObject.activeSelf;

        private string _searchText = DefaultSearchText;
        [UIValue("search-text")]
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (string.IsNullOrEmpty(value))
                    value = DefaultSearchText;
                else
                    value = value.ToUpper();

                if (_searchText == value)
                    return;

                _searchText = value;
                NotifyPropertyChanged();
            }
        }

        [UIValue("keyboard-height")]
        public float KeyboardHeight => KeyHeight * 4 + KeySpacing * 3;

#pragma warning disable CS0649
        [UIObject("keyboard-container")]
        private GameObject _keyboardContainer;
        [UIObject("keyboard-view")]
        private GameObject _keyboardView;
#pragma warning restore CS0649

        private LevelCollectionViewController _levelCollectionViewController;

        private PredictionBar _predictionBar;
        private LaserPointerManager _laserPointerManager;
        private SettingsModalManager _settingsModalManager;
        private SearchSettingsTab _searchSettingsTab;

        private List<CustomKeyboardKeyButton> _keys;

        private bool _symbolMode = false;
        private CustomKeyboardActionButton _symbolButton;

        public static readonly Vector3 DefaultKeyboardPosition = new Vector3(0f, 0.5f, 1.4f);
        public static readonly Quaternion DefaultKeyboardRotation = Quaternion.Euler(75f, 0f, 0f);

        private const float KeyWidth = 5.5f;
        private const float KeyHeight = 4.5f;
        private const float KeySpacing = 0.3f;
        private const float ScreenWidth = 13.5f * KeyWidth + 13 * KeySpacing + 4f;

        private const string DefaultSearchText = "<i>Search...</i>";

        public SearchKeyboardScreenManager(
            MainMenuViewController mainMenuVC,
            SoloFreePlayFlowCoordinator soloFC,
            PartyFreePlayFlowCoordinator partyFC,
            LevelCollectionNavigationController levelCollectionNC,
            PhysicsRaycasterWithCache physicsRaycaster,
            LevelCollectionViewController levelCollectionViewController,
            UIKeyboardManager uiKeyboardManager,
            LaserPointerManager laserPointerManager,
            SettingsModalManager settingsModalManager,
            SearchSettingsTab searchSettingsTab)
            : base(mainMenuVC, soloFC, partyFC, levelCollectionNC, physicsRaycaster, new Vector2(ScreenWidth, 40f), DefaultKeyboardPosition, DefaultKeyboardRotation)
        {
            _levelCollectionViewController = levelCollectionViewController;
            _laserPointerManager = laserPointerManager;
            _settingsModalManager = settingsModalManager;
            _searchSettingsTab = searchSettingsTab;

            this._screen.name = "HUISearchKeyboardScreen";
            this._screen.HandleSide = FloatingScreen.Side.Bottom;
            this._screen.ScreenPosition = PluginConfig.Instance.Search.KeyboardPosition;
            this._screen.ScreenRotation = PluginConfig.Instance.Search.KeyboardRotation;
            this._animationHandler.UsePointerAnimations = false;

            // prediction bar creation
            _predictionBar = new GameObject("PredictionBar").AddComponent<PredictionBar>();
            _predictionBar.RectTransform.SetParent(_keyboardView.transform, false);
            _predictionBar.RectTransform.anchorMin = new Vector2(0f, 1f);
            _predictionBar.RectTransform.anchorMax = Vector2.one;
            _predictionBar.RectTransform.pivot = new Vector2(0.5f, 1f);
            _predictionBar.RectTransform.anchoredPosition = new Vector2(0f, -10.5f);
            _predictionBar.RectTransform.sizeDelta = new Vector2(-4f, 6.25f);

            // keyboard screen creation
            GameObject keyPrefab = Object.Instantiate(uiKeyboardManager.keyboard.GetComponentsInChildren<UIKeyboardKey>().First().gameObject);
            Object.DestroyImmediate(keyPrefab.GetComponent<UIKeyboardKey>());
            Object.DestroyImmediate(keyPrefab.GetComponent<ButtonStaticAnimations>());
            Object.DestroyImmediate(keyPrefab.GetComponent<LayoutElement>());

            keyPrefab.AddComponent<CustomButtonAnimations>();
            keyPrefab.AddComponent<CustomKeyboardKeyButton>();

            // 26 letter + 10 number keys + space + comma + period
            _keys = new List<CustomKeyboardKeyButton>(39);

            Vector2 StandardKeySize = new Vector2(KeyWidth, KeyHeight);

            char[][] letters = new char[][]
            {
                new char[] { 'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p'},
                new char[] { 'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l' },
                new char[] { 'z', 'x', 'c', 'v', 'b', 'n', 'm' }
            };
            // comma, period, apostrophe and quotation mark will be on the bottom row
            char[][] altKeys = new char[][]
            {
                new char[] { '+', '-', '/', '*', '=', '_', '<', '>', '[', ']' },
                new char[] { '!', '@', '#', '$', '%', '^', '&', '(', ')' },
                new char[] { '`', '~', '{', '}', ':', ';', '?' }
            };
            for (int i = 0; i < letters.Length; ++i)
            {
                float xOffset = (KeyWidth + KeySpacing) * i / 2f;
                float yPos = (KeyHeight + KeySpacing) * -i;

                for (int j = 0; j < letters[i].Length; ++j)
                {
                    CustomKeyboardKeyButton newKey = Object.Instantiate(keyPrefab, _keyboardContainer.transform, false).GetComponent<CustomKeyboardKeyButton>();
                    newKey.name = letters[i][j].ToString().ToUpper();

                    newKey.rectTransform.anchorMin = new Vector2(0f, 1f);
                    newKey.rectTransform.anchorMax = new Vector2(0f, 1f);
                    newKey.rectTransform.pivot = new Vector2(0f, 1f);
                    newKey.rectTransform.sizeDelta = StandardKeySize;
                    newKey.rectTransform.anchoredPosition = new Vector2(xOffset + (KeyWidth + KeySpacing) * j, yPos);

                    newKey.Key = letters[i][j];
                    newKey.AltKey = altKeys[i][j];
                    newKey.KeyPressed += OnKeyPressed;

                    _keys.Add(newKey);
                }
            }

            // numbers
            char[][] numbers = new char[][]
            {
                new char[] { '7', '8', '9' },
                new char[] { '4', '5', '6' },
                new char[] { '1', '2', '3' },
                new char[] { '\0', '0' }
            };
            for (int i = 0; i < numbers.Length; ++i)
            {
                float yPos = (KeyHeight + KeySpacing) * -i;

                for (int j = 0; j < numbers[i].Length; ++j)
                {
                    if (numbers[i][j] == '\0')
                        continue;

                    CustomKeyboardKeyButton newKey = Object.Instantiate(keyPrefab, _keyboardContainer.transform, false).GetComponent<CustomKeyboardKeyButton>();
                    newKey.name = numbers[i][j].ToString();

                    newKey.rectTransform.anchorMin = Vector2.one;
                    newKey.rectTransform.anchorMax = Vector2.one;
                    newKey.rectTransform.pivot = Vector2.one;
                    newKey.rectTransform.sizeDelta = StandardKeySize;
                    newKey.rectTransform.anchoredPosition = new Vector2((j - 2) * (KeyWidth + KeySpacing), yPos);

                    newKey.Key = numbers[i][j];
                    newKey.AltKey = numbers[i][j];
                    newKey.KeyPressed += OnKeyPressed;

                    _keys.Add(newKey);
                }
            }

            // comma
            CustomKeyboardKeyButton commaButton = Object.Instantiate(keyPrefab, _keyboardContainer.transform, false).GetComponent<CustomKeyboardKeyButton>();
            commaButton.name = "Comma";
            commaButton.rectTransform.anchorMin = Vector2.zero;
            commaButton.rectTransform.anchorMax = Vector2.zero;
            commaButton.rectTransform.pivot = Vector2.zero;
            commaButton.rectTransform.sizeDelta = StandardKeySize;
            commaButton.rectTransform.anchoredPosition = new Vector2(1.5f * KeyWidth + 2 * KeySpacing, 0f);

            commaButton.Key = ',';
            commaButton.AltKey = '\'';
            commaButton.KeyPressed += OnKeyPressed;

            _keys.Add(commaButton);

            // space
            CustomKeyboardKeyButton spaceButton = Object.Instantiate(keyPrefab, _keyboardContainer.transform, false).GetComponent<CustomKeyboardKeyButton>();
            spaceButton.name = "Space";
            spaceButton.rectTransform.anchorMin = Vector2.zero;
            spaceButton.rectTransform.anchorMax = Vector2.zero;
            spaceButton.rectTransform.pivot = Vector2.zero;
            spaceButton.rectTransform.sizeDelta = new Vector2(5 * KeyWidth + 3 * KeySpacing, KeyHeight);
            spaceButton.rectTransform.anchoredPosition = new Vector2(2.5f * KeyWidth + 3 * KeySpacing, 0f);

            spaceButton.Key = ' ';
            spaceButton.AltKey = ' ';
            spaceButton.KeyPressed += OnKeyPressed;

            _keys.Add(spaceButton);

            // period
            CustomKeyboardKeyButton periodButton = Object.Instantiate(keyPrefab, _keyboardContainer.transform, false).GetComponent<CustomKeyboardKeyButton>();
            periodButton.name = "Period";
            periodButton.rectTransform.anchorMin = Vector2.zero;
            periodButton.rectTransform.anchorMax = Vector2.zero;
            periodButton.rectTransform.pivot = Vector2.zero;
            periodButton.rectTransform.sizeDelta = StandardKeySize;
            periodButton.rectTransform.anchoredPosition = new Vector2(7.5f * KeyWidth + 7 * KeySpacing, 0f);

            periodButton.Key = '.';
            periodButton.AltKey = '"';
            periodButton.KeyPressed += OnKeyPressed;

            _keys.Add(periodButton);

            // non-key buttons
            Object.DestroyImmediate(keyPrefab.GetComponent<CustomKeyboardKeyButton>());
            keyPrefab.AddComponent<CustomKeyboardActionButton>();

            // delete
            CustomKeyboardActionButton deleteButton = Object.Instantiate(keyPrefab, _keyboardContainer.transform, false).GetComponent<CustomKeyboardActionButton>();
            deleteButton.name = "Delete";
            deleteButton.rectTransform.anchorMin = Vector2.zero;
            deleteButton.rectTransform.anchorMax = Vector2.zero;
            deleteButton.rectTransform.pivot = Vector2.zero;
            deleteButton.rectTransform.sizeDelta = new Vector2(2 * KeyWidth + KeySpacing, KeyHeight);
            deleteButton.rectTransform.anchoredPosition = new Vector2(8 * (KeyWidth + KeySpacing), KeyHeight + KeySpacing);

            deleteButton.Text = "DEL";
            deleteButton.ButtonPressed += () => this.CallAndHandleAction(DeleteButtonPressed, nameof(DeleteButtonPressed));
            deleteButton.SelectedColour = new Color(1f, 0.216f, 0.067f);

            // symbols
            _symbolButton = Object.Instantiate(keyPrefab, _keyboardContainer.transform, false).GetComponent<CustomKeyboardActionButton>();
            _symbolButton.name = "Symbols";
            _symbolButton.rectTransform.anchorMin = Vector2.zero;
            _symbolButton.rectTransform.anchorMax = Vector2.zero;
            _symbolButton.rectTransform.pivot = Vector2.zero;
            _symbolButton.rectTransform.sizeDelta = new Vector2(1.5f * KeyWidth + KeySpacing, KeyHeight);
            _symbolButton.rectTransform.anchoredPosition = Vector2.zero;

            _symbolButton.Text = "(!?)";
            _symbolButton.ButtonPressed += delegate ()
            {
                _symbolMode = !_symbolMode;

                _symbolButton.Text = _symbolMode ? "ABC" : "(!?)";
                foreach (var key in _keys)
                    key.ShowAltKey = _symbolMode;
            };
            _symbolButton.SelectedColour = new Color(0.4f, 1f, 0.133f);

            // clear
            CustomKeyboardActionButton clearButton = Object.Instantiate(keyPrefab, _keyboardContainer.transform, false).GetComponent<CustomKeyboardActionButton>();
            clearButton.name = "Clear";
            clearButton.rectTransform.anchorMin = Vector2.zero;
            clearButton.rectTransform.anchorMax = Vector2.zero;
            clearButton.rectTransform.pivot = Vector2.zero;
            clearButton.rectTransform.sizeDelta = new Vector2(1.5f * KeyWidth + KeySpacing, KeyHeight);
            clearButton.rectTransform.anchoredPosition = new Vector2(8.5f * KeyWidth + 8 * KeySpacing, 0f);

            clearButton.Text = "CLEAR";
            clearButton.ButtonPressed += OnClearButtonPressed;
            clearButton.SelectedColour = new Color(1f, 0.216f, 0.067f);

            Object.Destroy(keyPrefab.gameObject);
        }

        public override void Initialize()
        {
            base.Initialize();

            _levelCollectionViewController.didSelectLevelEvent += OnLevelSelected;

            _predictionBar.PredictionButtonPressed += OnPredictionButtonPressed;

            _settingsModalManager.SettingsModalClosed += OnSettingsModalClosed;

            _searchSettingsTab.OffHandLaserPointerSettingChanged += OnOffHandLaserPointerSettingChanged;
            _searchSettingsTab.SearchOptionChanged += OnSearchOptionChanged;
            _searchSettingsTab.AllowScreenMovementClicked += OnAllowScreenMovementClicked;
            _searchSettingsTab.ResetScreenPositionClicked += OnResetScreenPositionClicked;
            _searchSettingsTab.DefaultScreenPositionClicked += OnDefaultScreenPositionClicked;

            _searchSettingsTab.ScreenPositionGetter = () => (_screen.ScreenPosition, _screen.ScreenRotation);
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_levelCollectionViewController != null)
                _levelCollectionViewController.didSelectLevelEvent -= OnLevelSelected;

            if (_predictionBar != null)
                _predictionBar.PredictionButtonPressed -= OnPredictionButtonPressed;


            _settingsModalManager.SettingsModalClosed -= OnSettingsModalClosed;

            if (_searchSettingsTab != null)
            {
                _searchSettingsTab.OffHandLaserPointerSettingChanged -= OnOffHandLaserPointerSettingChanged;
                _searchSettingsTab.SearchOptionChanged -= OnSearchOptionChanged;
                _searchSettingsTab.AllowScreenMovementClicked -= OnAllowScreenMovementClicked;
                _searchSettingsTab.ResetScreenPositionClicked -= OnResetScreenPositionClicked;
                _searchSettingsTab.DefaultScreenPositionClicked -= OnDefaultScreenPositionClicked;

                _searchSettingsTab.ScreenPositionGetter = null;
            }
        }

        protected override void OnLevelCollectionNavigationControllerActivated(bool firstActivation, bool addToHierarchy, bool screenSystemEnabling)
        {
            // do not show screen during activation
        }

        protected override void OnLevelCollectionNavigationControllerDeactivated(bool removedFromHierarchy, bool screenSystemDisabling) => HideScreen();

        private void OnKeyPressed(char key)
        {
            // exception logging done in CustomKeyboardButton
            KeyPressed?.Invoke(key);
        }

        public void ShowScreen()
        {
            _animationHandler.PlayRevealAnimation();

            _laserPointerManager.Enabled = PluginConfig.Instance.Search.UseOffHandLaserPointer;
        }

        public void HideScreen()
        {
            OnAllowScreenMovementClicked(false);

            _animationHandler.PlayConcealAnimation();

            _laserPointerManager.Enabled = false;
        }

        public void OnLevelCollectionSelected()
        {
            if (PluginConfig.Instance.Search.ClearQueryOnSelectLevelCollection)
                OnClearButtonPressed();

            if (PluginConfig.Instance.Search.CloseScreenOnSelectLevelCollection)
                HideScreen();
        }

        public void SetSuggestedWords(IEnumerable<SuggestedWord> suggestedWords)
        {
            _predictionBar.ClearAndSetPredictionButtons(suggestedWords);
        }

        private void OnAllowScreenMovementClicked(bool allowMovement)
        {
            _screen.ShowHandle = allowMovement;

            if (!allowMovement)
            {
                PluginConfig.Instance.Search.KeyboardPosition = _screen.ScreenPosition;
                PluginConfig.Instance.Search.KeyboardRotation = _screen.ScreenRotation;
            }
        }

        private void OnLevelSelected(LevelCollectionViewController viewController, IPreviewBeatmapLevel level)
        {
            if (PluginConfig.Instance.Search.CloseScreenOnSelectLevel)
                HideScreen();
        }

        private void OnPredictionButtonPressed(SuggestedWord suggestedWord) => PredictionButtonPressed?.Invoke(suggestedWord);

        private void OnClearButtonPressed()
        {
            string oldSearchText = SearchText.Trim();

            this.CallAndHandleAction(ClearButtonPressed, nameof(ClearButtonPressed));

            // show cleared search text as a predicted word to serve as an undo
            if (!string.IsNullOrWhiteSpace(oldSearchText) && oldSearchText != DefaultSearchText)
                SetSuggestedWords(new SuggestedWord[] { new SuggestedWord(oldSearchText.ToLower(), SuggestionType.FuzzyMatch) });
        }

        private void OnSettingsModalClosed()
        {
            if (_screen.ShowHandle)
            {
                _screen.ShowHandle = false;

                _screen.ScreenPosition = PluginConfig.Instance.Search.KeyboardPosition;
                _screen.ScreenRotation = PluginConfig.Instance.Search.KeyboardRotation;
            }
        }

        private void OnOffHandLaserPointerSettingChanged()
        {
            _laserPointerManager.Enabled = IsVisible && PluginConfig.Instance.Search.UseOffHandLaserPointer;
        }

        private void OnSearchOptionChanged()
        {
            // exception logging handled by SearchSettingsTab
            SearchOptionChanged?.Invoke();
        }

        private void OnResetScreenPositionClicked()
        {
            _screen.ScreenPosition = PluginConfig.Instance.Search.KeyboardPosition;
            _screen.ScreenRotation = PluginConfig.Instance.Search.KeyboardRotation;
        }

        private void OnDefaultScreenPositionClicked()
        {
            PluginConfig.Instance.Search.KeyboardPosition = DefaultKeyboardPosition;
            PluginConfig.Instance.Search.KeyboardRotation = DefaultKeyboardRotation;

            _screen.ScreenPosition = DefaultKeyboardPosition;
            _screen.ScreenRotation = DefaultKeyboardRotation;
        }

        [UIAction("settings-clicked")]
        private void OnSettingsClicked()
        {
            if (_settingsModalManager.IsVisible)
                _settingsModalManager.HideModal();
            else
                _settingsModalManager.ShowModal();
        }

        [UIAction("close-clicked")]
        private void OnCloseClicked() => HideScreen();
    }
}
