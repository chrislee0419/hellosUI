using System;
using UnityEngine;
using TMPro;
using HMUI;
using BeatSaberMarkupLanguage.Attributes;
using HUI.Search;
using HUI.Utilities;

namespace HUI.UI.Settings
{
    public class SearchSettingsTab : SettingsModalManager.SettingsModalTabBase
    {
        public event Action OffHandLaserPointerSettingChanged;
        public event Action SearchOptionChanged;
        public event Action<bool> AllowScreenMovementClicked;

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

        public override void OnModalClosed()
        {
            AllowScreenMovement = false;
        }

        [UIAction("tab-selected")]
        private void OnTabSelected(SegmentedControl segmentedControl, int index)
        {
            AllowScreenMovement = false;

            this.CallAndHandleAction(AllowScreenMovementClicked, nameof(AllowScreenMovementClicked), false);
        }
    }
}
