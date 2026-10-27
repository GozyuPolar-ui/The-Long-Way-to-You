using System.Collections.Generic;
using UnityEngine;

namespace Prince
{
    public sealed class WeaponHitbox : MonoBehaviour
    {
        public Collider hitboxCollider;
        public Transform ownerRoot;
        public WeaponAudio weaponAudio;
        [Tooltip("Assign layers used by hard surfaces such as stone or metal props.")]
        public LayerMask hardSurfaceLayers;
        [Min(0f)] public float damage = 20f;

        private readonly Collider[] _overlapHits = new Collider[32];
        private readonly HashSet<IDamageable> _damagedTargets = new HashSet<IDamageable>();
        private bool _isActive;
        private bool _playedHardHitThisSwing;
        private bool _warnedMissingCollider;
        private float _scheduledEndTime = -1f;

        private void Awake()
        {
            if (hitboxCollider == null)
                hitboxCollider = GetComponent<Collider>();
            if (ownerRoot == null)
                ownerRoot = transform.root;

            SetHitboxActive(false);
        }

        private void OnValidate()
        {
            damage = Mathf.Max(0f, damage);
        }

        private void FixedUpdate()
        {
            if (_scheduledEndTime >= 0f && Time.time >= _scheduledEndTime)
                SetHitboxActive(false);

            if (!_isActive || hitboxCollider == null || !hitboxCollider.enabled)
                return;

            Bounds bounds = hitboxCollider.bounds;
            Vector3 extents = bounds.extents + Vector3.one * 0.08f;
            int hitCount = Physics.OverlapBoxNonAlloc(
                bounds.center,
                extents,
                _overlapHits,
                Quaternion.identity,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                Collider other = _overlapHits[i];
                if (other != null && other != hitboxCollider)
                    ProcessCollider(other);
            }
        }

        public void OnAttackHitStart()
        {
            _scheduledEndTime = -1f;
            SetHitboxActive(true);
        }

        public void OnAttackHitEnd()
        {
            if (_isActive)
                _scheduledEndTime = Time.time + 0.18f;
        }

        public void SetHitboxActive(bool active)
        {
            _scheduledEndTime = -1f;
            bool startingNewSwing = active && !_isActive;
            _isActive = active;
            if (hitboxCollider == null)
            {
                if (active && !_warnedMissingCollider)
                {
                    Debug.LogWarning($"{nameof(WeaponHitbox)} on '{name}' has no collider.", this);
                    _warnedMissingCollider = true;
                }
                return;
            }

            if (startingNewSwing)
            {
                _damagedTargets.Clear();
                _playedHardHitThisSwing = false;
            }
            hitboxCollider.enabled = active;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive || other == null)
                return;

            ProcessCollider(other);
        }

        private void ProcessCollider(Collider other)
        {
            MonoBehaviour[] behaviours = other.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (!(behaviours[i] is IDamageable target) || IsOwnedByPlayer(behaviours[i].transform))
                    continue;
                if (!_damagedTargets.Add(target))
                    return;

                target.TakeDamage(
                    damage,
                    ColliderUtility.ClosestPoint(other, transform.position),
                    ownerRoot != null ? ownerRoot.gameObject : gameObject);
                if (weaponAudio != null)
                {
                    if (target is BearAI)
                        weaponAudio.PlayHitBear();
                    else
                        weaponAudio.PlayHitFlesh();
                }
                return;
            }

            int otherLayerMask = 1 << other.gameObject.layer;
            if (!_playedHardHitThisSwing && (hardSurfaceLayers.value & otherLayerMask) != 0)
            {
                _playedHardHitThisSwing = true;
                if (weaponAudio != null)
                    weaponAudio.PlayHitHard();
            }
        }

        private bool IsOwnedByPlayer(Transform target)
        {
            return ownerRoot != null && (target == ownerRoot || target.IsChildOf(ownerRoot));
        }
    }
}
