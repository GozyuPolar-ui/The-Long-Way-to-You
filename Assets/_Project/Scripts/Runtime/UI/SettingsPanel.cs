using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prince
{
    public sealed class SettingsPanel : MonoBehaviour
    {
        private TMP_FontAsset font;
        private Action onBack;
        private TMP_Dropdown qualityDropdown;
        private bool built;
        private int sliderRow;

        public void Build(TMP_FontAsset menuFont, Action backAction)
        {
            if (built)
                return;
            built = true;
            font = menuFont;
            onBack = backAction;
            RectTransform root = transform as RectTransform;
            if (root == null)
            {
                Debug.LogError("SettingsPanel must be attached to a RectTransform.");
                return;
            }

            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(670f, 760f);
            Image panelImage = gameObject.GetComponent<Image>();
            if (panelImage == null)
                panelImage = gameObject.AddComponent<Image>();
            panelImage.color = MenuUIFactory.PanelBackground;
            Outline outline = gameObject.GetComponent<Outline>();
            if (outline == null)
                outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(MenuUIFactory.Gold.r, MenuUIFactory.Gold.g, MenuUIFactory.Gold.b, 0.65f);
            outline.effectDistance = new Vector2(1f, -1f);

            TextMeshProUGUI title = MenuUIFactory.Text(
                "SettingsTitle", root, MenuStrings.Settings, 34f, MenuUIFactory.Cream, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(500f, 54f));

            AddVolumeSlider(root, MenuStrings.MasterVolume, "Master");
            AddVolumeSlider(root, MenuStrings.Music, "Music");
            AddVolumeSlider(root, MenuStrings.Effects, "Effects");
            AddQualityDropdown(root);
            AddFullscreenToggle(root);
            AddSensitivitySlider(root);

            Button back = MenuUIFactory.Button("SettingsBack", root, MenuStrings.Back, font);
            Place(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(310f, 56f));
            back.onClick.AddListener(() => onBack?.Invoke());
            gameObject.SetActive(false);
        }

        private void AddVolumeSlider(Transform root, string label, string channel)
        {
            float defaultValue = channel == "Master" ? MenuSettings.MasterVolume :
                channel == "Music" ? MenuSettings.MusicVolume : MenuSettings.EffectsVolume;
            AddSlider(root, label, defaultValue, (value) => MenuSettings.SetVolume(channel, value));
        }

        private void AddSensitivitySlider(Transform root)
        {
            float normalized = Mathf.InverseLerp(0.35f, 2.5f, MenuSettings.CameraSensitivity);
            AddSlider(root, MenuStrings.CameraSensitivity, normalized,
                value => MenuSettings.SetCameraSensitivity(Mathf.Lerp(0.35f, 2.5f, value)));
        }

        private void AddSlider(Transform root, string label, float value, Action<float> changed)
        {
            float y = -128f - sliderRow++ * 84f;
            TextMeshProUGUI text = MenuUIFactory.Text(label + "Label", root, label, 19f,
                MenuUIFactory.Cream, TextAlignmentOptions.MidlineLeft);
            Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(42f, y), new Vector2(250f, 34f));

            Slider slider = CreateSlider(root, label + "Slider");
            Place(slider.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(18f, y - 44f), new Vector2(-36f, 18f));
            slider.value = Mathf.Clamp01(value);
            slider.onValueChanged.AddListener(valueChanged => changed(valueChanged));
        }

        private void AddQualityDropdown(Transform root)
        {
            TextMeshProUGUI label = MenuUIFactory.Text("QualityLabel", root, MenuStrings.GraphicsQuality, 19f,
                MenuUIFactory.Cream);
            Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(42f, -456f), new Vector2(250f, 36f));

            RectTransform rect = MenuUIFactory.Rect("QualityDropdown", root, new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f, -452f), new Vector2(265f, 42f));
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.11f, 0.105f, 0.09f, 1f);
            qualityDropdown = rect.gameObject.AddComponent<TMP_Dropdown>();
            qualityDropdown.targetGraphic = image;
            qualityDropdown.captionText = CreateDropdownText(rect, "QualityValue", 17f);
            qualityDropdown.template = CreateDropdownTemplate(rect);
            qualityDropdown.options.Clear();
            foreach (string quality in QualitySettings.names)
                qualityDropdown.options.Add(new TMP_Dropdown.OptionData(quality));
            if (qualityDropdown.options.Count == 0)
                qualityDropdown.options.Add(new TMP_Dropdown.OptionData(MenuStrings.DefaultQuality));
            qualityDropdown.value = Mathf.Clamp(MenuSettings.QualityIndex, 0, qualityDropdown.options.Count - 1);
            qualityDropdown.onValueChanged.AddListener(MenuSettings.SetQuality);
            qualityDropdown.RefreshShownValue();
        }

        private void AddFullscreenToggle(Transform root)
        {
            TextMeshProUGUI label = MenuUIFactory.Text("FullscreenLabel", root, MenuStrings.Fullscreen, 19f,
                MenuUIFactory.Cream);
            Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(42f, -520f), new Vector2(250f, 36f));

            RectTransform rect = MenuUIFactory.Rect("FullscreenToggle", root, new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f, -520f), new Vector2(46f, 36f));
            Toggle toggle = rect.gameObject.AddComponent<Toggle>();
            Image background = rect.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.11f, 0.09f, 1f);
            RectTransform checkRect = MenuUIFactory.Rect("Checkmark", rect, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-10f, -10f));
            Image check = checkRect.gameObject.AddComponent<Image>();
            check.color = MenuUIFactory.Gold;
            toggle.targetGraphic = background;
            toggle.graphic = check;
            toggle.isOn = MenuSettings.Fullscreen;
            toggle.onValueChanged.AddListener(MenuSettings.SetFullscreen);
        }

        private RectTransform CreateDropdownTemplate(RectTransform owner)
        {
            RectTransform template = MenuUIFactory.Rect("Template", owner, new Vector2(0f, 0f),
                new Vector2(1f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0f),
                new Vector2(0f, Mathf.Max(100f, QualitySettings.names.Length * 36f)));
            Image image = template.gameObject.AddComponent<Image>();
            image.color = MenuUIFactory.PanelBackground;
            ScrollRect scroll = template.gameObject.AddComponent<ScrollRect>();
            RectTransform viewport = MenuUIFactory.Rect("Viewport", template, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = Color.white;
            RectTransform content = MenuUIFactory.Rect("Content", viewport, new Vector2(0f, 1f),
                new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 36f));
            Toggle item = new GameObject("Item", typeof(RectTransform), typeof(Toggle), typeof(Image))
                .GetComponent<Toggle>();
            item.transform.SetParent(content, false);
            item.targetGraphic = item.GetComponent<Image>();
            TextMeshProUGUI itemText = MenuUIFactory.Text("ItemLabel", item.transform, string.Empty, 17f,
                MenuUIFactory.Cream);
            itemText.rectTransform.offsetMin = new Vector2(12f, 0f);
            itemText.rectTransform.offsetMax = new Vector2(-8f, 0f);
            RectTransform itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(1f, 1f);
            itemRect.pivot = new Vector2(0.5f, 1f);
            itemRect.sizeDelta = new Vector2(0f, 36f);
            RectTransform checkmarkRect = MenuUIFactory.Rect("ItemCheckmark", item.transform,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-8f, 0f), new Vector2(14f, 14f));
            item.graphic = checkmarkRect.gameObject.AddComponent<Image>();
            item.graphic.color = MenuUIFactory.Gold;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            template.gameObject.SetActive(false);
            TMP_Dropdown dropdown = owner.GetComponent<TMP_Dropdown>();
            dropdown.itemText = itemText;
            dropdown.itemImage = null;
            return template;
        }

        private TextMeshProUGUI CreateDropdownText(Transform parent, string name, float size)
        {
            TextMeshProUGUI text = MenuUIFactory.Text(name, parent, string.Empty, size,
                MenuUIFactory.Cream);
            text.font = font;
            return text;
        }

        private static Slider CreateSlider(Transform parent, string name)
        {
            RectTransform root = MenuUIFactory.Rect(name, parent, Vector2.zero, Vector2.zero,
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(400f, 18f));
            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.2f, 0.19f, 0.17f, 1f);
            RectTransform fillArea = MenuUIFactory.Rect("Fill Area", root, Vector2.zero, Vector2.one,
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(-8f, -8f));
            RectTransform fill = MenuUIFactory.Rect("Fill", fillArea, Vector2.zero, Vector2.one,
                new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = MenuUIFactory.Gold;
            RectTransform handle = MenuUIFactory.Rect("Handle", root, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 28f));
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = MenuUIFactory.Cream;
            slider.targetGraphic = handleImage;
            slider.fillRect = fill;
            slider.handleRect = handle;
            return slider;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            SetRect(rect, anchorMin, anchorMax, pivot, position, size);
        }
    }
}
