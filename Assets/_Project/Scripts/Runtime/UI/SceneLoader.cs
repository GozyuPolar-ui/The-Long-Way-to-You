using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class SceneLoader : MonoBehaviour
    {
        private const string DataResourcePath = "LoadingScreen/LoadingLoreData";
        private const string LoaderResourcePath = "LoadingScreen/SceneLoader";

        [SerializeField] private LoadingLoreData loadingData;
        [SerializeField] private LoadingScreenController loadingScreen;
        private static SceneLoader instance;
        private bool loadInProgress;

        public static SceneLoader Instance => instance;

        public void Configure(LoadingLoreData data, LoadingScreenController screen)
        {
            loadingData = data;
            loadingScreen = screen;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            if (loadingScreen == null)
                loadingScreen = GetComponentInChildren<LoadingScreenController>(true);
            if (loadingData == null)
                loadingData = Resources.Load<LoadingLoreData>(DataResourcePath);
            loadingData?.SetLanguage(GameLocalization.CurrentLanguage);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public static void Load(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("SceneLoader.Load requires a non-empty scene name.");
                return;
            }

            if (instance != null)
            {
                instance.BeginLoad(sceneName);
                return;
            }

            LoadingLoreData data = Resources.Load<LoadingLoreData>(DataResourcePath);
            if (data == null)
            {
                Debug.LogWarning(
                    $"Loading lore data '{DataResourcePath}' was not found. " +
                    "Falling back to the existing scene transition.");
                LoadWithExistingFader(sceneName);
                return;
            }
            if (!data.EnableLoadingScreen)
            {
                LoadWithExistingFader(sceneName);
                return;
            }

            SceneLoader prefab = Resources.Load<SceneLoader>(LoaderResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"Loading screen is enabled but '{LoaderResourcePath}' was not found in Resources. " +
                    "Falling back to the existing scene transition.");
                LoadWithExistingFader(sceneName);
                return;
            }

            SceneLoader loader = Instantiate(prefab);
            if (loader == null)
            {
                Debug.LogError("Could not create the persistent SceneLoader.");
                LoadWithExistingFader(sceneName);
                return;
            }
            loader.BeginLoad(sceneName);
        }

        public static void SetLanguage(LoadingLoreLanguage language)
        {
            LoadingLoreData data = instance != null
                ? instance.loadingData
                : Resources.Load<LoadingLoreData>(DataResourcePath);
            if (data == null)
            {
                Debug.LogWarning(
                    $"Cannot set loading-screen language because '{DataResourcePath}' was not found.");
                return;
            }

            data.SetLanguage(language);
            if (instance != null)
                instance.loadingScreen?.SetLanguage(language);
        }

        private void BeginLoad(string sceneName)
        {
            if (loadInProgress)
                return;

            int buildIndex = FindBuildIndex(sceneName);
            if (buildIndex < 0)
            {
                Debug.LogError(
                    $"SceneLoader could not find scene '{sceneName}' in Build Settings. " +
                    "Add the scene to Build Settings before loading it.",
                    this);
                return;
            }

            if (loadingData == null)
                loadingData = Resources.Load<LoadingLoreData>(DataResourcePath);
            loadingData?.SetLanguage(GameLocalization.CurrentLanguage);
            if (loadingData == null)
            {
                Debug.LogWarning(
                    $"Loading lore data '{DataResourcePath}' was not found. " +
                    "Falling back to the existing scene transition.",
                    this);
                LoadWithExistingFader(sceneName);
                return;
            }
            if (!loadingData.EnableLoadingScreen)
            {
                LoadWithExistingFader(sceneName);
                return;
            }
            if (loadingScreen == null || loadingScreen.View == null)
            {
                Debug.LogError(
                    "SceneLoader requires a LoadingScreenController with a LoreSlideView; " +
                    "falling back to the existing scene transition.",
                    this);
                LoadWithExistingFader(sceneName);
                return;
            }

            loadInProgress = true;
            StartCoroutine(LoadRoutine(buildIndex));
        }

        private IEnumerator LoadRoutine(int buildIndex)
        {
            float startTime = Time.unscaledTime;
            AsyncOperation operation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
            if (operation == null)
            {
                loadInProgress = false;
                Debug.LogError($"SceneManager could not start loading build index {buildIndex}.", this);
                yield break;
            }
            operation.allowSceneActivation = false;

            yield return loadingScreen.FadeIn(loadingData);
            while (operation.progress < 0.9f)
            {
                loadingScreen.SetProgressTarget(operation.progress / 0.9f);
                yield return null;
            }

            loadingScreen.SetProgressTarget(1f);
            while (Time.unscaledTime - startTime < loadingData.MinimumDisplayDuration)
                yield return null;
            loadingScreen.StopSlideChanges();

            if (loadingData.WaitForInput)
            {
                loadingScreen.WaitForAnyInput(loadingData.ActiveLanguage);
                while (!loadingScreen.ContinueRequested)
                    yield return null;
            }

            while (loadingScreen.View != null && !loadingScreen.View.IsProgressSettled)
                yield return null;

            operation.allowSceneActivation = true;
            while (!operation.isDone)
                yield return null;

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.isLoaded || activeScene.buildIndex != buildIndex)
            {
                Debug.LogError(
                    $"Scene loading completed, but build index {buildIndex} is not the active loaded scene. " +
                    "The loading overlay will remain visible to avoid revealing the previous scene.",
                    this);
                loadInProgress = false;
                yield break;
            }

            yield return null;
            yield return new WaitForEndOfFrame();
            yield return loadingScreen.FadeOut(loadingData);
            loadInProgress = false;
        }

        private static int FindBuildIndex(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.Equals(Path.GetFileNameWithoutExtension(path), sceneName,
                        System.StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private static void LoadWithExistingFader(string sceneName)
        {
            SceneFader fader = FindAnyObjectByType<SceneFader>();
            if (fader != null)
            {
                fader.LoadScene(sceneName);
                return;
            }

            int buildIndex = FindBuildIndex(sceneName);
            if (buildIndex < 0)
            {
                Debug.LogError($"Scene '{sceneName}' is not present in Build Settings.");
                return;
            }
            AsyncOperation operation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
            if (operation == null)
                Debug.LogError($"Could not start loading scene '{sceneName}'.");
        }
    }
}
