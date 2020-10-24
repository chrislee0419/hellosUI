using UnityEngine;
using HMUI;

namespace HUI.UI.Components
{
    internal class CustomIconButtonAnimations : MonoBehaviour
    {
        private Color _normalBGColour = new Color(0f, 0f, 0f, 0.5f);
        public Color NormalBackgroundColour
        {
            get => _normalBGColour;
            set
            {
                if (_normalBGColour == value)
                    return;

                _normalBGColour = value;
                OnSelectionStateChanged(_button.selectionState);
            }
        }

        private Color _disabledBGColour = new Color(0f, 0f, 0f, 0.25f);
        public Color DisabledBackgroundColour
        {
            get => _disabledBGColour;
            set
            {
                if (_disabledBGColour == value)
                    return;

                _disabledBGColour = value;
                OnSelectionStateChanged(_button.selectionState);
            }
        }

        private Color _highlightedBGColour = new Color(253f/255f, 37f/255f, 224f/255f);
        public Color HighlightedBGColour
        {
            get => _highlightedBGColour;
            set
            {
                if (_highlightedBGColour == value)
                    return;

                _highlightedBGColour = value;
                OnSelectionStateChanged(_button.selectionState);
            }
        }

        private Color _pressedBGColour = new Color(253f / 255f, 37f / 255f, 224f / 255f);
        public Color PressedBGColour
        {
            get => _pressedBGColour;
            set
            {
                if (_pressedBGColour == value)
                    return;

                _pressedBGColour = value;
                OnSelectionStateChanged(_button.selectionState);
            }
        }

        private NoTransitionsButton _button;
        private ImageView _bg;
        private ImageView _icon;

        private static readonly Color TranslucentColour = new Color(1f, 1f, 1f, 0.5f);

        private void Awake()
        {
            _button = this.GetComponent<NoTransitionsButton>();
            _bg = this.transform.Find("BG").GetComponent<ImageView>();
            _icon = this.transform.Find("Content/Icon").GetComponent<ImageView>();

            _button.selectionStateDidChangeEvent += OnSelectionStateChanged;

            OnSelectionStateChanged(_button.selectionState);
        }

        private void Start()
        {
            OnSelectionStateChanged(_button.selectionState);
        }

        private void OnEnable()
        {
            OnSelectionStateChanged(_button.selectionState);
        }

        private void OnDestroy()
        {
            _button.selectionStateDidChangeEvent -= OnSelectionStateChanged;
        }

        private void OnSelectionStateChanged(NoTransitionsButton.SelectionState selectionState)
        {
            switch (selectionState)
            {
                case NoTransitionsButton.SelectionState.Disabled:
                    _icon.transform.localScale = new Vector3(1f, 1f, 1f);
                    _icon.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

                    _bg.color = _disabledBGColour;
                    _bg.color1 = Color.white;
                    break;

                case NoTransitionsButton.SelectionState.Normal:
                    _icon.transform.localScale = new Vector3(1f, 1f, 1f);
                    _icon.color = Color.grey;

                    _bg.color = _normalBGColour;
                    _bg.color1 = Color.white;
                    break;

                case NoTransitionsButton.SelectionState.Highlighted:
                    _icon.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                    _icon.color = Color.white;

                    _bg.color = _highlightedBGColour;
                    _bg.color1 = TranslucentColour;
                    break;

                case NoTransitionsButton.SelectionState.Pressed:
                    _icon.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                    _icon.color = Color.grey;

                    _bg.color = _pressedBGColour;
                    _bg.color1 = TranslucentColour;
                    break;
            }
        }
    }
}
