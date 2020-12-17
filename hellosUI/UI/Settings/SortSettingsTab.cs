using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using HMUI;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HUI.Interfaces;
using HUI.Sort.BuiltIn;
using HUI.UI.Components;
using HUI.Utilities;

namespace HUI.UI.Settings
{
    public class SortSettingsTab : SettingsModalManager.SettingsModalTabBase, IDisposable
    {
        public event Action SortModeListSettingChanged;

        public override string TabName => "Sort";
        protected override string AssociatedBSMLResource => "HUI.UI.Views.Settings.SortSettingsView.bsml";

        [UIValue("top-buttons-interactable")]
        public bool AnyChanges => _sortModeListCells.Any(x => x.HasChanges);

        [UIValue("up-buttons-interactable")]
        public bool UpButtonsInteractable => (_selectedSortMode?.Index ?? 0) > 0;

        [UIValue("down-buttons-interactable")]
        public bool DownButtonsInteractable
        {
            get
            {
                int count = _sortModeListCells.Count;
                return (_selectedSortMode?.Index ?? count) < count - 1;
            }
        }

        [UIValue("hide-button-interactable")]
        public bool HideButtonInteractable => _selectedSortMode != null && _selectedSortMode.SortMode.GetType() != typeof(DefaultSortMode);

        [UIValue("hide-unavailable-value")]
        public bool HideUnavailableValue
        {
            get => PluginConfig.Instance.Sort.HideUnavailable;
            set
            {
                if (PluginConfig.Instance.Sort.HideUnavailable == value)
                    return;

                PluginConfig.Instance.Sort.HideUnavailable = value;
                this.CallAndHandleAction(SortModeListSettingChanged, nameof(SortModeListSettingChanged));
            }
        }

        [UIValue("hide-button-text")]
        public string HideButtonText
        {
            get
            {
                if (_selectedSortMode == null)
                    return "Hide";
                else
                    return _selectedSortMode.Hidden ? "Unhide" : "Hide";
            }
        }

#pragma warning disable CS0649
        [UIComponent("sort-mode-list")]
        private CustomCellListTableData _sortModeListTableData;

        [UIObject("top-button")]
        private GameObject _topButton;
        [UIObject("up-button")]
        private GameObject _upButton;
        [UIObject("down-button")]
        private GameObject _downButton;
        [UIObject("bottom-button")]
        private GameObject _bottomButton;

        [UIObject("list-up-button")]
        private GameObject _listUpButton;
        [UIObject("list-down-button")]
        private GameObject _listDownButton;
#pragma warning restore CS0649

        private List<ISortMode> _sortModes = new List<ISortMode>();
        private List<SortModeListCell> _sortModeListCells = new List<SortModeListCell>();
        private SortModeListCell _selectedSortMode;

        public override void SetupView()
        {
            base.SetupView();

            GameObject.Destroy(_listUpButton.transform.Find("Underline").gameObject);
            GameObject.Destroy(_listDownButton.transform.Find("Underline").gameObject);

            // remove skew
            _listUpButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);
            _listDownButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);

            _topButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);
            _topButton.transform.Find("Underline").GetComponent<ImageView>().SetSkew(0f);
            _upButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);
            _upButton.transform.Find("Underline").GetComponent<ImageView>().SetSkew(0f);
            _downButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);
            _downButton.transform.Find("Underline").GetComponent<ImageView>().SetSkew(0f);
            _bottomButton.transform.Find("BG").GetComponent<ImageView>().SetSkew(0f);
            _bottomButton.transform.Find("Underline").GetComponent<ImageView>().SetSkew(0f);

            // reduce padding
            var offset = new RectOffset(0, 0, 4, 4);
            _listUpButton.transform.Find("Content").GetComponent<StackLayoutGroup>().padding = offset;
            _listDownButton.transform.Find("Content").GetComponent<StackLayoutGroup>().padding = offset;

            offset = new RectOffset();
            _topButton.GetComponent<StackLayoutGroup>().padding = offset;
            _upButton.GetComponent<StackLayoutGroup>().padding = offset;
            _downButton.GetComponent<StackLayoutGroup>().padding = offset;
            _bottomButton.GetComponent<StackLayoutGroup>().padding = offset;

            offset = new RectOffset(1, 1, 1, 1);
            _topButton.transform.Find("Content").GetComponent<StackLayoutGroup>().padding = offset;
            _bottomButton.transform.Find("Content").GetComponent<StackLayoutGroup>().padding = offset;

            offset = new RectOffset(2, 2, 2, 2);
            _upButton.transform.Find("Content").GetComponent<StackLayoutGroup>().padding = offset;
            _downButton.transform.Find("Content").GetComponent<StackLayoutGroup>().padding = offset;

            // rotate images
            _listUpButton.GetComponent<ButtonIconImage>().image.rectTransform.Rotate(0f, 0f, 180f, Space.Self);
            _downButton.GetComponent<ButtonIconImage>().image.rectTransform.Rotate(0f, 0f, 180f, Space.Self);
            _bottomButton.GetComponent<ButtonIconImage>().image.rectTransform.Rotate(0f, 0f, 180f, Space.Self);

            // custom button animations
            GameObject.Destroy(_listUpButton.GetComponent<ButtonStaticAnimations>());
            GameObject.Destroy(_listDownButton.GetComponent<ButtonStaticAnimations>());
            GameObject.Destroy(_topButton.GetComponent<ButtonStaticAnimations>());
            GameObject.Destroy(_upButton.GetComponent<ButtonStaticAnimations>());
            GameObject.Destroy(_downButton.GetComponent<ButtonStaticAnimations>());
            GameObject.Destroy(_bottomButton.GetComponent<ButtonStaticAnimations>());

            Color ListMoveColour = new Color(0.145f, 0.443f, 1f);

            var btnAnims = _listUpButton.AddComponent<CustomIconButtonAnimations>();
            btnAnims.HighlightedBGColour = ListMoveColour;
            btnAnims.PressedBGColour = ListMoveColour;

            btnAnims = _listDownButton.AddComponent<CustomIconButtonAnimations>();
            btnAnims.HighlightedBGColour = ListMoveColour;
            btnAnims.PressedBGColour = ListMoveColour;

            Vector3 HighlighedScale = new Vector3(1.2f, 1.2f, 1.2f);
            btnAnims = _topButton.AddComponent<CustomIconButtonAnimations>();
            btnAnims.HighlightedLocalScale = HighlighedScale;

            btnAnims = _upButton.AddComponent<CustomIconButtonAnimations>();
            btnAnims.HighlightedLocalScale = HighlighedScale;

            btnAnims = _downButton.AddComponent<CustomIconButtonAnimations>();
            btnAnims.HighlightedLocalScale = HighlighedScale;

            btnAnims = _bottomButton.AddComponent<CustomIconButtonAnimations>();
            btnAnims.HighlightedLocalScale = HighlighedScale;

            SetListData();
        }

        public void RefreshSortModeList(IEnumerable<ISortMode> sortModes)
        {
            // note: sortModes should already be in the correct order to display
            _sortModes = sortModes.ToList();

            if (_parserParams != null)
                SetListData();
        }

        private void SetListData()
        {
            _sortModeListCells.Clear();
            _sortModeListCells.AddRange(_sortModes.Select(sortMode => new SortModeListCell(sortMode)));
            UpdateListIndices();
            UpdateListHiddenStatus();

            _sortModeListTableData.data = _sortModeListCells.Select(x => (object)x).ToList();
            _sortModeListTableData.tableView.ReloadData();

            _selectedSortMode = null;

            NotifyPropertyChanged(nameof(UpButtonsInteractable));
            NotifyPropertyChanged(nameof(DownButtonsInteractable));
            NotifyPropertyChanged(nameof(HideButtonInteractable));
            NotifyPropertyChanged(nameof(HideButtonText));
        }

        private void RefreshCellPositions()
        {
            UpdateListIndices();
            _sortModeListCells.Sort();

            _sortModeListTableData.data = _sortModeListCells.Select(x => (object)x).ToList();
            _sortModeListTableData.tableView.ReloadData();

            if (_selectedSortMode != null)
            {
                _sortModeListTableData.tableView.ScrollToCellWithIdx(_selectedSortMode.Index, TableViewScroller.ScrollPositionType.Center, false);
                _sortModeListTableData.tableView.SelectCellWithIdx(_selectedSortMode.Index);

                NotifyPropertyChanged(nameof(UpButtonsInteractable));
                NotifyPropertyChanged(nameof(DownButtonsInteractable));
                NotifyPropertyChanged(nameof(HideButtonInteractable));
            }
        }

        private void UpdateListIndices()
        {
            for (int i = 0; i < _sortModeListCells.Count; ++i)
                _sortModeListCells[i].Index = i;
        }

        private void UpdateListHiddenStatus()
        {
            foreach (var sortModeCell in _sortModeListCells)
                sortModeCell.Hidden = PluginConfig.Instance.Sort.HiddenSortModes.Contains(sortModeCell.SortMode.GetIdentifier());
        }

        [UIAction("sort-mode-selected")]
        private void OnSortModeSelected(TableView tableView, object data)
        {
            _selectedSortMode = (SortModeListCell)data;

            NotifyPropertyChanged(nameof(UpButtonsInteractable));
            NotifyPropertyChanged(nameof(DownButtonsInteractable));
            NotifyPropertyChanged(nameof(HideButtonInteractable));
            NotifyPropertyChanged(nameof(HideButtonText));
        }

        [UIAction("apply-button-clicked")]
        private void OnApplyButtonClicked()
        {
            if (!AnyChanges)
            {
                NotifyPropertyChanged(nameof(AnyChanges));
                return;
            }

            ISet<string> hiddenSortModes = PluginConfig.Instance.Sort.HiddenSortModes;
            List<string> sortModeOrdering = PluginConfig.Instance.Sort.SortModeOrdering;

            hiddenSortModes.Clear();
            sortModeOrdering.Clear();
            foreach (var sortModeCell in _sortModeListCells)
            {
                string id = sortModeCell.SortMode.GetIdentifier();
                if (sortModeCell.Hidden)
                    hiddenSortModes.Add(id);
                sortModeOrdering.Add(id);
            }

            this.CallAndHandleAction(SortModeListSettingChanged, nameof(SortModeListSettingChanged));
        }

        [UIAction("undo-button-clicked")]
        private void OnUndoButtonClicked() => SetListData();

        private bool IsCellSelected()
        {
            if (_selectedSortMode == null)
            {
                NotifyPropertyChanged(nameof(UpButtonsInteractable));
                NotifyPropertyChanged(nameof(DownButtonsInteractable));
                NotifyPropertyChanged(nameof(HideButtonInteractable));
                NotifyPropertyChanged(nameof(AnyChanges));
                return false;
            }

            return true;
        }

        [UIAction("top-button-clicked")]
        private void OnTopButtonClicked()
        {
            if (IsCellSelected())
            {
                _sortModeListCells.Remove(_selectedSortMode);
                _sortModeListCells.Insert(0, _selectedSortMode);

                RefreshCellPositions();
                NotifyPropertyChanged(nameof(AnyChanges));
            }
        }

        [UIAction("up-button-clicked")]
        private void OnUpButtonClicked()
        {
            if (IsCellSelected())
            {
                int index = _selectedSortMode.Index - 1;
                if (index < 0)
                    index = 0;

                _sortModeListCells.Remove(_selectedSortMode);
                _sortModeListCells.Insert(index, _selectedSortMode);

                RefreshCellPositions();
                NotifyPropertyChanged(nameof(AnyChanges));
            }
        }

        [UIAction("down-button-clicked")]
        private void OnDownButtonClicked()
        {
            if (IsCellSelected())
            {
                int index = _selectedSortMode.Index + 1;
                if (index >= _sortModeListCells.Count)
                    index = _sortModeListCells.Count - 1;

                _sortModeListCells.Remove(_selectedSortMode);
                _sortModeListCells.Insert(index, _selectedSortMode);

                RefreshCellPositions();
                NotifyPropertyChanged(nameof(AnyChanges));
            }
        }

        [UIAction("bottom-button-clicked")]
        private void OnBottomButtonClicked()
        {
            if (IsCellSelected())
            {
                _sortModeListCells.Remove(_selectedSortMode);
                _sortModeListCells.Add(_selectedSortMode);

                RefreshCellPositions();
                NotifyPropertyChanged(nameof(AnyChanges));
            }
        }

        [UIAction("hide-button-clicked")]
        private void OnHideButtonClicked()
        {
            if (IsCellSelected())
            {
                _selectedSortMode.Hidden = !_selectedSortMode.Hidden;
                NotifyPropertyChanged(nameof(HideButtonText));
                NotifyPropertyChanged(nameof(AnyChanges));
            }
        }

        private class SortModeListCell : INotifyPropertyChanged, IComparable<SortModeListCell>
        {
            public event PropertyChangedEventHandler PropertyChanged;

            [UIValue("name-text")]
            public string NameText => SortMode.Name;

            [UIValue("status-text")]
            public string StatusText
            {
                get
                {
                    var sortSettings = PluginConfig.Instance.Sort;
                    string id = SortMode.GetIdentifier();
                    StringBuilder sb = new StringBuilder();

                    if (sortSettings.HiddenSortModes.Contains(id))
                    {
                        if (_hidden)
                            sb.Append("<size=70%><color=#808080>[Hidden]</color></size>  ");
                        else
                            sb.Append("<size=70%><color=#BB8060>[Unhidden]</color></size>  ");
                    }
                    else if (_hidden)
                    {
                        sb.Append("<size=70%><color=#BB8060>[Hidden]</color></size>  ");
                    }

                    if (sortSettings.SortModeOrdering.IndexOf(id) == _index)
                        sb.Append(_index + 1);
                    else
                        sb.Append("<color=#FFBBAA>").Append(_index + 1).Append("</color>");

                    return sb.ToString();
                }
            }

            private bool _hidden;
            public bool Hidden
            {
                get => _hidden;
                set
                {
                    if (_hidden == value)
                        return;

                    _hidden = value;
                    NotifyPropertyChanged(nameof(StatusText));
                }
            }

            private int _index;
            public int Index
            {
                get => _index;
                set
                {
                    if (_index == value)
                        return;

                    _index = value;
                    NotifyPropertyChanged(nameof(StatusText));
                }
            }

            public bool HasChanges
            {
                get
                {
                    var sortSettings = PluginConfig.Instance.Sort;
                    string id = SortMode.GetIdentifier();
                    return sortSettings.HiddenSortModes.Contains(id) != _hidden || sortSettings.SortModeOrdering.IndexOf(id) != _index;
                }
            }

            public ISortMode SortMode { get; private set; }

#pragma warning disable CS0649
            [UIObject("hovered-bg")]
            private GameObject _hoveredBG;
            [UIObject("selected-bg")]
            private GameObject _selectedBG;
#pragma warning restore CS0649

            private bool _initialized = false;

            public SortModeListCell(ISortMode sortMode)
            {
                if (sortMode == null)
                    throw new ArgumentNullException(nameof(sortMode));

                SortMode = sortMode;
            }

            [UIAction("refresh-visuals")]
            private void RefreshVisuals(bool selected, bool highlighted)
            {
                if (!_initialized)
                {
                    _hoveredBG.GetComponent<ImageView>().SetSkew(0f);
                    _selectedBG.GetComponent<ImageView>().SetSkew(0f);

                    _initialized = true;
                }
            }

            public int CompareTo(SortModeListCell other) => this.Index - other.Index;

            private void NotifyPropertyChanged([CallerMemberName] string propertyName = null) => this.CallAndHandleAction(PropertyChanged, propertyName);
        }
    }
}
