using UnityEngine;

namespace Prince
{
    public sealed class WeaponHolster : MonoBehaviour
    {
        public Transform weaponSocketBack;
        public Transform weaponSocketRightHand;
        public GameObject sword;
        public GameObject scabbard;
        public WeaponHitbox weaponHitbox;
        public WeaponAudio weaponAudio;
        [HideInInspector] public bool setupPoseConfigured;

        [Header("Back socket")]
        public Vector3 backSwordPosition;
        public Vector3 backSwordEulerAngles;
        public Vector3 scabbardPosition = new Vector3(0f, 0.658f, 0f);
        public Vector3 scabbardEulerAngles = new Vector3(180f, 0f, 0f);
        public Vector3 scabbardScale = Vector3.one;
        public Vector3 swordScale = Vector3.one;

        [Header("Right-hand socket")]
        public Vector3 handSwordPosition = new Vector3(0f, -0.12f, 0f);
        public Vector3 handSwordEulerAngles = new Vector3(0f, 0f, -90f);

        public bool IsDrawn { get; private set; }

        private bool _warnedMissingReferences;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (weaponSocketBack == null || weaponSocketRightHand == null || sword == null)
            {
                WarnMissingReferences();
                return;
            }

            if (scabbard == null)
                Debug.LogWarning($"{nameof(WeaponHolster)} on '{name}' has no scabbard assigned.", this);

            if (scabbard != null)
            {
                if (scabbard.transform.parent != weaponSocketBack)
                    scabbard.transform.SetParent(weaponSocketBack, false);
                ApplyScabbardPose();
            }

            IsDrawn = sword.transform.parent == weaponSocketRightHand;
            if (IsDrawn)
                ApplyHandSwordPose();
            else
            {
                if (sword.transform.parent != weaponSocketBack)
                    sword.transform.SetParent(weaponSocketBack, false);
                ApplyBackSwordPose();
            }

            if (weaponHitbox != null)
                weaponHitbox.SetHitboxActive(false);
        }

        public bool DrawSword()
        {
            if (!HasRequiredReferences())
                return false;
            if (IsDrawn)
                return true;

            sword.transform.SetParent(weaponSocketRightHand, false);
            ApplyHandSwordPose();
            IsDrawn = true;
            if (weaponHitbox != null)
                weaponHitbox.SetHitboxActive(false);
            if (weaponAudio != null)
                weaponAudio.PlayDraw();
            return true;
        }

        public bool SheathSword()
        {
            if (!HasRequiredReferences())
                return false;
            if (!IsDrawn && sword.transform.parent == weaponSocketBack)
                return true;

            sword.transform.SetParent(weaponSocketBack, false);
            ApplyBackSwordPose();
            IsDrawn = false;
            if (weaponHitbox != null)
                weaponHitbox.SetHitboxActive(false);
            if (weaponAudio != null)
                weaponAudio.PlaySheath();
            return true;
        }

        public void OnDrawSword()
        {
            DrawSword();
        }

        public void OnSheathSword()
        {
            SheathSword();
        }

        private bool HasRequiredReferences()
        {
            if (weaponSocketBack != null && weaponSocketRightHand != null && sword != null)
                return true;

            WarnMissingReferences();
            return false;
        }

        private void WarnMissingReferences()
        {
            if (_warnedMissingReferences)
                return;

            Debug.LogWarning(
                $"{nameof(WeaponHolster)} on '{name}' needs a sword and both weapon sockets.",
                this);
            _warnedMissingReferences = true;
        }

        private void ApplyBackSwordPose()
        {
            sword.transform.localPosition = backSwordPosition;
            sword.transform.localRotation = Quaternion.Euler(backSwordEulerAngles);
            sword.transform.localScale = swordScale;
        }

        private void ApplyHandSwordPose()
        {
            sword.transform.localPosition = handSwordPosition;
            sword.transform.localRotation = Quaternion.Euler(handSwordEulerAngles);
            sword.transform.localScale = swordScale;
        }

        private void ApplyScabbardPose()
        {
            scabbard.transform.localPosition = scabbardPosition;
            scabbard.transform.localRotation = Quaternion.Euler(scabbardEulerAngles);
            scabbard.transform.localScale = scabbardScale;
        }
    }
}
