using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HMUI;
using VRUIControls;
using BeatSaberMarkupLanguage.Attributes;
using HUI.Search;
using HUI.UI.Components;
using static HUI.Search.WordPredictionEngine;
using Object = UnityEngine.Object;

namespace HUI.UI.Screens
{
    public class SearchKeyboardScreenManager : ScreenManagerBase
    {
        public event Action<char> KeyPressed;
        public event Action DeleteButtonPressed;
        public event Action ClearButtonPressed;
        public event Action<SuggestedWord> PredictionButtonPressed;

        protected override string AssociatedBSMLResource => "HUI.UI.Views.SearchKeyboardScreenView.bsml";
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
        [UIObject("keyboard-view")]
        private GameObject _keyboardView;
        [UIObject("settings-view")]
        private GameObject _settingsView;

        [UIObject("keyboard-container")]
        private GameObject _keyboardContainer;
#pragma warning restore CS0649

        private WordPredictionEngine _wordPredictionEngine;
        private PredictionBar _predictionBar;

        private List<CustomKeyboardKeyButton> _keys;

        private bool _symbolMode = false;
        private CustomKeyboardActionButton _symbolButton;

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
            UIKeyboardManager uiKeyboardManager,
            WordPredictionEngine wordPredictionEngine)
            : base(mainMenuVC, soloFC, partyFC, levelCollectionNC, physicsRaycaster, new Vector2(ScreenWidth, 40f), new Vector3(0f, 0.5f, 1.4f), Quaternion.Euler(75f, 0f, 0f))
        {
            _wordPredictionEngine = wordPredictionEngine;

            this._screen.name = "HUISearchKeyboardScreen";
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
            deleteButton.ButtonPressed += delegate ()
            {
                try
                {
                    DeleteButtonPressed?.Invoke();
                }
                catch (Exception e)
                {
                    Plugin.Log.Warn($"Unexpected error occurred in {nameof(SearchKeyboardScreenManager)}:{nameof(DeleteButtonPressed)} event");
                    Plugin.Log.Debug(e);
                }
            };
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
            clearButton.ButtonPressed += delegate ()
            {
                string oldSearchText = SearchText.ToLower().Trim();

                try
                {
                    ClearButtonPressed?.Invoke();
                }
                catch (Exception e)
                {
                    Plugin.Log.Warn($"Unexpected error occurred in {nameof(SearchKeyboardScreenManager)}:{nameof(ClearButtonPressed)} event");
                    Plugin.Log.Debug(e);
                }

                // show cleared search text as a predicted word to serve as an undo
                if (!string.IsNullOrWhiteSpace(oldSearchText) && oldSearchText != DefaultSearchText)
                    SetSuggestedWords(new SuggestedWord[] { new SuggestedWord(oldSearchText, SuggestionType.FuzzyMatch) });
            };
            clearButton.SelectedColour = new Color(1f, 0.216f, 0.067f);

            Object.Destroy(keyPrefab.gameObject);
        }

        public override void Initialize()
        {
            base.Initialize();

            _predictionBar.PredictionButtonPressed += OnPredictionButtonPressed;
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_predictionBar != null)
                _predictionBar.PredictionButtonPressed -= OnPredictionButtonPressed;
        }

        protected override void OnLevelCollectionNavigationControllerActivated(bool firstActivation, bool addToHierarchy, bool screenSystemEnabling)
        {
            // overriding to not show screen during activation
        }

        private void OnKeyPressed(char key)
        {
            // exception logging done in CustomKeyboardButton
            KeyPressed?.Invoke(key);
        }

        public void ShowScreen() => _animationHandler.PlayRevealAnimation();

        public void HideScreen() => _animationHandler.PlayConcealAnimation();

        public void SetSuggestedWords(IEnumerable<SuggestedWord> suggestedWords)
        {
            _predictionBar.ClearAndSetPredictionButtons(suggestedWords);
        }

        private void OnPredictionButtonPressed(SuggestedWord suggestedWord) => PredictionButtonPressed?.Invoke(suggestedWord);
    }
}
