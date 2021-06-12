using UnityEngine;
using UnityEngine.UI;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Tags;
using HUI.UI.CustomBSML.Components;

namespace HUI.UI.CustomBSML.Tags
{
    public class HUIScrollingTextTag : BSMLTag
    {
        public override string[] Aliases => new[] { "hui-scrolling-text" };

        public override GameObject CreateObject(Transform parent)
        {
            GameObject gameObject = new GameObject("HUIScrollingText");
            gameObject.transform.SetParent(parent, false);

            HUIScrollingText scrollingText = gameObject.AddComponent<HUIScrollingText>();
            scrollingText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            scrollingText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollingText.rectTransform.sizeDelta = new Vector2(90f, 6f);
            scrollingText.textComponent.text = "Default Text";
            scrollingText.textComponent.fontSize = 3f;
            scrollingText.textComponent.color = Color.white;

            LayoutElement layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 6f;
            layoutElement.preferredWidth = 90f;

            ExternalComponents externalComponents = gameObject.AddComponent<ExternalComponents>();
            externalComponents.components.Add(scrollingText.textComponent);

            return gameObject;
        }
    }
}
