using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using VRUIControls;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.FloatingScreen;
using HUI.Utilities;
using BSMLUtilities = BeatSaberMarkupLanguage.Utilities;
using Object = UnityEngine.Object;

namespace HUI.UI.Screens
{
    public abstract class ScreenManagerBase : SinglePlayerManagerBase, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual string AssociatedBSMLResource => null;

        protected FloatingScreen _screen;
        protected ScreenAnimationHandler _animationHandler;

        private LevelCollectionNavigationController _levelCollectionNavigationController;

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

            _screen = FloatingScreen.CreateFloatingScreen(screenSize, false, screenPosition, screenRotation);
            _screen.gameObject.FixRaycaster(physicsRaycaster);

            _animationHandler = _screen.gameObject.AddComponent<ScreenAnimationHandler>();
            _animationHandler.DefaultSize = screenSize;

            if (!string.IsNullOrEmpty(AssociatedBSMLResource))
                BSMLParser.instance.Parse(BSMLUtilities.GetResourceContent(Assembly.GetExecutingAssembly(), AssociatedBSMLResource), _screen.gameObject, this);
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

        protected override void OnSinglePlayerLevelSelectionStarting()
        {
            _animationHandler.PlayRevealAnimation();

            _levelCollectionNavigationController.didActivateEvent += OnLevelCollectionNavigationControllerActivated;
            _levelCollectionNavigationController.didDeactivateEvent += OnLevelCollectionNavigationControllerDeactivated;
        }

        protected override void OnSinglePlayerLevelSelectionFinished()
        {
            _animationHandler.PlayConcealAnimation();

            _levelCollectionNavigationController.didActivateEvent -= OnLevelCollectionNavigationControllerActivated;
            _levelCollectionNavigationController.didDeactivateEvent -= OnLevelCollectionNavigationControllerDeactivated;
        }

        protected virtual void OnLevelCollectionNavigationControllerActivated(bool unused, bool unused2, bool unused3) => _animationHandler.PlayRevealAnimation();

        protected virtual void OnLevelCollectionNavigationControllerDeactivated(bool unused, bool unused2) => _animationHandler.PlayConcealAnimation();

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception e)
            {
                Plugin.Log.Warn($"Unexpected exception occurred in {GetType().Name}:{nameof(NotifyPropertyChanged)}");
                Plugin.Log.Debug(e);
            }
        }

        protected class ScreenAnimationHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public event Action PointerEntered;
            public event Action PointerExited;
            public event Action<AnimationType> AnimationFinished;

            public Vector2 DefaultSize { get; set; } = Vector2.zero;
            public virtual Vector2 ExpandedSize { get; set; } = Vector2.zero;
            public virtual bool UsePointerAnimations { get; set; } = true;

            public bool IsPointedAt { get; private set; } = false;

            protected Coroutine _revealAnimation = null;
            protected Coroutine _expandAnimation = null;
            protected Coroutine _contractAnimation = null;

            protected const float StopAnimationEpsilon = 0.0001f;
            protected const float LocalScale = 0.02f;
            protected static readonly WaitForSeconds CollapseAnimationDelay = new WaitForSeconds(0.8f);

            public void OnPointerEnter(PointerEventData eventData)
            {
                IsPointedAt = true;

                if (UsePointerAnimations)
                    PlayExpandAnimation();

                try
                {
                    PointerEntered?.Invoke();
                }
                catch (Exception e)
                {
                    Plugin.Log.Warn($"Unexpected exception occurred in {nameof(ScreenAnimationHandler)}:{nameof(PointerEntered)}");
                    Plugin.Log.Debug(e);
                }
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                IsPointedAt = false;

                if (UsePointerAnimations)
                    PlayContractAnimation();

                try
                {
                    PointerExited?.Invoke();
                }
                catch (Exception e)
                {
                    Plugin.Log.Warn($"Unexpected exception occurred in {nameof(ScreenAnimationHandler)}:{nameof(PointerExited)}");
                    Plugin.Log.Debug(e);
                }
            }

            public void PlayRevealAnimation()
            {
                this.gameObject.SetActive(true);

                StopAllAnimations();
                _revealAnimation = StartCoroutine(RevealAnimationCoroutine(LocalScale));
            }

            public void PlayConcealAnimation()
            {
                StopAllAnimations();
                _revealAnimation = StartCoroutine(RevealAnimationCoroutine(0f, false));
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

            private void OnAnimationFinished(AnimationType animationType)
            {
                try
                {
                    AnimationFinished?.Invoke(animationType);
                }
                catch (Exception e)
                {
                    Plugin.Log.Warn($"Unexpected exception occurred in {nameof(ScreenAnimationHandler)}:{nameof(OnAnimationFinished)}");
                    Plugin.Log.Debug(e);
                }
            }

            protected IEnumerator RevealAnimationCoroutine(float destAnimationValue, bool activateOnFinish = true)
            {
                yield return null;
                yield return null;

                Vector3 localScale = this.transform.localScale;
                while (Mathf.Abs(localScale.y - destAnimationValue) > StopAnimationEpsilon)
                {
                    float num = (localScale.y > destAnimationValue) ? 30f : 16f;
                    localScale.y = Mathf.Lerp(localScale.y, destAnimationValue, Time.deltaTime * num);
                    this.transform.localScale = localScale;

                    yield return null;
                }

                localScale.y = destAnimationValue;
                this.transform.localScale = localScale;

                // reset size delta as well
                (this.transform as RectTransform).sizeDelta = DefaultSize;

                this.gameObject.SetActive(activateOnFinish);
                _revealAnimation = null;
                OnAnimationFinished(activateOnFinish ? AnimationType.Reveal : AnimationType.Conceal);
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
}
