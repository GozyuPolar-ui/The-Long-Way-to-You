using System;
using UnityEngine;

namespace Prince
{
    [CreateAssetMenu(menuName = "Prince/Map Area", fileName = "MapArea")]
    public sealed class MapAreaDefinition : ScriptableObject
    {
        public string areaName = "Forest";
        [TextArea] public string areaDescription = "Wilayah ini menanti untuk dijelajahi.";
        public Vector2 centerXZ;
        public Vector2 sizeXZ = new Vector2(500f, 500f);
        [Min(1f)] public float cameraHeight = 350f;
        [Min(1f)] public float mapScale = 1f;
        public Color parchmentTint = new Color(0.67f, 0.53f, 0.34f, 1f);
        public Color accentColor = new Color(0.73f, 0.56f, 0.28f, 1f);
        public Texture2D parchmentTexture;
        public string[] pointsOfInterest = Array.Empty<string>();
    }

}
