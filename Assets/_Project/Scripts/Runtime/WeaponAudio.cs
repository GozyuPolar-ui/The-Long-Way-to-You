using UnityEngine;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class WeaponAudio : MonoBehaviour
    {
        [Header("References")]
        public AudioSource audioSource;
        public AudioClip[] drawClips = System.Array.Empty<AudioClip>();
        public AudioClip[] sheathClips = System.Array.Empty<AudioClip>();
        public AudioClip[] swingClips = System.Array.Empty<AudioClip>();
        public AudioClip[] hitFleshClips = System.Array.Empty<AudioClip>();
        public AudioClip[] bearHitClips = System.Array.Empty<AudioClip>();
        public AudioClip[] hitHardClips = System.Array.Empty<AudioClip>();

        [Header("Volume")]
        [Range(0f, 1f)] public float drawVolume = 1f;
        [Range(0f, 1f)] public float sheathVolume = 1f;
        [Range(0f, 1f)] public float swingVolume = 0.8f;
        [Range(0f, 1f)] public float hitFleshVolume = 1f;
        [Range(0f, 1f)] public float bearHitVolume = 1f;
        [Range(0f, 1f)] public float hitHardVolume = 1f;
        [Range(0f, 1f)] public float volumeVariation = 0.05f;

        [Header("Variation")]
        public Vector2 pitchRange = new Vector2(0.95f, 1.05f);
        [Range(0f, 1f)] public float fallbackSheathPitch = 0.85f;
        [Range(0f, 1f)] public float fallbackSheathVolume = 0.7f;

        private AudioClip _lastDraw;
        private AudioClip _lastSheath;
        private AudioClip _lastSwing;
        private AudioClip _lastHitFlesh;
        private AudioClip _lastBearHit;
        private AudioClip _lastHitHard;
        private bool _warnedDraw;
        private bool _warnedSheath;
        private bool _warnedSwing;
        private bool _warnedHitFlesh;
        private bool _warnedBearHit;
        private bool _warnedHitHard;

        private void Awake()
        {
            EnsureAudioSource();
        }

        private void OnValidate()
        {
            pitchRange.x = Mathf.Max(0.01f, pitchRange.x);
            pitchRange.y = Mathf.Max(pitchRange.x, pitchRange.y);
            volumeVariation = Mathf.Clamp01(volumeVariation);
        }

        public void EnsureAudioSource()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
        }

        public void PlayDraw()
        {
            Play(drawClips, ref _lastDraw, drawVolume, 1f, ref _warnedDraw, "draw");
        }

        public void PlaySheath()
        {
            if (HasClips(sheathClips))
            {
                Play(sheathClips, ref _lastSheath, sheathVolume, 1f, ref _warnedSheath, "sheath");
                return;
            }

            if (HasClips(drawClips))
            {
                Play(drawClips, ref _lastDraw, sheathVolume * fallbackSheathVolume,
                    fallbackSheathPitch, ref _warnedSheath, "sheath fallback");
                return;
            }

            WarnEmpty(ref _warnedSheath, "sheath");
        }

        public void PlaySwing()
        {
            Play(swingClips, ref _lastSwing, swingVolume, 1f, ref _warnedSwing, "swing");
        }

        public void PlayHitFlesh()
        {
            Play(hitFleshClips, ref _lastHitFlesh, hitFleshVolume, 1f, ref _warnedHitFlesh, "flesh hit");
        }

        public void PlayHitBear()
        {
            Play(bearHitClips, ref _lastBearHit, bearHitVolume, 1f, ref _warnedBearHit, "bear hit");
        }

        public void PlayHitHard()
        {
            Play(hitHardClips, ref _lastHitHard, hitHardVolume, 1f, ref _warnedHitHard, "hard hit");
        }

        private void Play(
            AudioClip[] clips,
            ref AudioClip lastClip,
            float volume,
            float pitchMultiplier,
            ref bool warned,
            string category)
        {
            AudioClip clip = PickClip(clips, lastClip);
            if (clip == null)
            {
                WarnEmpty(ref warned, category);
                return;
            }

            EnsureAudioSource();
            lastClip = clip;
            Vector2 effectivePitchRange = pitchRange;
            CombatFeel combatFeel = GetComponentInParent<CombatFeel>();
            CombatFeelSettings settings = combatFeel != null ? combatFeel.Settings : null;
            if (settings != null && settings.audioPitchVariationEnabled)
            {
                float variation = Mathf.Clamp(settings.audioPitchVariation, 0f, 0.07f);
                effectivePitchRange = new Vector2(1f - variation, 1f + variation);
            }
            audioSource.pitch = Random.Range(effectivePitchRange.x, effectivePitchRange.y) * pitchMultiplier;
            float variedVolume = volume * Random.Range(1f - volumeVariation, 1f + volumeVariation);
            audioSource.PlayOneShot(clip, Mathf.Clamp01(variedVolume));
        }

        private void WarnEmpty(ref bool warned, string category)
        {
            if (warned)
                return;

            Debug.LogWarning(
                $"{nameof(WeaponAudio)} on '{name}' has no audio clips for {category}.",
                this);
            warned = true;
        }

        private static bool HasClips(AudioClip[] clips)
        {
            if (clips == null)
                return false;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                    return true;
            }
            return false;
        }

        private static AudioClip PickClip(AudioClip[] clips, AudioClip lastClip)
        {
            if (!HasClips(clips))
                return null;

            int availableCount = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null && (clips.Length < 2 || clips[i] != lastClip))
                    availableCount++;
            }
            if (availableCount == 0)
                return lastClip;

            int selected = Random.Range(0, availableCount);
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null || (clips.Length >= 2 && clips[i] == lastClip))
                    continue;
                if (selected-- == 0)
                    return clips[i];
            }

            return null;
        }
    }
}
