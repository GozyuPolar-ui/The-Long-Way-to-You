using UnityEngine;

namespace Prince
{
    public sealed class NPCLookAtPlayer : MonoBehaviour
    {
        [SerializeField] private bool lookAtPlayer = true;
        [SerializeField, Min(0f)] private float radius = 5f;
        [SerializeField, Min(0f)] private float turnSpeed = 3f;

        private Transform player;
        private Quaternion initialRotation;
        private float nextPlayerSearch;

        private void Awake()
        {
            initialRotation = transform.rotation;
            FindPlayer();
        }

        private void Update()
        {
            if (!lookAtPlayer)
                return;
            if (player == null && Time.time >= nextPlayerSearch)
                FindPlayer();

            Vector3 direction = player != null ? player.position - transform.position : Vector3.zero;
            direction.y = 0f;
            bool shouldLook = player != null && direction.sqrMagnitude <= radius * radius;
            Quaternion target = shouldLook && direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : initialRotation;
            transform.rotation = Quaternion.Slerp(
                transform.rotation, target, Mathf.Clamp01(turnSpeed * Time.deltaTime));
        }

        private void FindPlayer()
        {
            nextPlayerSearch = Time.time + 2f;
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }
    }
}
