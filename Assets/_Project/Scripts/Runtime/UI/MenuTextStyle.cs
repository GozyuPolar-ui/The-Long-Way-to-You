using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prince
{
    [CreateAssetMenu(fileName = "MenuTextStyle", menuName = "Prince/Menu Text Style")]
    public sealed class MenuTextStyle : ScriptableObject
    {
        public enum TextStyleMode
        {
            DarkOnLight,
            LightOnDark
        }

        [Header("Style")]
        [SerializeField] private TextStyleMode textStyle = TextStyleMode.DarkOnLight;
        [SerializeField] private Color buttonColor = new Color32(0x22, 0x30, 0x4F, 0xFF);
        [SerializeField] private Color highlightedColor = new Color32(0x9A, 0x6B, 0x1F, 0xFF);
        [SerializeField] private Color pressedColor = new Color32(0x6E, 0x4A, 0x12, 0xFF);
        [SerializeField] private Color disabledColor = new Color32(0x22, 0x30, 0x4F, 0xFF);
        [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.45f;
        [SerializeField] private Color titleColor = new Color32(0x42, 0x2D, 0x16, 0xFF);
        [SerializeField] private Color subtitleColor = new Color32(0x3B, 0x2A, 0x1A, 0xFF);
        [SerializeField] private Color footerColor = new Color32(0x22, 0x30, 0x4F, 0xFF);
        [SerializeField] private Color lightTextColor = new Color32(0xFF, 0xF6, 0xE0, 0xFF);
        [SerializeField] private Color darkOutlineColor = new Color32(0x0B, 0x16, 0x30, 0xFF);
        [SerializeField] private Color lightOutlineColor = new Color32(0xFF, 0xF6, 0xE0, 0xFF);

        [Header("Typography")]
        [SerializeField] private TMP_FontAsset menuButtonFontAsset;
        [SerializeField] private bool boldMenuButtons = true;
        [SerializeField, Min(1f)] private float buttonBaseFontSize = 28f;
        [SerializeField, Min(1f)] private float buttonFontSizeMultiplier = 1.6875f;
        [SerializeField, Min(1f)] private float titleFontSize = 78f;
        [SerializeField, Min(1f)] private float subtitleFontSize = 31.25f;
        [SerializeField, Min(1f)] private float footerFontSize = 24f;
        [SerializeField, Min(1f)] private float copyrightFontSize = 22f;
        [SerializeField, Min(0f)] private float buttonCharacterSpacing = 3f;
        [SerializeField, Min(0f)] private float subtitleCharacterSpacing = 2f;
        [SerializeField, Min(0f)] private float menuLeftMargin = 76f;
        [SerializeField, Min(1f)] private float menuButtonSpacing = 78f;
        [SerializeField, Range(0f, 1f)] private float menuTitleOutlineThickness = 0.06f;
        [SerializeField, Range(0f, 1f)] private float menuSubtitleOutlineThickness = 0.06f;
        [SerializeField, Range(0f, 1f)] private float menuButtonOutlineThickness = 0.06f;
        [SerializeField, Range(0f, 1f)] private float menuFooterOutlineThickness = 0.06f;
        [SerializeField, Range(0f, 1f)] private float menuOutlineSoftness = 0.03f;
        [SerializeField] private Color menuUnderlayColor = new Color32(0xFF, 0xF6, 0xE0, 0x99);
        [SerializeField, Min(0f)] private float menuUnderlayOffsetX;
        [SerializeField, Min(0f)] private float menuUnderlayOffsetY = 0.5f;
        [SerializeField, Range(0f, 1f)] private float menuUnderlayDilate = 0.1f;
        [SerializeField, Range(0f, 1f)] private float menuUnderlaySoftness = 0.3f;
        [SerializeField, Range(0f, 1f)] private float copyrightUnderlayAlpha = 0.35f;
        [SerializeField, Range(0f, 1f)] private float buttonOutlineThickness = 0.12f;
        [SerializeField, Range(0f, 1f)] private float titleOutlineThickness = 0.15f;
        [SerializeField, Range(0f, 1f)] private float subtitleOutlineThickness = 0.08f;
        [SerializeField, Range(0f, 1f)] private float footerOutlineThickness = 0.1f;
        [SerializeField, Min(0f)] private float underlayOffsetX = 0.35f;
        [SerializeField, Min(0f)] private float underlayOffsetY = 0.35f;
        [SerializeField, Range(0f, 1f)] private float underlayDilate = 0.05f;
        [SerializeField, Range(0f, 1f)] private float underlaySoftness = 0.05f;

        [Header("Light-on-dark gradient")]
        [SerializeField] private Color gradientColor = new Color32(0x0B, 0x16, 0x30, 0xFF);
        [SerializeField, Range(0f, 1f)] private float gradientAlpha = 0.65f;
        [SerializeField, Range(0.1f, 1f)] private float gradientWidth = 0.5f;

        public TextStyleMode CurrentStyle => textStyle;
        public Color AuditButtonColor => textStyle == TextStyleMode.LightOnDark ? lightTextColor : buttonColor;
        public Color AuditTitleColor => textStyle == TextStyleMode.LightOnDark ? lightTextColor : titleColor;
        public Color AuditSubtitleColor => textStyle == TextStyleMode.LightOnDark ? lightTextColor : buttonColor;
        public Color AuditFooterColor => textStyle == TextStyleMode.LightOnDark ? lightTextColor : footerColor;
        public Color AuditGradientColor => gradientColor;
        public float AuditGradientAlpha => gradientAlpha;
        public float AuditGradientWidth => gradientWidth;

        public void ApplyMainMenu(
            Transform root,
            Image gradient,
            MenuVideoBackground videoBackground)
        {
            if (root == null)
            {
                Debug.LogError("MenuTextStyle cannot style a missing Main Menu content root.", this);
                return;
            }

            bool lightOnDark = textStyle == TextStyleMode.LightOnDark;
            Color bodyColor = lightOnDark ? lightTextColor : buttonColor;
            Color outline = lightOnDark ? darkOutlineColor : lightOutlineColor;
            ApplyMenuLayout(root);
            if (gradient != null)
            {
                gradient.raycastTarget = false;
                gradient.gameObject.SetActive(lightOnDark);
                if (lightOnDark && videoBackground != null)
                    videoBackground.ConfigureGradient(gradient, gradientAlpha, gradientWidth, gradientColor);
            }

            ApplyText(root, "TitleGroup/GameTitle", lightOnDark ? lightTextColor : titleColor,
                titleFontSize, titleFontSize * 0.72f, titleFontSize, menuTitleOutlineThickness,
                outline, FontWeight.Bold, TextAlignmentOptions.MidlineLeft);
            ApplyText(root, "TitleGroup/GameSubtitle", bodyColor,
                subtitleFontSize, subtitleFontSize * 0.8f, subtitleFontSize, menuSubtitleOutlineThickness,
                outline, FontWeight.SemiBold, TextAlignmentOptions.MidlineLeft);
            TMP_Text subtitle = root.Find("TitleGroup/GameSubtitle")?.GetComponent<TMP_Text>();
            if (subtitle != null)
                subtitle.characterSpacing = subtitleCharacterSpacing;

            string[] buttonNames =
            {
                "ContinueButton",
                "NewGameButton",
                "SettingsButton",
                "QuitButton"
            };
            foreach (string buttonName in buttonNames)
                ApplyButton(root, "MainMenuButtons/" + buttonName, outline, bodyColor);

            ApplyText(root, "FooterStudio", lightOnDark ? lightTextColor : footerColor,
                footerFontSize, 22f, footerFontSize, menuFooterOutlineThickness,
                outline, FontWeight.SemiBold, TextAlignmentOptions.BottomLeft);
            ApplyText(root, "FooterCopyright", lightOnDark ? lightTextColor : footerColor,
                copyrightFontSize, 20f, copyrightFontSize, 0f,
                outline, FontWeight.Medium, TextAlignmentOptions.BottomLeft, copyrightUnderlayAlpha);
            ApplyText(root, "FooterBeta", lightOnDark ? lightTextColor : footerColor,
                footerFontSize, 22f, footerFontSize, menuFooterOutlineThickness,
                outline, FontWeight.SemiBold, TextAlignmentOptions.BottomRight);
            ApplyText(root, "FooterVersion", lightOnDark ? lightTextColor : footerColor,
                footerFontSize, 22f, footerFontSize, menuFooterOutlineThickness,
                outline, FontWeight.SemiBold, TextAlignmentOptions.BottomRight);
        }

        public void ApplyClosingScene(Transform root)
        {
            if (root == null)
            {
                Debug.LogError("MenuTextStyle cannot style a missing Closing scene root.", this);
                return;
            }

            bool lightOnDark = textStyle == TextStyleMode.LightOnDark;
            Color bodyColor = lightOnDark ? lightTextColor : buttonColor;
            Color outline = lightOnDark ? darkOutlineColor : lightOutlineColor;

            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in texts)
            {
                if (text == null)
                    continue;

                bool title = text.gameObject.name == "ClosingTitle";
                Color color = title
                    ? lightOnDark ? lightTextColor : titleColor
                    : bodyColor;
                float outlineWidth = title ? titleOutlineThickness : buttonOutlineThickness;
                FontWeight weight = title ? FontWeight.Bold : FontWeight.SemiBold;
                text.enableAutoSizing = true;
                text.fontSizeMin = Mathf.Max(14f, text.fontSize * 0.8f);
                text.fontSizeMax = text.fontSize;
                text.textWrappingMode = TextWrappingModes.Normal;
                ApplyText(text, color, outlineWidth, outline, weight);
            }

            MenuButtonFX[] buttons = root.GetComponentsInChildren<MenuButtonFX>(true);
            foreach (MenuButtonFX button in buttons)
                button.ConfigureTextStyle(bodyColor, highlightedColor, pressedColor,
                    disabledColor, outline, disabledAlpha);

            Transform bottomGradient = root.Find("ClosingBottomGradient");
            if (bottomGradient != null)
                bottomGradient.gameObject.SetActive(lightOnDark);
        }

        private void ApplyMenuLayout(Transform root)
        {
            SetHorizontalMargin(root.Find("TitleGroup"), menuLeftMargin);
            Transform buttons = root.Find("MainMenuButtons");
            SetHorizontalMargin(buttons, menuLeftMargin);
            string[] names = { "ContinueButton", "NewGameButton", "SettingsButton", "QuitButton" };
            for (int i = 0; i < names.Length; i++)
            {
                RectTransform button = buttons != null
                    ? buttons.Find(names[i]) as RectTransform
                    : null;
                if (button != null)
                    button.anchoredPosition = new Vector2(button.anchoredPosition.x, -i * menuButtonSpacing);
            }

            SetHorizontalMargin(root.Find("FooterStudio"), menuLeftMargin);
            SetHorizontalMargin(root.Find("FooterCopyright"), menuLeftMargin);
            SetHorizontalMargin(root.Find("FooterBeta"), -menuLeftMargin);
            SetHorizontalMargin(root.Find("FooterVersion"), -menuLeftMargin);
        }

        private static void SetHorizontalMargin(Transform target, float margin)
        {
            RectTransform rect = target as RectTransform;
            if (rect != null)
                rect.anchoredPosition = new Vector2(margin, rect.anchoredPosition.y);
        }

        private void ApplyButton(Transform root, string path, Color outline, Color normalColor)
        {
            Transform buttonTransform = root.Find(path);
            if (buttonTransform == null)
            {
                Debug.LogWarning($"MenuTextStyle could not find menu button '{path}'.", this);
                return;
            }

            TMP_Text label = buttonTransform.Find("Label")?.GetComponent<TMP_Text>();
            if (label == null)
            {
                Debug.LogWarning($"Menu button '{path}' has no TMP label.", buttonTransform);
                return;
            }

            label.color = normalColor;
            if (menuButtonFontAsset != null)
                label.font = menuButtonFontAsset;
            label.fontSize = buttonBaseFontSize * buttonFontSizeMultiplier;
            FontWeight weight = boldMenuButtons ? FontWeight.Bold : FontWeight.SemiBold;
            label.fontWeight = weight;
            label.characterSpacing = buttonCharacterSpacing;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.enableAutoSizing = true;
            label.fontSizeMin = buttonBaseFontSize;
            label.fontSizeMax = buttonBaseFontSize * buttonFontSizeMultiplier;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;

            RectTransform textRect = label.rectTransform;
            textRect.offsetMin = new Vector2(0f, 3f);
            textRect.offsetMax = new Vector2(-8f, -3f);

            Image underline = buttonTransform.Find("Hover Underline")?.GetComponent<Image>();
            Image marker = buttonTransform.Find("Hover Marker")?.GetComponent<Image>();
            if (underline != null)
            {
                underline.color = new Color(highlightedColor.r, highlightedColor.g,
                    highlightedColor.b, underline.color.a);
                underline.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            }
            if (marker != null)
            {
                marker.color = new Color(highlightedColor.r, highlightedColor.g,
                    highlightedColor.b, marker.color.a);
                marker.rectTransform.anchoredPosition = new Vector2(-10f, 0f);
            }

            MenuButtonFX fx = buttonTransform.GetComponent<MenuButtonFX>();
            if (fx != null)
                fx.ConfigureTextStyle(normalColor, highlightedColor, pressedColor,
                    disabledColor, outline, disabledAlpha);
            ApplyMenuText(label, normalColor, menuButtonOutlineThickness, outline, weight, menuUnderlayColor.a);
            if (fx != null)
                fx.SetInteractable(buttonTransform.GetComponent<Button>().interactable);
        }

        private void ApplyText(
            Transform root,
            string path,
            Color color,
            float fontSize,
            float minimumSize,
            float maximumSize,
            float outlineWidth,
            Color outlineColor,
            FontWeight weight,
            TextAlignmentOptions alignment,
            float underlayAlpha = -1f)
        {
            Transform textTransform = root.Find(path);
            if (textTransform == null)
            {
                Debug.LogWarning($"MenuTextStyle could not find menu text '{path}'.", this);
                return;
            }

            TMP_Text text = textTransform.GetComponent<TMP_Text>();
            if (text == null)
            {
                Debug.LogWarning($"Menu text '{path}' has no TMP component.", textTransform);
                return;
            }

            text.fontSize = fontSize;
            text.fontSizeMin = minimumSize;
            text.fontSizeMax = maximumSize;
            text.enableAutoSizing = true;
            text.fontWeight = weight;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            ApplyMenuText(text, color, outlineWidth, outlineColor, weight,
                underlayAlpha < 0f ? menuUnderlayColor.a : underlayAlpha);
        }

        private void ApplyMenuText(
            TMP_Text text,
            Color color,
            float outlineWidth,
            Color outlineColor,
            FontWeight weight,
            float underlayAlpha)
        {
            text.color = color;
            text.fontWeight = weight;
            Material instance = text.fontMaterial;
            if (instance == null)
            {
                Debug.LogError($"Could not create an instance of the TMP material for '{text.name}'.", text);
                return;
            }

            if (instance.HasProperty("_OutlineColor"))
                instance.SetColor("_OutlineColor", outlineColor);
            if (instance.HasProperty("_OutlineWidth"))
                instance.SetFloat("_OutlineWidth", outlineWidth);
            if (instance.HasProperty("_OutlineSoftness"))
                instance.SetFloat("_OutlineSoftness", menuOutlineSoftness);
            if (instance.HasProperty("_UnderlayColor"))
            {
                Color underlay = menuUnderlayColor;
                underlay.a = underlayAlpha;
                instance.SetColor("_UnderlayColor", underlay);
            }
            if (instance.HasProperty("_UnderlayOffsetX"))
                instance.SetFloat("_UnderlayOffsetX", menuUnderlayOffsetX);
            if (instance.HasProperty("_UnderlayOffsetY"))
                instance.SetFloat("_UnderlayOffsetY", -menuUnderlayOffsetY);
            if (instance.HasProperty("_UnderlayDilate"))
                instance.SetFloat("_UnderlayDilate", menuUnderlayDilate);
            if (instance.HasProperty("_UnderlaySoftness"))
                instance.SetFloat("_UnderlaySoftness", menuUnderlaySoftness);
            instance.EnableKeyword("OUTLINE_ON");
            instance.EnableKeyword("UNDERLAY_ON");
        }

        private void ApplyText(
            TMP_Text text,
            Color color,
            float outlineWidth,
            Color outlineColor,
            FontWeight weight)
        {
            text.color = color;
            text.fontWeight = weight;
            Material instance = text.fontMaterial;
            if (instance == null)
            {
                Debug.LogError($"Could not create an instance of the TMP material for '{text.name}'.", text);
                return;
            }

            if (instance.HasProperty("_OutlineColor"))
                instance.SetColor("_OutlineColor", outlineColor);
            if (instance.HasProperty("_OutlineWidth"))
                instance.SetFloat("_OutlineWidth", outlineWidth);
            if (instance.HasProperty("_UnderlayColor"))
                instance.SetColor("_UnderlayColor", lightOutlineColor);
            if (instance.HasProperty("_UnderlayOffsetX"))
                instance.SetFloat("_UnderlayOffsetX", underlayOffsetX);
            if (instance.HasProperty("_UnderlayOffsetY"))
                instance.SetFloat("_UnderlayOffsetY", -underlayOffsetY);
            if (instance.HasProperty("_UnderlayDilate"))
                instance.SetFloat("_UnderlayDilate", underlayDilate);
            if (instance.HasProperty("_UnderlaySoftness"))
                instance.SetFloat("_UnderlaySoftness", underlaySoftness);
            instance.EnableKeyword("OUTLINE_ON");
            instance.EnableKeyword("UNDERLAY_ON");
        }
    }
}
