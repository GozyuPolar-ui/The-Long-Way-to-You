using System.Collections.Generic;
using UnityEngine;

namespace Prince
{
    public enum MapMarkerType
    {
        Quest,
        NPC,
        Pickup,
        Campfire,
        Enemy,
        Waypoint,
        PlayerPing,
        PointOfInterest
    }

    public sealed class MapMarker : MonoBehaviour
    {
        public MapMarkerType type = MapMarkerType.PointOfInterest;
        public string displayName;
        public Sprite icon;
        public Color color = Color.white;
        public bool visibleThroughFog;
        [HideInInspector] public bool createdByMapSetup;

        private static readonly List<MapMarker> ActiveMarkers = new List<MapMarker>();
        private QuestTarget questTarget;

        public static IReadOnlyList<MapMarker> Active => ActiveMarkers;
        public string DisplayName => !string.IsNullOrWhiteSpace(displayName)
            ? displayName
            : questTarget != null && !string.IsNullOrWhiteSpace(questTarget.displayName)
                ? questTarget.displayName
                : name;
        public string TargetId => questTarget != null ? questTarget.targetId : string.Empty;

        private void Awake()
        {
            questTarget = GetComponent<QuestTarget>();
            if (questTarget == null)
                questTarget = GetComponentInParent<QuestTarget>();
        }

        private void OnEnable()
        {
            if (!ActiveMarkers.Contains(this))
                ActiveMarkers.Add(this);
        }

        private void OnDisable()
        {
            ActiveMarkers.Remove(this);
        }

        private void OnDestroy()
        {
            ActiveMarkers.Remove(this);
        }
    }
}
