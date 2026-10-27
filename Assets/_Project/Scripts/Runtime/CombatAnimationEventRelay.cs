using UnityEngine;

namespace Prince
{
    public sealed class CombatAnimationEventRelay : MonoBehaviour
    {
        public WeaponHolster weaponHolster;
        public WeaponHitbox weaponHitbox;
        public WeaponAudio weaponAudio;

        private void Awake()
        {
            if (weaponHolster == null)
                weaponHolster = GetComponentInParent<WeaponHolster>();
            if (weaponHitbox == null)
                weaponHitbox = GetComponentInParent<WeaponHitbox>();
            if (weaponAudio == null)
                weaponAudio = GetComponentInParent<WeaponAudio>();
        }

        public void OnDrawSword()
        {
            if (weaponHolster != null)
                weaponHolster.OnDrawSword();
        }

        public void OnSheathSword()
        {
            if (weaponHolster != null)
                weaponHolster.OnSheathSword();
        }

        public void OnAttackHitStart()
        {
            if (weaponHitbox != null)
                weaponHitbox.OnAttackHitStart();
        }

        public void OnAttackHitEnd()
        {
            if (weaponHitbox != null)
                weaponHitbox.OnAttackHitEnd();
        }

        public void OnAttackSwing()
        {
            if (weaponAudio != null)
                weaponAudio.PlaySwing();
        }
    }
}
