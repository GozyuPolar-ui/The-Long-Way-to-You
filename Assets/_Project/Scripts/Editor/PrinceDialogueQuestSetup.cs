using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Prince;
using TargetIndicators;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prince.Editor
{
    internal static class PrinceDialogueQuestSetup
    {
        private const string QuestFolder = "Assets/_Project/Data/Quests";
        private const string QuestAssetPath = QuestFolder + "/Quest_RanselSangPemburu.asset";
        private const string LegacyQuestAssetPath = QuestFolder + "/Quest_TestFetch.asset";
        private const string DialoguePath = "Assets/_Project/Dialogue/TestQuest.yarn";
        private const string BagPrefabPath = "Assets/LowPoly Fantasy Bags/Prefab/Bag.3.prefab";
        private const string BackpackPrefabPath = "Assets/_Project/Prefabs/QuestItem_Backpack.prefab";
        private const string BackpackMaterialPath = "Assets/_Project/Materials/QuestBackpack_Brown.mat";
        private const string GiverPrefabPath = "Assets/_Project/Prefabs/NPC/NPC_Guard_01.prefab";
        private const string QuestIconPath = "Assets/_Project/UI/Map/Icons/MapIcon_Quest.png";
        private const string QuestId = "hunter_backpack";
        private const string BearDefeatObjectiveId = "defeat_guarding_bear";
        private const string BackpackTargetId = "bram_backpack";
        private const string BramTargetId = "bram";
        private const string PickupObjectiveId = "find_bram_backpack";
        private const string ReturnObjectiveId = "return_backpack_to_bram";
        private const float BackpackVisualScale = 0.1f;
        private const string PlacementObjectName = "BackpackQuest_Setup";
        private const string RuntimeObjectName = "BackpackQuest_Runtime";
        private const string LegacyTestObjectName = "QuestTest";

        private const string DialogueSourceText =
            @"title: Bram_Start
---
<<if bear_alive() == ""true"">>
Bram: Kau harus menghadapi beruang itu lebih dulu. Hati-hati.
<<endif>>
<<if quest_state(""hunter_backpack"") == ""NotStarted"">>
Bram: Hei, kau! Jangan lewat situ sembarangan. Eh... pakaianmu bagus sekali. Kau bukan orang sini, ya?
Pangeran: Aku sedang dalam perjalanan menuju kerajaan seberang. Ada yang bisa kubantu?
Bram: Perjalanan jauh, ya. Hmm... Sebenarnya ada. Tadi pagi aku dikejar babi hutan di dekat sungai. Aku lari tunggang-langgang dan ranselku jatuh di sana.
Bram: Di dalamnya ada peta, tali, dan sedikit bekalku. Tanpa itu aku tidak bisa berburu seminggu ini.
Pangeran: Tenang saja. Di mana tepatnya?
-> Aku akan mengambilnya.
    <<start_quest hunter_backpack>>
    <<goto Bram_Accepted>>
-> Seperti apa ransel itu?
    <<goto Bram_BackpackDescription>>
-> Maaf, aku belum bisa sekarang.
    Bram: Tidak apa-apa. Aku tidak ke mana-mana. Kembalilah kapan saja.
<<elseif quest_state(""hunter_backpack"") == ""Active"">>
Bram: Sudah ketemu? Dekat sungai, ranselnya cokelat dengan tali merah. Ikuti jalan setapak saja.
Pangeran: Belum, aku masih mencari.
Bram: Pelan-pelan saja. Hutan ini tidak ke mana-mana.
<<elseif quest_state(""hunter_backpack"") == ""ReadyToTurnIn"">>
Pangeran: Bram, ini ranselmu.
Bram: Itu dia! Astaga, kau benar-benar menemukannya!
Bram: Tali merahnya masih ada. Petaku... masih utuh. Bekalku... yah, tinggal separuh. Pasti dimakan tikus.
Bram: Terima kasih, Nak. Tidak ada yang mau repot membantu pemburu tua seperti aku.
Pangeran: Aku senang bisa membantu.
Bram: Tunggu. Ambil ini, Peta Hutan sebagai hadiah kecil dariku.
<<complete_quest hunter_backpack>>
Bram: Kalau kau lewat desa di timur, sebut namaku. Kau akan disambut dengan baik di sana.
<<else>>
Bram: Hati-hati di jalan. Semoga perjalananmu lancar.
<<endif>>
===
title: Bram_BackpackDescription
---
Bram: Cokelat tua, kulitnya sudah belel, ada tali merah di sisinya. Susah dilewatkan.
Bram: Oh, satu lagi. Babi itu masih berkeliaran di sekitar sana. Jangan sok berani. Kalau pingsan, kau akan terbangun di api unggun. Aku sudah pernah.
-> Aku akan mengambilnya.
    <<start_quest hunter_backpack>>
    <<goto Bram_Accepted>>
-> Maaf, aku belum bisa sekarang.
    Bram: Tidak apa-apa. Aku tidak ke mana-mana. Kembalilah kapan saja.
===
title: Bram_Accepted
---
Bram: Kau baik sekali, Pangeran... eh, maksudku, orang asing. Ikuti jalan setapak ke arah sungai. Ranselnya jatuh dekat situ.
Bram: Hati-hati dengan babi itu. Hindari dulu kalau bisa, hadapi kalau perlu.
===
";

        [MenuItem("Tools/Prince/Setup Backpack Quest")]
        private static void SetupMenu()
        {
            Setup();
        }

        private static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Setup Backpack Quest cannot run in Play mode.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("Open a loaded scene before setting up the backpack quest.");
                return;
            }

            Transform player = FindPlayer(scene);
            if (player == null)
            {
                Debug.LogError("PlayerPrince was not found in the active scene. Add it before running setup.");
                return;
            }

            if (!ValidateAssets(
                    out GameObject bagSource,
                    out GameObject giverSource,
                    out Sprite questIcon,
                    out Shader urpLitShader))
                return;

            EnsureFolder("Assets/_Project/Materials");
            EnsureFolder(QuestFolder);
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder("Assets/_Project/Dialogue");

            if (!UpdateDialogueSource())
                return;

            QuestDefinition quest = CreateOrUpdateQuest();
            if (quest == null)
                return;

            Material backpackMaterial = CreateOrUpdateBackpackMaterial(bagSource, urpLitShader);
            if (backpackMaterial == null ||
                !CreateOrUpdateBackpackPrefab(bagSource, backpackMaterial, quest, questIcon, out GameObject backpackPrefab))
                return;

            GameObject placementObject = GetOrCreatePlacementObject(scene);
            BackpackQuestPlacement placement = GetOrAddComponent<BackpackQuestPlacement>(placementObject);
            Terrain terrain = FindTerrain(scene);
            InitializePlacementIfNeeded(placement, player, terrain);

            RemoveLegacyTestObject(scene, "QuestTest_Barang_Tes");
            RemoveLegacyTestObject(scene, "QuestTest_Giver");

            GameObject bram = GetOrCreateBram(scene, giverSource);
            if (bram == null)
                return;
            ConfigureBram(bram);
            PlaceActor(bram.transform, placement.BramPosition, placementObject.transform);
            Vector3 towardPlayer = player.position - placement.BramPosition;
            towardPlayer.y = 0f;
            bram.transform.rotation = towardPlayer.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(towardPlayer.normalized, Vector3.up)
                : Quaternion.LookRotation(GetPlanarForward(player), Vector3.up);

            GameObject backpack = GetOrCreateBackpack(scene, backpackPrefab);
            if (backpack == null)
                return;
            if (!ConfigureBackpack(backpack, quest, questIcon))
                return;
            PlaceActor(backpack.transform, placement.BackpackPosition, placementObject.transform);

            QuestManager manager = GetOrCreateQuestManager(scene);
            if (manager == null)
                return;
            if (!SetQuestDefinitions(manager, quest))
                return;

            GameObject runtimeObject = GetOrCreateRuntimeObject(scene);
            QuestDialogueUI dialogueUI = FindComponent<QuestDialogueUI>(scene);
            if (dialogueUI == null)
                dialogueUI = GetOrAddComponent<QuestDialogueUI>(runtimeObject);
            QuestTrackingHUD trackingHUD = FindComponent<QuestTrackingHUD>(scene);
            if (trackingHUD == null)
                trackingHUD = GetOrAddComponent<QuestTrackingHUD>(runtimeObject);
            TargetIndicatorManager indicatorManager = GetOrAddComponent<TargetIndicatorManager>(
                trackingHUD.gameObject);
            DialogueInputLock inputLock = player.GetComponentInChildren<DialogueInputLock>(true);
            if (inputLock == null)
                inputLock = Undo.AddComponent<DialogueInputLock>(player.gameObject);
            PlayerInteractor interactor = player.GetComponentInChildren<PlayerInteractor>(true);
            if (interactor == null)
                interactor = Undo.AddComponent<PlayerInteractor>(player.gameObject);
            Camera mainCamera = FindMainCamera(scene);
            if (mainCamera == null)
                Debug.LogWarning("No MainCamera was found in the active scene. Quest tracking will wait for Camera.main.");

            TextAsset dialogueSource = AssetDatabase.LoadAssetAtPath<TextAsset>(DialoguePath);
            if (dialogueSource == null)
            {
                Debug.LogError($"The dialogue importer did not produce a TextAsset at '{DialoguePath}'.");
                return;
            }

            bool referencesSet =
                SetObjectReference(dialogueUI, "dialogueSource", dialogueSource) &
                SetObjectReference(dialogueUI, "questManager", manager) &
                SetObjectReference(dialogueUI, "inputLock", inputLock) &
                SetObjectReference(trackingHUD, "questManager", manager) &
                SetObjectReference(trackingHUD, "indicatorManager", indicatorManager) &
                SetObjectReference(trackingHUD, "targetCamera", mainCamera) &
                SetObjectReference(indicatorManager, "_camera", mainCamera);

            if (!referencesSet)
                return;

            EditorUtility.SetDirty(placement);
            EditorUtility.SetDirty(bram);
            EditorUtility.SetDirty(backpack);
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(dialogueUI);
            EditorUtility.SetDirty(trackingHUD);
            EditorUtility.SetDirty(interactor);
            EditorUtility.SetDirty(inputLock);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Backpack quest setup complete. Bram is placed 2 m ahead and the backpack 30 m ahead of the player " +
                "on the active Terrain. Adjust BackpackQuestPlacement and save the active scene.");
        }

        private static bool ValidateAssets(
            out GameObject bagSource,
            out GameObject giverSource,
            out Sprite questIcon,
            out Shader urpLitShader)
        {
            bagSource = AssetDatabase.LoadAssetAtPath<GameObject>(BagPrefabPath);
            giverSource = AssetDatabase.LoadAssetAtPath<GameObject>(GiverPrefabPath);
            questIcon = AssetDatabase.LoadAssetAtPath<Sprite>(QuestIconPath);
            urpLitShader = Shader.Find("Universal Render Pipeline/Lit");

            var missing = new List<string>();
            if (bagSource == null)
                missing.Add(BagPrefabPath);
            if (giverSource == null)
                missing.Add(GiverPrefabPath);
            if (AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/_Project/Scripts/Runtime/QuestPickup.cs") == null)
                missing.Add("Assets/_Project/Scripts/Runtime/QuestPickup.cs");
            if (AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/_Project/Scripts/Runtime/QuestTarget.cs") == null)
                missing.Add("Assets/_Project/Scripts/Runtime/QuestTarget.cs");
            if (questIcon == null)
                Debug.LogWarning($"Quest map icon was not found at '{QuestIconPath}'. The marker will use its default icon.");
            if (urpLitShader == null)
                missing.Add("Shader 'Universal Render Pipeline/Lit'");

            if (missing.Count == 0)
                return true;

            Debug.LogError("Backpack quest setup is missing required assets:\n- " + string.Join("\n- ", missing));
            return false;
        }

        private static bool UpdateDialogueSource()
        {
            string fullPath = Path.GetFullPath(DialoguePath);
            string currentText = File.Exists(fullPath) ? File.ReadAllText(fullPath) : string.Empty;
            if (!string.Equals(currentText, DialogueSourceText, StringComparison.Ordinal))
            {
                File.WriteAllText(fullPath, DialogueSourceText, new UTF8Encoding(false));
                AssetDatabase.ImportAsset(DialoguePath, ImportAssetOptions.ForceSynchronousImport);
            }

            if (AssetDatabase.LoadAssetAtPath<TextAsset>(DialoguePath) != null)
                return true;

            Debug.LogError($"The dialogue importer did not produce a TextAsset at '{DialoguePath}'.");
            return false;
        }

        private static QuestDefinition CreateOrUpdateQuest()
        {
            QuestDefinition quest = AssetDatabase.LoadAssetAtPath<QuestDefinition>(QuestAssetPath);
            if (quest == null && !File.Exists(Path.GetFullPath(QuestAssetPath)) &&
                File.Exists(Path.GetFullPath(LegacyQuestAssetPath)))
            {
                string moveError = AssetDatabase.MoveAsset(LegacyQuestAssetPath, QuestAssetPath);
                if (!string.IsNullOrEmpty(moveError))
                {
                    Debug.LogError($"Could not migrate the test quest asset: {moveError}");
                    return null;
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                quest = AssetDatabase.LoadAssetAtPath<QuestDefinition>(QuestAssetPath);
            }

            if (quest == null)
            {
                UnityEngine.Object existingAsset = AssetDatabase.LoadMainAssetAtPath(QuestAssetPath);
                if (existingAsset != null)
                {
                    Debug.LogError($"An asset already exists at '{QuestAssetPath}' but is not a QuestDefinition.", existingAsset);
                    return null;
                }

                quest = ScriptableObject.CreateInstance<QuestDefinition>();
                AssetDatabase.CreateAsset(quest, QuestAssetPath);
            }

            quest.name = "Quest_RanselSangPemburu";
            quest.id = QuestId;
            quest.title = "Ransel Sang Pemburu";
            quest.description = "Ransel Bram sudah dikembalikan. Hadiah: Peta Hutan.";
            quest.objectives = new[]
            {
                new QuestObjectiveDefinition
                {
                    id = BearDefeatObjectiveId,
                    description = "Kalahkan beruang yang menjaga ransel.",
                    type = QuestObjectiveType.Defeat,
                    targetId = "guarding_bear"
                },
                new QuestObjectiveDefinition
                {
                    id = PickupObjectiveId,
                    description = "Ambil ransel Bram di dekat sungai.",
                    type = QuestObjectiveType.Collect,
                    targetId = BackpackTargetId
                },
                new QuestObjectiveDefinition
                {
                    id = ReturnObjectiveId,
                    description = "Kembalilah ke Bram dan serahkan ranselnya.",
                    type = QuestObjectiveType.TalkTo,
                    targetId = BramTargetId
                }
            };
            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();
            return quest;
        }

        private static Material CreateOrUpdateBackpackMaterial(GameObject bagSource, Shader shader)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(BackpackMaterialPath);
            if (material == null)
            {
                UnityEngine.Object existingAsset = AssetDatabase.LoadMainAssetAtPath(BackpackMaterialPath);
                if (existingAsset != null)
                {
                    Debug.LogError($"An asset already exists at '{BackpackMaterialPath}' but is not a Material.", existingAsset);
                    return null;
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, BackpackMaterialPath);
            }

            material.shader = shader;
            material.name = "QuestBackpack_Brown";
            material.SetColor("_BaseColor", new Color(0.43f, 0.28f, 0.16f, 1f));
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.18f);

            Renderer sourceRenderer = bagSource.GetComponentInChildren<Renderer>(true);
            Material sourceMaterial = sourceRenderer != null ? sourceRenderer.sharedMaterial : null;
            Texture sourceTexture = sourceMaterial != null ? sourceMaterial.mainTexture : null;
            if (sourceTexture != null)
                material.SetTexture("_BaseMap", sourceTexture);
            else
                Debug.LogWarning($"The source bag prefab '{BagPrefabPath}' has no material texture; using the brown URP material color.");

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static bool CreateOrUpdateBackpackPrefab(
            GameObject bagSource,
            Material material,
            QuestDefinition quest,
            Sprite questIcon,
            out GameObject prefab)
        {
            prefab = null;
            bool isNew = AssetDatabase.LoadAssetAtPath<GameObject>(BackpackPrefabPath) == null;
            if (isNew && AssetDatabase.LoadMainAssetAtPath(BackpackPrefabPath) != null)
            {
                Debug.LogError($"An asset already exists at '{BackpackPrefabPath}' but is not a prefab GameObject.");
                return false;
            }

            GameObject root = isNew
                ? new GameObject("QuestItem_Backpack")
                : PrefabUtility.LoadPrefabContents(BackpackPrefabPath);
            if (root == null)
            {
                Debug.LogError($"Could not load or create the backpack prefab at '{BackpackPrefabPath}'.");
                return false;
            }

            try
            {
                root.name = "QuestItem_Backpack";
                root.transform.localScale = Vector3.one;
                if (root.GetComponentInChildren<Renderer>(true) == null)
                {
                    GameObject visual = PrefabUtility.InstantiatePrefab(bagSource, root.transform) as GameObject;
                    if (visual == null)
                    {
                        Debug.LogError($"Could not instantiate the source bag prefab '{BagPrefabPath}'.", bagSource);
                        return false;
                    }
                    visual.name = "Bag_Visual";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                }

                Transform visualTransform = root.transform.Find("Bag_Visual");
                if (visualTransform == null)
                {
                    Debug.LogError("The backpack prefab is missing its Bag_Visual child.", root);
                    return false;
                }
                visualTransform.localScale = Vector3.one * BackpackVisualScale;

                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    Debug.LogError($"The backpack prefab has no Renderer to display the source bag '{BagPrefabPath}'.", root);
                    return false;
                }
                foreach (Renderer renderer in renderers)
                    renderer.sharedMaterial = material;

                Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
                foreach (Collider collider in colliders)
                    if (!(collider is SphereCollider && collider.gameObject == root))
                        UnityEngine.Object.DestroyImmediate(collider, true);

                SphereCollider pickupCollider = root.GetComponent<SphereCollider>();
                if (pickupCollider == null)
                    pickupCollider = root.AddComponent<SphereCollider>();
                pickupCollider.isTrigger = true;
                pickupCollider.center = Vector3.zero;
                pickupCollider.radius = 0.65f;

                QuestTarget target = root.GetComponent<QuestTarget>();
                if (target == null)
                    target = root.AddComponent<QuestTarget>();
                target.targetId = BackpackTargetId;
                target.displayName = "Ransel Bram";
                target.trackable = true;
                target.icon = questIcon;

                QuestPickup pickup = root.GetComponent<QuestPickup>();
                if (pickup == null)
                    pickup = root.AddComponent<QuestPickup>();
                if (!SetObjectReference(pickup, "questTarget", target) ||
                    !SetObjectReference(pickup, "requiredQuest", quest) ||
                    !SetBoolean(pickup, "requireBearDefeated", true))
                    return false;

                MapMarker marker = root.GetComponent<MapMarker>();
                if (marker == null)
                    marker = root.AddComponent<MapMarker>();
                marker.type = MapMarkerType.Quest;
                marker.displayName = "Ransel Bram";
                marker.icon = questIcon;
                marker.color = new Color(0.92f, 0.68f, 0.2f, 1f);
                marker.visibleThroughFog = false;

                bool saved = PrefabUtility.SaveAsPrefabAsset(root, BackpackPrefabPath, out bool success);
                if (!success || saved == null)
                {
                    Debug.LogError($"Could not save the backpack prefab at '{BackpackPrefabPath}'.");
                    return false;
                }
            }
            finally
            {
                if (isNew)
                    UnityEngine.Object.DestroyImmediate(root);
                else
                    PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BackpackPrefabPath);
            if (prefab == null)
                Debug.LogError($"The backpack prefab could not be reloaded from '{BackpackPrefabPath}'.");
            return prefab != null;
        }

        private static GameObject GetOrCreatePlacementObject(Scene scene)
        {
            GameObject placement = FindGameObject(scene, PlacementObjectName);
            if (placement == null)
            {
                placement = FindGameObject(scene, LegacyTestObjectName);
                if (placement != null)
                {
                    Undo.RecordObject(placement, "Rename Backpack Quest Setup");
                    placement.name = PlacementObjectName;
                }
            }

            if (placement == null)
            {
                placement = new GameObject(PlacementObjectName);
                SceneManager.MoveGameObjectToScene(placement, scene);
                Undo.RegisterCreatedObjectUndo(placement, "Setup Backpack Quest");
            }

            return placement;
        }

        private static void InitializePlacementIfNeeded(
            BackpackQuestPlacement placement,
            Transform player,
            Terrain terrain)
        {
            if (placement.HasInitialPlacement)
                return;

            Vector3 forward = GetPlanarForward(player);
            placement.BramPosition = SampleTerrain(
                player.position + forward * 2f, terrain, player.position.y);
            placement.BackpackPosition = SampleTerrain(
                player.position + forward * 30f, terrain, player.position.y) + Vector3.up * 0.12f;
            placement.GuardAreaPosition = placement.BackpackPosition - Vector3.up * 0.12f;
            placement.GuardAreaRadius = 12f;
            placement.HasInitialPlacement = true;
            EditorUtility.SetDirty(placement);
        }

        private static GameObject GetOrCreateBram(Scene scene, GameObject giverPrefab)
        {
            GameObject bram = FindGameObject(scene, "Bram");
            if (bram != null && GetPrefabSourcePath(bram) != GiverPrefabPath)
            {
                Undo.DestroyObjectImmediate(bram);
                bram = null;
            }

            if (bram == null)
            {
                GameObject legacyGiver = FindGameObject(scene, "QuestTest_Giver");
                if (legacyGiver != null)
                    Undo.DestroyObjectImmediate(legacyGiver);
                bram = PrefabUtility.InstantiatePrefab(giverPrefab, scene) as GameObject;
                if (bram == null)
                {
                    Debug.LogError($"Could not instantiate Bram from '{GiverPrefabPath}'.", giverPrefab);
                    return null;
                }
                Undo.RegisterCreatedObjectUndo(bram, "Setup Backpack Quest");
            }

            bram.name = "Bram";
            return bram;
        }

        private static void ConfigureBram(GameObject bram)
        {
            NPCInfo info = bram.GetComponentInChildren<NPCInfo>(true);
            if (info == null)
                info = GetOrAddComponent<NPCInfo>(bram);
            info.displayName = "Bram";
            info.role = "Pemburu";
            info.dialogueId = "Bram_Start";

            QuestTarget target = bram.GetComponent<QuestTarget>();
            if (target == null)
                target = Undo.AddComponent<QuestTarget>(bram);
            target.targetId = BramTargetId;
            target.displayName = "Bram";
            target.trackable = true;

            EditorUtility.SetDirty(info);
            EditorUtility.SetDirty(target);
            EditorUtility.SetDirty(bram);
        }

        private static GameObject GetOrCreateBackpack(Scene scene, GameObject backpackPrefab)
        {
            GameObject backpack = FindGameObject(scene, "Ransel_Bram");
            if (backpack != null && GetPrefabSourcePath(backpack) != BackpackPrefabPath)
            {
                Undo.DestroyObjectImmediate(backpack);
                backpack = null;
            }

            if (backpack == null)
            {
                GameObject legacyPickup = FindGameObject(scene, "QuestTest_Barang_Tes");
                if (legacyPickup != null)
                    Undo.DestroyObjectImmediate(legacyPickup);
                backpack = PrefabUtility.InstantiatePrefab(backpackPrefab, scene) as GameObject;
                if (backpack == null)
                {
                    Debug.LogError($"Could not instantiate the backpack prefab '{BackpackPrefabPath}'.", backpackPrefab);
                    return null;
                }
                Undo.RegisterCreatedObjectUndo(backpack, "Setup Backpack Quest");
            }

            backpack.name = "Ransel_Bram";
            return backpack;
        }

        private static bool ConfigureBackpack(GameObject backpack, QuestDefinition quest, Sprite questIcon)
        {
            QuestTarget target = backpack.GetComponent<QuestTarget>();
            if (target == null)
                target = Undo.AddComponent<QuestTarget>(backpack);
            target.targetId = BackpackTargetId;
            target.displayName = "Ransel Bram";
            target.icon = questIcon;
            target.trackable = true;

            QuestPickup pickup = backpack.GetComponent<QuestPickup>();
            if (pickup == null)
                pickup = Undo.AddComponent<QuestPickup>(backpack);
            bool referencesSet =
                SetObjectReference(pickup, "questTarget", target) &
                SetObjectReference(pickup, "requiredQuest", quest);
            if (!referencesSet)
                return false;

            MapMarker marker = backpack.GetComponent<MapMarker>();
            if (marker == null)
                marker = Undo.AddComponent<MapMarker>(backpack);
            marker.type = MapMarkerType.Quest;
            marker.displayName = "Ransel Bram";
            marker.icon = questIcon;
            marker.color = new Color(0.92f, 0.68f, 0.2f, 1f);
            marker.visibleThroughFog = false;
            EditorUtility.SetDirty(backpack);
            EditorUtility.SetDirty(target);
            EditorUtility.SetDirty(pickup);
            EditorUtility.SetDirty(marker);
            return true;
        }

        private static QuestManager GetOrCreateQuestManager(Scene scene)
        {
            QuestManager manager = FindComponent<QuestManager>(scene);
            if (manager != null)
            {
                if (manager.gameObject.name == "QuestTest_Manager")
                {
                    Undo.RecordObject(manager.gameObject, "Rename Backpack Quest Manager");
                    manager.gameObject.name = "BackpackQuest_Manager";
                }
                return manager;
            }

            GameObject managerObject = new GameObject("BackpackQuest_Manager");
            SceneManager.MoveGameObjectToScene(managerObject, scene);
            Undo.RegisterCreatedObjectUndo(managerObject, "Setup Backpack Quest");
            return Undo.AddComponent<QuestManager>(managerObject);
        }

        private static bool SetQuestDefinitions(QuestManager manager, QuestDefinition quest)
        {
            var definitions = new List<QuestDefinition>();
            IReadOnlyList<QuestDefinition> currentDefinitions = manager.Definitions;
            if (currentDefinitions != null)
            {
                foreach (QuestDefinition definition in currentDefinitions)
                {
                    if (definition == null || definition == quest ||
                        string.Equals(definition.id, "test_fetch", StringComparison.Ordinal))
                        continue;
                    definitions.Add(definition);
                }
            }
            definitions.Add(quest);
            bool set = SetObjectArray(
                manager, "definitions", definitions.ConvertAll(item => (UnityEngine.Object)item).ToArray());
            if (set)
                EditorUtility.SetDirty(manager);
            return set;
        }

        private static GameObject GetOrCreateRuntimeObject(Scene scene)
        {
            GameObject runtime = FindGameObject(scene, RuntimeObjectName);
            if (runtime == null)
            {
                runtime = FindGameObject(scene, "QuestTest_Runtime");
                if (runtime != null)
                {
                    Undo.RecordObject(runtime, "Rename Backpack Quest Runtime");
                    runtime.name = RuntimeObjectName;
                }
            }
            if (runtime != null)
                return runtime;

            runtime = new GameObject(RuntimeObjectName);
            SceneManager.MoveGameObjectToScene(runtime, scene);
            Undo.RegisterCreatedObjectUndo(runtime, "Setup Backpack Quest");
            return runtime;
        }

        private static void PlaceActor(Transform actor, Vector3 position, Transform parent)
        {
            Undo.RecordObject(actor, "Place Backpack Quest Actor");
            actor.SetParent(parent, true);
            actor.position = position;
            EditorUtility.SetDirty(actor);
        }

        private static bool SetObjectArray(Component component, string propertyName, UnityEngine.Object[] objects)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                Debug.LogError($"Serialized array '{propertyName}' was not found on {component.GetType().Name}.", component);
                return false;
            }

            property.arraySize = objects.Length;
            for (int i = 0; i < objects.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static bool SetObjectReference(Component component, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"Serialized reference '{propertyName}' was not found on {component.GetType().Name}.", component);
                return false;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(target);
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }
            return null;
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

        private static void RemoveLegacyTestObject(Scene scene, string objectName)
        {
            GameObject legacyObject = FindGameObject(scene, objectName);
            if (legacyObject != null)
                Undo.DestroyObjectImmediate(legacyObject);
        }

        private static string GetPrefabSourcePath(GameObject sceneObject)
        {
            UnityEngine.Object source = PrefabUtility.GetCorrespondingObjectFromSource(sceneObject);
            return source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
        }

        private static Transform FindPlayer(Scene scene)
        {
            return FindTransform(scene, "PlayerPrince");
        }

        private static Transform FindTransform(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                    return root.transform;
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == objectName)
                        return child;
            }
            return null;
        }

        private static Terrain FindTerrain(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Terrain terrain = root.GetComponentInChildren<Terrain>(true);
                if (terrain != null)
                    return terrain;
            }
            return null;
        }

        private static Camera FindMainCamera(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
                foreach (Camera camera in cameras)
                    if (camera.CompareTag("MainCamera"))
                        return camera;
            }
            return Camera.main;
        }

        private static Vector3 GetPlanarForward(Transform player)
        {
            Vector3 forward = player.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
        }

        private static Vector3 SampleTerrain(Vector3 position, Terrain terrain, float fallbackY)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogWarning("No Terrain was found in the active scene. Backpack quest placement uses the player's Y coordinate.");
                return new Vector3(position.x, fallbackY, position.z);
            }

            return new Vector3(
                position.x,
                terrain.transform.position.y + terrain.SampleHeight(position),
                position.z);
        }

        private static void EnsureFolder(string assetPath)
        {
            string[] segments = assetPath.Split('/');
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
