using System;
using System.Collections.Generic;
using System.IO;
using Prince;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prince.Editor
{
    internal static class PrinceBearEnemySetup
    {
        private const string ForestScenePath = "Assets/_Project/Scene/UNS_Demo.unity";
        private const string SourcePrefabPath =
            "Assets/Blink/Art/Animals/Stylized/Bear/Bear_Prefabs/Bear_4.prefab";
        private const string SourceMaterialPath =
            "Assets/Blink/Art/Animals/Stylized/Bear/Bear_Materials/Bear_4.mat";
        private const string BearPrefabPath = "Assets/_Project/Prefabs/Bear_Enemy.prefab";
        private const string BearMaterialPath = "Assets/_Project/Materials/Bear/Bear_Light_URP.mat";
        private const string AnimatorPath = "Assets/_Project/Animations/Bear/Bear_Animator.controller";
        private const string GeneratedAnimationsPath = "Assets/_Project/Animations/Bear/Clips";
        private const string NavMeshAssetPath = "Assets/_Project/Data/Forest_NavMesh.asset";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string GrowlClipPath = "Assets/_Project/Audio/SFX/Bear/gargamel10-growling-bear-379127.mp3";

        [MenuItem("Tools/Prince/Setup Bear Enemy")]
        private static void SetupBearEnemy()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Setup Bear Enemy cannot run in Play mode.");
                return;
            }

            if (!ValidateBearAssets(out GameObject sourcePrefab, out Material sourceMaterial))
                return;

            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder("Assets/_Project/Materials");
            EnsureFolder("Assets/_Project/Materials/Bear");
            EnsureFolder("Assets/_Project/Animations");
            EnsureFolder("Assets/_Project/Animations/Bear");
            EnsureFolder(GeneratedAnimationsPath);

            Material lightMaterial = CreateOrUpdateLightMaterial(sourceMaterial);
            if (lightMaterial == null)
                return;

            Dictionary<string, AnimationClip> clips = CreateOrUpdateAnimationClips();
            AnimatorController animatorController = CreateOrUpdateAnimatorController(clips);
            if (animatorController == null)
                return;

            GameObject bearPrefab = CreateOrUpdateBearPrefab(sourcePrefab, lightMaterial, animatorController);
            if (bearPrefab == null)
                return;

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene forestScene = SceneManager.GetSceneByPath(ForestScenePath);
            bool openedForestScene = !forestScene.IsValid() || !forestScene.isLoaded;
            if (openedForestScene)
                forestScene = EditorSceneManager.OpenScene(ForestScenePath, OpenSceneMode.Additive);

            if (!forestScene.IsValid() || !forestScene.isLoaded)
            {
                Debug.LogError($"Could not open the forest scene '{ForestScenePath}'.");
                return;
            }

            try
            {
                BackpackQuestPlacement placement = FindComponent<BackpackQuestPlacement>(forestScene);
                QuestPickup backpack = FindComponent<QuestPickup>(forestScene);
                Terrain terrain = FindComponent<Terrain>(forestScene);
                if (placement == null && backpack == null)
                {
                    Debug.LogError(
                        "No BackpackQuestPlacement or QuestPickup exists in the forest scene. " +
                        "Run Tools > Prince > Setup Backpack Quest first.");
                    return;
                }

                Vector3 backpackPosition = backpack != null
                    ? backpack.transform.position
                    : placement.BackpackPosition;
                Vector3 guardCenter = placement != null
                    ? placement.GuardAreaPosition
                    : backpackPosition;
                float guardRadius = placement != null ? placement.GuardAreaRadius : 12f;
                Vector3 bramPosition = placement != null ? placement.BramPosition : guardCenter;

                GameObject existingBear = FindGameObject(forestScene, "Bear_Enemy");
                if (existingBear != null)
                {
                    BearAI existingAI = existingBear.GetComponent<BearAI>();
                    if (existingAI == null)
                    {
                        Debug.LogWarning(
                            "An object named 'Bear_Enemy' already exists without BearAI. " +
                            "It was left untouched to preserve its manual setup.",
                            existingBear);
                    }
                    else
                    {
                        Debug.Log(
                            "Bear_Enemy already exists. Its scene placement was preserved; no duplicate was created.",
                            existingBear);
                    }
                }
                else
                {
                    Vector3 spawnPosition = CalculateBearPosition(
                        backpackPosition, guardCenter, bramPosition, guardRadius, terrain);
                    GameObject bear = PrefabUtility.InstantiatePrefab(bearPrefab, forestScene) as GameObject;
                    if (bear == null)
                    {
                        Debug.LogError($"Could not instantiate '{BearPrefabPath}'.", bearPrefab);
                        return;
                    }

                    bear.name = "Bear_Enemy";
                    bear.transform.SetPositionAndRotation(
                        spawnPosition,
                        Quaternion.LookRotation(PlanarDirection(backpackPosition - spawnPosition), Vector3.up));
                    BearAI ai = bear.GetComponent<BearAI>();
                    if (ai == null)
                    {
                        Debug.LogError("The generated Bear_Enemy prefab has no BearAI component.", bear);
                        UnityEngine.Object.DestroyImmediate(bear);
                        return;
                    }
                    ai.GroundTerrain = terrain;
                    Undo.RegisterCreatedObjectUndo(bear, "Setup Bear Enemy");
                }

                EditorSceneManager.MarkSceneDirty(forestScene);
                if (!EditorSceneManager.SaveScene(forestScene))
                    Debug.LogError($"Could not save the forest scene '{ForestScenePath}'.");

                if (FindComponent<NavMeshSurface>(forestScene) == null)
                    Debug.LogWarning(
                        "No NavMeshSurface was found. Run Tools > Prince > Bake Forest NavMesh after setup. " +
                        "BearAI falls back to manual terrain movement until the NavMesh is baked.");
            }
            finally
            {
                if (openedForestScene && forestScene.IsValid() && forestScene.isLoaded)
                    EditorSceneManager.CloseScene(forestScene, true);
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(previousActiveScene);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Bear setup complete. Prefab: {BearPrefabPath}. Animator: {AnimatorPath}. " +
                "The bear is placed beside the backpack area without moving an existing Bear_Enemy.");
        }

        [MenuItem("Tools/Prince/Bake Forest NavMesh")]
        private static void BakeForestNavMesh()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Bake Forest NavMesh cannot run in Play mode.");
                return;
            }

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene forestScene = SceneManager.GetSceneByPath(ForestScenePath);
            bool openedForestScene = !forestScene.IsValid() || !forestScene.isLoaded;
            if (openedForestScene)
                forestScene = EditorSceneManager.OpenScene(ForestScenePath, OpenSceneMode.Additive);

            if (!forestScene.IsValid() || !forestScene.isLoaded)
            {
                Debug.LogError($"Could not open the forest scene '{ForestScenePath}'.");
                return;
            }

            try
            {
                Terrain terrain = FindComponent<Terrain>(forestScene);
                if (terrain == null || terrain.terrainData == null)
                {
                    Debug.LogError($"The forest scene '{ForestScenePath}' has no Terrain with TerrainData.");
                    return;
                }

                NavMeshSurface surface = FindComponent<NavMeshSurface>(forestScene);
                if (surface == null)
                {
                    GameObject surfaceObject = new GameObject("Forest_NavMeshSurface");
                    SceneManager.MoveGameObjectToScene(surfaceObject, forestScene);
                    surface = surfaceObject.AddComponent<NavMeshSurface>();
                    Undo.RegisterCreatedObjectUndo(surfaceObject, "Bake Forest NavMesh");
                }

                surface.agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID;
                surface.collectObjects = CollectObjects.Volume;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.ignoreNavMeshAgent = true;
                surface.ignoreNavMeshObstacle = true;
                surface.transform.SetPositionAndRotation(terrain.transform.position, Quaternion.identity);
                Vector3 terrainSize = terrain.terrainData.size;
                surface.center = new Vector3(terrainSize.x * 0.5f, terrainSize.y * 0.5f, terrainSize.z * 0.5f);
                surface.size = new Vector3(terrainSize.x, Mathf.Max(80f, terrainSize.y + 40f), terrainSize.z);
                EditorUtility.SetDirty(surface);

                surface.BuildNavMesh();
                NavMeshData bakedData = surface.navMeshData;
                if (bakedData == null)
                {
                    Debug.LogError("NavMesh baking produced no NavMeshData. Check the terrain collider and bake log.");
                    return;
                }

                if (AssetDatabase.LoadMainAssetAtPath(NavMeshAssetPath) != null &&
                    !AssetDatabase.DeleteAsset(NavMeshAssetPath))
                {
                    Debug.LogError($"Could not replace generated NavMesh asset '{NavMeshAssetPath}'.");
                    return;
                }

                AssetDatabase.CreateAsset(bakedData, NavMeshAssetPath);
                surface.navMeshData = bakedData;
                EditorUtility.SetDirty(surface);
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(forestScene);
                if (!EditorSceneManager.SaveScene(forestScene))
                    Debug.LogError($"Could not save the forest scene '{ForestScenePath}'.");
                else
                    Debug.Log($"Forest NavMesh baked and saved to '{NavMeshAssetPath}'.");
            }
            finally
            {
                if (openedForestScene && forestScene.IsValid() && forestScene.isLoaded)
                    EditorSceneManager.CloseScene(forestScene, true);
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(previousActiveScene);
            }
        }

        private static bool ValidateBearAssets(out GameObject sourcePrefab, out Material sourceMaterial)
        {
            sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
            var missing = new List<string>();
            if (sourcePrefab == null)
                missing.Add(SourcePrefabPath);
            if (sourceMaterial == null)
                missing.Add(SourceMaterialPath);

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
                missing.Add("Shader: Universal Render Pipeline/Lit");
            if (AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/_Project/Scripts/Runtime/BearAI.cs") == null)
                missing.Add("Assets/_Project/Scripts/Runtime/BearAI.cs");
            if (AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/_Project/Scripts/Runtime/BearHealthBar.cs") == null)
                missing.Add("Assets/_Project/Scripts/Runtime/BearHealthBar.cs");

            if (missing.Count == 0)
                return true;

            Debug.LogError("Bear setup cannot continue. Missing assets:\n- " + string.Join("\n- ", missing));
            return false;
        }

        private static Material CreateOrUpdateLightMaterial(Material sourceMaterial)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("The Universal Render Pipeline/Lit shader is unavailable.");
                return null;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(BearMaterialPath);
            if (material == null)
                material = new Material(shader);
            else
                material.shader = shader;

            material.name = "Bear_Light_URP";
            Texture baseMap = sourceMaterial.HasProperty("_MainTex")
                ? sourceMaterial.GetTexture("_MainTex")
                : sourceMaterial.mainTexture;
            Texture normalMap = sourceMaterial.HasProperty("_BumpMap")
                ? sourceMaterial.GetTexture("_BumpMap")
                : null;
            Texture occlusionMap = sourceMaterial.HasProperty("_OcclusionMap")
                ? sourceMaterial.GetTexture("_OcclusionMap")
                : null;
            if (baseMap != null)
                material.SetTexture("_BaseMap", baseMap);
            if (normalMap != null)
            {
                material.SetTexture("_BumpMap", normalMap);
                material.EnableKeyword("_NORMALMAP");
            }
            if (occlusionMap != null)
            {
                material.SetTexture("_OcclusionMap", occlusionMap);
                material.EnableKeyword("_OCCLUSIONMAP");
            }
            if (sourceMaterial.HasProperty("_Color"))
                material.SetColor("_BaseColor", sourceMaterial.GetColor("_Color"));
            material.SetFloat("_Smoothness", 0.18f);
            material.enableInstancing = true;

            if (AssetDatabase.LoadAssetAtPath<Material>(BearMaterialPath) == null)
                AssetDatabase.CreateAsset(material, BearMaterialPath);
            else
                EditorUtility.SetDirty(material);
            return material;
        }

        private static Dictionary<string, AnimationClip> CreateOrUpdateAnimationClips()
        {
            var clips = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
            AddClip(clips, "Idle", "Bear_Idle.fbx", true, true);
            AddClip(clips, "Walk", "Bear_WalkForward.fbx", true, false);
            AddClip(clips, "Run", "Bear_RunForward.fbx", true, false);
            AddClip(clips, "Attack", "Bear_Attack1.fbx", false, false);
            AddClip(clips, "Attack2", "Bear_Attack2.fbx", false, false);
            AddClip(clips, "Attack3", "Bear_Attack3.fbx", false, false);
            AddClip(clips, "Attack4", "Bear_Attack5.fbx", false, false);
            AddClip(clips, "Hit", "Bear_GetHitFromFront.fbx", false, false);
            AddClip(clips, "Death", "Bear_Death.fbx", false, false);
            AddClip(clips, "Roar", "Bear_Buff.fbx", false, false);
            return clips;
        }

        private static void AddClip(
            IDictionary<string, AnimationClip> clips,
            string key,
            string sourceFile,
            bool loop,
            bool required)
        {
            string sourcePath = "Assets/Blink/Art/Animals/Stylized/Bear/Bear_Animations/" + sourceFile;
            AnimationClip sourceClip = FindAnimationClip(sourcePath);
            if (sourceClip == null)
            {
                string message = $"Animation clip '{sourceFile}' was not found at '{sourcePath}'.";
                if (required)
                    Debug.LogError(message);
                else
                    Debug.LogWarning(message + " Its Animator state will be skipped.");
                return;
            }

            string generatedPath = GeneratedAnimationsPath + "/" + key + ".anim";
            AnimationClip generated = AssetDatabase.LoadAssetAtPath<AnimationClip>(generatedPath);
            if (generated == null)
            {
                generated = UnityEngine.Object.Instantiate(sourceClip);
                generated.name = key;
                generated.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
                AssetDatabase.CreateAsset(generated, generatedPath);
            }
            else
            {
                EditorUtility.CopySerialized(sourceClip, generated);
                generated.name = key;
                generated.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
                EditorUtility.SetDirty(generated);
            }
            clips.Add(key, generated);
        }

        private static AnimationClip FindAnimationClip(string path)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (UnityEngine.Object asset in assets)
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                    return clip;
            return null;
        }

        private static AnimatorController CreateOrUpdateAnimatorController(
            IReadOnlyDictionary<string, AnimationClip> clips)
        {
            if (!clips.TryGetValue("Idle", out AnimationClip idleClip))
            {
                Debug.LogError("The bear has no usable Idle clip; AnimatorController creation was stopped.");
                return null;
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorPath);
            if (controller == null)
            {
                Debug.LogError($"Could not create AnimatorController '{AnimatorPath}'.");
                return null;
            }

            foreach (AnimatorControllerParameter parameter in controller.parameters)
                controller.RemoveParameter(parameter);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("AttackVariant", AnimatorControllerParameterType.Int);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Roar", AnimatorControllerParameterType.Trigger);

            if (controller.layers.Length == 0)
                controller.AddLayer("Base Layer");
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in stateMachine.states)
                stateMachine.RemoveState(child.state);
            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions)
                stateMachine.RemoveAnyStateTransition(transition);

            AnimatorState idle = stateMachine.AddState("Idle");
            idle.motion = idleClip;
            stateMachine.defaultState = idle;

            AnimatorState walk = AddStateIfPresent(stateMachine, clips, "Walk");
            AnimatorState run = AddStateIfPresent(stateMachine, clips, "Run");
            AnimatorState[] attacks =
            {
                AddStateIfPresent(stateMachine, clips, "Attack"),
                AddStateIfPresent(stateMachine, clips, "Attack2"),
                AddStateIfPresent(stateMachine, clips, "Attack3"),
                AddStateIfPresent(stateMachine, clips, "Attack4")
            };
            AnimatorState hit = AddStateIfPresent(stateMachine, clips, "Hit");
            AnimatorState death = AddStateIfPresent(stateMachine, clips, "Death");
            AnimatorState roar = AddStateIfPresent(stateMachine, clips, "Roar");
            if (death != null)
                AddAnyStateBool(stateMachine, death, "Dead");

            if (walk != null)
            {
                AddConditionTransition(idle, walk, "Speed", AnimatorConditionMode.Greater, 0.1f);
                AddConditionTransition(walk, idle, "Speed", AnimatorConditionMode.Less, 0.1f);
                if (run != null)
                {
                    AddConditionTransition(walk, run, "Speed", AnimatorConditionMode.Greater, 1.8f);
                    AddConditionTransition(run, walk, "Speed", AnimatorConditionMode.Less, 1.8f);
                }
            }
            for (int i = 0; i < attacks.Length; i++)
                if (attacks[i] != null)
                    AddAnyStateAttack(stateMachine, attacks[i], i);
            if (hit != null)
                AddAnyStateTrigger(stateMachine, hit, "Hit");
            if (roar != null)
                AddAnyStateTrigger(stateMachine, roar, "Roar");
            if (clips.TryGetValue("Death", out AnimationClip deathClip))
            {
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(deathClip);
                settings.loopTime = false;
                AnimationUtility.SetAnimationClipSettings(deathClip, settings);
                EditorUtility.SetDirty(deathClip);
            }

            foreach (AnimatorState attack in attacks)
                AddExitTransition(attack, idle);
            AddExitTransition(hit, idle);
            AddExitTransition(roar, idle);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorState AddStateIfPresent(
            AnimatorStateMachine stateMachine,
            IReadOnlyDictionary<string, AnimationClip> clips,
            string key)
        {
            if (!clips.TryGetValue(key, out AnimationClip clip))
                return null;
            AnimatorState state = stateMachine.AddState(key);
            state.motion = clip;
            return state;
        }

        private static void AddConditionTransition(
            AnimatorState from,
            AnimatorState to,
            string parameter,
            AnimatorConditionMode mode,
            float threshold)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.12f;
            transition.AddCondition(mode, threshold, parameter);
        }

        private static void AddAnyStateTrigger(
            AnimatorStateMachine stateMachine,
            AnimatorState state,
            string parameter)
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.duration = 0.08f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
            transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
        }

        private static void AddAnyStateAttack(AnimatorStateMachine stateMachine, AnimatorState state, int variant)
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.duration = 0.08f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            transition.AddCondition(AnimatorConditionMode.Equals, variant, "AttackVariant");
            transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
        }

        private static void AddAnyStateBool(
            AnimatorStateMachine stateMachine,
            AnimatorState state,
            string parameter)
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.duration = 0.08f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void AddExitTransition(AnimatorState from, AnimatorState to)
        {
            if (from == null)
                return;
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = 0.9f;
            transition.duration = 0.12f;
        }

        private static GameObject CreateOrUpdateBearPrefab(
            GameObject sourcePrefab,
            Material lightMaterial,
            AnimatorController animatorController)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
            if (instance == null)
            {
                Debug.LogError($"Could not instantiate bear source prefab '{SourcePrefabPath}'.", sourcePrefab);
                return null;
            }

            try
            {
                instance.name = "Bear_Enemy";
                Animator animator = instance.GetComponentInChildren<Animator>(true);
                if (animator == null)
                {
                    Debug.LogError("The imported bear prefab has no Animator component.", instance);
                    return null;
                }
                animator.runtimeAnimatorController = animatorController;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                        materials[i] = lightMaterial;
                    renderer.sharedMaterials = materials;
                    renderer.receiveShadows = true;
                    if (renderer.bounds.size.magnitude < 0.75f)
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                CapsuleCollider capsule = instance.GetComponent<CapsuleCollider>();
                if (capsule == null)
                    capsule = instance.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0f, 1f, 0f);
                capsule.height = 2f;
                capsule.radius = 0.78f;
                capsule.isTrigger = false;

                Rigidbody body = instance.GetComponent<Rigidbody>();
                if (body == null)
                    body = instance.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                body.constraints = RigidbodyConstraints.FreezeAll;

                NavMeshAgent agent = instance.GetComponent<NavMeshAgent>();
                if (agent == null)
                    agent = instance.AddComponent<NavMeshAgent>();
                agent.radius = 0.78f;
                agent.height = 2f;
                agent.speed = 2.7f;
                agent.acceleration = 8f;
                agent.angularSpeed = 0f;
                agent.stoppingDistance = 2.1f;
                agent.autoBraking = true;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

                AudioSource audioSource = instance.GetComponent<AudioSource>();
                if (audioSource == null)
                    audioSource = instance.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
                audioSource.minDistance = 3f;
                audioSource.maxDistance = 28f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;

                BearAI ai = instance.GetComponent<BearAI>();
                if (ai == null)
                    ai = instance.AddComponent<BearAI>();
                SetObjectReference(ai, "audioSource", audioSource);
                AudioClip growlClip = LoadGrowlClip();
                if (growlClip != null)
                    SetObjectReference(ai, "roarClip", growlClip);

                BuildHealthBar(instance);
                PrefabUtility.SaveAsPrefabAsset(instance, BearPrefabPath, out bool success);
                if (!success)
                {
                    Debug.LogError($"Could not save the bear prefab to '{BearPrefabPath}'.", instance);
                    return null;
                }

                return AssetDatabase.LoadAssetAtPath<GameObject>(BearPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void BuildHealthBar(GameObject bear)
        {
            Transform existing = bear.transform.Find("Bear_HealthBar");
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing.gameObject);

            GameObject canvasObject = new GameObject(
                "Bear_HealthBar", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(BearHealthBar));
            canvasObject.transform.SetParent(bear.transform, false);
            canvasObject.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            canvasObject.transform.localScale = Vector3.one * 0.01f;
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(120f, 20f);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            canvasObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 8f;
            canvasObject.AddComponent<GraphicRaycaster>();
            CanvasGroup group = canvasObject.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            Image background = CreateBarImage("Background", canvasObject.transform,
                new Color(0.035f, 0.025f, 0.02f, 0.88f), false);
            background.rectTransform.anchorMin = Vector2.zero;
            background.rectTransform.anchorMax = Vector2.one;
            background.rectTransform.offsetMin = Vector2.zero;
            background.rectTransform.offsetMax = Vector2.zero;

            Image fill = CreateBarImage("Fill", canvasObject.transform, new Color(0.65f, 0.22f, 0.16f, 1f), false);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = new Vector2(2f, 2f);
            fill.rectTransform.offsetMax = new Vector2(-2f, -2f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            GameObject labelObject = new GameObject(
                "HealthLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(canvasObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "120 / 120";
            label.fontSize = 11f;
            label.color = new Color(0.95f, 0.88f, 0.73f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            if (font != null)
                label.font = font;
            else
                Debug.LogWarning($"TMP font was not found at '{FontPath}'. Unity's default TMP font will be used.", bear);

            BearHealthBar healthBar = canvasObject.GetComponent<BearHealthBar>();
            SetObjectReference(healthBar, "canvasGroup", group);
            SetObjectReference(healthBar, "fillImage", fill);
            SetObjectReference(healthBar, "healthLabel", label);
        }

        private static Image CreateBarImage(string name, Transform parent, Color color, bool raycastTarget)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static Vector3 CalculateBearPosition(
            Vector3 backpackPosition,
            Vector3 guardCenter,
            Vector3 bramPosition,
            float guardRadius,
            Terrain terrain)
        {
            Vector3 pathDirection = PlanarDirection(backpackPosition - bramPosition);
            if (pathDirection.sqrMagnitude < 0.001f)
                pathDirection = Vector3.forward;
            Vector3 side = new Vector3(-pathDirection.z, 0f, pathDirection.x);
            float offset = guardRadius > 0f
                ? Mathf.Min(7f, guardRadius * 0.65f)
                : 5f;
            Vector3 candidate = guardCenter + side * offset;
            candidate.y = terrain != null && terrain.terrainData != null
                ? terrain.transform.position.y + terrain.SampleHeight(candidate)
                : backpackPosition.y;
            return candidate;
        }

        private static Vector3 PlanarDirection(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
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

        private static bool SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (target == null)
                return false;
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError(
                    $"Could not assign '{propertyName}' on '{target.name}'; the serialized field was not found.",
                    target);
                return false;
            }
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            return true;
        }

        private static AudioClip LoadGrowlClip()
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(GrowlClipPath);
            if (clip == null)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(GrowlClipPath);
            }

            if (clip == null)
                Debug.LogWarning($"Bear growl audio was not imported at '{GrowlClipPath}'. The roar audio slot remains empty.");
            return clip;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent))
                AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
