using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class LoreSlideView : MonoBehaviour
    {
        [SerializeField] private RawImage firstImage;
        [SerializeField] private RawImage secondImage;
        [SerializeField] private CanvasGroup loreTextGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Image progressFill;
        [SerializeField] private GameObject progressRoot;
        [SerializeField] private TMP_Text continuePrompt;
        [SerializeField] private RectTransform dotsRoot;

        private LoadingLoreData data;
        private readonly List<Image> dots = new List<Image>();
        private int[] slideOrder = Array.Empty<int>();
        private Texture2D gradientTexture;
        private Sprite gradientSprite;
        private int currentSlide = -1;
        private int nextSlide = -1;
        private int activeImageIndex;
        private int orderPosition;
        private bool changingSlide;
        private bool slideChangesEnabled;
        private bool nextTextAssigned;
        private bool warnedAboutMissingImage;
        private float slideElapsed;
        private float slideDuration;
        private float transitionElapsed;
        private float transitionDuration;
        private float progressTarget;
        private float smoothedProgress;
        private float zoomElapsed;

        public float SmoothedProgress => smoothedProgress;
        public bool IsProgressSettled => smoothedProgress >= 0.999f;

        public void Configure(
            RawImage backgroundA,
            RawImage backgroundB,
            CanvasGroup textGroup,
            TMP_Text title,
            TMP_Text body,
            Image fill,
            GameObject progressContainer,
            TMP_Text prompt,
            RectTransform slideDots)
        {
            firstImage = backgroundA;
            secondImage = backgroundB;
            loreTextGroup = textGroup;
            titleText = title;
            bodyText = body;
            progressFill = fill;
            progressRoot = progressContainer;
            continuePrompt = prompt;
            dotsRoot = slideDots;
        }

        public void Begin(LoadingLoreData loreData)
        {
            data = loreData;
            progressTarget = 0f;
            smoothedProgress = 0f;
            if (progressFill != null)
                progressFill.fillAmount = 0f;
            if (progressRoot != null)
                progressRoot.SetActive(data.ShowProgressBar);
            if (continuePrompt != null)
                continuePrompt.gameObject.SetActive(false);

            BuildBottomGradient();
            BuildSlideOrder();
            ClearSlides();
            if (!data.UseLoreSlides || data.Entries.Count == 0)
                return;

            int first = NextSlideIndex(-1);
            SetSlideImmediate(first, 0);
            slideChangesEnabled = true;
            slideElapsed = 0f;
            zoomElapsed = 0f;
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            if (data != null)
                smoothedProgress = Mathf.MoveTowards(
                    smoothedProgress, progressTarget,
                    Mathf.Max(0.01f, data.ProgressUnitsPerSecond) * deltaTime);
            if (progressFill != null)
                progressFill.fillAmount = smoothedProgress;

            if (currentSlide < 0 || data == null || !data.UseLoreSlides)
                return;

            zoomElapsed += deltaTime;
            ApplyZoom(firstImage, activeImageIndex == 0);
            ApplyZoom(secondImage, activeImageIndex == 1);

            if (changingSlide)
            {
                UpdateSlideTransition(deltaTime);
                return;
            }

            if (slideChangesEnabled && data.Entries.Count > 1)
            {
                slideElapsed += deltaTime;
                if (slideElapsed >= slideDuration)
                    BeginNextSlide();
            }
        }

        public void SetProgressTarget(float value)
        {
            progressTarget = Mathf.Clamp01(value);
        }

        public void StopSlideChanges()
        {
            slideChangesEnabled = false;
        }

        public void ShowContinuePrompt(LoadingLoreLanguage language)
        {
            if (continuePrompt == null)
                return;
            continuePrompt.text = language == LoadingLoreLanguage.English
                ? "Press any button to continue"
                : "Tekan tombol apa saja untuk lanjut";
            continuePrompt.gameObject.SetActive(true);
        }

        public void SetLanguage(LoadingLoreLanguage language)
        {
            if (data == null)
                return;
            data.SetLanguage(language);
            if (currentSlide >= 0)
                SetText(data.Entries[currentSlide]);
        }

        private void BuildSlideOrder()
        {
            int count = data.Entries.Count;
            slideOrder = new int[count];
            for (int i = 0; i < count; i++)
                slideOrder[i] = i;
            orderPosition = count;
            if (data.RandomizeSlides)
                ShuffleOrder();
        }

        private void ShuffleOrder()
        {
            for (int i = slideOrder.Length - 1; i > 0; i--)
            {
                int other = UnityEngine.Random.Range(0, i + 1);
                (slideOrder[i], slideOrder[other]) = (slideOrder[other], slideOrder[i]);
            }
            if (slideOrder.Length > 1 && slideOrder[0] == currentSlide)
                (slideOrder[0], slideOrder[1]) = (slideOrder[1], slideOrder[0]);
            orderPosition = 0;
        }

        private int NextSlideIndex(int previous)
        {
            if (slideOrder.Length == 0)
                return -1;
            if (!data.RandomizeSlides)
                return previous < 0 ? 0 : (previous + 1) % slideOrder.Length;
            if (orderPosition >= slideOrder.Length)
                ShuffleOrder();
            return slideOrder[orderPosition++];
        }

        private void SetSlideImmediate(int index, int imageIndex)
        {
            if (index < 0 || index >= data.Entries.Count)
                return;
            currentSlide = index;
            activeImageIndex = imageIndex;
            LoreEntry entry = data.Entries[index];
            RawImage image = GetImage(imageIndex);
            RawImage other = GetImage(1 - imageIndex);
            if (image != null)
            {
                SetImageTexture(image, entry.Image);
                SetImageAlpha(image, 1f);
                image.rectTransform.localScale = Vector3.one;
            }
            WarnIfImageMissing(entry, index);
            if (other != null)
            {
                SetImageTexture(other, null);
                SetImageAlpha(other, 0f);
                other.rectTransform.localScale = Vector3.one;
            }

            slideDuration = Mathf.Max(0.1f, entry.DisplayDuration);
            SetText(entry);
            UpdateDots(index);
        }

        private void BeginNextSlide()
        {
            nextSlide = NextSlideIndex(currentSlide);
            if (nextSlide < 0 || nextSlide == currentSlide)
                return;

            int nextImageIndex = 1 - activeImageIndex;
            RawImage nextImage = GetImage(nextImageIndex);
            if (nextImage == null)
            {
                SetSlideImmediate(nextSlide, activeImageIndex);
                slideElapsed = 0f;
                return;
            }

            LoreEntry entry = data.Entries[nextSlide];
            SetImageTexture(nextImage, entry.Image);
            WarnIfImageMissing(entry, nextSlide);
            nextImage.rectTransform.localScale = Vector3.one;
            SetImageAlpha(nextImage, data.UseCrossFade ? 0f : 1f);
            changingSlide = data.UseCrossFade;
            nextTextAssigned = false;
            transitionElapsed = 0f;
            transitionDuration = EffectiveDuration(data.CrossFadeDuration);
            if (!data.UseCrossFade)
            {
                CompleteSlideTransition(nextImageIndex);
                return;
            }
            slideElapsed = 0f;
        }

        private void UpdateSlideTransition(float deltaTime)
        {
            transitionElapsed += deltaTime;
            float amount = Mathf.Clamp01(transitionElapsed / transitionDuration);
            RawImage currentImage = GetImage(activeImageIndex);
            RawImage nextImage = GetImage(1 - activeImageIndex);
            SetImageAlpha(currentImage, 1f - amount);
            SetImageAlpha(nextImage, amount);

            if (!nextTextAssigned && amount >= 0.5f)
            {
                SetText(data.Entries[nextSlide]);
                UpdateDots(nextSlide);
                nextTextAssigned = true;
            }
            if (loreTextGroup != null)
                loreTextGroup.alpha = amount < 0.5f ? 1f - amount * 2f : (amount - 0.5f) * 2f;

            if (amount >= 1f)
                CompleteSlideTransition(1 - activeImageIndex);
        }

        private void CompleteSlideTransition(int nextImageIndex)
        {
            RawImage oldImage = GetImage(activeImageIndex);
            if (oldImage != null)
            {
                oldImage.texture = null;
                SetImageAlpha(oldImage, 0f);
            }
            activeImageIndex = nextImageIndex;
            currentSlide = nextSlide;
            nextSlide = -1;
            LoreEntry entry = data.Entries[currentSlide];
            if (!nextTextAssigned)
            {
                SetText(entry);
                UpdateDots(currentSlide);
            }
            slideDuration = Mathf.Max(0.1f, entry.DisplayDuration);
            slideElapsed = 0f;
            zoomElapsed = 0f;
            changingSlide = false;
            nextTextAssigned = false;
            if (loreTextGroup != null)
                loreTextGroup.alpha = 1f;
        }

        private void SetText(LoreEntry entry)
        {
            if (titleText != null)
                titleText.text = entry.GetTitle(data.ActiveLanguage);
            if (bodyText != null)
                bodyText.text = entry.GetText(data.ActiveLanguage);
        }

        private void UpdateDots(int activeIndex)
        {
            for (int i = 0; i < dots.Count; i++)
                dots[i].color = i == activeIndex
                    ? new Color(0.82f, 0.65f, 0.38f, 1f)
                    : new Color(0.84f, 0.78f, 0.65f, 0.42f);
        }

        private void ApplyZoom(RawImage image, bool active)
        {
            if (image == null || image.texture == null || !active || !data.UseSlowZoom)
                return;
            float progress = Mathf.Clamp01(zoomElapsed / slideDuration);
            float scale = 1f + data.ZoomAmount * progress;
            image.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        private RawImage GetImage(int index) => index == 0 ? firstImage : secondImage;

        private void WarnIfImageMissing(LoreEntry entry, int index)
        {
            if (entry.Image != null || warnedAboutMissingImage)
                return;
            warnedAboutMissingImage = true;
            Debug.LogWarning(
                $"Loading lore entry {index + 1} has no image assigned; its slide will display without artwork.",
                this);
        }

        private void ClearSlides()
        {
            dots.Clear();
            if (dotsRoot != null)
                for (int i = dotsRoot.childCount - 1; i >= 0; i--)
                    Destroy(dotsRoot.GetChild(i).gameObject);

            SetImage(firstImage, null, 0f);
            SetImage(secondImage, null, 0f);
            currentSlide = -1;
            nextSlide = -1;
            changingSlide = false;

            if (data == null || !data.UseLoreSlides)
                return;

            int count = data.Entries.Count;
            if (dotsRoot == null)
                return;
            for (int i = 0; i < count; i++)
            {
                GameObject dotObject = new GameObject(
                    $"LoreDot_{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                dotObject.transform.SetParent(dotsRoot, false);
                RectTransform rect = dotObject.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(8f, 8f);
                Image dot = dotObject.GetComponent<Image>();
                dot.raycastTarget = false;
                dots.Add(dot);
            }
        }

        private static void SetImage(RawImage image, Texture texture, float alpha)
        {
            if (image == null)
                return;
            SetImageTexture(image, texture);
            SetImageAlpha(image, alpha);
        }

        private static void SetImageTexture(RawImage image, Texture texture)
        {
            if (image == null)
                return;
            image.texture = texture;
            if (texture == null)
                return;
            AspectRatioFitter fitter = image.GetComponent<AspectRatioFitter>();
            if (fitter != null && texture.height > 0)
                fitter.aspectRatio = (float)texture.width / texture.height;
        }

        private static void SetImageAlpha(RawImage image, float alpha)
        {
            if (image == null)
                return;
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private void BuildBottomGradient()
        {
            Transform gradientTransform = transform.Find("BottomGradient");
            Image gradient = gradientTransform != null ? gradientTransform.GetComponent<Image>() : null;
            if (gradient == null)
                return;

            const int textureHeight = 128;
            gradientTexture = new Texture2D(1, textureHeight, TextureFormat.RGBA32, false, true)
            {
                name = "Loading Lore Bottom Gradient",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            Color32[] pixels = new Color32[textureHeight];
            for (int y = 0; y < textureHeight; y++)
            {
                float t = y / (float)(textureHeight - 1);
                float alpha = 0.76f * (1f - t) * (1f - t);
                pixels[y] = new Color(0.025f, 0.028f, 0.03f, alpha);
            }
            gradientTexture.SetPixels32(pixels);
            gradientTexture.Apply(false, true);
            gradientSprite = Sprite.Create(
                gradientTexture, new Rect(0f, 0f, 1f, textureHeight),
                new Vector2(0.5f, 0.5f), textureHeight);
            gradientSprite.name = "Loading Lore Bottom Gradient";
            gradientSprite.hideFlags = HideFlags.HideAndDontSave;
            gradient.sprite = gradientSprite;
            gradient.type = Image.Type.Simple;
            gradient.raycastTarget = false;
        }

        private void OnDestroy()
        {
            if (gradientSprite != null)
                Destroy(gradientSprite);
            if (gradientTexture != null)
                Destroy(gradientTexture);
        }

        private static float EffectiveDuration(float duration) => Mathf.Max(1f / 60f, duration);
    }
}
