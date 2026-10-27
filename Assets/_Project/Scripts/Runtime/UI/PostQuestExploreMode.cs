using UnityEngine;
using UnityEngine.InputSystem;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class PostQuestExploreMode : MonoBehaviour
    {
        private const string BackpackQuestId = "hunter_backpack";

        [SerializeField] private bool calmBearsAfterQuest = true;
        [SerializeField] private string bagTargetId = "bram_backpack";

        public void Configure(bool calmBears, string collectedBagTargetId)
        {
            calmBearsAfterQuest = calmBears;
            bagTargetId = collectedBagTargetId;
        }

        private void Start()
        {
            if (!Prince.Closing.QuestProgress.IsCompleted)
                return;

            QuestManager questManager = QuestManager.Instance;
            if (questManager == null ||
                questManager.GetState(BackpackQuestId) != QuestState.Completed)
            {
                Debug.LogWarning(
                    "Ignored a saved Closing completion flag because the backpack quest is not completed in this session. The backpack and bears remain available.",
                    this);
                return;
            }

            HideCollectedBag();
            if (calmBearsAfterQuest)
                DisableBearAggression();
            RestoreInputState();
        }

        private void HideCollectedBag()
        {
            QuestTarget[] targets = FindObjectsByType<QuestTarget>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < targets.Length; i++)
            {
                QuestTarget target = targets[i];
                if (target != null &&
                    string.Equals(target.targetId, bagTargetId, System.StringComparison.Ordinal))
                    target.gameObject.SetActive(false);
            }
        }

        private static void DisableBearAggression()
        {
            BearAI[] bears = FindObjectsByType<BearAI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < bears.Length; i++)
                if (bears[i] != null)
                    bears[i].enabled = false;
        }

        private static void RestoreInputState()
        {
            PlayerInput input = FindFirstObjectByType<PlayerInput>();
            if (input != null)
                ClosingSceneController.RestoreActionMap(input);

            ClosingSceneController.RestoreCursorState();
        }
    }
}
