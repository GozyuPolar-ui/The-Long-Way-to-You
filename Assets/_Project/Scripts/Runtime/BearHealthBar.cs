using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class BearHealthBar : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text healthLabel;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 2.1f, 0f);
        [SerializeField, Range(0.001f, 0.1f)] private float worldScale = 0.01f;

        private BearAI _bear;
        private Camera _targetCamera;
        private int _lastDisplayedHealth = int.MinValue;
        private Texture2D _runtimeFillTexture;
        private Sprite _runtimeFillSprite;

        public void Initialize(BearAI bear)
        {
            _bear = bear;
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (fillImage == null)
                fillImage = GetComponentInChildren<Image>(true);
            if (healthLabel == null)
                healthLabel = GetComponentInChildren<TMP_Text>(true);
            if (canvasGroup == null || fillImage == null)
                Debug.LogWarning($"BearHealthBar on '{name}' is missing its CanvasGroup or fill Image.", this);
            if (fillImage != null)
            {
                if (fillImage.sprite == null)
                {
                    _runtimeFillTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true)
                    {
                        name = "Bear Health Fill Texture",
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    _runtimeFillTexture.SetPixel(0, 0, Color.white);
                    _runtimeFillTexture.Apply(false, true);
                    _runtimeFillSprite = Sprite.Create(
                        _runtimeFillTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                    _runtimeFillSprite.name = "Bear Health Fill";
                    _runtimeFillSprite.hideFlags = HideFlags.HideAndDontSave;
                    fillImage.sprite = _runtimeFillSprite;
                }
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
            transform.localScale = Vector3.one * Mathf.Clamp(worldScale, 0.001f, 0.1f);
            Hide();
        }

        public void Show()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }

        public void Hide()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        private void OnDestroy()
        {
            if (_runtimeFillSprite != null)
                Destroy(_runtimeFillSprite);
            if (_runtimeFillTexture != null)
                Destroy(_runtimeFillTexture);
        }

        private void LateUpdate()
        {
            if (_bear == null)
                return;
            transform.localScale = Vector3.one * Mathf.Clamp(worldScale, 0.001f, 0.1f);
            if (_targetCamera == null)
                _targetCamera = Camera.main;

            transform.position = _bear.transform.position + cameraOffset;
            if (_targetCamera != null)
                transform.rotation = Quaternion.LookRotation(
                    _targetCamera.transform.position - transform.position, Vector3.up);

            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(_bear.CurrentHealth / _bear.MaxHealth);
            if (healthLabel != null)
            {
                int displayedHealth = Mathf.CeilToInt(_bear.CurrentHealth);
                if (displayedHealth != _lastDisplayedHealth)
                {
                    healthLabel.text = $"{displayedHealth} / {Mathf.CeilToInt(_bear.MaxHealth)}";
                    _lastDisplayedHealth = displayedHealth;
                }
            }
        }
    }
}
