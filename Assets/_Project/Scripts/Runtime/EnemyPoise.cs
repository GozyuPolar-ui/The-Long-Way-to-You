using UnityEngine;
using UnityEngine.AI;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class EnemyPoise : MonoBehaviour
    {
        [SerializeField] private CombatFeelSettings settings;
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private NavMeshAgent agent;

        private float accumulatedPoise;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            if (agent == null)
                agent = GetComponent<NavMeshAgent>();
        }

        public void ApplyHit(
            CombatHitStopTier tier,
            float strength,
            Vector3 sourcePosition,
            CombatFeelSettings feelSettings)
        {
            if (feelSettings == null || !feelSettings.enemyPoiseEnabled || strength <= 0f)
                return;

            settings = feelSettings;
            float tierPoise = tier == CombatHitStopTier.Finisher
                ? feelSettings.finisherPoiseThreshold
                : tier == CombatHitStopTier.Medium
                    ? feelSettings.mediumPoiseThreshold
                    : feelSettings.mediumPoiseThreshold * 0.35f;
            float bearScale = GetComponent<BearAI>() != null ? feelSettings.bearPoiseMultiplier : 1f;
            accumulatedPoise += tierPoise * strength * 0.5f;
            float interruptThreshold = feelSettings.mediumPoiseThreshold * bearScale;

            ApplyKnockback(sourcePosition, strength * (tier == CombatHitStopTier.Light ? 0.12f : 0.22f));
            if (animator == null || !HasHitTrigger())
                return;

            if (accumulatedPoise < interruptThreshold)
            {
                animator.ResetTrigger("Hit");
                return;
            }

            accumulatedPoise = Mathf.Max(0f, accumulatedPoise - interruptThreshold);
            animator.SetTrigger("Hit");
            if (tier != CombatHitStopTier.Light && animator.HasState(0, Animator.StringToHash("Base Layer.Hit")))
                animator.CrossFadeInFixedTime("Base Layer.Hit", 0.08f, 0, 0f);
        }

        private bool HasHitTrigger()
        {
            if (animator == null)
                return false;
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
                if (parameters[i].nameHash == Animator.StringToHash("Hit") &&
                    parameters[i].type == AnimatorControllerParameterType.Trigger)
                    return true;
            return false;
        }

        private void ApplyKnockback(Vector3 sourcePosition, float distance)
        {
            if (distance <= 0f)
                return;

            Vector3 direction = Vector3.ProjectOnPlane(transform.position - sourcePosition, Vector3.up);
            if (direction.sqrMagnitude < 0.001f)
                direction = transform.forward;
            direction.Normalize();

            Vector3 displacement = direction * distance;
            if (characterController != null && characterController.enabled)
            {
                characterController.Move(displacement);
                return;
            }

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.Move(displacement);
                return;
            }

            transform.position += displacement;
        }
    }
}
