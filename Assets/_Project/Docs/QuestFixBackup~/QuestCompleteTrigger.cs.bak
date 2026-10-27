using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class QuestCompleteTrigger : MonoBehaviour
    {
        [SerializeField] private string questId = "hunter_backpack";
        [SerializeField] private string objectiveId = "find_bram_backpack";
        [SerializeField] private string bagTargetId = "bram_backpack";
        [SerializeField] private string closingSceneName = "Closing";
        [SerializeField, Min(0f)] private float delay = 2.5f;
        [SerializeField, Min(0f)] private float fadeDuration = 0.6f;

        private QuestManager questManager;
        private CanvasGroup fadeGroup;
        private bool transitionStarted;

        public void Configure(
            string requiredQuestId,
            string requiredObjectiveId,
            string requiredBagTargetId,
            string targetSceneName,
            float delaySeconds,
            float fadeSeconds)
        {
            questId = requiredQuestId;
            objectiveId = requiredObjectiveId;
            bagTargetId = requiredBagTargetId;
            closingSceneName = targetSceneName;
            delay = Mathf.Max(0f, delaySeconds);
            fadeDuration = Mathf.Max(0f, fadeSeconds);
        }

        private void OnEnable()
        {
            questManager = GetComponent<QuestManager>();
            if (questManager == null)
                questManager = QuestManager.Instance;
            if (questManager == null)
            {
                Debug.LogWarning(
                    "QuestCompleteTrigger could not find QuestManager; call NotifyBagTaken() from the bag pickup event.",
                    this);
                return;
            }

            questManager.OnObjectiveChanged += HandleObjectiveChanged;
        }

        private void OnDisable()
        {
            if (questManager != null)
                questManager.OnObjectiveChanged -= HandleObjectiveChanged;
        }

        private void HandleObjectiveChanged(
            QuestDefinition quest,
            QuestObjectiveDefinition objective,
            bool completed)
        {
            if (!completed || quest == null || objective == null ||
                !string.Equals(quest.id, questId, System.StringComparison.Ordinal) ||
                !string.Equals(objective.id, objectiveId, System.StringComparison.Ordinal) ||
                objective.type != QuestObjectiveType.Collect ||
                !string.Equals(objective.targetId, bagTargetId, System.StringComparison.Ordinal))
                return;

            NotifyBagTaken();
        }

        public void NotifyBagTaken()
        {
            if (transitionStarted || Prince.Closing.QuestProgress.IsCompleted)
                return;

            if (!IsSceneInBuildSettings(closingSceneName))
            {
                Debug.LogError(
                    $"Closing scene '{closingSceneName}' is not in Build Settings. " +
                    "The bag was taken, but the closing transition cannot start.",
                    this);
                return;
            }

            transitionStarted = true;
            Prince.Closing.QuestProgress.Complete();
            ClosingSceneController.CaptureReturnState();
            StartCoroutine(TransitionRoutine());
        }

        private IEnumerator TransitionRoutine()
        {
            CreateFadeOverlay();
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);
            yield return FadeToOpaque(fadeGroup, fadeDuration);
            SceneLoader.Load(closingSceneName);
        }

        private void CreateFadeOverlay()
        {
            GameObject canvasObject = new GameObject(
                "QuestCompleteFade",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 3000;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            fadeGroup = canvasObject.GetComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.interactable = false;
            fadeGroup.blocksRaycasts = true;

            GameObject imageObject = new GameObject(
                "Fade",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = imageObject.GetComponent<Image>();
            image.color = new Color(0.025f, 0.035f, 0.045f, 1f);
            image.raycastTarget = false;
        }

        private static IEnumerator FadeToOpaque(CanvasGroup group, float duration)
        {
            if (group == null)
                yield break;

            float effectiveDuration = Mathf.Max(1f / 60f, duration);
            float elapsed = 0f;
            while (elapsed < effectiveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(elapsed / effectiveDuration);
                yield return null;
            }
            group.alpha = 1f;
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
                        System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
