using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prince
{
    internal static class MenuUIFactory
    {
        internal static readonly Color Gold = new Color(0.72f, 0.55f, 0.30f, 1f);
        internal static readonly Color Cream = new Color(0.91f, 0.82f, 0.64f, 1f);
        internal static readonly Color Muted = new Color(0.72f, 0.64f, 0.48f, 1f);
        internal static readonly Color TitleGold = new Color(0.94f, 0.78f, 0.43f, 1f);
        internal static readonly Color PanelBackground = new Color(0.035f, 0.04f, 0.043f, 0.88f);

        internal static RectTransform Rect(string name, Transform parent, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        internal static Image Image(string name, Transform parent, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            RectTransform rect = Rect(name, parent, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        internal static TextMeshProUGUI Text(string name, Transform parent, string value, float size,
            Color color, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(14f, 2f);
            rect.offsetMax = new Vector2(-14f, -2f);
            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            Shadow shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.015f, 0.032f, 0.05f, 0.9f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        internal static RectTransform Panel(string name, Transform parent, Vector2 anchor,
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            RectTransform panel = Rect(name, parent, anchor, anchor, pivot, position, size);
            Image image = panel.gameObject.AddComponent<Image>();
            image.color = PanelBackground;
            image.raycastTarget = false;
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.55f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            return panel;
        }

        internal static Button Button(string name, Transform parent, string label, TMP_FontAsset font = null)
        {
            RectTransform rect = Rect(name, parent, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(430f, 62f));
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            TextMeshProUGUI text = Text("Label", rect, label, 28f, Cream, TextAlignmentOptions.MidlineLeft);
            text.fontWeight = FontWeight.SemiBold;
            if (font != null)
                text.font = font;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 3f);
            textRect.offsetMax = new Vector2(-8f, -3f);

            Image underline = Image("Hover Underline", rect, Gold,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(14f, 4f), new Vector2(190f, 1.5f));
            underline.rectTransform.pivot = new Vector2(0f, 0.5f);
            underline.color = new Color(Gold.r, Gold.g, Gold.b, 0f);
            Image marker = Image("Hover Marker", rect, Gold,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(7f, 7f));
            marker.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            marker.color = new Color(Gold.r, Gold.g, Gold.b, 0f);
            MenuButtonFX fx = rect.gameObject.AddComponent<MenuButtonFX>();
            fx.Target = rect;
            fx.Configure(text, underline, marker, Cream, new Color(1f, 0.78f, 0.43f), Muted);
            return button;
        }
    }
}
