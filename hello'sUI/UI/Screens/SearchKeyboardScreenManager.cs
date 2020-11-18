using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using HMUI;
using VRUIControls;
using BS_Utils.Utilities;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage;
using HUI.Search;
using HUI.UI.Components;
using static HUI.Search.WordPredictionEngine;
using Object = UnityEngine.Object;
using BSMLUtilities = BeatSaberMarkupLanguage.Utilities;

namespace HUI.UI.Screens
{
    public class SearchKeyboardScreenManager : ScreenManagerBase
    {
        public event Action<char> KeyPressed;
        public event Action DeleteButtonPressed;
        public event Action ClearButtonPressed;
        public event Action<SuggestedWord> PredictionButtonPressed;
        public event Action SearchOptionChanged;

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

        [UIValue("close-screen-on-select-level-value")]
        public bool CloseScreenOnSelectLevelValue
        {
            get => PluginConfig.Instance.Search.CloseScreenOnSelectLevel;
            set
            {
                if (PluginConfig.Instance.Search.CloseScreenOnSelectLevel == value)
                    return;

                PluginConfig.Instance.Search.CloseScreenOnSelectLevel = value;
            }
        }

        [UIValue("close-screen-on-select-level-collection-value")]
        public bool CloseScreenOnLevelCollectionSelectValue
        {
            get => PluginConfig.Instance.Search.CloseScreenOnSelectLevelCollection;
            set
            {
                if (PluginConfig.Instance.Search.CloseScreenOnSelectLevelCollection == value)
                    return;

                PluginConfig.Instance.Search.CloseScreenOnSelectLevelCollection = value;
            }
        }

        [UIValue("clear-query-on-select-level-collection-value")]
        public bool ClearQueryOnSelectLevelCollection
        {
            get => PluginConfig.Instance.Search.ClearQueryOnSelectLevelCollection;
            set
            {
                if (PluginConfig.Instance.Search.ClearQueryOnSelectLevelCollection == value)
                    return;

                PluginConfig.Instance.Search.ClearQueryOnSelectLevelCollection = value;
            }
        }

        [UIValue("off-hand-laser-pointer-value")]
        public bool OffHandLaserPointer
        {
            get => PluginConfig.Instance.Search.UseOffHandLaserPointer;
            set
            {
                if (PluginConfig.Instance.Search.UseOffHandLaserPointer == value)
                    return;

                PluginConfig.Instance.Search.UseOffHandLaserPointer = value;

                _laserPointerManager.Enabled = IsVisible && PluginConfig.Instance.Search.UseOffHandLaserPointer;
            }
        }

        [UIValue("strip-symbols-value")]
        public bool StripSymbols
        {
            get => PluginConfig.Instance.Search.StripSymbols;
            set
            {
                if (PluginConfig.Instance.Search.StripSymbols == value)
                    return;

                PluginConfig.Instance.Search.StripSymbols = value;
                OnSearchOptionChanged();
            }
        }

        [UIValue("split-query-value")]
        public bool SplitQueryByWords
        {
            get => PluginConfig.Instance.Search.SplitQueryByWords;
            set
            {
                if (PluginConfig.Instance.Search.SplitQueryByWords == value)
                    return;

                PluginConfig.Instance.Search.SplitQueryByWords = value;
                OnSearchOptionChanged();
            }
        }

        [UIValue("song-fields-song-title-value")]
        public bool SearchSongTitleFieldValue
        {
            get => (PluginConfig.Instance.Search.SongFieldsToSearch | WordSearchEngine.SearchableSongFields.SongName) != 0;
            set
            {
                WordSearchEngine.SearchableSongFields expectedValue;
                if (value)
                    expectedValue = PluginConfig.Instance.Search.SongFieldsToSearch | WordSearchEngine.SearchableSongFields.SongName;
                else
                    expectedValue = PluginConfig.Instance.Search.SongFieldsToSearch & ~WordSearchEngine.SearchableSongFields.SongName;

                if (PluginConfig.Instance.Search.SongFieldsToSearch == expectedValue)
                    return;

                PluginConfig.Instance.Search.SongFieldsToSearch = expectedValue;
                OnSearchOptionChanged();
            }
        }

        [UIValue("song-fields-song-author-value")]
        public bool SearchSongAuthorFieldValue
        {
            get => (PluginConfig.Instance.Search.SongFieldsToSearch | WordSearchEngine.SearchableSongFields.SongAuthor) != 0;
            set
            {
                WordSearchEngine.SearchableSongFields expectedValue;
                if (value)
                    expectedValue = PluginConfig.Instance.Search.SongFieldsToSearch | WordSearchEngine.SearchableSongFields.SongAuthor;
                else
                    expectedValue = PluginConfig.Instance.Search.SongFieldsToSearch & ~WordSearchEngine.SearchableSongFields.SongAuthor;

                if (PluginConfig.Instance.Search.SongFieldsToSearch == expectedValue)
                    return;

                PluginConfig.Instance.Search.SongFieldsToSearch = expectedValue;
                OnSearchOptionChanged();
            }
        }

        [UIValue("song-fields-level-author-value")]
        public bool SearchLevelAuthorFieldValue
        {
            get => (PluginConfig.Instance.Search.SongFieldsToSearch | WordSearchEngine.SearchableSongFields.LevelAuthor) != 0;
            set
            {
                WordSearchEngine.SearchableSongFields expectedValue;
                if (value)
                    expectedValue = PluginConfig.Instance.Search.SongFieldsToSearch | WordSearchEngine.SearchableSongFields.LevelAuthor;
                else
                    expectedValue = PluginConfig.Instance.Search.SongFieldsToSearch & ~WordSearchEngine.SearchableSongFields.LevelAuthor;

                if (PluginConfig.Instance.Search.SongFieldsToSearch == expectedValue)
                    return;

                PluginConfig.Instance.Search.SongFieldsToSearch = expectedValue;
                OnSearchOptionChanged();
            }
        }

        [UIValue("song-fields-contributors-value")]
        public bool SearchContributorsFieldValue
        {
            get => (PluginConfig.Instance.Search.SongFieldsToSearch | WordSearchEngine.SearchableSongFields.Contributors) != 0;
            set
            {
                WordSearchEngine.SearchableSongFields expectedValue;
                if (value)
                    expectedValue = PluginConfig.Instance.Search.SongFieldsToSearch | WordSearchEngine.SearchableSongFields.Contributors;
                else
                    expectedValue = PluginConfig.Instance.Search.SongFieldsToSearch & ~WordSearchEngine.SearchableSongFields.Contributors;

                if (PluginConfig.Instance.Search.SongFieldsToSearch == expectedValue)
                    return;

                PluginConfig.Instance.Search.SongFieldsToSearch = expectedValue;
                OnSearchOptionChanged();
            }
        }

        [UIValue("keyboard-height")]
        public float KeyboardHeight => KeyHeight * 4 + KeySpacing * 3;

        [UIValue("list-data")]
        private List<object> _settingsList = new List<object>
        {
            "General Settings",
            "Search Options",
            "Screen Position"
        };

        private GameObject[] _settingsContainers;
        private BSMLParserParams _settingsViewParserParams;

#pragma warning disable CS0649
        [UIObject("keyboard-view")]
        private GameObject _keyboardView;
        [UIObject("settings-view")]
        private GameObject _settingsView;

        [UIObject("keyboard-container")]
        private GameObject _keyboardContainer;

        [UIObject("general-settings-container")]
        private GameObject _generalSettingsContainer;
        [UIObject("search-settings-container")]
        private GameObject _searchSettingsContainer;
        [UIObject("position-settings-container")]
        private GameObject _positionSettingsContainer;

        [UIComponent("settings-image")]
        private ClickableImage _settingsImage;
        [UIComponent("close-image")]
        private ClickableImage _closeImage;

        [UIComponent("movement-lock-image")]
        private ClickableImage _lockImage;
        [UIComponent("movement-reset-image")]
        private ClickableImage _resetImage;
#pragma warning restore CS0649

        private LevelCollectionViewController _levelCollectionViewController;

        private WordPredictionEngine _wordPredictionEngine;
        private PredictionBar _predictionBar;
        private LaserPointerManager _laserPointerManager;

        private List<CustomKeyboardKeyButton> _keys;

        private bool _symbolMode = false;
        private CustomKeyboardActionButton _symbolButton;

        public static readonly Vector3 DefaultKeyboardPosition = new Vector3(0f, 0.5f, 1.4f);
        public static readonly Quaternion DefaultKeyboardRotation = Quaternion.Euler(75f, 0f, 0f);

        private static Sprite _lockSprite;
        private static Sprite _unlockSprite;

        private static readonly Color LockDefaultColour = new Color(0.47f, 0.47f, 0.47f);
        private static readonly Color UnlockDefaultColour = new Color(0.67f, 0.67f, 0.67f);

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
            WordPredictionEngine wordPredictionEngine,
            LaserPointerManager laserPointerManager)
            : base(mainMenuVC, soloFC, partyFC, levelCollectionNC, physicsRaycaster, new Vector2(ScreenWidth, 40f), DefaultKeyboardPosition, DefaultKeyboardRotation)
        {
            _levelCollectionViewController = levelCollectionViewController;
            _wordPredictionEngine = wordPredictionEngine;
            _laserPointerManager = laserPointerManager;

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
            deleteButton.ButtonPressed += delegate ()
            {
                try
                {
                    DeleteButtonPressed?.Invoke();
                }
                catch (Exception e)
                {
                    Plugin.Log.Warn($"Unexpected exception occurred in {nameof(SearchKeyboardScreenManager)}:{nameof(DeleteButtonPressed)} event");
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
            clearButton.ButtonPressed += OnClearButtonPressed;
            clearButton.SelectedColour = new Color(1f, 0.216f, 0.067f);

            Object.Destroy(keyPrefab.gameObject);

            // button sprites
            _settingsImage.sprite = UIUtilities.LoadSpriteFromResources("HUI.Assets.settings.png");
            _closeImage.sprite = UIUtilities.LoadSpriteFromResources("HUI.Assets.cross.png");
            _resetImage.sprite = UIUtilities.LoadSpriteFromResources("HUI.Assets.refresh.png");

            if (_lockSprite == null)
                _lockSprite = UIUtilities.LoadSpriteFromResources("HUI.Assets.lock.png");
            if (_unlockSprite == null)
                _unlockSprite = UIUtilities.LoadSpriteFromResources("HUI.Assets.unlock.png");

            _lockImage.sprite = _lockSprite;
        }

        public override void Initialize()
        {
            base.Initialize();

            // late loading of some bsml stuff
            _settingsViewParserParams = BSMLParser.instance.Parse(BSMLUtilities.GetResourceContent(Assembly.GetExecutingAssembly(), "HUI.UI.Views.DelayedSearchKeyboardScreenView.bsml"), _settingsView, this);

            // minify all toggles
            IEnumerable<Toggle> allToggles = _generalSettingsContainer.GetComponentsInChildren<Toggle>(true).Concat(_searchSettingsContainer.GetComponentsInChildren<Toggle>(true));
            foreach (var toggle in allToggles)
                toggle.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            // it is dumb that bsml returns some deeply nested container instead of the root gameobject of the newly created scrollview
            _searchSettingsContainer = _searchSettingsContainer.transform.parent.parent.parent.gameObject;

            // resize search settings container
            RectTransform rt = _searchSettingsContainer.transform.Find("Viewport") as RectTransform;
            rt.sizeDelta = new Vector2(-4f, 0f);
            rt.anchoredPosition = new Vector2(-2f, rt.anchoredPosition.y);

            rt = _searchSettingsContainer.transform.Find("ScrollBar") as RectTransform;
            rt.sizeDelta = new Vector2(4f, rt.sizeDelta.y);

            rt = _searchSettingsContainer.transform.Find("ScrollBar/UpButton/Icon") as RectTransform;
            rt.anchoredPosition = new Vector2(-2f, rt.anchoredPosition.y);

            rt = _searchSettingsContainer.transform.Find("ScrollBar/DownButton/Icon") as RectTransform;
            rt.anchoredPosition = new Vector2(-2f, rt.anchoredPosition.y);

            // settings containers
            _settingsContainers = new GameObject[]
            {
                _generalSettingsContainer,
                _searchSettingsContainer,
                _positionSettingsContainer
            };

            PluginConfig.Instance.ConfigReloaded += OnPluginConfigReloaded;

            _levelCollectionViewController.didSelectLevelEvent += OnLevelSelected;

            _predictionBar.PredictionButtonPressed += OnPredictionButtonPressed;
        }

        public override void Dispose()
        {
            base.Dispose();

            if (PluginConfig.Instance != null)
                PluginConfig.Instance.ConfigReloaded -= OnPluginConfigReloaded;

            if (_levelCollectionViewController != null)
                _levelCollectionViewController.didSelectLevelEvent -= OnLevelSelected;

            if (_predictionBar != null)
                _predictionBar.PredictionButtonPressed -= OnPredictionButtonPressed;
        }

        protected override void OnLevelCollectionNavigationControllerActivated(bool firstActivation, bool addToHierarchy, bool screenSystemEnabling)
        {
            // do not show screen during activation
        }

        private void OnKeyPressed(char key)
        {
            // exception logging done in CustomKeyboardButton
            KeyPressed?.Invoke(key);
        }

        public void ShowScreen()
        {
            // always show keyboard view when being revealed
            _keyboardView.SetActive(true);
            _settingsView.SetActive(false);

            _animationHandler.PlayRevealAnimation();

            _laserPointerManager.Enabled = PluginConfig.Instance.Search.UseOffHandLaserPointer;
        }

        public void HideScreen()
        {
            AllowScreenMovement(false);

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

        private void AllowScreenMovement(bool allowMovement)
        {
            if (allowMovement)
            {
                _lockImage.sprite = _unlockSprite;
                _lockImage.DefaultColor = UnlockDefaultColour;

                _screen.ShowHandle = true;
                _resetImage.gameObject.SetActive(true);
            }
            else
            {
                _lockImage.sprite = _lockSprite;
                _lockImage.DefaultColor = LockDefaultColour;

                _screen.ShowHandle = false;
                _resetImage.gameObject.SetActive(false);

                PluginConfig.Instance.Search.KeyboardPosition = _screen.ScreenPosition;
                PluginConfig.Instance.Search.KeyboardRotation = _screen.ScreenRotation;
            }
        }

        private void OnPluginConfigReloaded() => _settingsViewParserParams.EmitEvent("refresh-all-values");

        private void OnLevelSelected(LevelCollectionViewController viewController, IPreviewBeatmapLevel level)
        {
            if (PluginConfig.Instance.Search.CloseScreenOnSelectLevel)
                HideScreen();
        }

        private void OnPredictionButtonPressed(SuggestedWord suggestedWord) => PredictionButtonPressed?.Invoke(suggestedWord);

        private void OnClearButtonPressed()
        {
            string oldSearchText = SearchText.Trim();

            try
            {
                ClearButtonPressed?.Invoke();
            }
            catch (Exception e)
            {
                Plugin.Log.Warn($"Unexpected exception occurred in {nameof(SearchKeyboardScreenManager)}:{nameof(ClearButtonPressed)} event");
                Plugin.Log.Debug(e);
            }

            // show cleared search text as a predicted word to serve as an undo
            if (!string.IsNullOrWhiteSpace(oldSearchText) && oldSearchText != DefaultSearchText)
                SetSuggestedWords(new SuggestedWord[] { new SuggestedWord(oldSearchText.ToLower(), SuggestionType.FuzzyMatch) });
        }

        private void OnSearchOptionChanged()
        {
            try
            {
                SearchOptionChanged?.Invoke();
            }
            catch (Exception e)
            {
                Plugin.Log.Warn($"Unexpected exception occurred in {nameof(SearchKeyboardScreenManager)}:{nameof(SearchOptionChanged)} event");
                Plugin.Log.Debug(e);
            }
        }

        [UIAction("settings-clicked")]
        private void OnSettingsClicked()
        {
            OnResetClicked();
            AllowScreenMovement(false);

            if (_keyboardView.activeSelf)
            {
                _keyboardView.SetActive(false);
                _settingsView.SetActive(true);
            }
            else
            {
                _keyboardView.SetActive(true);
                _settingsView.SetActive(false);
            }
        }

        [UIAction("close-clicked")]
        private void OnCloseClicked() => HideScreen();

        [UIAction("lock-clicked")]
        private void OnLockClicked() => AllowScreenMovement(!_screen.ShowHandle);

        [UIAction("reset-clicked")]
        private void OnResetClicked()
        {
            _screen.ScreenPosition = PluginConfig.Instance.Search.KeyboardPosition;
            _screen.ScreenRotation = PluginConfig.Instance.Search.KeyboardRotation;
        }

        [UIAction("list-cell-selected")]
        private void OnListCellSelected(SegmentedControl control, int index)
        {
            foreach (GameObject go in _settingsContainers)
                go.SetActive(false);

            _settingsContainers[index].SetActive(true);
        }
    }
}
