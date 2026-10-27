using System;
using System.Collections.Generic;
using System.IO;
using Prince;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace Prince.Editor
{
    internal static class PrinceClosingSceneSetup
    {
        private const string MainMenuScenePath = "Assets/_Project/Scene/Main-Menu.unity";
        private const string ForestScenePath = "Assets/_Project/Scene/UNS_Demo.unity";
        private const string ClosingScenePath = "Assets/_Project/Scene/Closing.unity";
        private const string ClosingDataPath = "Assets/_Project/Data/Closing/ClosingData.asset";
        private const string FeedbackQrPath = "Assets/_Project/UI/Feedback/qrcode.png";
        private const string BackupFolder = "Assets/_Project/Docs/ClosingBackup~";
        private const string RuntimeObjectName = "BackpackQuest_Runtime";
        private const string QuestId = "hunter_backpack";
        private const string BackpackObjectiveId = "find_bram_backpack";
        private const string BackpackTargetId = "bram_backpack";
        private const string MenuFontPath =
            "Assets/_Project/Fonts/TMP/CormorantGaramond-SemiBold SDF.asset";
        private const string TitleFontPath =
            "Assets/_Project/Fonts/TMP/CinzelDecorative-Bold SDF.asset";

        [MenuItem("Tools/Prince/Setup Closing Scene")]
        private static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Setup Closing Scene cannot run in Play mode.");
                return;
            }

            if (!ValidateSources())
                return;

            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Closing");
            EnsureFolder("Assets/_Project/Docs");
            Directory.CreateDirectory(BackupFolder);

            ClosingData data = CreateOrLoadData();
            if (data == null)
                return;
            if (!AssignFeedbackQr(data))
                return;

            if (!CreateBackupIfMissing(ForestScenePath) ||
                !CreateBackupIfMissing("ProjectSettings/EditorBuildSettings.asset"))
                return;

            if (!CreateOrUpdateClosingScene(data))
                return;
            if (!InstallForestComponents(data))
                return;

            if (!AddClosingSceneToBuildSettings())
                return;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Closing Scene setup complete. Main-Menu, quest/pickup source scripts, combat scripts, " +
                "SceneLoader, and loading-screen assets were not modified.");
        }

        [MenuItem("Tools/Prince/Reset Closing Quest Progress")]
        private static void ResetQuestProgress()
        {
            Prince.Closing.QuestProgress.Reset();
            Debug.Log("Closing quest completion flag reset. The backpack quest can be tested again.");
        }

        private static bool ValidateSources()
        {
            string[] requiredPaths =
            {
                MainMenuScenePath,
                ForestScenePath,
                "Assets/_Project/Scripts/Runtime/UI/MainMenuController.cs",
                "Assets/_Project/Scripts/Runtime/UI/MenuVideoBackground.cs",
                "Assets/_Project/Scripts/Runtime/UI/MenuMusicPlayer.cs",
                FeedbackQrPath,
                MenuFontPath,
                TitleFontPath,
                "Assets/_Project/UI/MainMenu/gemini_generated_video_728ee3eb.mp4",
                "Assets/_Project/Audio/MAIN MENU/royaltyfreemusicstudio-medieval-fantasy-music-462199.mp3"
            };

            List<string> missing = new List<string>();
            for (int i = 0; i < requiredPaths.Length; i++)
            {
                if (!File.Exists(requiredPaths[i]) &&
                    AssetDatabase.LoadMainAssetAtPath(requiredPaths[i]) == null)
                    missing.Add(requiredPaths[i]);
            }

            if (missing.Count == 0)
                return true;

            Debug.LogError("Closing setup is missing required source assets:\n- " +
                           string.Join("\n- ", missing));
            return false;
        }

        private static bool AssignFeedbackQr(ClosingData data)
        {
            TextureImporter importer = AssetImporter.GetAtPath(FeedbackQrPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError(
                    $"Could not import feedback QR image '{FeedbackQrPath}' as a texture.");
                return false;
            }

            bool needsReimport =
                importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.textureCompression != TextureImporterCompression.Uncompressed ||
                importer.filterMode != FilterMode.Point;
            if (needsReimport)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            Sprite qrSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FeedbackQrPath);
            if (qrSprite == null)
            {
                Debug.LogError(
                    $"Feedback QR image '{FeedbackQrPath}' did not import as a Sprite.");
                return false;
            }

            data.ConfigureFeedbackQr(qrSprite);
            EditorUtility.SetDirty(data);
            return true;
        }

        private static ClosingData CreateOrLoadData()
        {
            ClosingData data = AssetDatabase.LoadAssetAtPath<ClosingData>(ClosingDataPath);
            if (data != null)
                return data;

            data = ScriptableObject.CreateInstance<ClosingData>();
            data.ConfigureDefaults();
            AssetDatabase.CreateAsset(data, ClosingDataPath);
            EditorUtility.SetDirty(data);
            return data;
        }

        private static bool CreateOrUpdateClosingScene(ClosingData data)
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(ClosingScenePath))
            {
                if (!AssetDatabase.CopyAsset(MainMenuScenePath, ClosingScenePath))
                {
                    Debug.LogError(
                        $"Could not copy the original Main Menu scene to '{ClosingScenePath}'.");
                    return false;
                }
            }

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene closingScene = SceneManager.GetSceneByPath(ClosingScenePath);
            bool openedScene = !closingScene.IsValid() || !closingScene.isLoaded;
            if (openedScene)
                closingScene = EditorSceneManager.OpenScene(ClosingScenePath, OpenSceneMode.Additive);

            bool success = false;
            try
            {
                SceneManager.SetActiveScene(closingScene);
                if (!ValidateCopiedMenu(closingScene))
                    return false;

                RemoveMainMenuBehavior(closingScene);
                RemoveSceneFaders(closingScene);
                ConfigureVideoAudio(closingScene);

                Canvas canvas = FindComponent<Canvas>(closingScene);
                if (canvas == null)
                {
                    Debug.LogError(
                        $"The copied scene '{ClosingScenePath}' has no Canvas; it was not saved.");
                    return false;
                }

                ClosingSceneController controller =
                    canvas.GetComponent<ClosingSceneController>();
                if (controller == null)
                    controller = Undo.AddComponent<ClosingSceneController>(canvas.gameObject);

                TMP_FontAsset bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MenuFontPath);
                TMP_FontAsset headingFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFontPath);
                controller.Configure(data, bodyFont, headingFont);
                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(closingScene);
                success = EditorSceneManager.SaveScene(closingScene, ClosingScenePath);
                if (!success)
                    Debug.LogError($"Could not save the copied Closing scene at '{ClosingScenePath}'.");
            }
            finally
            {
                RestoreEditorScene(previousActiveScene, closingScene, openedScene);
            }

            return success;
        }

        private static bool ValidateCopiedMenu(Scene scene)
        {
            AudioListener[] listeners = FindComponents<AudioListener>(scene);
            int activeListeners = 0;
            for (int i = 0; i < listeners.Length; i++)
                if (listeners[i] != null && listeners[i].isActiveAndEnabled)
                    activeListeners++;

            VideoPlayer videoPlayer = FindComponent<VideoPlayer>(scene);
            MenuVideoBackground videoBackground = FindComponent<MenuVideoBackground>(scene);
            MenuMusicPlayer musicPlayer = FindComponent<MenuMusicPlayer>(scene);
            AudioSource musicSource = musicPlayer != null
                ? musicPlayer.GetComponent<AudioSource>()
                : null;

            List<string> missing = new List<string>();
            if (activeListeners != 1)
                missing.Add($"exactly one active AudioListener (found {activeListeners})");
            if (videoPlayer == null || videoPlayer.clip == null)
                missing.Add("a VideoPlayer with its video clip assigned");
            if (videoBackground == null)
                missing.Add("the MenuVideoBackground component");
            if (musicPlayer == null || musicSource == null || musicSource.clip == null)
                missing.Add("MenuMusicPlayer with an AudioSource clip assigned");

            if (missing.Count == 0)
                return true;

            Debug.LogError(
                $"The copied Main Menu is missing required Closing Scene components/assets: " +
                string.Join(", ", missing) + ". No Closing scene changes were saved.");
            return false;
        }

        private static void RemoveMainMenuBehavior(Scene scene)
        {
            MainMenuController[] controllers = FindComponents<MainMenuController>(scene);
            for (int i = 0; i < controllers.Length; i++)
                if (controllers[i] != null)
                    Undo.DestroyObjectImmediate(controllers[i]);
        }

        private static void RemoveSceneFaders(Scene scene)
        {
            SceneFader[] faders = FindComponents<SceneFader>(scene);
            HashSet<GameObject> removed = new HashSet<GameObject>();
            for (int i = 0; i < faders.Length; i++)
            {
                if (faders[i] == null || !removed.Add(faders[i].gameObject))
                    continue;
                Undo.DestroyObjectImmediate(faders[i].gameObject);
            }
        }

        private static void ConfigureVideoAudio(Scene scene)
        {
            MenuVideoBackground[] backgrounds = FindComponents<MenuVideoBackground>(scene);
            for (int i = 0; i < backgrounds.Length; i++)
            {
                SerializedObject serialized = new SerializedObject(backgrounds[i]);
                SerializedProperty useAudio = serialized.FindProperty("useVideoAudio");
                if (useAudio == null)
                {
                    Debug.LogWarning(
                        "Copied menu video has no useVideoAudio setting; its audio route was left unchanged.",
                        backgrounds[i]);
                    continue;
                }

                useAudio.boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(backgrounds[i]);
            }
        }

        private static bool InstallForestComponents(ClosingData data)
        {
            if (!File.Exists(ForestScenePath))
            {
                Debug.LogError($"Forest scene was not found at '{ForestScenePath}'.");
                return false;
            }

            if (!CreateBackupIfMissing(ForestScenePath))
                return false;

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene forestScene = SceneManager.GetSceneByPath(ForestScenePath);
            bool openedScene = !forestScene.IsValid() || !forestScene.isLoaded;
            if (openedScene)
                forestScene = EditorSceneManager.OpenScene(ForestScenePath, OpenSceneMode.Additive);

            bool success = false;
            try
            {
                SceneManager.SetActiveScene(forestScene);
                GameObject runtimeObject = FindGameObject(forestScene, RuntimeObjectName);
                if (runtimeObject == null)
                {
                    Debug.LogError(
                        $"Expected quest runtime object '{RuntimeObjectName}' was not found in the forest scene. " +
                        $"No quest trigger was installed in '{ForestScenePath}'.");
                    return false;
                }

                QuestCompleteTrigger trigger =
                    runtimeObject.GetComponent<QuestCompleteTrigger>();
                if (trigger == null)
                    trigger = Undo.AddComponent<QuestCompleteTrigger>(runtimeObject);
                trigger.Configure(
                    QuestId, BackpackObjectiveId, BackpackTargetId, "Closing",
                    data.BagTakenDelay, data.TransitionFadeDuration);
                EditorUtility.SetDirty(trigger);

                PostQuestExploreMode exploreMode =
                    runtimeObject.GetComponent<PostQuestExploreMode>();
                if (exploreMode == null)
                    exploreMode = Undo.AddComponent<PostQuestExploreMode>(runtimeObject);
                exploreMode.Configure(true, BackpackTargetId);
                EditorUtility.SetDirty(exploreMode);

                EditorSceneManager.MarkSceneDirty(forestScene);
                success = EditorSceneManager.SaveScene(forestScene, ForestScenePath);
                if (!success)
                    Debug.LogError($"Could not save the quest components to '{ForestScenePath}'.");
            }
            finally
            {
                RestoreEditorScene(previousActiveScene, forestScene, openedScene);
            }

            return success;
        }

        private static bool CreateBackupIfMissing(string sourcePath)
        {
            string backupPath = Path.Combine(BackupFolder, Path.GetFileName(sourcePath) + ".bak");
            if (File.Exists(backupPath))
                return true;

            try
            {
                File.Copy(sourcePath, backupPath, false);
                AssetDatabase.Refresh();
                Debug.Log($"Created pre-change scene backup at '{backupPath}'.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not create required backup '{backupPath}': {exception.Message}");
                return false;
            }
        }

        private static bool AddClosingSceneToBuildSettings()
        {
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            for (int i = 0; i < current.Length; i++)
            {
                if (string.Equals(current[i].path, ClosingScenePath, StringComparison.OrdinalIgnoreCase))
                {
                    if (!current[i].enabled)
                    {
                        current[i] = new EditorBuildSettingsScene(ClosingScenePath, true);
                        EditorBuildSettings.scenes = current;
                    }
                    return true;
                }
            }

            EditorBuildSettingsScene[] updated =
                new EditorBuildSettingsScene[current.Length + 1];
            Array.Copy(current, updated, current.Length);
            updated[current.Length] = new EditorBuildSettingsScene(ClosingScenePath, true);
            EditorBuildSettings.scenes = updated;
            Debug.Log($"Added '{ClosingScenePath}' to Build Settings without changing existing scene entries.");
            return true;
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            T[] components = FindComponents<T>(scene);
            return components.Length > 0 ? components[0] : null;
        }

        private static T[] FindComponents<T>(Scene scene) where T : Component
        {
            List<T> components = new List<T>();
            if (!scene.IsValid() || !scene.isLoaded)
                return components.ToArray();

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                components.AddRange(roots[i].GetComponentsInChildren<T>(true));
            return components.ToArray();
        }

        private static GameObject FindGameObject(Scene scene, string objectName)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                    return roots[i];

                Transform[] children = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < children.Length; j++)
                    if (children[j].name == objectName)
                        return children[j].gameObject;
            }

            return null;
        }

        private static void RestoreEditorScene(Scene previous, Scene edited, bool opened)
        {
            if (previous.IsValid() && previous.isLoaded)
                SceneManager.SetActiveScene(previous);
            if (opened && edited.IsValid() && edited.isLoaded)
                EditorSceneManager.CloseScene(edited, true);
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }
    }
}
