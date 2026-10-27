using UnityEngine;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class BackpackQuestPlacement : MonoBehaviour
    {
        [SerializeField] private bool hasInitialPlacement;
        [SerializeField] private Vector3 bramPosition;
        [SerializeField] private Vector3 backpackPosition;
        [SerializeField] private Vector3 guardAreaPosition;
        [SerializeField, Min(0f)] private float guardAreaRadius = 12f;

        public bool HasInitialPlacement
        {
            get => hasInitialPlacement;
            set => hasInitialPlacement = value;
        }

        public Vector3 BramPosition
        {
            get => bramPosition;
            set => bramPosition = value;
        }

        public Vector3 BackpackPosition
        {
            get => backpackPosition;
            set => backpackPosition = value;
        }

        public Vector3 GuardAreaPosition
        {
            get => guardAreaPosition;
            set => guardAreaPosition = value;
        }

        public float GuardAreaRadius
        {
            get => guardAreaRadius;
            set => guardAreaRadius = Mathf.Max(0f, value);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            UnityEditor.Handles.color = new Color(0.85f, 0.24f, 0.18f, 0.9f);
            Gizmos.color = new Color(0.85f, 0.24f, 0.18f, 0.75f);
            Gizmos.DrawWireSphere(guardAreaPosition, Mathf.Max(0f, guardAreaRadius));
            UnityEditor.Handles.Label(guardAreaPosition, "Area babi hutan (opsional)");
        }
#endif
    }
}
