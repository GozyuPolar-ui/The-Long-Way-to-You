using UnityEngine;

namespace Prince
{
    public sealed class QuestTarget : MonoBehaviour
    {
        public string targetId;
        public string displayName;
        public Sprite icon;
        public bool trackable = true;

        private void Start()
        {
            QuestManager.Instance?.RegisterTarget(this);
        }

        private void OnEnable()
        {
            if (QuestManager.Instance != null)
                QuestManager.Instance.RegisterTarget(this);
        }

        private void OnDisable()
        {
            QuestManager.Instance?.UnregisterTarget(this);
        }
    }
}
