using UnityEngine;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class AttackLunge : MonoBehaviour
    {
        private readonly RaycastHit[] groundHits = new RaycastHit[12];
        [SerializeField] private CombatFeelSettings settings;
        [SerializeField] private CharacterController characterController;
        [SerializeField, Min(0f)] private float groundProbeDistance = 1.5f;

        private float remainingDistance;
        private float remainingDuration;
        private bool active;

        private void Awake()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            if (settings != null)
                groundProbeDistance = settings.lungeGroundCheckDistance;
        }

        private void Update()
        {
            if (!active || characterController == null || !characterController.enabled)
                return;

            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0f || remainingDuration <= 0f)
            {
                StopLunge();
                return;
            }

            float distanceThisFrame = Mathf.Min(
                remainingDistance, remainingDistance * deltaTime / remainingDuration);
            Vector3 direction = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 displacement = direction * distanceThisFrame;
            if (HasGroundAhead(transform.position + displacement))
                characterController.Move(displacement);
            else
                StopLunge();

            remainingDistance = Mathf.Max(0f, remainingDistance - distanceThisFrame);
            remainingDuration = Mathf.Max(0f, remainingDuration - deltaTime);
            if (remainingDistance <= 0f || remainingDuration <= 0f)
                StopLunge();
        }

        public void Begin(float distance, float duration)
        {
            if (settings == null || !settings.attackLungeEnabled || distance <= 0f || duration <= 0f)
            {
                StopLunge();
                return;
            }

            remainingDistance = distance;
            remainingDuration = Mathf.Max(duration, Time.unscaledDeltaTime);
            active = true;
        }

        private bool HasGroundAhead(Vector3 position)
        {
            float originHeight = characterController.height + 0.15f;
            Vector3 origin = position + Vector3.up * originHeight;
            float rayDistance = originHeight + Mathf.Max(0.1f, groundProbeDistance);
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                rayDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = groundHits[i].collider;
                if (hitCollider == null || hitCollider.transform == transform ||
                    hitCollider.transform.IsChildOf(transform) ||
                    Vector3.Dot(groundHits[i].normal, Vector3.up) < 0.5f)
                    continue;
                return true;
            }

            return false;
        }

        public void StopLunge()
        {
            active = false;
            remainingDistance = 0f;
            remainingDuration = 0f;
        }
    }
}
