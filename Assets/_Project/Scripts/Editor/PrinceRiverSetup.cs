using System;
using System.IO;
using Prince;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prince.Editor
{
    internal static class PrinceRiverSetup
    {
        private const string ForestScenePath = "Assets/_Project/Scene/UNS_Demo.unity";
        private const string BackupFolder = "Assets/_Project/Docs/RiverSetupBackup~";
        private const string RiverClipPath =
            "Assets/_Project/Audio/BGM forest/dragon-studio-soothing-river-flow-372456.mp3";
        private const string SourceWaterMaterialPath =
            "Assets/_Project/Materials/Nature/UNS_Water.mat";
        private const string RiverMaterialPath = "Assets/_Project/Materials/Nature/UNS_River_Water.mat";
        private const string WaterShaderName = "Prince/Stylized Water (URP)";
        private const string SetupObjectName = "River_Environment_Setup";
        private const float WaterSearchDistance = 65f;

        [MenuItem("Tools/Prince/Setup River Environment")]
        private static void SetupRiverEnvironment()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Setup River Environment cannot run in Play mode.");
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(ForestScenePath);
            bool openedScene = !scene.IsValid() || !scene.isLoaded;
            Scene previousActiveScene = SceneManager.GetActiveScene();
            if (openedScene)
                scene = EditorSceneManager.OpenScene(ForestScenePath, OpenSceneMode.Additive);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError($"Could not open forest scene '{ForestScenePath}'.");
                return;
            }

            bool saved = false;
            try
            {
                if (scene.isDirty && !ConfirmSaveDirtyScene(scene))
                    return;
                if (!CreateSceneBackup())
                    return;

                BackpackQuestPlacement placement = FindComponent<BackpackQuestPlacement>(scene);
                if (placement == null)
                {
                    Debug.LogError("BackpackQuestPlacement is missing; the river setup has no safe location reference.");
                    return;
                }

                if (!TryFindRiverRenderers(scene, placement.BackpackPosition,
                        out Renderer[] riverRenderers, out Bounds waterBounds))
                    return;

                Terrain islandTerrain = FindTerrain(scene);
                if (islandTerrain == null || islandTerrain.terrainData == null)
                {
                    Debug.LogError(
                        "Could not find a Terrain with TerrainData in the forest scene. " +
                        "Island-edge barriers were not created.");
                    return;
                }

                Material riverMaterial = CreateOrUpdateRiverMaterial();
                if (riverMaterial == null)
                    return;

                AudioClip riverClip = AssetDatabase.LoadAssetAtPath<AudioClip>(RiverClipPath);
                if (riverClip == null)
                {
                    Debug.LogError($"River ambience clip was not found at '{RiverClipPath}'.");
                    return;
                }

                GameObject setupObject = FindGameObject(scene, SetupObjectName);
                if (setupObject == null)
                {
                    setupObject = new GameObject(SetupObjectName);
                    SceneManager.MoveGameObjectToScene(setupObject, scene);
                    Undo.RegisterCreatedObjectUndo(setupObject, "Setup River Environment");
                }

                setupObject.transform.SetParent(placement.transform, false);
                RiverEnvironment environment = GetOrAddComponent<RiverEnvironment>(setupObject);
                Vector3 travelDirection = placement.BackpackPosition - placement.BramPosition;
                Vector3 flowDirection = GetFlowDirection(waterBounds, travelDirection);
                Vector3 riverCenter = waterBounds.center;
                environment.Configure(
                    riverCenter,
                    flowDirection,
                    waterBounds.extents.magnitude * 2f,
                    waterBounds.extents.magnitude,
                    14f,
                    8f,
                    35f,
                    0.85f);

                Material sourceWaterMaterial = AssetDatabase.LoadAssetAtPath<Material>(SourceWaterMaterialPath);
                AssignRiverMaterial(riverRenderers, sourceWaterMaterial, riverMaterial);
                ConfigureIslandBoundaryWalls(setupObject.transform, islandTerrain);
                ConfigureRiverAudio(setupObject.transform, environment, riverClip);

                EditorUtility.SetDirty(environment);
                EditorSceneManager.MarkSceneDirty(scene);
                saved = EditorSceneManager.SaveScene(scene, ForestScenePath);
                if (!saved)
                    Debug.LogError($"Could not save river setup into '{ForestScenePath}'.");
            }
            finally
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(previousActiveScene);
                if (openedScene && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }

            if (!saved)
                return;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "River setup complete: animated project-owned water material, two invisible bank colliders, and distance-faded river ambience were added near the backpack without editing Asset Store assets.");
        }

        private static bool ConfirmSaveDirtyScene(Scene scene)
        {
            if (!EditorUtility.DisplayDialog(
                    "Unsaved Forest Scene",
                    "The forest scene has unsaved changes. Save them before setting up the river?",
                    "Save and Continue",
                    "Cancel"))
                return false;

            if (EditorSceneManager.SaveScene(scene, ForestScenePath))
                return true;

            Debug.LogError($"Could not save unsaved changes in '{ForestScenePath}'. River setup was not applied.");
            return false;
        }

        private static bool CreateSceneBackup()
        {
            string source = Path.GetFullPath(ForestScenePath);
            string backupFolder = Path.GetFullPath(BackupFolder);
            string backup = Path.Combine(backupFolder, Path.GetFileName(ForestScenePath) + ".bak");
            Directory.CreateDirectory(backupFolder);
            if (File.Exists(backup))
                return true;

            try
            {
                File.Copy(source, backup, false);
            }
            catch (IOException exception)
            {
                Debug.LogError($"Could not create river scene backup '{backup}': {exception.Message}");
                return false;
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogError($"Could not create river scene backup '{backup}': {exception.Message}");
                return false;
            }

            AssetDatabase.Refresh();
            return true;
        }

        private static bool TryFindRiverRenderers(
            Scene scene,
            Vector3 referencePosition,
            out Renderer[] selectedRenderers,
            out Bounds selectedBounds)
        {
            Material sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(SourceWaterMaterialPath);
            if (sourceMaterial == null)
            {
                Debug.LogError($"Source river material was not found at '{SourceWaterMaterialPath}'.");
                selectedRenderers = Array.Empty<Renderer>();
                selectedBounds = default;
                return false;
            }

            Renderer closestRenderer = null;
            float closestDistanceSquared = WaterSearchDistance * WaterSearchDistance;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null || !UsesMaterial(renderer, sourceMaterial))
                        continue;

                    Vector3 closestPoint = renderer.bounds.ClosestPoint(referencePosition);
                    float distanceSquared = (closestPoint - referencePosition).sqrMagnitude;
                    if (distanceSquared >= closestDistanceSquared)
                        continue;

                    closestDistanceSquared = distanceSquared;
                    closestRenderer = renderer;
                }
            }

            if (closestRenderer == null)
            {
                Debug.LogError(
                    $"No water renderer using '{sourceMaterial.name}' was found within {WaterSearchDistance:0} m of the backpack placement.");
                selectedRenderers = Array.Empty<Renderer>();
                selectedBounds = default;
                return false;
            }

            selectedBounds = closestRenderer.bounds;
            Vector3 center = selectedBounds.center;
            var matches = new System.Collections.Generic.List<Renderer>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null || !UsesMaterial(renderer, sourceMaterial) ||
                        (renderer.bounds.center - center).sqrMagnitude > 0.25f)
                        continue;

                    matches.Add(renderer);
                    selectedBounds.Encapsulate(renderer.bounds);
                }
            }

            selectedRenderers = matches.ToArray();
            return selectedRenderers.Length > 0;
        }

        private static bool UsesMaterial(Renderer renderer, Material source)
        {
            foreach (Material material in renderer.sharedMaterials)
                if (material == source)
                    return true;
            return false;
        }

        private static Material CreateOrUpdateRiverMaterial()
        {
            Material source = AssetDatabase.LoadAssetAtPath<Material>(SourceWaterMaterialPath);
            Shader waterShader = Shader.Find(WaterShaderName);
            if (source == null || waterShader == null)
            {
                Debug.LogError(
                    $"Could not load the source water material or shader '{WaterShaderName}'. " +
                    "Run the project's shader import and try again.");
                return null;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(RiverMaterialPath);
            if (material == null)
            {
                material = new Material(source);
                AssetDatabase.CreateAsset(material, RiverMaterialPath);
            }

            material.shader = waterShader;
            Color sourceColor = source.HasProperty("_BaseColor")
                ? source.GetColor("_BaseColor")
                : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
            material.SetColor("_ShallowColor", sourceColor);
            material.SetColor(
                "_DeepColor",
                Color.Lerp(sourceColor, new Color(0.12f, 0.25f, 0.34f, 1f), 0.48f));
            material.SetFloat("_Alpha", sourceColor.a);
            material.SetFloat("_WaveSpeed", 1.15f);
            material.SetFloat("_WaveHeight", 0.035f);
            material.SetFloat("_WaveScale", 1.2f);
            material.SetFloat("_NormalStrength", 0.65f);
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AssignRiverMaterial(
            Renderer[] renderers,
            Material sourceMaterial,
            Material riverMaterial)
        {
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != sourceMaterial)
                        continue;
                    materials[i] = riverMaterial;
                    changed = true;
                }

                if (!changed)
                    continue;

                Undo.RecordObject(renderer, "Assign Animated River Water");
                renderer.sharedMaterials = materials;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                EditorUtility.SetDirty(renderer);
            }
        }

        private static Vector3 GetFlowDirection(Bounds bounds, Vector3 travelDirection)
        {
            if (bounds.size.x > bounds.size.z * 1.1f)
                return Vector3.right;
            if (bounds.size.z > bounds.size.x * 1.1f)
                return Vector3.forward;

            travelDirection.y = 0f;
            if (travelDirection.sqrMagnitude < 0.001f)
                return Vector3.right;
            travelDirection.Normalize();
            return new Vector3(travelDirection.z, 0f, -travelDirection.x).normalized;
        }

        private static void ConfigureIslandBoundaryWalls(Transform parent, Terrain terrain)
        {
            TerrainData data = terrain.terrainData;
            Vector3 scale = terrain.transform.lossyScale;
            float width = data.size.x * Mathf.Abs(scale.x);
            float length = data.size.z * Mathf.Abs(scale.z);
            float thickness = 1f;
            float bottom = terrain.transform.position.y - 10f;
            float top = terrain.transform.position.y +
                        data.size.y * Mathf.Abs(scale.y) + 10f;
            float height = Mathf.Max(20f, top - bottom);
            float centerY = (bottom + top) * 0.5f;
            Vector3 localRight = terrain.transform.right.normalized;
            Vector3 localForward = terrain.transform.forward.normalized;

            DisableOldRiverWall(parent, "River_InvisibleWall_Left");
            DisableOldRiverWall(parent, "River_InvisibleWall_Right");
            DisableOldRiverWall(parent, "River_InvisibleWall_Start");
            DisableOldRiverWall(parent, "River_InvisibleWall_End");

            ConfigureTerrainEdgeWall(
                parent,
                "Island_InvisibleWall_West",
                terrain,
                new Vector3(-thickness * 0.5f, 0f, data.size.z * 0.5f),
                localForward,
                length + thickness,
                height,
                thickness,
                centerY);
            ConfigureTerrainEdgeWall(
                parent,
                "Island_InvisibleWall_East",
                terrain,
                new Vector3(data.size.x + thickness * 0.5f, 0f, data.size.z * 0.5f),
                localForward,
                length + thickness,
                height,
                thickness,
                centerY);
            ConfigureTerrainEdgeWall(
                parent,
                "Island_InvisibleWall_North",
                terrain,
                new Vector3(data.size.x * 0.5f, 0f, -thickness * 0.5f),
                localRight,
                width + thickness,
                height,
                thickness,
                centerY);
            ConfigureTerrainEdgeWall(
                parent,
                "Island_InvisibleWall_South",
                terrain,
                new Vector3(data.size.x * 0.5f, 0f, data.size.z + thickness * 0.5f),
                localRight,
                width + thickness,
                height,
                thickness,
                centerY);
        }

        private static void ConfigureTerrainEdgeWall(
            Transform parent,
            string wallName,
            Terrain terrain,
            Vector3 terrainLocalPosition,
            Vector3 direction,
            float length,
            float height,
            float thickness,
            float centerY)
        {
            GameObject wall = GetOrCreateChild(parent, wallName);
            Vector3 center = terrain.transform.TransformPoint(terrainLocalPosition);
            center.y = centerY;
            Undo.RecordObject(wall.transform, "Place Island Boundary Wall");
            ConfigureWall(wall, center, direction, length, height, thickness);
            EditorUtility.SetDirty(wall.transform);
        }

        private static void DisableOldRiverWall(Transform parent, string wallName)
        {
            Transform oldWall = parent.Find(wallName);
            if (oldWall == null)
                return;

            BoxCollider oldCollider = oldWall.GetComponent<BoxCollider>();
            if (oldCollider != null && oldCollider.enabled)
            {
                Undo.RecordObject(oldCollider, "Disable Old River Barrier");
                oldCollider.enabled = false;
                EditorUtility.SetDirty(oldCollider);
            }
        }

        private static void ConfigureWall(
            GameObject wall,
            Vector3 center,
            Vector3 direction,
            float length,
            float height,
            float thickness)
        {
            wall.transform.SetPositionAndRotation(
                center,
                Quaternion.LookRotation(direction, Vector3.up));
            BoxCollider collider = GetOrAddComponent<BoxCollider>(wall);
            Undo.RecordObject(collider, "Configure Island Boundary Wall");
            collider.center = Vector3.zero;
            collider.size = new Vector3(thickness, height, length);
            collider.isTrigger = false;
            collider.enabled = true;
            EditorUtility.SetDirty(collider);
        }

        private static void ConfigureRiverAudio(Transform parent, RiverEnvironment environment, AudioClip clip)
        {
            GameObject soundObject = GetOrCreateChild(parent, "River_Ambience");
            soundObject.transform.position = environment.RiverCenter;
            AudioSource source = GetOrAddComponent<AudioSource>(soundObject);
            source.clip = clip;
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            RiverAmbience ambience = GetOrAddComponent<RiverAmbience>(soundObject);
            ambience.Configure(
                source,
                environment.SoundFadeInDistance,
                environment.SoundFadeOutDistance,
                environment.SoundMaximumVolume);
            EditorUtility.SetDirty(source);
            EditorUtility.SetDirty(ambience);
        }

        private static GameObject GetOrCreateChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
                return child.gameObject;

            GameObject created = new GameObject(childName);
            created.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(created, "Setup River Environment");
            return created;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(gameObject);
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
    }
}
