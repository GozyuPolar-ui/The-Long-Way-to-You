using UnityEngine;
using UnityEngine.InputSystem;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class PostQuestExploreMode : MonoBehaviour
    {
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
