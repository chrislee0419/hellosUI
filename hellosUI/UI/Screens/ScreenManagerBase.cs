using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HMUI;
using VRUIControls;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.FloatingScreen;
using HUI.UI.Settings;
using HUI.Interfaces;
using HUI.Utilities;
using BSMLUtilities = BeatSaberMarkupLanguage.Utilities;
using Object = UnityEngine.Object;

namespace HUI.UI.Screens
{
    public abstract class ScreenManagerBase : SinglePlayerManagerBase, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual string AssociatedBSMLResource => null;
        protected virtual bool ShowScreenOnSinglePlayerLevelSelectionStarting => true;
        protected virtual bool HideScreenOnSinglePlayerLevelSelectionFinished => true;

        protected FloatingScreen _screen;
        protected ScreenAnimationHandler _animationHandler;

        private LevelCollectionNavigationController _levelCollectionNavigationController;

        // for whatever reason, a screen radius of 0 messes up the depth sorting of the raycasts or something
        // cause it causes masked off interactables/selectables to still be able to be hovered over
        // maybe there's an issue in VRGraphicRaycaster.RaycastCanvas()?
        // anyways, because of that, the screen needs to have some radius to actually mask raycasts properly
        // so, we just use some high radius to make it look flat enough but not too high,
        // otherwise it messes up some raycast calculations
        private const float ScreenRadius = 5000f;

        public ScreenManagerBase(
            MainMenuViewController mainMenuVC,
            SoloFreePlayFlowCoordinator soloFC,
            PartyFreePlayFlowCoordinator partyFC,
            LevelCollectionNavigationController levelCollectionNC,
            PhysicsRaycasterWithCache physicsRaycaster,
            Vector2 screenSize,
            Vector3 screenPosition,
            Quaternion screenRotation)
            : base(mainMenuVC, soloFC, partyFC)
        {
            Plugin.Log.Debug($"Creating {GetType().Name}");

            _levelCollectionNavigationController = levelCollectionNC;

            _screen = FloatingScreen.CreateFloatingScreen(screenSize, false, screenPosition, screenRotation, ScreenRadius);
            _screen.gameObject.FixRaycaster(physicsRaycaster);

            _animationHandler = _screen.gameObject.AddComponent<ScreenAnimationHandler>();
            _animationHandler.DefaultSize = screenSize;

            if (!string.IsNullOrEmpty(AssociatedBSMLResource))
            {
                BSMLParser.instance.Parse(BSMLUtilities.GetResourceContent(this.GetType().Assembly, AssociatedBSMLResource), _screen.gameObject, this);

                // add touchable if necessary to the root element
                if (_screen.transform.childCount != 0)
                {
                    var rootElement = _screen.transform.GetChild(0);
                    if (rootElement != null)
                    {
                        var graphic = rootElement.GetComponent<Graphic>();
                        if (graphic != null)
                            graphic.raycastTarget = true;
                        else
                            rootElement.gameObject.AddComponent<Touchable>();
                    }
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            _screen.gameObject.SetActive(false);
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_levelCollectionNavigationController != null)
            {
                _levelCollectionNavigationController.didActivateEvent -= OnLevelCollectionNavigationControllerActivated;
                _levelCollectionNavigationController.didDeactivateEvent -= OnLevelCollectionNavigationControllerDeactivated;
            }

            if (_screen != null)
                Object.Destroy(_screen.gameObject);
        }

        protected override void OnSinglePlayerLevelSelectionStarting(bool isSolo)
        {
            if (ShowScreenOnSinglePlayerLevelSelectionStarting)
                _animationHandler.PlayRevealAnimation();

            _levelCollectionNavigationController.didActivateEvent += OnLevelCollectionNavigationControllerActivated;
            _levelCollectionNavigationController.didDeactivateEvent += OnLevelCollectionNavigationControllerDeactivated;
        }

        protected override void OnSinglePlayerLevelSelectionFinished()
        {
            if (HideScreenOnSinglePlayerLevelSelectionFinished)
                _animationHandler.PlayConcealAnimation();

            _levelCollectionNavigationController.didActivateEvent -= OnLevelCollectionNavigationControllerActivated;
            _levelCollectionNavigationController.didDeactivateEvent -= OnLevelCollectionNavigationControllerDeactivated;
        }

        protected virtual void OnLevelCollectionNavigationControllerActivated(bool firstActivation, bool addToHierarchy, bool screenSystemEnabling) => _animationHandler.PlayRevealAnimation();

        protected virtual void OnLevelCollectionNavigationControllerDeactivated(bool removedFromHierarchy, bool screenSystemDisabling) => _animationHandler.PlayConcealAnimation();

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null) => this.CallAndHandleAction(PropertyChanged, propertyName);

        protected class ScreenAnimationHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public event Action PointerEntered;
            public event Action PointerExited;
            public event Action<AnimationType> AnimationFinished;

            public Vector2 DefaultSize { get; set; } = Vector2.zero;
            public virtual Vector2 ExpandedSize { get; set; } = Vector2.zero;
            public float LocalScale { get; set; } = DefaultLocalScale;
            public virtual bool UsePointerAnimations { get; set; } = true;

            public bool IsPointedAt { get; private set; } = false;

            protected Coroutine _revealAnimation = null;
            protected Coroutine _expandAnimation = null;
            protected Coroutine _contractAnimation = null;

            public const float DefaultLocalScale = 0.03f;
            public const float CollapsedLocalScale = 0.0001f;
            protected const float StopAnimationEpsilon = 0.0001f;
            protected static readonly WaitForSeconds CollapseAnimationDelay = new WaitForSeconds(0.8f);

            private void Start()
            {
                this.transform.localScale = new Vector3(LocalScale, LocalScale, LocalScale);
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                IsPointedAt = true;

                if (UsePointerAnimations)
                    PlayExpandAnimation();

                this.CallAndHandleAction(PointerEntered, nameof(PointerEntered));
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                IsPointedAt = false;

                if (UsePointerAnimations)
                    PlayContractAnimation();

                this.CallAndHandleAction(PointerExited, nameof(PointerExited));
            }

            public void PlayRevealAnimation()
            {
                this.gameObject.SetActive(true);

                StopAllAnimations();
                _revealAnimation = StartCoroutine(RevealAnimationCoroutine(LocalScale));
            }

            public void PlayConcealAnimation()
            {
                if (!this.gameObject.activeSelf)
                    return;

                StopAllAnimations();
                _revealAnimation = StartCoroutine(RevealAnimationCoroutine(CollapsedLocalScale, true));
            }

            public void PlayExpandAnimation()
            {
                if (_revealAnimation != null || _expandAnimation != null)
                    return;

                if (_contractAnimation != null)
                {
                    StopCoroutine(_contractAnimation);
                    _contractAnimation = null;
                }

                _expandAnimation = StartCoroutine(ExpandAnimationCoroutine());
            }

            public void PlayContractAnimation(bool immediate = false)
            {
                if (_revealAnimation != null || _contractAnimation != null)
                    return;

                if (_expandAnimation != null)
                {
                    StopCoroutine(_expandAnimation);
                    _expandAnimation = null;
                    immediate = true;
                }

                _contractAnimation = StartCoroutine(ContractAnimationCoroutine(immediate));
            }

            protected virtual void StopAllAnimations()
            {
                if (_revealAnimation != null)
                {
                    StopCoroutine(_revealAnimation);
                    _revealAnimation = null;
                }
                else if (_expandAnimation != null)
                {
                    StopCoroutine(_expandAnimation);
                    _expandAnimation = null;
                }
                else if (_contractAnimation != null)
                {
                    StopCoroutine(_contractAnimation);
                    _contractAnimation = null;
                }
            }

            private void OnAnimationFinished(AnimationType animationType) => this.CallAndHandleAction(AnimationFinished, nameof(AnimationFinished), animationType);

            protected IEnumerator RevealAnimationCoroutine(float destAnimationValue, bool deactivateOnFinish = false)
            {
                yield return null;
                yield return null;

                Vector3 localScale = this.transform.localScale;
                float multiplier = (localScale.y > destAnimationValue) ? 30f : 16f;
                while (Mathf.Abs(localScale.y - destAnimationValue) > StopAnimationEpsilon)
                {
                    localScale.y = Mathf.Lerp(localScale.y, destAnimationValue, Time.deltaTime * multiplier);
                    this.transform.localScale = localScale;

                    yield return null;
                }

                localScale.y = destAnimationValue;
                this.transform.localScale = localScale;

                // reset size delta as well
                (this.transform as RectTransform).sizeDelta = DefaultSize;

                if (deactivateOnFinish)
                    this.gameObject.SetActive(false);

                _revealAnimation = null;
                OnAnimationFinished(deactivateOnFinish ? AnimationType.Reveal : AnimationType.Conceal);
            }

            protected IEnumerator ExpandAnimationCoroutine()
            {
                RectTransform rt = this.transform as RectTransform;
                Vector3 sizeDelta = rt.sizeDelta;
                float targetX = ExpandedSize.x;
                float targetY = ExpandedSize.y;

                while (Mathf.Abs(sizeDelta.x - targetX) > StopAnimationEpsilon || Mathf.Abs(sizeDelta.y - targetY) > StopAnimationEpsilon)
                {
                    const float SpeedScale = 45f;

                    float t = Time.deltaTime * SpeedScale;
                    sizeDelta.x = Mathf.Lerp(sizeDelta.x, targetX, t);
                    sizeDelta.y = Mathf.Lerp(sizeDelta.y, targetY, t);
                    rt.sizeDelta = sizeDelta;

                    yield return null;
                }

                rt.sizeDelta = ExpandedSize;

                _expandAnimation = null;
                OnAnimationFinished(AnimationType.Expand);
            }

            protected IEnumerator ContractAnimationCoroutine(bool immediate = false)
            {
                RectTransform rt = this.transform as RectTransform;
                Vector3 sizeDelta = rt.sizeDelta;
                float targetX = DefaultSize.x;
                float targetY = DefaultSize.y;

                if (!immediate)
                {
                    yield return CollapseAnimationDelay;
                    if (IsPointedAt)
                        yield break;
                }

                while (Mathf.Abs(sizeDelta.x - targetX) > StopAnimationEpsilon || Mathf.Abs(sizeDelta.y - targetY) > StopAnimationEpsilon)
                {
                    const float SpeedScale = 30f;

                    float t = Time.deltaTime * SpeedScale;
                    sizeDelta.x = Mathf.Lerp(sizeDelta.x, targetX, t);
                    sizeDelta.y = Mathf.Lerp(sizeDelta.y, targetY, t);
                    rt.sizeDelta = sizeDelta;

                    yield return null;
                }

                rt.sizeDelta = DefaultSize;

                _contractAnimation = null;
                OnAnimationFinished(AnimationType.Contract);
            }
        }

        public enum AnimationType
        {
            Reveal,
            Conceal,
            Expand,
            Contract
        }
    }

    public abstract class ModifiableScreenManagerBase : ScreenManagerBase, IModifiableScreen
    {
        public abstract string ScreenName { get; }

        public virtual Color BackgroundColor
        {
            get => _background?.color ?? Color.black;
            set
            {
                if (_background == null)
                    return;

                _background.color = value;
            }
        }
        public virtual bool AllowMovement
        {
            get => _screen.ShowHandle;
            set => _screen.ShowHandle = value;
        }
        protected virtual ScreensSettingsTab.BackgroundOpacity DefaultBGOpacity => ScreensSettingsTab.BackgroundOpacity.Transparent;

#pragma warning disable CS0649
        [UIComponent("background")]
        protected Graphic _background;
#pragma warning restore CS0649

        private Vector3 _defaultScreenPosition;
        private Quaternion _defaultScreenRotation;

        public ModifiableScreenManagerBase(
            MainMenuViewController mainMenuVC,
            SoloFreePlayFlowCoordinator soloFC,
            PartyFreePlayFlowCoordinator partyFC,
            LevelCollectionNavigationController levelCollectionNC,
            PhysicsRaycasterWithCache physicsRaycaster,
            Vector2 screenSize,
            Vector3 screenPosition,
            Quaternion screenRotation)
            : base(mainMenuVC, soloFC, partyFC, levelCollectionNC, physicsRaycaster, screenSize, screenPosition, screenRotation)
        {
            _defaultScreenPosition = screenPosition;
            _defaultScreenRotation = screenRotation;

            _screen.HandleSide = FloatingScreen.Side.Bottom;

            if (!PluginConfig.Instance.Screens.ScreenOpacities.ContainsKey(this.GetIdentifier()))
                PluginConfig.Instance.Screens.ScreenOpacities[this.GetIdentifier()] = DefaultBGOpacity;
        }

        public virtual void ResetPosition()
        {
            _screen.ScreenPosition = _defaultScreenPosition;
            _screen.ScreenRotation = _defaultScreenRotation;

            SavePosition();
        }

        public virtual void SavePosition()
        {
            string id = this.GetIdentifier();

            PluginConfig.Instance.Screens.ScreenPositions[id] = _screen.ScreenPosition;
            PluginConfig.Instance.Screens.ScreenRotations[id] = _screen.ScreenRotation;
        }

        public virtual void LoadPosition()
        {
            if (_screen == null)
            {
                Plugin.Log.Warn($"Unable to load the screen position for \"{this.GetType().FullName}\" (FloatingScreen not initialized)");
                return;
            }

            string id = this.GetIdentifier();

            if (PluginConfig.Instance.Screens.ScreenPositions.TryGetValue(id, out Vector3 pos))
                _screen.ScreenPosition = pos;
            if (PluginConfig.Instance.Screens.ScreenRotations.TryGetValue(id, out Quaternion rot))
                _screen.ScreenRotation = rot;
        }
    }
}
