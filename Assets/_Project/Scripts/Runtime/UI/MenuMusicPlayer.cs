using System.Collections;
using UnityEngine;

namespace Prince
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class MenuMusicPlayer : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float baseVolume = 0.48f;
        [SerializeField, Min(0f)] private float fadeInDuration = 1.5f;

        private AudioSource musicSource;
        private float settingsVolume;
        private float fadeEnvelope;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            musicSource = GetComponent<AudioSource>();
            settingsVolume = MenuSettings.MusicVolume;
            fadeEnvelope = 0f;

            if (musicSource == null)
            {
                Debug.LogWarning("MenuMusicPlayer has no AudioSource.", this);
                enabled = false;
                return;
            }

            musicSource.loop = true;
            musicSource.playOnAwake = true;
            musicSource.mute = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0f;

            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (listeners.Length == 0)
                Debug.LogWarning("Menu BGM has no AudioListener and will be inaudible.", this);
            else if (listeners.Length > 1)
                Debug.LogWarning($"Menu BGM found {listeners.Length} AudioListeners; only one should be enabled.", this);
        }

        private IEnumerator Start()
        {
            if (musicSource.clip == null)
            {
                Debug.LogWarning("MenuMusic AudioSource has no AudioClip assigned.", this);
                enabled = false;
                yield break;
            }

            if (!musicSource.isPlaying)
                musicSource.Play();
            while (musicSource.clip.loadState == AudioDataLoadState.Loading)
                yield return null;
            if (musicSource.clip.loadState == AudioDataLoadState.Failed)
            {
                Debug.LogWarning("Menu BGM audio data failed to load.", this);
                enabled = false;
                yield break;
            }

            FadeTo(1f, fadeInDuration);
        }

        public void SetMusicVolume(float volume)
        {
            settingsVolume = Mathf.Clamp01(volume);
            ApplyVolume();
        }

        public void FadeOut(float duration)
        {
            FadeTo(0f, duration);
        }

        private void FadeTo(float target, float duration)
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine(target, duration));
        }

        private IEnumerator FadeRoutine(float target, float duration)
        {
            float start = fadeEnvelope;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeEnvelope = Mathf.Lerp(start, target,
                    duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f);
                ApplyVolume();
                yield return null;
            }

            fadeEnvelope = target;
            ApplyVolume();
            fadeRoutine = null;
        }

        private void ApplyVolume()
        {
            if (musicSource != null)
                musicSource.volume = baseVolume * settingsVolume * fadeEnvelope;
        }
    }
}
