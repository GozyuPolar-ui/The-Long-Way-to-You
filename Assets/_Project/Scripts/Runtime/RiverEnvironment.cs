using UnityEngine;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class RiverEnvironment : MonoBehaviour
    {
        [SerializeField] private Vector3 riverCenter;
        [SerializeField] private Vector3 flowDirection = Vector3.right;
        [SerializeField, Min(1f)] private float wallLength = 40f;
        [SerializeField, Min(0f)] private float bankHalfWidth = 6f;
        [SerializeField, Min(1f)] private float wallHeight = 14f;
        [SerializeField, Min(0f)] private float soundFadeInDistance = 8f;
        [SerializeField, Min(0.01f)] private float soundFadeOutDistance = 35f;
        [SerializeField, Range(0f, 1f)] private float soundMaximumVolume = 0.85f;

        public Vector3 RiverCenter => riverCenter;
        public Vector3 FlowDirection => flowDirection.sqrMagnitude > 0.001f
            ? flowDirection.normalized
            : Vector3.right;
        public float WallLength => Mathf.Max(1f, wallLength);
        public float BankHalfWidth => Mathf.Max(0f, bankHalfWidth);
        public float WallHeight => Mathf.Max(1f, wallHeight);
        public float SoundFadeInDistance => Mathf.Max(0f, soundFadeInDistance);
        public float SoundFadeOutDistance => Mathf.Max(soundFadeInDistance + 0.01f, soundFadeOutDistance);
        public float SoundMaximumVolume => Mathf.Clamp01(soundMaximumVolume);

        public void Configure(
            Vector3 center,
            Vector3 direction,
            float length,
            float halfWidth,
            float height,
            float fadeInDistance,
            float fadeOutDistance,
            float maximumVolume)
        {
            riverCenter = center;
            direction.y = 0f;
            flowDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.right;
            wallLength = Mathf.Max(1f, length);
            bankHalfWidth = Mathf.Max(0f, halfWidth);
            wallHeight = Mathf.Max(1f, height);
            soundFadeInDistance = Mathf.Max(0f, fadeInDistance);
            soundFadeOutDistance = Mathf.Max(soundFadeInDistance + 0.01f, fadeOutDistance);
            soundMaximumVolume = Mathf.Clamp01(maximumVolume);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.15f, 0.85f, 1f, 0.7f);
            Gizmos.DrawWireSphere(riverCenter, SoundFadeOutDistance);
            DrawWallGizmo("Island_InvisibleWall_West");
            DrawWallGizmo("Island_InvisibleWall_East");
            DrawWallGizmo("Island_InvisibleWall_North");
            DrawWallGizmo("Island_InvisibleWall_South");
        }

        private void DrawWallGizmo(string wallName)
        {
            Transform wall = transform.Find(wallName);
            if (wall == null)
                return;

            BoxCollider wallCollider = wall.GetComponent<BoxCollider>();
            if (wallCollider != null && wallCollider.enabled)
                Gizmos.DrawWireCube(wallCollider.bounds.center, wallCollider.bounds.size);
        }
    }
}
