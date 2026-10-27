using UnityEngine;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class AttackBuffer : MonoBehaviour
    {
        private float expiresAt = -1f;

        public bool HasBufferedAttack => expiresAt >= Time.unscaledTime;

        public void Queue(float duration)
        {
            expiresAt = Time.unscaledTime + Mathf.Max(0f, duration);
        }

        public bool TryConsume()
        {
            if (!HasBufferedAttack)
            {
                Clear();
                return false;
            }

            Clear();
            return true;
        }

        public void Clear()
        {
            expiresAt = -1f;
        }
    }
}
