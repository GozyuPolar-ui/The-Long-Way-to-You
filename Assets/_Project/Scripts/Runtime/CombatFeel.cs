using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class CombatFeel : MonoBehaviour
    {
        [SerializeField] private CombatFeelSettings settings;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private bool enemyMovementDust;
        [SerializeField] private AttackLunge attackLunge;
        [SerializeField] private HitStop hitStop;
        [SerializeField] private CameraImpulse cameraImpulse;
        [SerializeField] private SwordTrail swordTrail;
        [SerializeField, Min(0f)] private float runDustSpeed = 1.8f;
        [SerializeField, Min(0.01f)] private float runDustInterval = 0.42f;

        private readonly Collider[] targetBuffer = new Collider[32];
        private readonly List<ParticleSystem> effects = new List<ParticleSystem>();
        private PlayerHealth playerHealth;
        private InputAction jumpAction;
        private AttackFeelProfile activeAttack;
        private float lastRunDustTime;
        private Vector3 previousPosition;
        private bool attackActive;
        private bool hitWindowActive;
        private bool warnedMissingParticleMaterial;

        public CombatFeelSettings Settings => settings;
        public bool IsHitWindowActive => hitWindowActive;
        public AttackFeelProfile ActiveAttack => activeAttack;

        private void Awake()
        {
            if (playerCombat == null)
                playerCombat = GetComponent<PlayerCombat>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            if (attackLunge == null)
                attackLunge = GetComponent<AttackLunge>();
            if (hitStop == null)
                hitStop = GetComponent<HitStop>();
            if (cameraImpulse == null)
                cameraImpulse = GetComponent<CameraImpulse>();
            if (swordTrail == null)
                swordTrail = GetComponentInChildren<SwordTrail>(true);
            playerHealth = GetComponent<PlayerHealth>();
            previousPosition = transform.position;

            if (playerCombat != null)
            {
                jumpAction = new InputAction("CombatJumpCancel", InputActionType.Button);
                jumpAction.AddBinding("<Keyboard>/space");
                jumpAction.AddBinding("<Gamepad>/buttonSouth");
                jumpAction.performed += OnJumpInput;
            }
        }

        private void OnEnable()
        {
            if (jumpAction != null)
                jumpAction.Enable();
            if (playerHealth != null)
                playerHealth.OnHit.AddListener(OnPlayerHit);
        }

        private void OnDisable()
        {
            if (jumpAction != null)
                jumpAction.Disable();
            if (playerHealth != null)
                playerHealth.OnHit.RemoveListener(OnPlayerHit);
            EndAttack();
            SetHitWindow(false);
            ClearEffects();
        }

        private void OnDestroy()
        {
            if (jumpAction == null)
                return;
            jumpAction.performed -= OnJumpInput;
            jumpAction.Dispose();
        }

        private void Update()
        {
            if (attackActive && !enemyMovementDust)
            {
                UpdateAutoFace();
                if (settings != null && settings.finisherSlowMotionEnabled &&
                    activeAttack != null && playerCombat != null && !playerCombat.IsAttacking)
                    EndAttack();
            }

            if (enemyMovementDust)
                UpdateRunDust();
            CleanupEffects();
        }

        public void BeginAttack(int comboIndex)
        {
            activeAttack = settings != null ? settings.GetAttack(comboIndex) : null;
            attackActive = activeAttack != null;
            SetHitWindow(false);
            if (hitStop != null && settings != null && settings.attackSpeedEnabled)
                hitStop.SetSpeedMultiplier(
                    attackActive ? activeAttack.speedMultiplier : 1f);
        }

        public void EndAttack()
        {
            attackActive = false;
            activeAttack = null;
            NotifyAttackWindowEnded();
            if (hitStop != null)
                hitStop.SetSpeedMultiplier(1f);
        }

        public float GetCancelWindowStart(float fallback)
        {
            return settings != null && settings.attackPhasesEnabled && activeAttack != null
                ? activeAttack.cancelWindowStart
                : fallback;
        }

        public float GetFallbackHitStart(float fallback)
        {
            if (settings == null || !settings.attackPhasesEnabled || activeAttack == null)
                return fallback;

            float total = activeAttack.startupTime + activeAttack.activeWindow + activeAttack.recoveryTime;
            return total > 0f ? Mathf.Clamp01(activeAttack.startupTime / total) : fallback;
        }

        public float GetFallbackHitEnd(float fallback)
        {
            if (settings == null || !settings.attackPhasesEnabled || activeAttack == null)
                return fallback;

            float total = activeAttack.startupTime + activeAttack.activeWindow + activeAttack.recoveryTime;
            return total > 0f
                ? Mathf.Clamp01((activeAttack.startupTime + activeAttack.activeWindow) / total)
                : fallback;
        }

        public float GetBufferDuration(float fallback)
        {
            return settings != null && settings.attackBufferEnabled
                ? Mathf.Clamp(settings.attackBufferSeconds, 0.08f, 0.25f)
                : fallback;
        }

        public bool IsAttackCancelable(float normalizedTime)
        {
            return attackActive && settings != null && settings.jumpCancelEnabled &&
                   activeAttack != null && normalizedTime >= activeAttack.cancelWindowStart;
        }

        public void NotifyAttackWindowStarted()
        {
            SetHitWindow(true);
            if (attackLunge != null && activeAttack != null)
                attackLunge.Begin(activeAttack.lungeDistance, activeAttack.activeWindow);
            if (swordTrail != null)
                swordTrail.SetTrailActive(true);
        }

        public void NotifyAttackWindowEnded()
        {
            SetHitWindow(false);
            if (attackLunge != null)
                attackLunge.StopLunge();
            if (swordTrail != null)
                swordTrail.SetTrailActive(false);
        }

        public void RegisterHit(IDamageable target, Vector3 hitPoint, GameObject source)
        {
            if (settings == null || target == null || activeAttack == null)
                return;

            CombatHitStopTier tier = activeAttack.hitStopTier;
            float hitStopDuration = settings.GetHitStopDuration(tier);
            if (settings.hitStopEnabled && settings.accessibilityHitStopEnabled)
            {
                if (hitStop != null)
                    hitStop.ApplyHitStop(hitStopDuration);

                HitStop targetHitStop = FindTargetComponent<HitStop>(target);
                if (targetHitStop != null)
                    targetHitStop.ApplyHitStop(hitStopDuration);
            }

            if (settings.enemyPoiseEnabled)
            {
                EnemyPoise poise = FindTargetComponent<EnemyPoise>(target);
                if (poise != null)
                    poise.ApplyHit(
                        tier, activeAttack.knockbackStrength, transform.position, settings);
            }

            if (settings.hitFlashEnabled && settings.visualEffectsEnabled &&
                settings.screenFlashEnabled)
            {
                HitFlash flash = FindTargetComponent<HitFlash>(target);
                if (flash != null)
                    flash.Flash(settings.hitFlashDuration);
            }

            if (settings.impactParticlesEnabled && settings.visualEffectsEnabled)
                SpawnImpact(hitPoint, settings.impactColor, 12, 2.2f);

            if (settings.cameraImpulseEnabled && settings.visualEffectsEnabled && cameraImpulse != null)
                cameraImpulse.Emit(tier, settings.cameraShakePercent);

            if (settings.finisherSlowMotionEnabled && tier == CombatHitStopTier.Finisher &&
                IsTargetDefeated(target))
            {
                if (hitStop != null)
                    hitStop.ApplyTemporarySlowMotion(
                        settings.finisherSlowMotionScale, settings.finisherSlowMotionDuration);
                HitStop targetHitStop = FindTargetComponent<HitStop>(target);
                if (targetHitStop != null)
                    targetHitStop.ApplyTemporarySlowMotion(
                        settings.finisherSlowMotionScale, settings.finisherSlowMotionDuration);
            }
        }

        public void NotifyBearAttackHit()
        {
            if (settings == null || !settings.hitStopEnabled || !settings.accessibilityHitStopEnabled)
                return;

            if (hitStop != null)
                hitStop.ApplyHitStop(settings.GetHitStopDuration(CombatHitStopTier.Light));
            if (settings.cameraImpulseEnabled && cameraImpulse != null)
                cameraImpulse.Emit(CombatHitStopTier.Light, settings.cameraShakePercent * 0.65f);

            BearAI bear = FindNearestEngagedBear();
            if (bear != null)
            {
                HitStop bearHitStop = bear.GetComponent<HitStop>();
                if (bearHitStop != null)
                    bearHitStop.ApplyHitStop(settings.GetHitStopDuration(CombatHitStopTier.Light));
            }
        }

        public float ScaleIncomingDamage(float damage, GameObject source)
        {
            if (settings == null || !settings.enemyDamageScalingEnabled ||
                settings.preset != CombatFeelPreset.Lenient || source == null ||
                source.GetComponentInParent<BearAI>() == null)
                return damage;

            float multiplier = settings.lenientEnemyDamageMultiplier;
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier))
            {
                Debug.LogWarning(
                    "CombatFeelSettings has an invalid Lenient enemy damage multiplier; using unscaled damage.",
                    settings);
                return damage;
            }

            return damage * Mathf.Clamp(multiplier, 0.1f, 1f);
        }

        private void OnJumpInput(InputAction.CallbackContext context)
        {
            if (settings == null || !settings.jumpCancelEnabled || playerCombat == null)
                return;

            Animator layerAnimator = playerCombat.animator;
            if (layerAnimator == null)
                return;
            int layer = layerAnimator.GetLayerIndex(playerCombat.combatLayerName);
            if (layer < 0 || !playerCombat.IsAttacking)
                return;

            float normalizedTime = layerAnimator.GetCurrentAnimatorStateInfo(layer).normalizedTime;
            if (IsAttackCancelable(normalizedTime))
                playerCombat.CancelAttackForJump();
        }

        private void UpdateAutoFace()
        {
            if (settings == null || !settings.autoFaceEnabled)
                return;

            float radius = Mathf.Max(0f, settings.autoFaceDistance);
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, radius, targetBuffer,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Transform nearest = null;
            float nearestSquaredDistance = float.PositiveInfinity;
            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

            for (int i = 0; i < count; i++)
            {
                Collider candidate = targetBuffer[i];
                if (candidate == null)
                    continue;

                IDamageable damageable = FindDamageable(candidate);
                if (damageable == null || IsOwnedTarget(candidate.transform))
                    continue;

                Transform target = ((Component)damageable).transform;
                Vector3 toTarget = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
                float distanceSquared = toTarget.sqrMagnitude;
                if (distanceSquared < 0.001f || distanceSquared >= nearestSquaredDistance ||
                    Vector3.Angle(flatForward, toTarget) > settings.autoFaceAngle)
                    continue;

                nearest = target;
                nearestSquaredDistance = distanceSquared;
            }

            if (nearest == null)
                return;

            Vector3 direction = Vector3.ProjectOnPlane(nearest.position - transform.position, Vector3.up);
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(direction.normalized, Vector3.up),
                    settings.autoFaceTurnSpeed * Time.unscaledDeltaTime);
        }

        private void UpdateRunDust()
        {
            if (settings == null || !settings.enemyRunDustEnabled || !settings.visualEffectsEnabled)
            {
                previousPosition = transform.position;
                return;
            }

            float delta = Time.unscaledDeltaTime;
            if (delta <= 0f)
                return;

            float speed = Vector3.ProjectOnPlane(transform.position - previousPosition, Vector3.up).magnitude / delta;
            previousPosition = transform.position;
            if (speed < runDustSpeed || Time.unscaledTime - lastRunDustTime < runDustInterval)
                return;

            lastRunDustTime = Time.unscaledTime;
            SpawnImpact(
                transform.position + Vector3.down * 0.05f,
                new Color(0.72f, 0.66f, 0.55f, 0.45f),
                7,
                0.8f);
        }

        private void OnPlayerHit(float damage)
        {
            NotifyBearAttackHit();
        }

        private void SetHitWindow(bool active)
        {
            hitWindowActive = active;
        }

        private void SpawnImpact(Vector3 position, Color color, int count, float speed)
        {
            if (settings == null || settings.impactParticleMaterial == null)
            {
                if (!warnedMissingParticleMaterial)
                {
                    Debug.LogWarning(
                        "CombatFeelSettings has no URP impact particle material; combat particles are disabled.",
                        settings != null ? settings : this);
                    warnedMissingParticleMaterial = true;
                }
                return;
            }

            GameObject effect = new GameObject("CombatFeel_Impact");
            effect.transform.position = position;
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.25f;
            main.loop = false;
            main.useUnscaledTime = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.55f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.09f);
            main.startColor = color;
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.045f;
            ParticleSystemRenderer particleRenderer = effect.GetComponent<ParticleSystemRenderer>();
            particleRenderer.material = settings.impactParticleMaterial;
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;

            particles.Emit(count);
            particles.Play();
            effects.Add(particles);
        }

        private void CleanupEffects()
        {
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                if (effects[i] == null || !effects[i].IsAlive(true))
                    effects.RemoveAt(i);
            }
        }

        private void ClearEffects()
        {
            for (int i = 0; i < effects.Count; i++)
                if (effects[i] != null)
                    Destroy(effects[i].gameObject);
            effects.Clear();
        }

        private bool IsOwnedTarget(Transform target)
        {
            return target == transform || target.IsChildOf(transform);
        }

        private static IDamageable FindDamageable(Collider collider)
        {
            MonoBehaviour[] behaviours = collider.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
                if (behaviours[i] is IDamageable damageable)
                    return damageable;
            return null;
        }

        private static T FindTargetComponent<T>(IDamageable target) where T : Component
        {
            Component component = target as Component;
            return component != null ? component.GetComponentInParent<T>() : null;
        }

        private static bool IsTargetDefeated(IDamageable target)
        {
            if (target is BearAI bear)
                return bear.IsDead;
            if (target is PlayerHealth player)
                return player.IsDefeated;
            return false;
        }

        private BearAI FindNearestEngagedBear()
        {
            BearAI[] bears = FindObjectsByType<BearAI>(FindObjectsSortMode.None);
            BearAI nearest = null;
            float nearestDistance = 3.5f * 3.5f;
            for (int i = 0; i < bears.Length; i++)
            {
                if (bears[i] == null || !bears[i].IsEngaged || bears[i].IsDead)
                    continue;

                float distance = (bears[i].transform.position - transform.position).sqrMagnitude;
                if (distance >= nearestDistance)
                    continue;
                nearestDistance = distance;
                nearest = bears[i];
            }

            return nearest;
        }
    }
}
