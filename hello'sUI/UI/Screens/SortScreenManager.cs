using UnityEngine;
using UnityEngine.UI;
using VRUIControls;
using IPA.Utilities;

namespace HUI.UI.Screens
{
    public class SortScreenManager : ScreenManagerBase
    {
        private static readonly Vector2 DefaultSize = new Vector2(50f, 10f);
        private static readonly Vector2 ExpandedSize = new Vector2(50f, 60f);

        public SortScreenManager(
            MainMenuViewController mainMenuVC,
            SoloFreePlayFlowCoordinator soloFC,
            PartyFreePlayFlowCoordinator partyFC,
            PhysicsRaycasterWithCache physicsRaycaster)
            : base(mainMenuVC, soloFC, partyFC, physicsRaycaster, DefaultSize, new Vector3(-0.95f, 0.45f, 2.3f), Quaternion.Euler(75f, 345f, 0f))
        {
            this._screen.name = "HUISortScreen";

            this._animationHandler.ExpandedSize = ExpandedSize;

            // move pivot so the screen expands downwards
            var rt = this._screen.transform as RectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);

            // temp image
            var img = new GameObject("WhiteImage").AddComponent<Image>();
            img.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
            img.rectTransform.SetParent(this._screen.transform, false);
            img.rectTransform.anchorMin = Vector2.zero;
            img.rectTransform.anchorMax = Vector2.one;
            img.rectTransform.anchoredPosition = Vector2.zero;
            img.rectTransform.sizeDelta = Vector2.zero;
        }
    }
}
