using System;
using UnityEngine;

namespace Prince
{
    public enum CombatFeelPreset
    {
        Standard,
        Lenient
    }

    public enum CombatHitStopTier
    {
        Light,
        Medium,
        Finisher
    }

    [Serializable]
    public sealed class AttackFeelProfile
    {
        public string attackName = "Attack";
        [Min(0.01f)] public float startupTime = 0.4f;
        [Min(0.01f)] public float activeWindow = 0.35f;
        [Min(0.01f)] public float recoveryTime = 0.65f;
        [Range(1.05f, 1.3f)] public float speedMultiplier = 1.15f;
        [Range(0.1f, 0.95f)] public float cancelWindowStart = 0.55f;
        public CombatHitStopTier hitStopTier = CombatHitStopTier.Light;
        [Min(0f)] public float lungeDistance = 0.35f;
        [Min(0f)] public float knockbackStrength = 1.2f;
    }

    [CreateAssetMenu(
        fileName = "CombatFeelSettings",
        menuName = "Prince/Combat Feel Settings")]
    public sealed class CombatFeelSettings : ScriptableObject
    {
        [Header("Preset")]
        public CombatFeelPreset preset = CombatFeelPreset.Lenient;
        [Range(0.08f, 0.25f)] public float attackBufferSeconds = 0.22f;

        [Header("Feature toggles")]
        public bool attackBufferEnabled = true;
        public bool jumpCancelEnabled = true;
        public bool attackPhasesEnabled = true;
        public bool attackSpeedEnabled = true;
        public bool audioPitchVariationEnabled = true;
        public bool hitStopEnabled = true;
        public bool attackLungeEnabled = true;
        public bool autoFaceEnabled = true;
        public bool enemyPoiseEnabled = true;
        public bool enemyDamageScalingEnabled = true;
        public bool cameraImpulseEnabled = true;
        public bool swordTrailEnabled = true;
        public bool hitFlashEnabled = true;
        public bool impactParticlesEnabled = true;
        public bool enemyRunDustEnabled = true;
        public bool finisherSlowMotionEnabled;

        [Header("Accessibility")]
        [Range(0f, 1f)] public float cameraShakePercent = 0.65f;
        public bool screenFlashEnabled = true;
        public bool accessibilityHitStopEnabled = true;
        public bool visualEffectsEnabled = true;

        [Header("Timing")]
        [Min(0.001f)] public float lightHitStop = 0.05f;
        [Min(0.001f)] public float mediumHitStop = 0.07f;
        [Min(0.001f)] public float finisherHitStop = 0.10f;
        [Range(0.01f, 0.1f)] public float frozenAnimatorSpeed = 0.02f;
        [Min(0f)] public float finisherSlowMotionScale = 0.25f;
        [Min(0.01f)] public float finisherSlowMotionDuration = 0.4f;
        [Range(0f, 0.07f)] public float audioPitchVariation = 0.07f;

        [Header("Targeting and movement")]
        [Min(0f)] public float autoFaceDistance = 5f;
        [Range(0f, 180f)] public float autoFaceAngle = 100f;
        [Min(0f)] public float autoFaceTurnSpeed = 540f;
        [Min(0f)] public float lungeGroundCheckDistance = 1.5f;

        [Header("Poise and feedback")]
        [Min(0f)] public float mediumPoiseThreshold = 1f;
        [Min(0f)] public float finisherPoiseThreshold = 2f;
        [Min(1f)] public float bearPoiseMultiplier = 2f;
        [Range(0.1f, 1f)] public float lenientEnemyDamageMultiplier = 0.8f;
        [Range(0f, 1f)] public float cameraImpulseLight = 0.18f;
        [Range(0f, 1f)] public float cameraImpulseMedium = 0.28f;
        [Range(0f, 1f)] public float cameraImpulseFinisher = 0.35f;
        [Min(0.01f)] public float cameraImpulseDuration = 0.25f;
        [Min(0.01f)] public float hitFlashDuration = 0.08f;
        public Color trailColor = new Color(0.78f, 0.94f, 1f, 0.75f);
        public Color impactColor = new Color(1f, 0.72f, 0.34f, 1f);
        public Material impactParticleMaterial;
        public AttackFeelProfile[] attacks =
        {
            new AttackFeelProfile
            {
                attackName = "Attack 1",
                startupTime = 0.46f,
                activeWindow = 0.45f,
                recoveryTime = 0.75f,
                speedMultiplier = 1.15f,
                cancelWindowStart = 0.52f,
                hitStopTier = CombatHitStopTier.Light,
                lungeDistance = 0.28f,
                knockbackStrength = 1.1f
            },
            new AttackFeelProfile
            {
                attackName = "Attack 2",
                startupTime = 0.50f,
                activeWindow = 0.48f,
                recoveryTime = 0.80f,
                speedMultiplier = 1.18f,
                cancelWindowStart = 0.50f,
                hitStopTier = CombatHitStopTier.Medium,
                lungeDistance = 0.35f,
                knockbackStrength = 1.8f
            },
            new AttackFeelProfile
            {
                attackName = "Attack 3",
                startupTime = 0.39f,
                activeWindow = 0.37f,
                recoveryTime = 0.62f,
                speedMultiplier = 1.22f,
                cancelWindowStart = 0.48f,
                hitStopTier = CombatHitStopTier.Finisher,
                lungeDistance = 0.45f,
                knockbackStrength = 2.8f
            }
        };

        public AttackFeelProfile GetAttack(int comboIndex)
        {
            if (attacks == null || attacks.Length == 0)
                return null;

            int index = Mathf.Clamp(comboIndex - 1, 0, attacks.Length - 1);
            return attacks[index];
        }

        public float GetHitStopDuration(CombatHitStopTier tier)
        {
            switch (tier)
            {
                case CombatHitStopTier.Medium:
                    return mediumHitStop;
                case CombatHitStopTier.Finisher:
                    return finisherHitStop;
                default:
                    return lightHitStop;
            }
        }

        public float GetCameraImpulse(CombatHitStopTier tier)
        {
            switch (tier)
            {
                case CombatHitStopTier.Medium:
                    return cameraImpulseMedium;
                case CombatHitStopTier.Finisher:
                    return cameraImpulseFinisher;
                default:
                    return cameraImpulseLight;
            }
        }

        public void ApplyPreset(CombatFeelPreset selectedPreset)
        {
            preset = selectedPreset;
            bool lenient = selectedPreset == CombatFeelPreset.Lenient;
            attackBufferSeconds = lenient ? 0.22f : 0.15f;
            autoFaceDistance = lenient ? 5.5f : 4.5f;
            autoFaceAngle = lenient ? 115f : 90f;
            autoFaceTurnSpeed = lenient ? 630f : 480f;

            if (attacks == null)
                return;
            for (int i = 0; i < attacks.Length; i++)
            {
                AttackFeelProfile profile = attacks[i];
                if (profile == null)
                    continue;
                float cancelStart = i == 2 ? 0.48f : i == 1 ? 0.50f : 0.52f;
                profile.cancelWindowStart = lenient
                    ? Mathf.Max(0.35f, cancelStart - 0.08f)
                    : Mathf.Min(0.85f, cancelStart + 0.08f);
            }
        }

        public void SetCameraShakePercent(float percent)
        {
            cameraShakePercent = Mathf.Clamp01(percent);
        }

        public void SetScreenFlashEnabled(bool enabled)
        {
            screenFlashEnabled = enabled;
        }

        public void SetHitStopEnabled(bool enabled)
        {
            accessibilityHitStopEnabled = enabled;
        }

        public void SetVisualEffectsEnabled(bool enabled)
        {
            visualEffectsEnabled = enabled;
        }
    }
}
