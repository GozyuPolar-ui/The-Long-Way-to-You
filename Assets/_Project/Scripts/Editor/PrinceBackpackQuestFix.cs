using System;
using System.Collections.Generic;
using System.IO;
using Prince;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prince.Editor
{
    internal static class PrinceBackpackQuestFix
    {
        private const string QuestAssetPath =
            "Assets/_Project/Data/Quests/Quest_RanselSangPemburu.asset";
        private const string BackpackPrefabPath =
            "Assets/_Project/Prefabs/QuestItem_Backpack.prefab";
        private const string ForestScenePath = "Assets/_Project/Scene/UNS_Demo.unity";
        private const string DialoguePath = "Assets/_Project/Dialogue/TestQuest.yarn";
        private const string BackupFolder = "Assets/_Project/Docs/QuestFixBackup~";
        private const string QuestId = "hunter_backpack";
        private const string BearTargetId = "guarding_bear";
        private const string PickupTargetId = "bram_backpack";
        private const string BearObjectiveId = "defeat_guarding_bear";
        private const string PickupObjectiveId = "find_bram_backpack";
        private const string ReturnObjectiveId = "return_backpack_to_bram";

        [MenuItem("Tools/Prince/Fix Backpack Quest Flow")]
        private static void FixBackpackQuestFlow()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Fix Backpack Quest Flow cannot run in Play mode.");
                return;
            }

            Scene loadedForest = SceneManager.GetSceneByPath(ForestScenePath);
            if (loadedForest.IsValid() && loadedForest.isLoaded &&
                loadedForest.isDirty)
            {
                bool saveAndContinue = EditorUtility.DisplayDialog(
                    "Unsaved Forest Scene",
                    "The forest scene has unsaved changes. Save them before applying the backpack quest setup?",
                    "Save and Continue",
                    "Cancel");
                if (!saveAndContinue)
                    return;

                if (!EditorSceneManager.SaveScene(loadedForest, ForestScenePath))
                {
                    Debug.LogError(
                        $"Could not save unsaved changes in '{ForestScenePath}'. Backpack quest setup was not applied.");
                    return;
                }
            }

            if (!CreateBackups())
                return;

            QuestDefinition quest = AssetDatabase.LoadAssetAtPath<QuestDefinition>(QuestAssetPath);
            if (quest == null)
            {
                Debug.LogError($"Quest definition was not found at '{QuestAssetPath}'.");
                return;
            }
            UpdateQuestDefinition(quest);

            if (!UpdateBackpackPrefab(quest))
                return;

            AssetDatabase.ImportAsset(DialoguePath, ImportAssetOptions.ForceSynchronousImport);
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene forestScene = SceneManager.GetSceneByPath(ForestScenePath);
            bool openedScene = !forestScene.IsValid() || !forestScene.isLoaded;
            if (openedScene)
                forestScene = EditorSceneManager.OpenScene(ForestScenePath, OpenSceneMode.Additive);

            if (!forestScene.IsValid() || !forestScene.isLoaded)
            {
                Debug.LogError($"Could not open the forest scene '{ForestScenePath}'.");
                return;
            }

            bool saved = false;
            try
            {
                SceneManager.SetActiveScene(forestScene);
                QuestManager manager = FindComponent<QuestManager>(forestScene);
                QuestPickup backpack = FindComponent<QuestPickup>(forestScene);
                if (manager == null)
                {
                    Debug.LogError(
                        $"The forest scene has no QuestManager. No replacement quest manager was created.");
                    return;
                }

                if (!EnsureQuestRegistered(manager, quest))
                    return;
                if (backpack == null)
                    backpack = CreateBackpack(forestScene);
                if (backpack == null)
                    return;

                BearAI bear = FindGuardBear(forestScene, backpack);
                if (!ConfigureBearTargets(forestScene, bear))
                    return;
                OrientGuardBearTowardBram(bear, forestScene);
                if (!ConfigureBackpackPickup(backpack, quest, bear))
                    return;
                ConfigureCompletionTrigger(manager, quest);
                EnsureSceneFader(forestScene);
                if (!ConfigureDialogue(forestScene, manager))
                    return;

                EditorSceneManager.MarkSceneDirty(forestScene);
                saved = EditorSceneManager.SaveScene(forestScene, ForestScenePath);
                if (!saved)
                    Debug.LogError($"Could not save the forest scene '{ForestScenePath}'.");
            }
            finally
            {
                RestoreEditorScene(previousActiveScene, forestScene, openedScene);
            }

            if (!saved)
                return;

            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Backpack quest flow fixed. The backpack, quest objectives, guarding bear, tracker target, and forest scene were updated without duplicating scene objects.");
        }

        private static bool CreateBackups()
        {
            string[] paths = { QuestAssetPath, BackpackPrefabPath, ForestScenePath };
            Directory.CreateDirectory(BackupFolder);
            foreach (string path in paths)
            {
                string source = Path.GetFullPath(path);
                string backup = Path.Combine(
                    Path.GetFullPath(BackupFolder),
                    Path.GetFileName(path) + ".bak");
                if (!File.Exists(source))
                {
                    Debug.LogError($"Cannot create a quest backup because '{path}' does not exist.");
                    return false;
                }
                if (File.Exists(backup))
                    continue;

                try
                {
                    File.Copy(source, backup, false);
                    Debug.Log($"Created pre-change backup at '{backup}'.");
                }
                catch (IOException exception)
                {
                    Debug.LogError($"Could not create backup '{backup}': {exception.Message}");
                    return false;
                }
                catch (UnauthorizedAccessException exception)
                {
                    Debug.LogError($"Could not create backup '{backup}': {exception.Message}");
                    return false;
                }
            }

            AssetDatabase.Refresh();
            return true;
        }

        private static void UpdateQuestDefinition(QuestDefinition quest)
        {
            quest.id = QuestId;
            quest.title = "Ransel Sang Pemburu";
            quest.objectives = new[]
            {
                new QuestObjectiveDefinition
                {
                    id = BearObjectiveId,
                    description = "Kalahkan beruang yang menjaga ransel.",
                    type = QuestObjectiveType.Defeat,
                    targetId = BearTargetId
                },
                new QuestObjectiveDefinition
                {
                    id = PickupObjectiveId,
                    description = "Ambil ransel Bram di dekat sungai.",
                    type = QuestObjectiveType.Collect,
                    targetId = PickupTargetId
                },
                new QuestObjectiveDefinition
                {
                    id = ReturnObjectiveId,
                    description = "Kembalilah ke Bram dan serahkan ranselnya.",
                    type = QuestObjectiveType.TalkTo,
                    targetId = "bram"
                }
            };
            EditorUtility.SetDirty(quest);
        }

        private static bool UpdateBackpackPrefab(QuestDefinition quest)
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(BackpackPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Could not load the backpack prefab at '{BackpackPrefabPath}'.");
                return false;
            }

            try
            {
                QuestTarget target = prefab.GetComponent<QuestTarget>();
                if (target == null)
                    target = prefab.AddComponent<QuestTarget>();
                QuestPickup pickup = prefab.GetComponent<QuestPickup>();
                if (pickup == null)
                    pickup = prefab.AddComponent<QuestPickup>();

                target.targetId = PickupTargetId;
                target.trackable = true;
                if (!SetObjectReference(pickup, "questTarget", target) ||
                    !SetObjectReference(pickup, "requiredQuest", quest) ||
                    !SetObjectReference(pickup, "bearGuard", null) ||
                    !SetBoolean(pickup, "requireBearDefeated", true))
                    return false;

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(
                    prefab, BackpackPrefabPath, out bool success);
                if (!success || savedPrefab == null)
                {
                    Debug.LogError($"Could not save the backpack prefab at '{BackpackPrefabPath}'.");
                    return false;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            return true;
        }

        private static bool EnsureQuestRegistered(QuestManager manager, QuestDefinition quest)
        {
            SerializedObject serialized = new SerializedObject(manager);
            SerializedProperty definitions = serialized.FindProperty("definitions");
            if (definitions == null || !definitions.isArray)
            {
                Debug.LogError("QuestManager has no serialized quest definitions array.", manager);
                return false;
            }

            for (int i = 0; i < definitions.arraySize; i++)
                if (definitions.GetArrayElementAtIndex(i).objectReferenceValue == quest)
                    return true;

            int index = definitions.arraySize;
            definitions.InsertArrayElementAtIndex(index);
            definitions.GetArrayElementAtIndex(index).objectReferenceValue = quest;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(manager);
            return true;
        }

        private static bool ConfigureBearTargets(Scene scene, BearAI guardBear)
        {
            foreach (BearAI bear in FindComponents<BearAI>(scene))
                if (!SetString(bear, "questTargetId", bear == guardBear ? BearTargetId : string.Empty))
                    return false;
            return true;
        }

        private static bool ConfigureBackpackPickup(
            QuestPickup backpack,
            QuestDefinition quest,
            BearAI bear)
        {
            QuestTarget target = backpack.GetComponent<QuestTarget>();
            if (target == null)
                target = Undo.AddComponent<QuestTarget>(backpack.gameObject);
            target.targetId = PickupTargetId;
            target.displayName = "Ransel Bram";
            target.trackable = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);

            if (!SetObjectReference(backpack, "questTarget", target) ||
                !SetObjectReference(backpack, "requiredQuest", quest) ||
                !SetObjectReference(backpack, "bearGuard", bear) ||
                !SetBoolean(backpack, "requireBearDefeated", true))
                return false;

            PrefabUtility.RecordPrefabInstancePropertyModifications(backpack);
            EditorUtility.SetDirty(backpack);
            if (bear == null)
                Debug.LogWarning(
                    "No BearAI was found in the backpack guard area. Pickup will remain available to prevent a soft-lock.",
                    backpack);
            return true;
        }

        private static BearAI FindGuardBear(Scene scene, QuestPickup backpack)
        {
            if (backpack.GuardBear != null)
                return backpack.GuardBear;

            BackpackQuestPlacement placement = FindComponent<BackpackQuestPlacement>(scene);
            BearAI[] bears = FindComponents<BearAI>(scene);
            BearAI nearest = null;
            float closestDistanceSquared = float.PositiveInfinity;
            float radiusSquared = placement != null
                ? placement.GuardAreaRadius * placement.GuardAreaRadius
                : float.PositiveInfinity;
            foreach (BearAI bear in bears)
            {
                float distanceSquared = placement != null
                    ? (bear.transform.position - placement.GuardAreaPosition).sqrMagnitude
                    : (bear.transform.position - backpack.transform.position).sqrMagnitude;
                if (placement == null && bears.Length != 1)
                    continue;
                if (distanceSquared > radiusSquared || distanceSquared >= closestDistanceSquared)
                    continue;
                closestDistanceSquared = distanceSquared;
                nearest = bear;
            }

            return nearest;
        }

        private static void OrientGuardBearTowardBram(BearAI bear, Scene scene)
        {
            if (bear == null)
                return;

            BackpackQuestPlacement placement = FindComponent<BackpackQuestPlacement>(scene);
            if (placement == null)
            {
                Debug.LogWarning(
                    "BackpackQuestPlacement is missing; the guarding bear's current facing direction was preserved.",
                    bear);
                return;
            }

            Vector3 direction = placement.BramPosition - bear.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                Debug.LogWarning(
                    "Bram and the guarding bear share the same position; the bear's current facing direction was preserved.",
                    bear);
                return;
            }

            Undo.RecordObject(bear.transform, "Face Guard Bear Toward Bram");
            bear.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            PrefabUtility.RecordPrefabInstancePropertyModifications(bear.transform);
            EditorUtility.SetDirty(bear.transform);
        }

        private static QuestPickup CreateBackpack(Scene scene)
        {
            GameObject existing = FindGameObject(scene, "Ransel_Bram");
            if (existing != null)
            {
                QuestPickup existingPickup = existing.GetComponent<QuestPickup>();
                if (existingPickup == null)
                    Debug.LogError(
                        "An object named 'Ransel_Bram' already exists without QuestPickup. It was left untouched to avoid duplication.",
                        existing);
                return existingPickup;
            }

            BackpackQuestPlacement placement = FindComponent<BackpackQuestPlacement>(scene);
            if (placement == null)
            {
                Debug.LogError(
                    "BackpackQuestPlacement is missing; the backpack was not created at a guessed position.");
                return null;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BackpackPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Could not load the backpack prefab at '{BackpackPrefabPath}'.");
                return null;
            }

            GameObject backpack = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (backpack == null)
            {
                Debug.LogError($"Could not instantiate the backpack prefab at '{BackpackPrefabPath}'.", prefab);
                return null;
            }

            Undo.RegisterCreatedObjectUndo(backpack, "Fix Backpack Quest Flow");
            Undo.RecordObject(backpack.transform, "Place Backpack for Quest");
            backpack.name = "Ransel_Bram";
            backpack.transform.SetParent(placement.transform, true);
            backpack.transform.position = placement.BackpackPosition;
            PrefabUtility.RecordPrefabInstancePropertyModifications(backpack.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(backpack);
            QuestPickup pickup = backpack.GetComponent<QuestPickup>();
            if (pickup == null)
            {
                Debug.LogError("The backpack prefab has no QuestPickup component.", backpack);
                Undo.DestroyObjectImmediate(backpack);
                return null;
            }

            EditorUtility.SetDirty(backpack);
            return pickup;
        }

        private static void ConfigureCompletionTrigger(QuestManager manager, QuestDefinition quest)
        {
            QuestCompleteTrigger trigger = FindComponent<QuestCompleteTrigger>(manager.gameObject.scene);
            if (trigger == null)
                trigger = Undo.AddComponent<QuestCompleteTrigger>(manager.gameObject);
            trigger.Configure(
                quest.id, ReturnObjectiveId, PickupTargetId, "Closing", 1.8f, 0.7f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(trigger);
            EditorUtility.SetDirty(trigger);
        }

        private static bool ConfigureDialogue(Scene scene, QuestManager manager)
        {
            QuestDialogueUI dialogue = FindComponent<QuestDialogueUI>(scene);
            if (dialogue == null)
            {
                Debug.LogError("QuestDialogueUI is missing from the forest scene.");
                return false;
            }

            TextAsset source = AssetDatabase.LoadAssetAtPath<TextAsset>(DialoguePath);
            if (source == null)
            {
                Debug.LogError($"Could not import dialogue source '{DialoguePath}'.", dialogue);
                return false;
            }

            if (!SetObjectReference(dialogue, "dialogueSource", source) ||
                !SetObjectReference(dialogue, "questManager", manager))
                return false;
            EditorUtility.SetDirty(dialogue);
            return true;
        }

        private static void EnsureSceneFader(Scene scene)
        {
            SceneFader fader = FindComponent<SceneFader>(scene);
            GameObject canvasObject = fader != null
                ? fader.gameObject
                : FindGameObject(scene, "SceneFaderCanvas");
            if (canvasObject == null)
            {
                canvasObject = new GameObject("SceneFaderCanvas", typeof(RectTransform));
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
                Undo.RegisterCreatedObjectUndo(canvasObject, "Add Scene Fader");
            }

            Canvas canvas = GetOrAddComponent<Canvas>(canvasObject);
            CanvasGroup group = GetOrAddComponent<CanvasGroup>(canvasObject);
            Image image = GetOrAddComponent<Image>(canvasObject);
            GetOrAddComponent<GraphicRaycaster>(canvasObject);
            GetOrAddComponent<SceneFader>(canvasObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            group.alpha = 1f;
            group.blocksRaycasts = true;
            image.color = Color.black;
            image.raycastTarget = true;
            RectTransform rect = canvasObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static bool SetObjectReference(
            Component component,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError(
                    $"Serialized reference '{propertyName}' was not found on {component.GetType().Name}.",
                    component);
                return false;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            return true;
        }

        private static bool SetBoolean(Component component, string propertyName, bool value)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"Serialized field '{propertyName}' was not found on {component.GetType().Name}.", component);
                return false;
            }

            property.boolValue = value;
            serialized.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            return true;
        }

        private static bool SetString(Component component, string propertyName, string value)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"Serialized field '{propertyName}' was not found on {component.GetType().Name}.", component);
                return false;
            }

            property.stringValue = value;
            serialized.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorUtility.SetDirty(component);
            return true;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(gameObject);
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

            foreach (GameObject root in scene.GetRootGameObjects())
                components.AddRange(root.GetComponentsInChildren<T>(true));
            return components.ToArray();
        }

        private static GameObject FindGameObject(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                    return root;
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == objectName)
                        return child.gameObject;
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
    }
}
