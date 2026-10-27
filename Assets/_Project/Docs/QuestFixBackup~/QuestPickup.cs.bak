using UnityEngine;

namespace Prince
{
    [RequireComponent(typeof(Collider))]
    public sealed class QuestPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private QuestTarget questTarget;
        [SerializeField] private QuestDefinition requiredQuest;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField, Min(0f)] private float soundVolume = 1f;

        public string PromptText => requiredQuest != null &&
            QuestManager.Instance != null &&
            QuestManager.Instance.GetState(requiredQuest.id) == QuestState.NotStarted
                ? "Bicara dengan Bram untuk menerima misi"
                : $"Tekan E untuk mengambil {GetDisplayName()}";

        private void Awake()
        {
            if (questTarget == null)
                questTarget = GetComponent<QuestTarget>();
            if (questTarget == null)
                Debug.LogWarning($"QuestPickup on '{name}' has no QuestTarget.", this);
            if (requiredQuest != null && string.IsNullOrWhiteSpace(requiredQuest.id))
                Debug.LogWarning($"QuestPickup on '{name}' references a quest without an ID.", this);
        }

        public void Interact()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager == null || questTarget == null)
            {
                Debug.LogWarning($"QuestPickup '{name}' cannot update progress because QuestManager or QuestTarget is missing.", this);
                return;
            }

            if (requiredQuest != null && manager.GetState(requiredQuest.id) != QuestState.Active)
                return;

            manager.NotifyTargetCollected(questTarget.targetId);
            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, soundVolume);
            gameObject.SetActive(false);
        }

        private string GetDisplayName()
        {
            return questTarget != null && !string.IsNullOrWhiteSpace(questTarget.displayName)
                ? questTarget.displayName
                : name;
        }
    }
}
