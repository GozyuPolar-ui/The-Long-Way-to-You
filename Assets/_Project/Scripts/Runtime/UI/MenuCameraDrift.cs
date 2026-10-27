using UnityEngine;

namespace Prince
{
    public sealed class MenuCameraDrift : MonoBehaviour
    {
        [SerializeField] private Transform orbitTarget;
        [SerializeField, Min(0f)] private float degreesPerSecond = 0.65f;
        [SerializeField] private float orbitAngle = 2.5f;
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        public void Configure(Transform target, float speed, float angle)
        {
            orbitTarget = target;
            degreesPerSecond = Mathf.Max(0f, speed);
            orbitAngle = angle;
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private void Awake()
        {
            if (orbitTarget == null)
                return;
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private void Update()
        {
            if (orbitTarget == null)
                return;
            float angle = Mathf.Sin(Time.unscaledTime * degreesPerSecond * Mathf.Deg2Rad) * orbitAngle;
            Vector3 offset = initialPosition - orbitTarget.position;
            transform.position = orbitTarget.position + Quaternion.Euler(0f, angle, 0f) * offset;
            transform.rotation = Quaternion.LookRotation(orbitTarget.position - transform.position, Vector3.up);
        }
    }
}
