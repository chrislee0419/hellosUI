using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;
using VRUIControls;
using IPA.Utilities;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.FloatingScreen;
using BSMLUtilities = BeatSaberMarkupLanguage.Utilities;

namespace HUI.UI.Screens
{
    public abstract class ScreenManagerBase : IInitializable, IDisposable, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual string AssociatedBSMLResource => null;

        private MainMenuViewController _mainMenuVC;
        private SoloFreePlayFlowCoordinator _soloFC;
        private PartyFreePlayFlowCoordinator _partyFC;

        protected FloatingScreen _screen;
        protected ScreenAnimationHandler _animationHandler;

        public ScreenManagerBase(
            MainMenuViewController mainMenuVC,
            SoloFreePlayFlowCoordinator soloFC,
            PartyFreePlayFlowCoordinator partyFC,
            PhysicsRaycasterWithCache physicsRaycaster,
            Vector2 screenSize,
            Vector3 screenPosition,
            Quaternion screenRotation)
        {
            Plugin.Log.Debug($"Creating {GetType().Name}");

            _mainMenuVC = mainMenuVC;
            _soloFC = soloFC;
            _partyFC = partyFC;

            _screen = FloatingScreen.CreateFloatingScreen(screenSize, false, screenPosition, screenRotation);

            // fix for null exceptions during raycast
            // most likely, the FloatingScreen is created too "early", so BSML can't find PhysicsRaycaster
            var vrGraphicRaycaster = this._screen.GetComponent<VRGraphicRaycaster>();
            FieldAccessor<VRGraphicRaycaster, PhysicsRaycasterWithCache>.Set(ref vrGraphicRaycaster, "_physicsRaycaster", physicsRaycaster);

            _animationHandler = _screen.gameObject.AddComponent<ScreenAnimationHandler>();
            _animationHandler.DefaultSize = screenSize;

            if (!string.IsNullOrEmpty(AssociatedBSMLResource))
                BSMLParser.instance.Parse(BSMLUtilities.GetResourceContent(Assembly.GetExecutingAssembly(), AssociatedBSMLResource), _screen.gameObject, this);
        }

        public virtual void Initialize()
        {
            _mainMenuVC.didFinishEvent += OnMainMenuViewControllerDidFinish;
            _soloFC.didFinishEvent += OnSinglePlayerLevelSelectionFlowCoordinatorDidFinish;
            _partyFC.didFinishEvent += OnSinglePlayerLevelSelectionFlowCoordinatorDidFinish;

            _screen.gameObject.SetActive(false);
        }

        public virtual void Dispose()
        {
            _mainMenuVC.didFinishEvent -= OnMainMenuViewControllerDidFinish;
            _soloFC.didFinishEvent -= OnSinglePlayerLevelSelectionFlowCoordinatorDidFinish;
            _partyFC.didFinishEvent -= OnSinglePlayerLevelSelectionFlowCoordinatorDidFinish;
        }

        private void OnMainMenuViewControllerDidFinish(MainMenuViewController _, MainMenuViewController.MenuButton buttonType)
        {
            if (buttonType == MainMenuViewController.MenuButton.SoloFreePlay || buttonType == MainMenuViewController.MenuButton.Party)
            {
                if (_animationHandler != null)
                    _animationHandler.PlayRevealAnimation();

                try
                {
                    OnSinglePlayerLevelSelectionStarting();
                }
                catch (Exception e)
                {
                    Plugin.Log.Warn($"Unexpected exception occurred in {GetType().Name}:{nameof(OnSinglePlayerLevelSelectionStarting)}");
                    Plugin.Log.Debug(e);
                }
            }
        }

        protected virtual void OnSinglePlayerLevelSelectionStarting()
        {

        }

        private void OnSinglePlayerLevelSelectionFlowCoordinatorDidFinish(SinglePlayerLevelSelectionFlowCoordinator _)
        {
            if (_animationHandler != null)
                _animationHandler.PlayConcealAnimation();

            try
            {
                OnSinglePlayerLevelSelectionFinished();
            }
            catch (Exception e)
            {
                Plugin.Log.Warn($"Unexpected exception occurred in {GetType().Name}:{nameof(OnSinglePlayerLevelSelectionFinished)}");
                Plugin.Log.Debug(e);
            }
        }

        protected virtual void OnSinglePlayerLevelSelectionFinished()
        {

        }

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
            public Vector2 DefaultSize { get; set; } = Vector2.zero;
            public virtual Vector2 ExpandedSize { get; set; } = Vector2.zero;
            public virtual bool UsePointerAnimations { get; set; } = true;

            public bool IsPointedAt { get; private set; } = false;

            protected Coroutine _revealAnimation = null;
            protected Coroutine _expandAnimation = null;
            protected Coroutine _contractAnimation = null;

            protected const float StopAnimationEpsilon = 0.0001f;
            protected const float LocalScale = 0.02f;
            protected static readonly WaitForSeconds CollapseAnimationDelay = new WaitForSeconds(1.2f);

            public void OnPointerEnter(PointerEventData eventData)
            {
                IsPointedAt = true;

                if (!UsePointerAnimations || _revealAnimation != null)
                    return;

                if (_contractAnimation != null)
                {
                    StopCoroutine(_contractAnimation);
                    _contractAnimation = null;
                }

                _expandAnimation = StartCoroutine(ExpandAnimationCoroutine());
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                IsPointedAt = false;

                if (!UsePointerAnimations || _revealAnimation != null)
                    return;

                bool immediate = false;
                if (_expandAnimation != null)
                {
                    StopCoroutine(_expandAnimation);
                    _expandAnimation = null;
                    immediate = true;
                }

                _contractAnimation = StartCoroutine(ContractAnimationCoroutine(immediate));
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
            }
        }
    }
}
