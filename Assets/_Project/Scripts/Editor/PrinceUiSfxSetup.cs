using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Prince;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Prince.Editor
{
    internal static class PrinceUiSfxSetup
    {
        private const string AudioFolder = "Assets/_Project/Audio/SFX/UI";
        private const string ResourcesFolder = "Assets/_Project/Resources";
        private const string SoundSetPath = ResourcesFolder + "/UISoundSet.asset";

        private sealed class SoundMapping
        {
            public UISoundType type;
            public string[] clipNames;
            public float volume;
            public float cooldown;
        }

        private static readonly SoundMapping[] Mappings =
        {
            Map(UISoundType.Hover, 0.35f, 0.06f, "tick_001", "tick_002"),
            Map(UISoundType.Click, 0.40f, 0.04f, "click_001", "click_004"),
            Map(UISoundType.Back, 0.35f, 0.06f, "close_001", "close_002"),
            Map(UISoundType.Open, 0.35f, 0.08f, "open_001", "open_002"),
            Map(UISoundType.Close, 0.35f, 0.08f, "close_001", "close_003"),
            Map(UISoundType.Toggle, 0.35f, 0.06f, "toggle_003", "toggle_004"),
            Map(UISoundType.SliderTick, 0.20f, 0.06f, "tick_001", "tick_002"),
            Map(UISoundType.DropdownOpen, 0.25f, 0.08f, "open_001", "open_002"),
            Map(UISoundType.DropdownSelect, 0.22f, 0.06f, "select_008", "select_007"),
            Map(UISoundType.Error, 0.17f, 0.12f, "error_008", "error_002"),
            Map(UISoundType.Confirmation, 0.16f, 0.08f, "confirmation_003", "confirmation_004"),
            Map(UISoundType.QuestUpdated, 0.10f, 0.20f, "confirmation_003", "confirmation_004"),
            Map(UISoundType.QuestCompleted, 0.15f, 0.20f, "confirmation_001", "confirmation_004"),
            Map(UISoundType.DialogueAdvance, 0.12f, 0.06f, "tick_001", "tick_002"),
            Map(UISoundType.DialogueChoice, 0.16f, 0.06f, "select_008", "select_007"),
            Map(UISoundType.MapOpen, 0.17f, 0.08f, "maximize_003", "maximize_001"),
            Map(UISoundType.MapClose, 0.17f, 0.08f, "minimize_003", "minimize_001"),
            Map(UISoundType.MapMarker, 0.25f, 0.06f, "click_001", "click_004"),
            Map(UISoundType.MapZoom, 0.06f, 0.12f, "scroll_001", "scroll_004"),
            Map(UISoundType.MapPan, 0.04f, 0.12f, "scroll_001", "scroll_004"),
            Map(UISoundType.Pickup, 0.30f, 0.08f, "pluck_001", "pluck_002")
        };

        [MenuItem("Tools/Prince/Setup UI Sounds")]
        private static void SetupMenu()
        {
            Setup();
        }

        private static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Setup UI Sounds cannot run in Play mode.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(AudioFolder))
            {
                Debug.LogError($"UI sound folder was not found at '{AudioFolder}'.");
                return;
            }

            EnsureFolder("Assets/_Project");
            EnsureFolder(ResourcesFolder);
            UISoundSet soundSet = AssetDatabase.LoadAssetAtPath<UISoundSet>(SoundSetPath);
            if (soundSet == null)
            {
                soundSet = ScriptableObject.CreateInstance<UISoundSet>();
                soundSet.name = "UISoundSet";
                AssetDatabase.CreateAsset(soundSet, SoundSetPath);
            }

            Dictionary<string, AudioClip> clips = LoadClips();
            int mappedTypes = 0;
            foreach (SoundMapping mapping in Mappings)
            {
                UISoundSet.Entry entry = soundSet.Get(mapping.type);
                bool isNewEntry = entry == null;
                if (isNewEntry)
                    entry = soundSet.GetOrCreate(mapping.type);

                AudioClip[] mappedClips = ResolveClips(mapping, clips);
                if (mappedClips.Length > 0)
                {
                    entry.clips = mappedClips;
                    mappedTypes++;
                }
                else if (entry.clips == null || entry.clips.Length == 0)
                {
                    Debug.LogWarning(
                        $"No audio file matched the UI sound mapping '{mapping.type}' ({string.Join(", ", mapping.clipNames)}).");
                }

                if (isNewEntry)
                {
                    entry.volume = mapping.volume;
                    entry.pitchRange = new Vector2(0.95f, 1.05f);
                    entry.cooldown = mapping.cooldown;
                }
            }

            if (soundSet.OutputGroup == null)
                soundSet.SetOutputGroup(FindMixerGroup());

            EditorUtility.SetDirty(soundSet);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"UI sound setup complete: configured {mappedTypes} sound types in '{SoundSetPath}'. " +
                "UISoundPlayer installs itself at runtime and adds UIButtonSound to Selectables in Main-Menu, UNS_Demo, and Closing.");
        }

        private static SoundMapping Map(
            UISoundType type,
            float volume,
            float cooldown,
            params string[] clipNames)
        {
            return new SoundMapping
            {
                type = type,
                volume = volume,
                cooldown = cooldown,
                clipNames = clipNames
            };
        }

        private static Dictionary<string, AudioClip> LoadClips()
        {
            var clips = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                    clips[Path.GetFileNameWithoutExtension(path)] = clip;
            }
            return clips;
        }

        private static AudioClip[] ResolveClips(
            SoundMapping mapping,
            Dictionary<string, AudioClip> clips)
        {
            var resolved = new List<AudioClip>();
            foreach (string clipName in mapping.clipNames)
                if (clips.TryGetValue(clipName, out AudioClip clip) && clip != null)
                    resolved.Add(clip);

            if (resolved.Count > 0)
                return resolved.ToArray();

            string prefix = mapping.clipNames[0].Substring(
                0, mapping.clipNames[0].LastIndexOf('_') + 1);
            float maximumDuration = mapping.type == UISoundType.MapZoom ||
                                    mapping.type == UISoundType.MapPan
                ? 1f
                : 0.5f;
            return clips
                .Where(pair => pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                               pair.Value != null && pair.Value.length <= maximumDuration)
                .OrderBy(pair => pair.Value.length)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .Select(pair => pair.Value)
                .ToArray();
        }

        private static AudioMixerGroup FindMixerGroup()
        {
            string[] mixerGuids = AssetDatabase.FindAssets("t:AudioMixer");
            foreach (string desiredName in new[] { "UI", "SFX", "Master" })
            {
                foreach (string guid in mixerGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
                    if (mixer == null)
                        continue;

                    foreach (AudioMixerGroup group in mixer.FindMatchingGroups(desiredName))
                        if (group != null &&
                            string.Equals(group.name, desiredName, StringComparison.OrdinalIgnoreCase))
                            return group;
                }
            }

            Debug.LogWarning(
                "No UI, SFX, or Master AudioMixer group was found. UI sounds will use AudioListener output and the existing Effects slider.");
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent))
                AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
