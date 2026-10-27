using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Prince
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class LoadingScreenController : MonoBehaviour
    {
        [SerializeField] private LoreSlideView loreSlideView;
        private CanvasGroup canvasGroup;
        private IDisposable inputSubscription;
        private bool continueRequested;

        public LoreSlideView View => loreSlideView;

        public void Configure(LoreSlideView view)
        {
            loreSlideView = view;
        }

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (loreSlideView == null)
                loreSlideView = GetComponent<LoreSlideView>();
            if (loreSlideView == null)
                Debug.LogError("LoadingScreenController requires a LoreSlideView.", this);
        }

        public IEnumerator FadeIn(LoadingLoreData data)
        {
            if (loreSlideView == null || data == null)
                yield break;
            loreSlideView.Begin(data);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            if (data.UseScreenFade)
                yield return FadeTo(1f, data.FadeDuration);
            else
                canvasGroup.alpha = 1f;
        }

        public IEnumerator FadeOut(LoadingLoreData data)
        {
            if (data != null && data.UseScreenFade)
                yield return FadeTo(0f, data.FadeDuration);
            else if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        public void SetLanguage(LoadingLoreLanguage language)
        {
            loreSlideView?.SetLanguage(language);
        }

        public void WaitForAnyInput(LoadingLoreLanguage language)
        {
            if (loreSlideView == null)
                return;
            continueRequested = false;
            loreSlideView.ShowContinuePrompt(language);
            inputSubscription?.Dispose();
            inputSubscription = InputSystem.onAnyButtonPress.CallOnce(OnAnyButtonPressed);
        }

        public bool ContinueRequested => continueRequested;

        public void SetProgressTarget(float progress)
        {
            loreSlideView?.SetProgressTarget(progress);
        }

        public void StopSlideChanges()
        {
            loreSlideView?.StopSlideChanges();
        }

        private void OnAnyButtonPressed(InputControl control)
        {
            continueRequested = true;
            inputSubscription?.Dispose();
            inputSubscription = null;
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            if (canvasGroup == null)
                yield break;
            float start = canvasGroup.alpha;
            float elapsed = 0f;
            float effectiveDuration = Mathf.Max(1f / 60f, duration);
            while (elapsed < effectiveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / effectiveDuration));
                yield return null;
            }
            canvasGroup.alpha = target;
        }

        private void OnDestroy()
        {
            inputSubscription?.Dispose();
            inputSubscription = null;
        }
    }
}
