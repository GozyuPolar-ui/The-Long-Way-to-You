using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Prince
{
    public sealed class MenuButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField, Min(1f)] private float highlightedScale = 1.035f;
        [SerializeField, Min(0f)] private float animationSpeed = 10f;
        private Vector3 restingScale = Vector3.one;
        private float targetScale = 1f;
        private TMP_Text label;
        private Image underline;
        private Image marker;
        private Color normalColor = MenuUIFactory.Cream;
        private Color highlightedColor = MenuUIFactory.Gold;
        private Color pressedColor = MenuUIFactory.Muted;
        private Color disabledColor = MenuUIFactory.Cream;
        private Color outlineColor = MenuUIFactory.Cream;
        private float disabledAlpha = 0.45f;
        private float targetHighlight;
        private bool pressed;
        private bool interactable = true;

        public RectTransform Target { private get; set; }

        public void Configure(TMP_Text text, Image underlineImage, Image focusMarker,
            Color normal, Color highlighted, Color pressedText)
        {
            label = text;
            underline = underlineImage;
            marker = focusMarker;
            normalColor = normal;
            highlightedColor = highlighted;
            pressedColor = pressedText;
            disabledColor = normal;
        }

        public void ConfigureTextStyle(
            Color normal,
            Color highlighted,
            Color pressedText,
            Color disabled,
            Color outline,
            float disabledTextAlpha)
        {
            normalColor = normal;
            highlightedColor = highlighted;
            pressedColor = pressedText;
            disabledColor = disabled;
            outlineColor = outline;
            disabledAlpha = Mathf.Clamp01(disabledTextAlpha);
            ApplyOutlineAlpha();
        }

        public void SetInteractable(bool value)
        {
            interactable = value;
            if (!interactable)
            {
                targetHighlight = 0f;
                targetScale = 1f;
                pressed = false;
            }
            ApplyOutlineAlpha();
        }

        private void Awake()
        {
            if (Target == null)
                Target = transform as RectTransform;
            if (Target != null)
                restingScale = Target.localScale;
        }

        private void Update()
        {
            if (Target == null)
                return;
            float amount = 1f - Mathf.Exp(-animationSpeed * Time.unscaledDeltaTime);
            Target.localScale = Vector3.Lerp(Target.localScale, restingScale * targetScale, amount);
            if (label != null)
            {
                Color targetColor = !interactable
                    ? new Color(disabledColor.r, disabledColor.g, disabledColor.b, disabledAlpha)
                    : pressed ? pressedColor : targetHighlight > 0.5f ? highlightedColor : normalColor;
                label.color = Color.Lerp(label.color, targetColor, amount);
            }
            if (underline != null)
            {
                Color color = underline.color;
                color.a = Mathf.Lerp(color.a, targetHighlight, amount);
                underline.color = color;
            }
            if (marker != null)
            {
                Color color = marker.color;
                color.a = Mathf.Lerp(color.a, targetHighlight, amount);
                marker.color = color;
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => SetHighlighted(true);
        public void OnPointerExit(PointerEventData eventData) => SetHighlighted(false);
        public void OnSelect(BaseEventData eventData) => SetHighlighted(true);
        public void OnDeselect(BaseEventData eventData) => SetHighlighted(false);

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!interactable)
                return;
            pressed = true;
            targetScale = 0.99f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!interactable)
                return;
            pressed = false;
            targetScale = targetHighlight > 0.5f ? highlightedScale : 1f;
        }

        private void SetHighlighted(bool highlighted)
        {
            if (!interactable)
                return;
            targetHighlight = highlighted ? 1f : 0f;
            targetScale = highlighted ? highlightedScale : 1f;
            pressed = false;
        }

        private void ApplyOutlineAlpha()
        {
            if (label == null)
                return;
            Material material = label.fontMaterial;
            if (material == null || !material.HasProperty("_OutlineColor"))
                return;

            Color color = outlineColor;
            color.a = interactable ? 1f : disabledAlpha;
            material.SetColor("_OutlineColor", color);
        }
    }
}
