using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class MenuVideoBackground : MonoBehaviour
    {
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RawImage videoImage;
        [SerializeField] private CanvasGroup videoCanvasGroup;
        [SerializeField] private RawImage fallbackImage;
        [SerializeField] private Image gradientImage;
        [SerializeField] private AudioSource videoAudioSource;
        [SerializeField] private bool useVideoAudio;
        [SerializeField, Min(0f)] private float loopAudioFadeSeconds = 0.3f;
        [SerializeField, Min(0f)] private float videoFadeSeconds = 0.5f;
        [SerializeField, Min(0f)] private float audioFadeInSeconds = 1.5f;
        [SerializeField, Min(0f)] private float audioBaseVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float gradientAlpha = 0.6f;
        [SerializeField, Range(0.1f, 1f)] private float gradientWidth = 0.55f;
        [SerializeField] private Color gradientColor = new Color(0.09f, 0.14f, 0.19f, 1f);

        private Texture2D gradientTexture;
        private Sprite gradientSprite;
        private float audioEnvelope;
        private float settingsMusicVolume;
        private double previousVideoTime;
        private bool fadingForLoop;
        private bool prepared;
        private bool failed;

        public bool UseVideoAudio => useVideoAudio && videoPlayer != null && videoAudioSource != null && !failed;

        private void Awake()
        {
            settingsMusicVolume = MenuSettings.MusicVolume;
            if (videoCanvasGroup != null)
                videoCanvasGroup.alpha = 0f;
            if (fallbackImage != null)
                fallbackImage.enabled = true;

            BuildGradient();
            if (videoAudioSource != null)
            {
                videoAudioSource.playOnAwake = false;
                videoAudioSource.loop = false;
                videoAudioSource.spatialBlend = 0f;
                videoAudioSource.mute = !useVideoAudio;
                videoAudioSource.volume = 0f;
            }
        }

        private void Start()
        {
            if (videoPlayer == null || videoPlayer.clip == null || videoImage == null ||
                videoCanvasGroup == null)
            {
                FailToFallback("Menu video, clip, or display references are missing.");
                return;
            }

            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = true;
            videoPlayer.skipOnDrop = true;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.audioOutputMode = useVideoAudio
                ? VideoAudioOutputMode.AudioSource
                : VideoAudioOutputMode.None;
            videoPlayer.errorReceived += OnVideoError;
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnLoopPointReached;
            videoPlayer.Prepare();
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.errorReceived -= OnVideoError;
                videoPlayer.prepareCompleted -= OnVideoPrepared;
                videoPlayer.loopPointReached -= OnLoopPointReached;
            }

            if (gradientSprite != null)
                Destroy(gradientSprite);
            if (gradientTexture != null)
                Destroy(gradientTexture);
        }

        private void Update()
        {
            if (!prepared || failed || videoPlayer == null || !videoPlayer.isPlaying ||
                !UseVideoAudio || loopAudioFadeSeconds <= 0f || videoPlayer.length <= 0d)
                return;

            double currentTime = videoPlayer.time;
            if (currentTime + 0.05d < previousVideoTime)
                OnLoopPointReached(videoPlayer);
            previousVideoTime = currentTime;

            double remaining = videoPlayer.length - currentTime;
            if (!fadingForLoop && remaining <= loopAudioFadeSeconds)
            {
                fadingForLoop = true;
                StartCoroutine(FadeAudio(0f, Mathf.Max(0.01f, (float)remaining)));
            }
        }

        public void SetUseVideoAudio(bool enabled)
        {
            useVideoAudio = enabled;
            if (videoAudioSource != null)
                videoAudioSource.mute = !enabled;
            if (videoPlayer != null)
                videoPlayer.audioOutputMode = enabled
                    ? VideoAudioOutputMode.AudioSource
                    : VideoAudioOutputMode.None;
            if (!enabled)
                return;

            if (prepared && !failed && videoPlayer != null && videoPlayer.audioTrackCount > 0 &&
                videoAudioSource != null)
            {
                videoPlayer.SetTargetAudioSource(0, videoAudioSource);
                videoPlayer.EnableAudioTrack(0, true);
                StartCoroutine(FadeAudio(1f, audioFadeInSeconds));
            }
        }

        public void SetSettingsMusicVolume(float volume)
        {
            settingsMusicVolume = Mathf.Clamp01(volume);
            UpdateAudioVolume();
        }

        public void ConfigureGradient(Image image, float alpha, float width, Color color)
        {
            gradientImage = image;
            gradientAlpha = Mathf.Clamp01(alpha);
            gradientWidth = Mathf.Clamp(width, 0.1f, 1f);
            gradientColor = color;
            if (gradientSprite != null)
                Destroy(gradientSprite);
            if (gradientTexture != null)
                Destroy(gradientTexture);
            BuildGradient();
        }

        public void FadeAudioOut(float duration)
        {
            if (UseVideoAudio && videoAudioSource != null)
                StartCoroutine(FadeAudio(0f, duration));
        }

        private void BuildGradient()
        {
            if (gradientImage == null)
                return;

            const int width = 256;
            gradientTexture = new Texture2D(width, 1, TextureFormat.RGBA32, false, true)
            {
                name = "Main Menu Left Gradient",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            Color32[] pixels = new Color32[width];
            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                float alpha = gradientAlpha * (1f - t) * (1f - t);
                pixels[x] = new Color(
                    gradientColor.r, gradientColor.g, gradientColor.b, gradientColor.a * alpha);
            }
            gradientTexture.SetPixels32(pixels);
            gradientTexture.Apply(false, true);
            gradientSprite = Sprite.Create(
                gradientTexture, new Rect(0f, 0f, width, 1f),
                new Vector2(0.5f, 0.5f), width);
            gradientSprite.name = "Main Menu Left Gradient";
            gradientSprite.hideFlags = HideFlags.HideAndDontSave;
            gradientImage.sprite = gradientSprite;
            gradientImage.type = Image.Type.Simple;
            gradientImage.color = Color.white;
            gradientImage.raycastTarget = false;

            RectTransform rect = gradientImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(Mathf.Clamp(gradientWidth, 0.1f, 1f), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void OnVideoPrepared(VideoPlayer source)
        {
            if (source == null || source != videoPlayer)
                return;

            prepared = true;
            if (UseVideoAudio && source.audioTrackCount > 0 && videoAudioSource != null)
            {
                source.SetTargetAudioSource(0, videoAudioSource);
                source.EnableAudioTrack(0, true);
                videoAudioSource.mute = false;
            }
            else if (UseVideoAudio)
            {
                Debug.LogWarning(
                    $"Menu video '{source.clip.name}' has no usable embedded audio track; MenuMusic remains the menu audio source.",
                    this);
                SetUseVideoAudio(false);
            }

            source.Play();
            StartCoroutine(FadeCanvasGroup(videoCanvasGroup, 1f, videoFadeSeconds));
            if (fallbackImage != null)
                StartCoroutine(FadeFallbackOut());
            if (UseVideoAudio)
                StartCoroutine(FadeAudio(1f, audioFadeInSeconds));
        }

        private void OnVideoError(VideoPlayer source, string message)
        {
            if (source == null || source != videoPlayer)
                return;
            FailToFallback($"Menu video could not be played: {message}");
        }

        private void OnLoopPointReached(VideoPlayer source)
        {
            if (source == null || source != videoPlayer)
                return;
            fadingForLoop = false;
            if (UseVideoAudio)
                StartCoroutine(FadeAudio(1f, loopAudioFadeSeconds));
        }

        private void FailToFallback(string message)
        {
            if (failed)
                return;
            failed = true;
            prepared = false;
            if (videoPlayer != null && videoPlayer.isPlaying)
                videoPlayer.Stop();
            if (videoCanvasGroup != null)
                videoCanvasGroup.alpha = 0f;
            if (fallbackImage != null)
            {
                fallbackImage.enabled = true;
                Color fallbackColor = fallbackImage.color;
                fallbackColor.a = 1f;
                fallbackImage.color = fallbackColor;
            }
            if (videoAudioSource != null)
            {
                videoAudioSource.mute = true;
                videoAudioSource.Stop();
            }
            Debug.LogWarning($"{message} The main menu fallback background will remain visible.", this);
        }

        private IEnumerator FadeFallbackOut()
        {
            if (fallbackImage == null)
                yield break;
            Color start = fallbackImage.color;
            float elapsed = 0f;
            while (elapsed < videoFadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                Color color = start;
                color.a = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.01f, videoFadeSeconds));
                fallbackImage.color = color;
                yield return null;
            }
            fallbackImage.enabled = false;
        }

        private static IEnumerator FadeCanvasGroup(CanvasGroup group, float target, float duration)
        {
            if (group == null)
                yield break;
            float start = group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f);
                yield return null;
            }
            group.alpha = target;
        }

        private IEnumerator FadeAudio(float target, float duration)
        {
            if (videoAudioSource == null)
                yield break;
            float start = audioEnvelope;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                audioEnvelope = Mathf.Lerp(start, target, duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f);
                UpdateAudioVolume();
                yield return null;
            }
            audioEnvelope = target;
            UpdateAudioVolume();
        }

        private void UpdateAudioVolume()
        {
            if (videoAudioSource != null)
                videoAudioSource.volume = audioBaseVolume * settingsMusicVolume * audioEnvelope;
        }
    }
}
