using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Prince
{
    public enum UISoundType
    {
        None,
        Click,
        Hover,
        Back,
        Open,
        Close,
        Toggle,
        SliderTick,
        DropdownOpen,
        DropdownSelect,
        Error,
        Confirmation,
        QuestUpdated,
        QuestCompleted,
        DialogueAdvance,
        DialogueChoice,
        MapOpen,
        MapClose,
        MapMarker,
        MapZoom,
        MapPan,
        Pickup
    }

    [CreateAssetMenu(menuName = "Prince/UI Sound Set")]
    public sealed class UISoundSet : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public UISoundType type;
            public AudioClip[] clips = Array.Empty<AudioClip>();
            [Range(0f, 1f)] public float volume = 0.3f;
            public Vector2 pitchRange = new Vector2(0.95f, 1.05f);
            [Min(0f)] public float cooldown = 0.06f;
        }

        [SerializeField] private List<Entry> sounds = new List<Entry>();
        [SerializeField] private AudioMixerGroup outputGroup;

        public IReadOnlyList<Entry> Sounds => sounds;
        public AudioMixerGroup OutputGroup => outputGroup;

        public Entry Get(UISoundType type)
        {
            if (sounds == null)
                return null;

            for (int i = 0; i < sounds.Count; i++)
                if (sounds[i] != null && sounds[i].type == type)
                    return sounds[i];
            return null;
        }

        public Entry GetOrCreate(UISoundType type)
        {
            if (sounds == null)
                sounds = new List<Entry>();

            Entry existing = Get(type);
            if (existing != null)
                return existing;

            var entry = new Entry { type = type };
            sounds.Add(entry);
            return entry;
        }

        public void SetOutputGroup(AudioMixerGroup group)
        {
            outputGroup = group;
        }
    }
}
