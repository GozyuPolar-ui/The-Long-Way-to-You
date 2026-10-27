using UnityEngine;
using UnityEngine.Events;

namespace Prince
{
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Min(1f)] public float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        public UnityEvent<float> OnHit = new UnityEvent<float>();
        public UnityEvent OnDefeated = new UnityEvent();
        public UnityEvent OnKnockedOut = new UnityEvent();
        public Animator animator;

        public float CurrentHealth => currentHealth;
        public bool IsDefeated { get; private set; }

        private bool _warnedMissingHitParameter;
        private bool _warnedMissingAnimator;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            IsDefeated = currentHealth <= 0f;
        }

        public void TakeDamage(float amount, Vector3 hitPoint, GameObject source)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f)
            {
                Debug.LogWarning($"{nameof(PlayerHealth)} ignored invalid damage amount '{amount}'.", this);
                return;
            }
            if (IsDefeated)
                return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            if (OnHit != null)
                OnHit.Invoke(amount);
            PlayHitReaction();

            if (currentHealth > 0f)
                return;

            IsDefeated = true;
            if (OnDefeated != null)
                OnDefeated.Invoke();
            if (OnKnockedOut != null)
                OnKnockedOut.Invoke();
        }

        [ContextMenu("Debug/Take Test Damage")]
        private void TakeTestDamage()
        {
            TakeDamage(Mathf.Min(10f, currentHealth), transform.position, gameObject);
        }

        private void PlayHitReaction()
        {
            if (animator == null)
            {
                if (!_warnedMissingAnimator)
                {
                    Debug.LogWarning($"{nameof(PlayerHealth)} on '{name}' has no Animator for hit reactions.", this);
                    _warnedMissingAnimator = true;
                }
                return;
            }

            for (int i = 0; i < animator.parameters.Length; i++)
            {
                if (animator.parameters[i].nameHash != Animator.StringToHash("Hit") ||
                    animator.parameters[i].type != AnimatorControllerParameterType.Trigger)
                    continue;

                animator.SetTrigger("Hit");
                return;
            }

            if (!_warnedMissingHitParameter)
            {
                Debug.LogWarning($"Animator on '{animator.name}' has no trigger parameter named 'Hit'.", animator);
                _warnedMissingHitParameter = true;
            }
        }
    }
}
