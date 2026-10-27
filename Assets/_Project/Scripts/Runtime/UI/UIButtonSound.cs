using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class UIButtonSound : MonoBehaviour,
        IPointerEnterHandler, ISelectHandler, IPointerClickHandler, ISubmitHandler, ICancelHandler
    {
        [SerializeField] private bool playOnFirstSelect;
        [SerializeField] private UISoundType clickSound = UISoundType.Click;

        private Selectable selectable;
        private Button button;
        private Toggle toggle;
        private Slider slider;
        private TMP_Dropdown dropdown;
        private bool selectedBefore;

        public static UIButtonSound Attach(GameObject target)
        {
            if (target == null)
                return null;

            UIButtonSound sound = target.GetComponent<UIButtonSound>();
            return sound != null ? sound : target.AddComponent<UIButtonSound>();
        }

        public static void SetClickSound(GameObject target, UISoundType type)
        {
            UIButtonSound sound = Attach(target);
            if (sound != null)
                sound.clickSound = type;
        }

        private void Awake()
        {
            CacheControls();
            ConfigureDefaultSound();
        }

        private void OnEnable()
        {
            CacheControls();
            if (button != null)
                button.onClick.AddListener(PlayClick);
            if (toggle != null && !IsDropdownOption())
                toggle.onValueChanged.AddListener(OnToggleChanged);
            if (slider != null)
                slider.onValueChanged.AddListener(OnSliderChanged);
            if (dropdown != null)
                dropdown.onValueChanged.AddListener(OnDropdownChanged);
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(PlayClick);
            if (toggle != null)
                toggle.onValueChanged.RemoveListener(OnToggleChanged);
            if (slider != null)
                slider.onValueChanged.RemoveListener(OnSliderChanged);
            if (dropdown != null)
                dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            selectedBefore = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (CanInteract())
                UISoundPlayer.Play(UISoundType.Hover);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!CanInteract())
                return;
            if (!selectedBefore)
            {
                selectedBefore = true;
                if (!playOnFirstSelect)
                    return;
            }
            UISoundPlayer.Play(UISoundType.Hover);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (selectable != null && !selectable.IsInteractable())
            {
                UISoundPlayer.Play(UISoundType.Error);
                return;
            }

            if (dropdown != null)
                PlayDropdownStateSound();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (selectable != null && !selectable.IsInteractable())
            {
                UISoundPlayer.Play(UISoundType.Error);
                return;
            }

            if (dropdown != null)
                PlayDropdownStateSound();
        }

        public void OnCancel(BaseEventData eventData)
        {
            if (CanInteract())
                UISoundPlayer.Play(UISoundType.Back);
        }

        private void CacheControls()
        {
            selectable = GetComponent<Selectable>();
            button = GetComponent<Button>();
            toggle = GetComponent<Toggle>();
            slider = GetComponent<Slider>();
            dropdown = GetComponent<TMP_Dropdown>();
        }

        private void ConfigureDefaultSound()
        {
            string controlName = gameObject.name;
            if (controlName.StartsWith("Choice_", System.StringComparison.Ordinal))
                clickSound = UISoundType.DialogueChoice;
            else if (Contains(controlName, "Back") || Contains(controlName, "Cancel") ||
                     Contains(controlName, "Close"))
                clickSound = UISoundType.Back;
            else if (Contains(controlName, "SettingsButton") ||
                     Contains(controlName, "FeedbackButton"))
                clickSound = UISoundType.Open;
            else if (Contains(controlName, "ConfirmAction"))
                clickSound = UISoundType.Confirmation;
        }

        private void PlayClick()
        {
            if (clickSound != UISoundType.None)
                UISoundPlayer.Play(clickSound);
        }

        private void OnToggleChanged(bool value)
        {
            UISoundPlayer.Play(UISoundType.Toggle);
        }

        private void OnSliderChanged(float value)
        {
            UISoundPlayer.Play(UISoundType.SliderTick);
        }

        private void OnDropdownChanged(int value)
        {
            UISoundPlayer.Play(UISoundType.DropdownSelect);
        }

        private void PlayDropdownStateSound()
        {
            UISoundPlayer.Play(dropdown.IsExpanded
                ? UISoundType.DropdownOpen
                : UISoundType.Close);
        }

        private bool CanInteract()
        {
            return isActiveAndEnabled && (selectable == null || selectable.IsInteractable());
        }

        private bool IsDropdownOption()
        {
            TMP_Dropdown parentDropdown = GetComponentInParent<TMP_Dropdown>();
            return (parentDropdown != null && parentDropdown.gameObject != gameObject) ||
                   gameObject.name.StartsWith("Item", System.StringComparison.Ordinal);
        }

        private static bool Contains(string value, string fragment)
        {
            return value.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
