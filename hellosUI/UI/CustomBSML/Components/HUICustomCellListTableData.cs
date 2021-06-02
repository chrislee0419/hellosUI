using System.Collections.Generic;
using UnityEngine;
using HMUI;
using BeatSaberMarkupLanguage.Components;
using HUI.Utilities;

namespace HUI.UI.CustomBSML.Components
{
    public class HUICustomCellListTableData : CustomCellListTableData
    {
        private Color _hoveredColour = DefaultHoveredColour;
        public Color HoveredColour
        {
            get => _hoveredColour;
            set
            {
                _hoveredColour = value;

                foreach (var img in _hoveredBGs)
                    img.color = value;
            }
        }

        private Color _selectedColour = DefaultSelectedColour;
        public Color SelectedColour
        {
            get => _selectedColour;
            set
            {
                _selectedColour = value;

                foreach (var img in _selectedBGs)
                    img.color = value;
            }
        }

        private HashSet<ImageView> _hoveredBGs = new HashSet<ImageView>();
        private HashSet<ImageView> _selectedBGs = new HashSet<ImageView>();

        public static readonly Color DefaultHoveredColour = new Color(1f, 1f, 1f, 0.5f);
        public static readonly Color DefaultSelectedColour = new Color(0f, 170f / 255f, 1f, 0.75f);

        private const string BackgroundName = "panel-fade-gradient";

        public override TableCell CellForIdx(TableView tableView, int idx)
        {
            var tableCell = base.CellForIdx(tableView, idx) as CustomCellTableCell;

            var img = CreateBG("HoveredBG", tableCell.transform);
            img.color = _hoveredColour;

            tableCell.hoveredTags.Add(img.gameObject);
            _hoveredBGs.Add(img);

            img = CreateBG("SelectedBG", tableCell.transform);
            img.color = _selectedColour;

            tableCell.selectedTags.Add(img.gameObject);
            _selectedBGs.Add(img);

            return tableCell;
        }

        private ImageView CreateBG(string name, Transform parent)
        {
            var bg = new GameObject(name).AddComponent<Backgroundable>();
            bg.ApplyBackground(BackgroundName);

            bg.transform.SetParent(parent, false);
            bg.transform.SetAsFirstSibling();

            var rt = bg.background.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            var img = bg.background as ImageView;
            img.SetSkew(0f);

            return img;
        }
    }
}
