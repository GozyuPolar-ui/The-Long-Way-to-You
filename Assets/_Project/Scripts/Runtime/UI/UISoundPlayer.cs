using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Prince
{
    [DefaultExecutionOrder(-1100)]
    public sealed class UISoundPlayer : MonoBehaviour
    {
        private const string SoundSetResource = "UISoundSet";
        private const int ChannelCount = 8;
        private static readonly HashSet<string> SupportedSceneNames = new HashSet<string>
        {
            "Main-Menu",
            "UNS_Demo",
            "Closing"
        };

        private static UISoundPlayer instance;
        private readonly AudioSource[] channels = new AudioSource[ChannelCount];
        private readonly float[] channelAvailableAt = new float[ChannelCount];
        private readonly Dictionary<UISoundType, float> nextPlayTime =
            new Dictionary<UISoundType, float>();
        private readonly HashSet<UISoundType> missingSoundWarnings =
            new HashSet<UISoundType>();
        private UISoundSet soundSet;
        private QuestManager subscribedQuestManager;
        private int nextChannel;
        private bool missingSoundSetReported;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            UISoundPlayer existing = FindAnyObjectByType<UISoundPlayer>();
            if (existing != null)
            {
                instance = existing;
                return;
            }

            var playerObject = new GameObject("Prince UI Sound Player");
            playerObject.AddComponent<UISoundPlayer>();
        }

        public static void Play(UISoundType type)
        {
            if (type == UISoundType.None)
                return;

            if (instance == null)
                Bootstrap();
            if (instance != null)
                instance.PlayInternal(type);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            soundSet = Resources.Load<UISoundSet>(SoundSetResource);
            if (soundSet == null)
            {
                missingSoundSetReported = true;
                Debug.LogError(
                    $"UI sound set was not found at Resources/{SoundSetResource}. Run Tools > Prince > Setup UI Sounds.");
            }

            for (int i = 0; i < channels.Length; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.dopplerLevel = 0f;
                source.priority = 64;
                source.outputAudioMixerGroup = soundSet != null ? soundSet.OutputGroup : null;
                channels[i] = source;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            SubscribeToQuestManager();
            AttachControls(SceneManager.GetActiveScene());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (subscribedQuestManager != null)
            {
                subscribedQuestManager.OnQuestChanged -= OnQuestChanged;
                subscribedQuestManager.OnQuestCompleted -= OnQuestCompleted;
            }

            if (instance == this)
                instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AttachControls(scene);
            SubscribeToQuestManager();
        }

        private void AttachControls(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || !SupportedSceneNames.Contains(scene.name))
                return;

            Selectable[] controls =
                FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Selectable control in controls)
            {
                if (control != null && control.gameObject.scene == scene)
                    UIButtonSound.Attach(control.gameObject);
            }
        }

        private void SubscribeToQuestManager()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager == subscribedQuestManager)
                return;

            if (subscribedQuestManager != null)
            {
                subscribedQuestManager.OnQuestChanged -= OnQuestChanged;
                subscribedQuestManager.OnQuestCompleted -= OnQuestCompleted;
            }

            subscribedQuestManager = manager;
            if (subscribedQuestManager != null)
            {
                subscribedQuestManager.OnQuestChanged += OnQuestChanged;
                subscribedQuestManager.OnQuestCompleted += OnQuestCompleted;
            }
        }

        private void OnQuestChanged(QuestDefinition definition, QuestProgress progress)
        {
            if (progress != null && progress.state != QuestState.Completed)
                PlayInternal(UISoundType.QuestUpdated);
        }

        private void OnQuestCompleted(QuestDefinition definition, QuestProgress progress)
        {
            PlayInternal(UISoundType.QuestCompleted);
        }

        private void PlayInternal(UISoundType type)
        {
            if (soundSet == null)
            {
                if (!missingSoundSetReported)
                {
                    missingSoundSetReported = true;
                    Debug.LogError(
                        $"UI sound set was not found at Resources/{SoundSetResource}. Run Tools > Prince > Setup UI Sounds.",
                        this);
                }
                return;
            }

            UISoundSet.Entry entry = soundSet.Get(type);
            if (entry == null || entry.clips == null || entry.clips.Length == 0)
            {
                if (missingSoundWarnings.Add(type))
                    Debug.LogWarning($"No clips are configured for UI sound '{type}'.", soundSet);
                return;
            }

            float now = Time.unscaledTime;
            if (nextPlayTime.TryGetValue(type, out float allowedAt) && now < allowedAt)
                return;
            nextPlayTime[type] = now + Mathf.Max(0f, entry.cooldown);

            AudioClip clip = entry.clips[Random.Range(0, entry.clips.Length)];
            if (clip == null)
            {
                if (missingSoundWarnings.Add(type))
                    Debug.LogWarning($"The UI sound '{type}' contains a missing AudioClip.", soundSet);
                return;
            }

            int channelIndex = GetAvailableChannel(now);
            AudioSource source = channels[channelIndex];
            float minimumPitch = Mathf.Min(entry.pitchRange.x, entry.pitchRange.y);
            float maximumPitch = Mathf.Max(entry.pitchRange.x, entry.pitchRange.y);
            source.pitch = Random.Range(minimumPitch, maximumPitch);
            source.outputAudioMixerGroup = soundSet.OutputGroup;
            source.PlayOneShot(clip, Mathf.Clamp01(entry.volume));
            channelAvailableAt[channelIndex] = now + clip.length / Mathf.Max(0.01f, source.pitch);
        }

        private int GetAvailableChannel(float now)
        {
            for (int offset = 0; offset < channels.Length; offset++)
            {
                int index = (nextChannel + offset) % channels.Length;
                if (!channels[index].isPlaying || now >= channelAvailableAt[index])
                {
                    nextChannel = (index + 1) % channels.Length;
                    return index;
                }
            }

            int oldestChannel = 0;
            for (int i = 1; i < channelAvailableAt.Length; i++)
                if (channelAvailableAt[i] < channelAvailableAt[oldestChannel])
                    oldestChannel = i;

            channels[oldestChannel].Stop();
            nextChannel = (oldestChannel + 1) % channels.Length;
            return oldestChannel;
        }
    }
}
