using UnityEngine;

namespace Prince
{
    public sealed class MapRevealState : MonoBehaviour
    {
        [SerializeField, Range(64, 512)] private int resolution = 256;
        [SerializeField] private bool revealed;
        [SerializeField] private byte[] explored = new byte[256 * 256];

        private Texture2D revealTexture;

        public Texture2D RevealTexture
        {
            get
            {
                EnsureTexture();
                return revealTexture;
            }
        }

        public void Configure(int textureResolution)
        {
            int clampedResolution = Mathf.Clamp(textureResolution, 64, 512);
            if (resolution == clampedResolution && explored != null &&
                explored.Length == resolution * resolution)
                return;

            resolution = clampedResolution;
            explored = new byte[resolution * resolution];
            revealed = false;
            RecreateTexture();
        }

        public void RevealAll()
        {
            EnsureData();
            for (int i = 0; i < explored.Length; i++)
                explored[i] = byte.MaxValue;
            revealed = true;
            UploadTexture();
        }

        public bool IsPositionRevealed(Vector2 worldPosition, MapAreaDefinition area)
        {
            if (area == null)
                return false;
            EnsureData();
            float u = (worldPosition.x - (area.centerXZ.x - area.sizeXZ.x * 0.5f)) /
                Mathf.Max(0.001f, area.sizeXZ.x);
            float v = (worldPosition.y - (area.centerXZ.y - area.sizeXZ.y * 0.5f)) /
                Mathf.Max(0.001f, area.sizeXZ.y);
            if (u < 0f || u > 1f || v < 0f || v > 1f)
                return false;

            int x = Mathf.Clamp(Mathf.RoundToInt(u * (resolution - 1)), 0, resolution - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(v * (resolution - 1)), 0, resolution - 1);
            return explored[y * resolution + x] > 96;
        }

        public void Reveal(Vector2 worldPosition, float radius, MapAreaDefinition area)
        {
            if (area == null || radius <= 0f || revealed)
                return;

            EnsureData();
            float width = Mathf.Max(1f, area.sizeXZ.x);
            float height = Mathf.Max(1f, area.sizeXZ.y);
            float centerU = (worldPosition.x - (area.centerXZ.x - width * 0.5f)) / width;
            float centerV = (worldPosition.y - (area.centerXZ.y - height * 0.5f)) / height;
            float radiusU = radius / width;
            float radiusV = radius / height;
            int minX = Mathf.Clamp(Mathf.FloorToInt((centerU - radiusU) * resolution), 0, resolution - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt((centerU + radiusU) * resolution), 0, resolution - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt((centerV - radiusV) * resolution), 0, resolution - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt((centerV + radiusV) * resolution), 0, resolution - 1);
            bool changed = false;

            for (int y = minY; y <= maxY; y++)
            {
                float worldZ = area.centerXZ.y - height * 0.5f + (y + 0.5f) / resolution * height;
                for (int x = minX; x <= maxX; x++)
                {
                    float worldX = area.centerXZ.x - width * 0.5f + (x + 0.5f) / resolution * width;
                    float distance = Vector2.Distance(worldPosition, new Vector2(worldX, worldZ));
                    if (distance >= radius)
                        continue;

                    float amount = Mathf.SmoothStep(0f, 1f, 1f - distance / radius);
                    byte value = (byte)Mathf.RoundToInt(amount * 255f);
                    int index = y * resolution + x;
                    if (value > explored[index])
                    {
                        explored[index] = value;
                        changed = true;
                    }
                }
            }

            if (changed)
                UploadTexture();
        }

        private void OnEnable()
        {
            EnsureTexture();
        }

        private void OnDisable()
        {
            if (revealTexture == null)
                return;
            if (Application.isPlaying)
                Destroy(revealTexture);
            else
                DestroyImmediate(revealTexture);
            revealTexture = null;
        }

        private void EnsureTexture()
        {
            EnsureData();
            if (revealTexture == null)
                RecreateTexture();
        }

        private void EnsureData()
        {
            int expectedLength = resolution * resolution;
            if (explored == null || explored.Length != expectedLength)
                explored = new byte[expectedLength];
        }

        private void RecreateTexture()
        {
            if (revealTexture != null)
            {
                if (Application.isPlaying)
                    Destroy(revealTexture);
                else
                    DestroyImmediate(revealTexture);
            }

            revealTexture = new Texture2D(resolution, resolution, TextureFormat.R8, false, true)
            {
                name = "Map Reveal",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            UploadTexture();
        }

        private void UploadTexture()
        {
            if (revealTexture == null)
                return;

            revealTexture.SetPixelData(explored, 0);
            revealTexture.Apply(false, false);
        }
    }
}
