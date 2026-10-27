using System.Collections.Generic;
using UnityEngine;

namespace Prince
{
    public static class MenuSettings
    {
        private const string MasterKey = "Prince.Audio.Master";
        private const string MusicKey = "Prince.Audio.Music";
        private const string EffectsKey = "Prince.Audio.Effects";
        private const string QualityKey = "Prince.Graphics.Quality";
        private const string FullscreenKey = "Prince.Display.Fullscreen";
        private const string SensitivityKey = "Prince.Camera.Sensitivity";
        private static readonly Dictionary<AudioSource, float> OriginalVolumes =
            new Dictionary<AudioSource, float>();

        public static float MasterVolume => PlayerPrefs.GetFloat(MasterKey, 0.8f);
        public static float MusicVolume => PlayerPrefs.GetFloat(MusicKey, 0.65f);
        public static float EffectsVolume => PlayerPrefs.GetFloat(EffectsKey, 0.8f);
        public static float CameraSensitivity => PlayerPrefs.GetFloat(SensitivityKey, 1f);
        public static int QualityIndex => PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
        public static bool Fullscreen => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        public static LoadingLoreLanguage Language => GameLocalization.CurrentLanguage;

        public static void SetLanguage(LoadingLoreLanguage language)
        {
            GameLocalization.SetLanguage(language);
        }

        public static void SetVolume(string channel, float value)
        {
            value = Mathf.Clamp01(value);
            string key;
            switch (channel)
            {
                case "Master":
                    key = MasterKey;
                    break;
                case "Music":
                    key = MusicKey;
                    break;
                case "Effects":
                    key = EffectsKey;
                    break;
                default:
                    Debug.LogWarning($"Unknown audio settings channel '{channel}'.");
                    return;
            }

            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
            ApplyAudio();
        }

        public static void SetQuality(int index)
        {
            if (QualitySettings.names.Length == 0)
            {
                Debug.LogWarning("No graphics quality levels are configured.");
                return;
            }
            int safeIndex = Mathf.Clamp(index, 0, QualitySettings.names.Length - 1);
            PlayerPrefs.SetInt(QualityKey, safeIndex);
            PlayerPrefs.Save();
            QualitySettings.SetQualityLevel(safeIndex, true);
        }

        public static void SetFullscreen(bool fullscreen)
        {
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
            Screen.fullScreen = fullscreen;
        }

        public static void SetCameraSensitivity(float sensitivity)
        {
            PlayerPrefs.SetFloat(SensitivityKey, Mathf.Clamp(sensitivity, 0.35f, 2.5f));
            PlayerPrefs.Save();
            ApplyCameraSensitivity();
        }

        public static void ApplyAll()
        {
            int quality = QualityIndex;
            if (QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(Mathf.Clamp(quality, 0, QualitySettings.names.Length - 1), true);
            Screen.fullScreen = Fullscreen;
            ApplyAudio();
            ApplyCameraSensitivity();
            SceneLoader.SetLanguage(Language);
        }

        public static void ApplyAudio()
        {
            AudioListener.volume = MasterVolume;

            AudioSource[] sources = Object.FindObjectsByType<AudioSource>();
            var activeSources = new HashSet<AudioSource>();
            foreach (AudioSource source in sources)
            {
                if (source == null)
                    continue;
                activeSources.Add(source);
                MenuMusicPlayer musicPlayer = source.GetComponent<MenuMusicPlayer>();
                if (musicPlayer != null)
                {
                    musicPlayer.SetMusicVolume(MusicVolume);
                    continue;
                }

                MenuVideoBackground videoBackground = source.GetComponentInParent<MenuVideoBackground>();
                if (videoBackground != null)
                {
                    videoBackground.SetSettingsMusicVolume(MusicVolume);
                    continue;
                }

                if (!OriginalVolumes.TryGetValue(source, out float baseVolume))
                {
                    baseVolume = source.volume;
                    OriginalVolumes.Add(source, baseVolume);
                }

                bool music =
                    source.gameObject.name.IndexOf("music", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    source.gameObject.name.IndexOf("bgm", System.StringComparison.OrdinalIgnoreCase) >= 0;
                source.volume = baseVolume * (music ? MusicVolume : EffectsVolume);
            }

            if (OriginalVolumes.Count != activeSources.Count)
            {
                var staleSources = new List<AudioSource>();
                foreach (AudioSource source in OriginalVolumes.Keys)
                    if (source == null || !activeSources.Contains(source))
                        staleSources.Add(source);
                foreach (AudioSource source in staleSources)
                    OriginalVolumes.Remove(source);
            }
        }

        private static void ApplyCameraSensitivity()
        {
            StarterAssets.StarterAssetsInputs inputs =
                Object.FindAnyObjectByType<StarterAssets.StarterAssetsInputs>();
            if (inputs == null)
                return;
            MenuCameraSensitivityAdapter adapter =
                inputs.GetComponent<MenuCameraSensitivityAdapter>();
            if (adapter == null)
                adapter = inputs.gameObject.AddComponent<MenuCameraSensitivityAdapter>();
            adapter.Sensitivity = CameraSensitivity;
        }
    }

    [DefaultExecutionOrder(-10)]
    public sealed class MenuCameraSensitivityAdapter : MonoBehaviour
    {
        public float Sensitivity { private get; set; } = 1f;
        private StarterAssets.StarterAssetsInputs inputs;
        private Vector2 previousOutput;
        private float previousSensitivity = 1f;
        private bool hasOutput;

        private void Awake()
        {
            inputs = GetComponent<StarterAssets.StarterAssetsInputs>();
        }

        private void Update()
        {
            if (inputs == null)
                return;

            Vector2 current = inputs.look;
            Vector2 rawInput = current;
            if (hasOutput && previousSensitivity > 0.001f &&
                (current - previousOutput).sqrMagnitude < 0.000001f)
                rawInput /= previousSensitivity;

            Vector2 scaledInput = rawInput * Sensitivity;
            inputs.look = scaledInput;
            previousOutput = scaledInput;
            previousSensitivity = Sensitivity;
            hasOutput = true;
        }
    }
}
