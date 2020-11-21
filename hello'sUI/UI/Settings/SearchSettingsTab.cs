using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HMUI;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using HUI.Search;
using HUI.Utilities;

namespace HUI.UI.Settings
{
    public class SearchSettingsTab : SettingsModalManager.SettingsModalTabBase, IDisposable
    {
        public event Action OffHandLaserPointerSettingChanged;
        public event Action SearchOptionChanged;
        public event Action<bool> AllowScreenMovementClicked;
        public event Action ResetScreenPositionClicked;
        public event Action DefaultScreenPositionClicked;

        public Func<(Vector3 position, Quaternion rotation)> ScreenPositionGetter { private get; set; }

        public override string TabName => "Search";
        protected override string AssociatedBSMLResource => "HUI.UI.Views.Settings.SearchSettingsView.bsml";

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

                this.CallAndHandleAction(OffHandLaserPointerSettingChanged, nameof(OffHandLaserPointerSettingChanged));
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
                this.CallAndHandleAction(SearchOptionChanged, nameof(SearchOptionChanged));
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
                this.CallAndHandleAction(SearchOptionChanged, nameof(SearchOptionChanged));
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
                this.CallAndHandleAction(SearchOptionChanged, nameof(SearchOptionChanged));
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
                this.CallAndHandleAction(SearchOptionChanged, nameof(SearchOptionChanged));
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
                this.CallAndHandleAction(SearchOptionChanged, nameof(SearchOptionChanged));
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
                this.CallAndHandleAction(SearchOptionChanged, nameof(SearchOptionChanged));
            }
        }

        [UIValue("allow-screen-movement-button-text")]
        public string AllowScreenMovementButtonText => _allowScreenMovement ? "Disable Repositioning" : "Enable Repositioning";

        [UIValue("set-default-screen-position-button-text")]
        public string SetDefaultScreenPositionButtonText => _allowScreenMovement ? "Undo Changes" : "Reset to Default";

        private bool _allowScreenMovement = false;
        public bool AllowScreenMovement
        {
            get => _allowScreenMovement;
            set
            {
                if (_allowScreenMovement == value)
                    return;

                _allowScreenMovement = value;
                NotifyPropertyChanged(nameof(AllowScreenMovementButtonText));
                NotifyPropertyChanged(nameof(SetDefaultScreenPositionButtonText));
            }
        }

#pragma warning disable CS0649
        [UIObject("root-container")]
        private GameObject _rootContainer;

        [UIObject("search-settings-container")]
        private GameObject _searchSettingsContainer;

        [UIComponent("screen-pos-x-text")]
        private TextMeshProUGUI _screenPosXText;
        [UIComponent("screen-pos-y-text")]
        private TextMeshProUGUI _screenPosYText;
        [UIComponent("screen-pos-z-text")]
        private TextMeshProUGUI _screenPosZText;
        [UIComponent("screen-rot-x-text")]
        private TextMeshProUGUI _screenRotXText;
        [UIComponent("screen-rot-y-text")]
        private TextMeshProUGUI _screenRotYText;
        [UIComponent("screen-rot-z-text")]
        private TextMeshProUGUI _screenRotZText;

        [UIParams]
        private BSMLParserParams _parserParams;
#pragma warning restore CS0649

        private static readonly WaitForSeconds ScreenPositionTextUpdateTime = new WaitForSeconds(0.2f);

        private const string ScreenPositionStringFormat = "F3";
        private const string ScreenRotationStringFormat = "F2";

        public override void SetupView()
        {
            base.SetupView();

            //// minify all toggles
            //IEnumerable<Toggle> allToggles = _rootContainer.GetComponentsInChildren<Toggle>(true).Concat(_searchSettingsContainer.GetComponentsInChildren<Toggle>(true));
            //foreach (var toggle in allToggles)
            //    toggle.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            //// it is dumb that bsml returns some deeply nested container instead of the root gameobject of the newly created scrollview
            //_searchSettingsContainer = _searchSettingsContainer.transform.parent.parent.parent.gameObject;

            //// resize search settings container
            //RectTransform rt = _searchSettingsContainer.transform.Find("Viewport") as RectTransform;
            //rt.sizeDelta = new Vector2(-4f, 0f);
            //rt.anchoredPosition = new Vector2(-2f, rt.anchoredPosition.y);

            //rt = _searchSettingsContainer.transform.Find("ScrollBar") as RectTransform;
            //rt.sizeDelta = new Vector2(4f, rt.sizeDelta.y);

            //rt = _searchSettingsContainer.transform.Find("ScrollBar/UpButton/Icon") as RectTransform;
            //rt.anchoredPosition = new Vector2(-2f, rt.anchoredPosition.y);

            //rt = _searchSettingsContainer.transform.Find("ScrollBar/DownButton/Icon") as RectTransform;
            //rt.anchoredPosition = new Vector2(-2f, rt.anchoredPosition.y);

            PluginConfig.Instance.ConfigReloaded += OnPluginConfigReloaded;

            UpdateScreenPositionText(true);
        }

        public override void OnModalClosed()
        {
            AllowScreenMovement = false;
        }

        public void Dispose()
        {
            if (PluginConfig.Instance != null)
                PluginConfig.Instance.ConfigReloaded -= OnPluginConfigReloaded;
        }

        private IEnumerator UpdateScreenPositionTextCoroutine()
        {
            while (AllowScreenMovement)
            {
                UpdateScreenPositionText(false);

                yield return ScreenPositionTextUpdateTime;
            }

            yield return null;

            UpdateScreenPositionText(true);
        }

        private void UpdateScreenPositionText(bool useStored)
        {
            if (useStored)
            {
                _screenPosXText.text = PluginConfig.Instance.Search.KeyboardPosition.x.ToString(ScreenPositionStringFormat);
                _screenPosYText.text = PluginConfig.Instance.Search.KeyboardPosition.y.ToString(ScreenPositionStringFormat);
                _screenPosZText.text = PluginConfig.Instance.Search.KeyboardPosition.z.ToString(ScreenPositionStringFormat);
                _screenRotXText.text = PluginConfig.Instance.Search.KeyboardRotation.eulerAngles.x.ToString(ScreenRotationStringFormat);
                _screenRotYText.text = PluginConfig.Instance.Search.KeyboardRotation.eulerAngles.y.ToString(ScreenRotationStringFormat);
                _screenRotZText.text = PluginConfig.Instance.Search.KeyboardRotation.eulerAngles.z.ToString(ScreenRotationStringFormat);
            }
            else
            {
                (Vector3 screenPos, Quaternion screenRot) = ScreenPositionGetter?.Invoke() ?? (default, default);

                _screenPosXText.text = screenPos.x.ToString(ScreenPositionStringFormat);
                _screenPosYText.text = screenPos.y.ToString(ScreenPositionStringFormat);
                _screenPosZText.text = screenPos.z.ToString(ScreenPositionStringFormat);
                _screenRotXText.text = screenRot.eulerAngles.x.ToString(ScreenRotationStringFormat);
                _screenRotYText.text = screenRot.eulerAngles.y.ToString(ScreenRotationStringFormat);
                _screenRotZText.text = screenRot.eulerAngles.z.ToString(ScreenRotationStringFormat);
            }
        }

        private void OnPluginConfigReloaded() => _parserParams.EmitEvent("refresh-all-values");

        [UIAction("tab-selected")]
        private void OnTabSelected(SegmentedControl segmentedControl, int index)
        {
            AllowScreenMovement = false;

            this.CallAndHandleAction(AllowScreenMovementClicked, nameof(AllowScreenMovementClicked), false);
        }

        [UIAction("allow-screen-movement-clicked")]
        private void OnAllowScreenMovementClicked()
        {
            AllowScreenMovement = !AllowScreenMovement;

            SharedCoroutineStarter.instance.StartCoroutine(UpdateScreenPositionTextCoroutine());

            this.CallAndHandleAction(AllowScreenMovementClicked, nameof(AllowScreenMovementClicked), AllowScreenMovement);
        }

        [UIAction("reset-screen-position-clicked")]
        private void OnResetScreenPositionClicked()
        {
            if (AllowScreenMovement)
            {
                this.CallAndHandleAction(ResetScreenPositionClicked, nameof(ResetScreenPositionClicked));
            }
            else
            {
                this.CallAndHandleAction(DefaultScreenPositionClicked, nameof(DefaultScreenPositionClicked));

                UpdateScreenPositionText(true);
            }
        }
    }
}
