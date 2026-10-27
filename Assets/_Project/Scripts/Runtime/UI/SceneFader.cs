using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prince
{
    [RequireComponent(typeof(CanvasGroup), typeof(Image))]
    public sealed class SceneFader : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float duration = 0.7f;
        private CanvasGroup canvasGroup;
        private bool transitionInProgress;

        public float FadeDuration => duration;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            yield return Fade(0f);
            canvasGroup.blocksRaycasts = false;
        }

        public void LoadScene(string sceneName)
        {
            if (!transitionInProgress && !string.IsNullOrWhiteSpace(sceneName))
                StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            transitionInProgress = true;
            canvasGroup.blocksRaycasts = true;
            yield return Fade(1f);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                Debug.LogError($"Could not start loading scene '{sceneName}'.");
                yield return Fade(0f);
                canvasGroup.blocksRaycasts = false;
                transitionInProgress = false;
                yield break;
            }

            while (!operation.isDone)
                yield return null;

            yield return Fade(0f);
            canvasGroup.blocksRaycasts = false;
            transitionInProgress = false;
            Destroy(gameObject);
        }

        private IEnumerator Fade(float target)
        {
            float start = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            canvasGroup.alpha = target;
        }
    }
}
