using UnityEngine;
using UnityEngine.Serialization;

namespace Prince
{
    public enum MapQuality
    {
        Low,
        Medium,
        High
    }

    [CreateAssetMenu(menuName = "Prince/Map Settings", fileName = "MapSettings")]
    public sealed class MapSettings : ScriptableObject
    {
        [Header("Quality")]
        public MapQuality quality = MapQuality.Medium;
        [Min(128)] public int minimapResolution = 768;
        [Min(256)] public int fullMapResolution = 2048;
        [Min(32f)] public float renderDistance = 500f;

        [Header("Minimap")]
        public bool rotateMinimap = true;
        [Range(160f, 440f)] public float minimapSize = 240f;
        [Range(0.25f, 1f)] public float minimapOpacity = 0.9f;
        [Range(0f, 64f)] public float minimapEdgeMargin = 24f;
        [Min(1f)] public float defaultZoom = 40f;
        [Min(1f)] public float minimumZoom = 30f;
        [Min(1f)] public float maximumZoom = 150f;
        [Min(0f)] public float zoomStep = 8f;

        [Header("Features")]
        public bool fogOfWar = true;
        [FormerlySerializedAs("showTrees")] public bool showTreesOnMap;
        public bool showCompassTape = true;
        public bool lockInputOnFullMap = true;
        [Range(0f, 1f)] public float mapTintStrength = 0.3f;
        [Range(0f, 0.5f)] public float mapVignetteStrength = 0.2f;
        [Min(0.01f)] public float revealRadius = 40f;
        [Min(0.02f)] public float revealInterval = 0.2f;
        [Range(64, 512)] public int revealResolution = 256;

        [Header("Optional UI")]
        public AudioClip openMapSound;
        public AudioClip closeMapSound;
    }
}
