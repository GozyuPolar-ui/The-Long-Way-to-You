using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class QuestCompleteTrigger : MonoBehaviour
    {
        [SerializeField] private string questId = "hunter_backpack";
        [SerializeField] private string closingSceneName = "Closing";
        [SerializeField, Min(0f)] private float delay = 1.8f;

        private QuestManager questManager;
        private static bool transitionStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTransitionGuard()
        {
            transitionStarted = false;
        }

        public void Configure(
            string requiredQuestId,
            string requiredObjectiveId,
            string requiredBagTargetId,
            string targetSceneName,
            float delaySeconds,
            float fadeSeconds)
        {
            questId = requiredQuestId;
            closingSceneName = targetSceneName;
            delay = Mathf.Clamp(delaySeconds, 1.5f, 2f);
        }

        private void OnEnable()
        {
            delay = Mathf.Clamp(delay, 1.5f, 2f);
            questManager = GetComponent<QuestManager>();
            if (questManager == null)
                questManager = QuestManager.Instance;
            if (questManager == null)
            {
                Debug.LogWarning("QuestCompleteTrigger could not find QuestManager.", this);
                return;
            }

            questManager.OnQuestCompleted += HandleQuestCompleted;
        }

        private void OnDisable()
        {
            if (questManager != null)
                questManager.OnQuestCompleted -= HandleQuestCompleted;
        }

        private void HandleQuestCompleted(QuestDefinition quest, QuestProgress progress)
        {
            if (transitionStarted || quest == null || progress == null ||
                progress.state != QuestState.Completed ||
                !string.Equals(quest.id, questId, StringComparison.Ordinal))
                return;

            if (!IsSceneInBuildSettings(closingSceneName))
            {
                Debug.LogError(
                    $"Closing scene '{closingSceneName}' is not in Build Settings; the completed quest cannot transition.",
                    this);
                return;
            }

            transitionStarted = true;
            StartCoroutine(TransitionRoutine());
        }

        private IEnumerator TransitionRoutine()
        {
            DialogueInputLock inputLock = FindAnyObjectByType<DialogueInputLock>();
            while (QuestDialogueUI.IsDialogueOpen || (inputLock != null && inputLock.IsLocked))
                yield return null;

            ClosingSceneController.CaptureReturnState();
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            SceneFader fader = FindAnyObjectByType<SceneFader>();
            if (fader == null)
            {
                Debug.LogError(
                    "Quest completed, but no SceneFader is available to transition to the Closing scene.",
                    this);
                yield break;
            }

            Prince.Closing.QuestProgress.Complete();
            fader.LoadScene(closingSceneName);
        }

        private static bool IsSceneInBuildSettings(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return false;

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.Equals(
                        System.IO.Path.GetFileNameWithoutExtension(path),
                        sceneName,
                        StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
