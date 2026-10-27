using UnityEngine;

namespace Prince
{
    [RequireComponent(typeof(Collider))]
    public sealed class QuestPickup : MonoBehaviour, IInteractable
    {
        private const string BearDefeatObjectiveId = "defeat_guarding_bear";

        [SerializeField] private QuestTarget questTarget;
        [SerializeField] private QuestDefinition requiredQuest;
        [SerializeField] private bool requireBearDefeated = true;
        [SerializeField] private BearAI bearGuard;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField, Min(0f)] private float soundVolume = 1f;

        private bool warnedMissingBear;

        public string PromptText
        {
            get
            {
                if (requireBearDefeated && IsGuardBearAlive)
                    return GameLocalization.Get("The bear is guarding it.");

                return requiredQuest != null &&
                    QuestManager.Instance != null &&
                    QuestManager.Instance.GetState(requiredQuest.id) == QuestState.NotStarted
                        ? GameLocalization.Get("Talk to Bram to accept the quest")
                        : GameLocalization.Get("Press E to pick up {0}",
                            GameLocalization.Get(GetDisplayName()));
            }
        }

        public bool IsGuardBearAlive
        {
            get
            {
                BearAI guard = FindGuardBear();
                return guard != null && !guard.IsDead;
            }
        }

        public BearAI GuardBear => bearGuard;

        private void Awake()
        {
            if (questTarget == null)
                questTarget = GetComponent<QuestTarget>();
            if (questTarget == null)
                Debug.LogWarning($"QuestPickup on '{name}' has no QuestTarget.", this);
            if (requiredQuest != null && string.IsNullOrWhiteSpace(requiredQuest.id))
                Debug.LogWarning($"QuestPickup on '{name}' references a quest without an ID.", this);
            FindGuardBear();
        }

        public void Interact()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager == null || questTarget == null)
            {
                Debug.LogWarning($"QuestPickup '{name}' cannot update progress because QuestManager or QuestTarget is missing.", this);
                return;
            }

            if (requireBearDefeated)
            {
                BearAI guard = FindGuardBear();
                if (guard != null && !guard.IsDead)
                {
                    UISoundPlayer.Play(UISoundType.Error);
                    return;
                }
                if (guard == null)
                    WarnMissingBear();
            }

            if (requiredQuest != null && manager.GetState(requiredQuest.id) != QuestState.Active)
            {
                UISoundPlayer.Play(UISoundType.Error);
                return;
            }

            if (requiredQuest != null)
                manager.SetObjectiveDone(requiredQuest.id, BearDefeatObjectiveId);

            manager.NotifyTargetCollected(questTarget.targetId);
            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, soundVolume);
            else
                UISoundPlayer.Play(UISoundType.Pickup);
            gameObject.SetActive(false);
        }

        private BearAI FindGuardBear()
        {
            if (bearGuard != null)
                return bearGuard;

            BearAI[] bears = FindObjectsByType<BearAI>(FindObjectsSortMode.None);
            BackpackQuestPlacement placement = FindAnyObjectByType<BackpackQuestPlacement>();
            if (placement != null)
            {
                float radiusSquared = placement.GuardAreaRadius * placement.GuardAreaRadius;
                float closestDistanceSquared = float.PositiveInfinity;
                foreach (BearAI bear in bears)
                {
                    float distanceSquared = (bear.transform.position - placement.GuardAreaPosition).sqrMagnitude;
                    if (distanceSquared > radiusSquared || distanceSquared >= closestDistanceSquared)
                        continue;

                    closestDistanceSquared = distanceSquared;
                    bearGuard = bear;
                }
            }
            else if (bears.Length == 1)
            {
                bearGuard = bears[0];
            }

            if (bearGuard == null && requireBearDefeated)
                WarnMissingBear();

            return bearGuard;
        }

        private void WarnMissingBear()
        {
            if (warnedMissingBear)
                return;

            warnedMissingBear = true;
            Debug.LogWarning(
                $"QuestPickup on '{name}' could not find the guarding BearAI; the backpack remains available to prevent a soft-lock.",
                this);
        }

        private string GetDisplayName()
        {
            return questTarget != null && !string.IsNullOrWhiteSpace(questTarget.displayName)
                ? questTarget.displayName
                : name;
        }
    }
}
