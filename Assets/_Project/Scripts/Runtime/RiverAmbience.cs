using UnityEngine;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class RiverAmbience : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField, Min(0f)] private float fadeInDistance = 8f;
        [SerializeField, Min(0.01f)] private float fadeOutDistance = 35f;
        [SerializeField, Range(0f, 1f)] private float maximumVolume = 0.85f;
        [SerializeField, Min(0f)] private float fadeSpeed = 1.5f;

        private PlayerHealth player;
        private float nextPlayerSearchTime;
        private float targetVolume;
        private bool warnedMissingPlayer;

        public void Configure(
            AudioSource source,
            float startDistance,
            float endDistance,
            float volume)
        {
            audioSource = source;
            fadeInDistance = Mathf.Max(0f, startDistance);
            fadeOutDistance = Mathf.Max(fadeInDistance + 0.01f, endDistance);
            maximumVolume = Mathf.Clamp01(volume);
        }

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError($"River ambience on '{name}' requires an AudioSource.", this);
                enabled = false;
                return;
            }

            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0f;
        }

        private void Start()
        {
            if (audioSource != null && audioSource.clip != null)
                audioSource.Play();
            else
                Debug.LogWarning($"River ambience on '{name}' has no audio clip assigned.", this);
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextPlayerSearchTime)
            {
                nextPlayerSearchTime = Time.unscaledTime + 1f;
                player = FindFirstObjectByType<PlayerHealth>();
                if (player == null && !warnedMissingPlayer)
                {
                    Debug.LogWarning($"River ambience on '{name}' cannot find a PlayerHealth listener.", this);
                    warnedMissingPlayer = true;
                }
                else if (player != null)
                {
                    warnedMissingPlayer = false;
                }
            }

            targetVolume = 0f;
            if (player != null && !player.IsDefeated)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                float fade = 1f - Mathf.InverseLerp(fadeInDistance, fadeOutDistance, distance);
                targetVolume = maximumVolume * fade * fade;
            }

            audioSource.volume = Mathf.MoveTowards(
                audioSource.volume, targetVolume, fadeSpeed * Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            if (audioSource != null)
            {
                audioSource.volume = 0f;
                audioSource.Stop();
            }
        }
    }
}
