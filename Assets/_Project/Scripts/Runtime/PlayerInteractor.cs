using UnityEngine;
using UnityEngine.InputSystem;

namespace Prince
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float interactionRadius = 2.5f;
        [SerializeField, Range(-1f, 1f)] private float minimumForwardDot = 0.1f;

        private readonly Collider[] nearbyColliders = new Collider[64];
        private InputAction interactAction;
        private IInteractable currentTarget;
        private NPCInfo currentNPC;
        private QuestTarget currentQuestTarget;
        private QuestDialogueUI dialogueUI;

        private void Awake()
        {
            interactAction = new InputAction("Interact", InputActionType.Button);
            interactAction.AddBinding("<Keyboard>/e");
            interactAction.AddBinding("<Gamepad>/buttonEast");
            interactAction.performed += OnInteract;
        }

        private void OnEnable()
        {
            interactAction?.Enable();
        }

        private void OnDisable()
        {
            interactAction?.Disable();
            currentTarget = null;
            QuestDialogueUI.SetPrompt(string.Empty);
        }

        private void OnDestroy()
        {
            if (interactAction == null)
                return;
            interactAction.performed -= OnInteract;
            interactAction.Dispose();
        }

        private void Update()
        {
            if (QuestDialogueUI.IsDialogueOpen)
            {
                SetCurrentTarget(null, null, null);
                return;
            }

            FindNearestTarget();
        }

        private void FindNearestTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, interactionRadius, nearbyColliders,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            float bestDistance = float.PositiveInfinity;
            IInteractable best = null;
            NPCInfo bestNPC = null;
            QuestTarget bestQuestTarget = null;

            for (int i = 0; i < count; i++)
            {
                Collider candidateCollider = nearbyColliders[i];
                nearbyColliders[i] = null;
                if (candidateCollider == null || candidateCollider.transform == transform ||
                    candidateCollider.transform.IsChildOf(transform))
                    continue;

                Vector3 targetPoint = ColliderUtility.ClosestPoint(candidateCollider, transform.position);
                Vector3 toTarget = targetPoint - transform.position;
                float distance = toTarget.magnitude;
                if (distance > interactionRadius || distance > bestDistance ||
                    (distance > 0.001f && Vector3.Dot(transform.forward, toTarget / distance) < minimumForwardDot))
                    continue;

                IInteractable interactable = FindInteractable(candidateCollider);
                if (interactable == null)
                    continue;

                bestDistance = distance;
                best = interactable;
                bestNPC = candidateCollider.GetComponentInParent<NPCInfo>();
                bestQuestTarget = candidateCollider.GetComponentInParent<QuestTarget>();
            }

            SetCurrentTarget(best, bestNPC, bestQuestTarget);
        }

        private void SetCurrentTarget(IInteractable target, NPCInfo npc, QuestTarget questTarget)
        {
            if (ReferenceEquals(currentTarget, target) &&
                currentNPC == npc && currentQuestTarget == questTarget)
                return;

            currentTarget = target;
            currentNPC = npc;
            currentQuestTarget = questTarget;
            QuestDialogueUI.SetPrompt(currentTarget != null ? currentTarget.PromptText : string.Empty);
        }

        private static IInteractable FindInteractable(Collider candidateCollider)
        {
            MonoBehaviour[] components = candidateCollider.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
            {
                if (component is IInteractable interactable)
                    return interactable;
            }

            return null;
        }

        private void OnInteract(InputAction.CallbackContext context)
        {
            if (QuestDialogueUI.IsDialogueOpen || currentTarget == null)
                return;

            if (currentQuestTarget != null && QuestManager.Instance != null && currentNPC != null)
                QuestManager.Instance.NotifyTargetTalkedTo(currentQuestTarget.targetId);

            currentTarget.Interact();
            if (currentNPC != null)
            {
                if (dialogueUI == null)
                    dialogueUI = FindAnyObjectByType<QuestDialogueUI>();
                if (dialogueUI != null)
                    dialogueUI.StartDialogue(currentNPC);
                else if (!string.IsNullOrWhiteSpace(currentNPC.dialogueId))
                    Debug.LogWarning("NPC has a dialogue ID, but QuestDialogueUI is not present in the scene.", currentNPC);
            }
        }
    }
}
