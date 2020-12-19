using UnityEngine;
using UnityEngine.UI;
using HMUI;

namespace HUI.UI.Components
{
    public class CustomButtonAnimations : MonoBehaviour
    {
        protected Color _normalBGColour = new Color(0f, 0f, 0f, 0.5f);
        public Color NormalBGColour
        {
            get => _normalBGColour;
            set
            {
                if (_normalBGColour == value)
                    return;

                _normalBGColour = value;
                if (_button != null)
                    OnSelectionStateChanged(_button.selectionState);
            }
        }

        protected Color _disabledBGColour = new Color(0f, 0f, 0f, 0.25f);
        public Color DisabledBGColour
        {
            get => _disabledBGColour;
            set
            {
                if (_disabledBGColour == value)
                    return;

                _disabledBGColour = value;
                if (_button != null)
                    OnSelectionStateChanged(_button.selectionState);
            }
        }

        protected Color _highlightedBGColour = new Color(253f / 255f, 37f / 255f, 224f / 255f);
        public Color HighlightedBGColour
        {
            get => _highlightedBGColour;
            set
            {
                if (_highlightedBGColour == value)
                    return;

                _highlightedBGColour = value;
                if (_button != null)
                    OnSelectionStateChanged(_button.selectionState);
            }
        }

        protected Color _pressedBGColour = new Color(253f / 255f, 37f / 255f, 224f / 255f);
        public Color PressedBGColour
        {
            get => _pressedBGColour;
            set
            {
                if (_pressedBGColour == value)
                    return;

                _pressedBGColour = value;
                if (_button != null)
                    OnSelectionStateChanged(_button.selectionState);
            }
        }

        private bool _normalUseTranslucent = false;
        public bool NormalSelectionUseTranslucentColour
        {
            get => _normalUseTranslucent;
            set
            {
                if (_normalUseTranslucent == value)
                    return;

                _normalUseTranslucent = value;
                if (_button != null)
                    OnSelectionStateChanged(_button.selectionState);
            }
        }

        protected NoTransitionsButton _button;
        protected ImageView _bg;
        protected GameObject _underlineGO;

        protected static readonly Color TranslucentColour = new Color(1f, 1f, 1f, 0.5f);

        protected virtual void Awake()
        {
            _button = this.GetComponent<NoTransitionsButton>();
            _bg = this.transform.Find("BG").GetComponent<ImageView>();
            _underlineGO = this.transform.Find("Underline")?.gameObject;

            _bg.enabled = true;

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

        protected virtual void OnSelectionStateChanged(NoTransitionsButton.SelectionState selectionState)
        {
            switch (selectionState)
            {
                case NoTransitionsButton.SelectionState.Disabled:
                    _bg.color = _disabledBGColour;
                    _bg.color1 = Color.white;
                    if (_underlineGO != null)
                        _underlineGO.SetActive(false);
                    break;

                case NoTransitionsButton.SelectionState.Normal:
                    _bg.color = _normalBGColour;
                    _bg.color1 = _normalUseTranslucent ? TranslucentColour : Color.white;
                    if (_underlineGO != null)
                        _underlineGO.SetActive(true);
                    break;

                case NoTransitionsButton.SelectionState.Highlighted:
                    _bg.color = _highlightedBGColour;
                    _bg.color1 = TranslucentColour;
                    if (_underlineGO != null)
                        _underlineGO.SetActive(true);
                    break;

                case NoTransitionsButton.SelectionState.Pressed:
                    _bg.color = _pressedBGColour;
                    _bg.color1 = TranslucentColour;
                    if (_underlineGO != null)
                        _underlineGO.SetActive(true);
                    break;
            }
        }
    }

    public class CustomIconButtonAnimations : CustomButtonAnimations
    {
        private Color _normalIconColour = Color.grey;
        public Color NormalIconColour
        {
            get => _normalIconColour;
            set
            {
                if (_normalIconColour == value)
                    return;

                _normalIconColour = value;
                if (_button != null)
                    OnSelectionStateChanged(_button.selectionState);
            }
        }

        private Color _disabledIconColour = new Color(1f, 1f, 1f, 0.25f);
        public Color DisabledIconColour
        {
            get => _disabledIconColour;
            set
            {
                if (_disabledIconColour == value)
                    return;

                _disabledIconColour = value;
                if (_button != null)
                    OnSelectionStateChanged(_button.selectionState);
            }
        }

        protected Color _highlightedIconColour = Color.white;
        public Color HighlightedIconColour
        {
            get => _highlightedIconColour;
            set
            {
                if (_highlightedIconColour == value)
                    return;

                _highlightedIconColour = value;
                if (_button != null)
                    OnSelectionStateChanged(_button.selectionState);
            }
        }

        protected Color _pressedIconColour = Color.grey;
        public Color PressedIconColour
        {
            get => _pressedIconColour;
            set
            {
                if (_pressedIconColour == value)
                    return;

                _pressedIconColour = value;
                if (_button != null)
                    OnSelectionStateChanged(_button.selectionState);
            }
        }

        protected Vector3 _highlightScale = new Vector3(1.5f, 1.5f, 1.5f);
        public Vector3 HighlightedLocalScale
        {
            get => _highlightScale;
            set
            {
                if (_highlightScale == value)
                    return;

                _highlightScale = value;
                if (_button != null)
                    OnSelectionStateChanged(_button.selectionState);
            }
        }

        private Image _icon;

        protected override void Awake()
        {
            _icon = this.transform.Find("Content/Icon").GetComponent<Image>();

            base.Awake();
        }

        protected override void OnSelectionStateChanged(NoTransitionsButton.SelectionState selectionState)
        {
            base.OnSelectionStateChanged(selectionState);

            switch (selectionState)
            {
                case NoTransitionsButton.SelectionState.Disabled:
                    _icon.transform.localScale = Vector3.one;
                    _icon.color = _disabledIconColour;
                    break;

                case NoTransitionsButton.SelectionState.Normal:
                    _icon.transform.localScale = Vector3.one;
                    _icon.color = _normalIconColour;
                    break;

                case NoTransitionsButton.SelectionState.Highlighted:
                    _icon.transform.localScale = _highlightScale;
                    _icon.color = _highlightedIconColour;
                    break;

                case NoTransitionsButton.SelectionState.Pressed:
                    _icon.transform.localScale = _highlightScale;
                    _icon.color = _pressedIconColour;
                    break;
            }
        }
    }
}
