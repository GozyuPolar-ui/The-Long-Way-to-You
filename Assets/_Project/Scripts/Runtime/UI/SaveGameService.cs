using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prince
{
    [Serializable]
    internal sealed class SaveGameData
    {
        public string sceneName;
        public Vector3 playerPosition;
        public Quaternion playerRotation;
    }

    public sealed class SaveGameService : MonoBehaviour
    {
        private const string MenuSceneName = "MainMenu";
        private const float SaveInterval = 15f;
        private static SaveGameService instance;
        private static SaveGameData pendingRestore;
        private float nextSaveTime;
        private string savePath;

        public static bool HasSave => TryRead(out _);
        public static bool SaveFileExists =>
            File.Exists(Path.Combine(Application.persistentDataPath, "prince-save.json"));

        public static SaveGameService EnsureInstance()
        {
            if (instance != null)
                return instance;
            GameObject serviceObject = new GameObject("SaveGameService");
            return serviceObject.AddComponent<SaveGameService>();
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
            savePath = Path.Combine(Application.persistentDataPath, "prince-save.json");
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextSaveTime)
                return;
            nextSaveTime = Time.unscaledTime + SaveInterval;
            SaveCurrentGame();
        }

        private void OnApplicationQuit() => SaveCurrentGame();
        private void OnApplicationPause(bool paused)
        {
            if (paused)
                SaveCurrentGame();
        }

        public static bool TryLoadSavedGame(out string sceneName)
        {
            sceneName = null;
            if (!TryRead(out SaveGameData data) || string.IsNullOrWhiteSpace(data.sceneName))
                return false;
            pendingRestore = data;
            sceneName = data.sceneName;
            return true;
        }

        public static bool DeleteSave()
        {
            string path = Path.Combine(Application.persistentDataPath, "prince-save.json");
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException exception)
            {
                Debug.LogError($"Could not delete the save file at '{path}': {exception.Message}");
                return false;
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogError($"Permission denied while deleting the save file at '{path}': {exception.Message}");
                return false;
            }
            pendingRestore = null;
            return true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            MenuSettings.ApplyAll();
            if (scene.name == MenuSceneName)
                return;
            StartCoroutine(RestoreAndSave(scene));
        }

        private IEnumerator RestoreAndSave(Scene scene)
        {
            yield return null;
            StarterAssets.ThirdPersonController player =
                FindAnyObjectByType<StarterAssets.ThirdPersonController>();
            if (player == null)
            {
                Debug.LogWarning($"Scene '{scene.name}' has no ThirdPersonController; player position was not restored or saved.");
                yield break;
            }

            if (pendingRestore != null && pendingRestore.sceneName == scene.name)
            {
                CharacterController characterController = player.GetComponent<CharacterController>();
                if (characterController != null)
                    characterController.enabled = false;
                player.transform.SetPositionAndRotation(
                    pendingRestore.playerPosition, pendingRestore.playerRotation);
                if (characterController != null)
                    characterController.enabled = true;
                pendingRestore = null;
            }
            SaveCurrentGame();
        }

        private void SaveCurrentGame()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name == MenuSceneName)
                return;
            StarterAssets.ThirdPersonController player =
                FindAnyObjectByType<StarterAssets.ThirdPersonController>();
            if (player == null)
                return;

            var data = new SaveGameData
            {
                sceneName = scene.name,
                playerPosition = player.transform.position,
                playerRotation = player.transform.rotation
            };
            try
            {
                File.WriteAllText(savePath, JsonUtility.ToJson(data, true));
            }
            catch (IOException exception)
            {
                Debug.LogError($"Could not write the save file at '{savePath}': {exception.Message}", this);
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogError($"Permission denied while writing the save file at '{savePath}': {exception.Message}", this);
            }
        }

        private static bool TryRead(out SaveGameData data)
        {
            data = null;
            string path = Path.Combine(Application.persistentDataPath, "prince-save.json");
            if (!File.Exists(path))
                return false;
            try
            {
                data = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(path));
                return data != null && !string.IsNullOrWhiteSpace(data.sceneName);
            }
            catch (Exception exception) when (exception is IOException ||
                                              exception is UnauthorizedAccessException ||
                                              exception is ArgumentException)
            {
                Debug.LogWarning($"Could not read the save file at '{path}': {exception.Message}");
                return false;
            }
        }
    }
}
