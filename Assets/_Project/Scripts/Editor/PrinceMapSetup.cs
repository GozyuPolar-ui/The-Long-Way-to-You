using System;
using System.Collections.Generic;
using System.IO;
using Prince;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prince.Editor
{
    internal static class PrinceMapSetup
    {
        private const string DataFolder = "Assets/_Project/Data";
        private const string MapAreasFolder = DataFolder + "/MapAreas";
        private const string SettingsPath = DataFolder + "/MapSettings.asset";
        private const string RenderTextureFolder = "Assets/_Project/Rendering/Map";
        private const string IconFolder = "Assets/_Project/UI/Map/Icons";
        private const string FrameFolder = "Assets/_Project/UI/Map/Frame";
        private const string FramePath = FrameFolder + "/MapFrame.png";
        private const string MapShaderPath = "Assets/_Project/Shaders/PrinceMapDisplay.shader";
        private const string MapMaterialPath = "Assets/_Project/Materials/MapDisplay.mat";
        private const string MapRootName = "Map System";
        private const string AreaObjectName = "Map Area";
        private const string HiddenLayerName = "MapHidden";
        private const string TreeLayerName = "MapTrees";

        [MenuItem("Tools/Prince/Setup Map System")]
        private static void SetupMenu()
        {
            Setup();
        }

        [MenuItem("Tools/Prince/Remove Map System")]
        private static void RemoveMenu()
        {
            RemoveMapSystem();
        }

        [MenuItem("Tools/Prince/Revert Map Layers")]
        private static void RevertLayersMenu()
        {
            RevertMapLayers(SceneManager.GetActiveScene());
        }

        private static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Setup Map System cannot run in Play mode.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("Open a loaded scene before setting up the map.");
                return;
            }

            Terrain terrain = FindTerrain(scene);
            Transform player = FindPlayer(scene);
            if (terrain == null)
            {
                Debug.LogError("Setup Map System requires a Terrain in the active scene to calculate the map bounds.");
                return;
            }
            if (player == null)
            {
                Debug.LogError("PlayerPrince was not found in the active scene. Add it and run Setup Map System again.");
                return;
            }
            if (terrain.terrainData == null)
            {
                Debug.LogError("The active scene Terrain has no TerrainData asset.");
                return;
            }

            if (player.GetComponent<DialogueInputLock>() == null &&
                player.GetComponentInChildren<DialogueInputLock>() == null)
            {
                Undo.AddComponent<DialogueInputLock>(player.gameObject);
                Debug.LogWarning(
                    "DialogueInputLock was added to the scene PlayerPrince instance so the full map can lock player controls.");
            }

            EnsureFolder("Assets/_Project");
            EnsureFolder(DataFolder);
            EnsureFolder(MapAreasFolder);
            EnsureFolder("Assets/_Project/Rendering");
            EnsureFolder(RenderTextureFolder);
            EnsureFolder("Assets/_Project/UI");
            EnsureFolder("Assets/_Project/UI/Map");
            EnsureFolder(IconFolder);
            EnsureFolder(FrameFolder);
            EnsureFolder("Assets/_Project/Materials");

            if (!EnsureLayer(HiddenLayerName, out int hiddenLayer) ||
                !EnsureLayer(TreeLayerName, out int treeLayer))
                return;

            string sceneSafeName = SanitizeName(scene.name);
            MapSettings settings = GetOrCreateAsset<MapSettings>(SettingsPath);
            string areaPath = MapAreasFolder + "/MapArea_" + sceneSafeName + ".asset";
            bool isNewAreaDefinition = AssetDatabase.LoadAssetAtPath<MapAreaDefinition>(areaPath) == null;
            MapAreaDefinition definition = GetOrCreateAsset<MapAreaDefinition>(areaPath);
            RenderTexture miniTexture = GetOrCreateRenderTexture(
                RenderTextureFolder + "/MapMinimap_" + sceneSafeName + ".renderTexture",
                settings != null ? settings.minimapResolution : 768);
            RenderTexture fullTexture = GetOrCreateRenderTexture(
                RenderTextureFolder + "/MapFull_" + sceneSafeName + ".renderTexture",
                settings != null ? settings.fullMapResolution : 2048);
            if (settings == null || definition == null || miniTexture == null || fullTexture == null)
                return;

            SetAreaFromTerrain(definition, terrain, isNewAreaDefinition);
            SetRenderSettings(definition);
            if (!EnsureIcons(out Sprite[] icons) || !EnsureMapFrame(out Sprite frameSprite))
                return;
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(MapShaderPath);
            if (shader == null)
            {
                Debug.LogError($"Map display shader was not found at '{MapShaderPath}'.");
                return;
            }

            Material mapMaterial = GetOrCreateMapMaterial(shader);
            MapArea mapArea = GetOrCreateAreaObject(scene, definition, terrain);
            if (mapArea == null)
                return;

            GameObject mapRoot = FindRootObject(scene, MapRootName);
            if (mapRoot == null)
            {
                mapRoot = new GameObject(MapRootName);
                Undo.RegisterCreatedObjectUndo(mapRoot, "Setup Map System");
                SceneManager.MoveGameObjectToScene(mapRoot, scene);
            }

            Camera mapCamera = GetOrCreateMapCamera(mapRoot.transform);
            MapSystem mapSystem = mapRoot.GetComponent<MapSystem>();
            if (mapSystem == null)
                mapSystem = Undo.AddComponent<MapSystem>(mapRoot);
            AssignMapReferences(
                mapSystem, mapArea, settings, mapCamera, miniTexture, fullTexture,
                shader, mapMaterial, player, hiddenLayer, treeLayer, icons, frameSprite);

            int hiddenCount = ApplyMapLayers(scene, player, hiddenLayer, treeLayer);
            AssignQuestMarkers(scene, icons);

            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(definition);
            EditorUtility.SetDirty(mapArea);
            EditorUtility.SetDirty(mapRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Map setup complete for '{scene.name}'. Terrain bounds: {definition.sizeXZ.x:0} x {definition.sizeXZ.y:0} m; " +
                $"{hiddenCount} foliage/player/NPC render objects assigned to map visibility layers. " +
                "Save the scene to keep the setup.");
        }

        private static MapArea GetOrCreateAreaObject(
            Scene scene,
            MapAreaDefinition definition,
            Terrain terrain)
        {
            GameObject areaObject = FindRootObject(scene, AreaObjectName);
            if (areaObject == null)
            {
                areaObject = new GameObject(AreaObjectName);
                Undo.RegisterCreatedObjectUndo(areaObject, "Setup Map System");
                SceneManager.MoveGameObjectToScene(areaObject, scene);
            }

            MapArea area = areaObject.GetComponent<MapArea>();
            if (area == null)
                area = Undo.AddComponent<MapArea>(areaObject);
            MapRevealState reveal = areaObject.GetComponent<MapRevealState>();
            if (reveal == null)
                reveal = Undo.AddComponent<MapRevealState>(areaObject);
            area.definition = definition;
            area.terrain = terrain;
            area.revealState = reveal;
            EditorUtility.SetDirty(areaObject);
            return area;
        }

        private static Camera GetOrCreateMapCamera(Transform parent)
        {
            Transform cameraTransform = parent.Find("MapCamera");
            GameObject cameraObject;
            if (cameraTransform == null)
            {
                cameraObject = new GameObject("MapCamera");
                Undo.RegisterCreatedObjectUndo(cameraObject, "Setup Map System");
                cameraObject.transform.SetParent(parent, false);
                cameraObject.transform.localPosition = new Vector3(0f, 300f, 0f);
                cameraObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
                cameraObject = cameraTransform.gameObject;

            Camera camera = cameraObject.GetComponent<Camera>();
            if (camera == null)
                camera = Undo.AddComponent<Camera>(cameraObject);
            camera.enabled = false;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.105f, 0.065f, 1f);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            return camera;
        }

        private static void AssignMapReferences(
            MapSystem system,
            MapArea area,
            MapSettings settings,
            Camera camera,
            RenderTexture mini,
            RenderTexture full,
            Shader shader,
            Material material,
            Transform player,
            int hiddenLayer,
            int treeLayer,
            Sprite[] icons,
            Sprite frameSprite)
        {
            SerializedObject serialized = new SerializedObject(system);
            SetReference(serialized, "area", area);
            SetReference(serialized, "settings", settings);
            SetReference(serialized, "mapCamera", camera);
            SetReference(serialized, "minimapTexture", mini);
            SetReference(serialized, "fullMapTexture", full);
            SetReference(serialized, "displayShader", shader);
            SetReference(serialized, "displayMaterial", material);
            SetReference(serialized, "player", player);
            SetReference(serialized, "questManager", FindComponentInScene<QuestManager>(system.gameObject.scene));
            SetReference(serialized, "dialogueInputLock",
                player.GetComponent<DialogueInputLock>() ?? player.GetComponentInChildren<DialogueInputLock>());
            SetReference(serialized, "playerInteractor",
                player.GetComponent<PlayerInteractor>() ?? player.GetComponentInChildren<PlayerInteractor>());
            SetReference(serialized, "trackingHUD", FindComponentInScene<QuestTrackingHUD>(system.gameObject.scene));
            SetInt(serialized, "hiddenLayer", hiddenLayer);
            SetInt(serialized, "treeLayer", treeLayer);
            SetObjectArray(serialized, "markerIcons", icons);
            SetReference(serialized, "mapFrameSprite", frameSprite);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(system);
        }

        private static int ApplyMapLayers(Scene scene, Transform player, int hiddenLayer, int treeLayer)
        {
            var candidates = new List<(GameObject root, bool tree)>();
            var selected = new HashSet<GameObject>();

            if (player != null && selected.Add(player.gameObject))
                candidates.Add((player.gameObject, false));

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform item in transforms)
                {
                    if (item == null ||
                        (item.parent != null && item.parent.GetComponentInParent<MapLayerVisibility>() != null))
                        continue;
                    MapLayerVisibility existingVisibility = item.GetComponent<MapLayerVisibility>();
                    if (existingVisibility != null)
                    {
                        if (selected.Add(item.gameObject))
                            candidates.Add((item.gameObject, existingVisibility.IsTreeGroup));
                        continue;
                    }
                    if (item == player || HasClassifiedFoliageAncestor(item))
                        continue;
                    if (item.GetComponentInParent<NPCInfo>() == item.GetComponent<NPCInfo>() &&
                        item.GetComponent<NPCInfo>() != null)
                    {
                        if (selected.Add(item.gameObject))
                            candidates.Add((item.gameObject, false));
                        continue;
                    }

                    if (!TryClassifyFoliage(item.name, out bool isTree))
                        continue;
                    GameObject candidate = item.gameObject;
                    if (selected.Add(candidate))
                        candidates.Add((candidate, isTree));
                }
            }

            int rendererCount = 0;
            foreach ((GameObject candidate, bool isTree) in candidates)
            {
                if (candidate == null)
                    continue;
                MapLayerVisibility visibility = candidate.GetComponent<MapLayerVisibility>();
                if (visibility == null)
                    visibility = Undo.AddComponent<MapLayerVisibility>(candidate);
                visibility.Apply(hiddenLayer, treeLayer, isTree);
                rendererCount += candidate.GetComponentsInChildren<Renderer>(true).Length;
                EditorUtility.SetDirty(candidate);
            }
            return rendererCount;
        }

        private static bool HasClassifiedFoliageAncestor(Transform item)
        {
            Transform ancestor = item.parent;
            while (ancestor != null)
            {
                if (TryClassifyFoliage(ancestor.name, out _))
                    return true;
                ancestor = ancestor.parent;
            }
            return false;
        }

        private static bool TryClassifyFoliage(string objectName, out bool tree)
        {
            string name = objectName.ToLowerInvariant();
            tree = name.Contains("tree") || name.Contains("spruce");
            bool hiddenFoliage = name.Contains("grass") || name.Contains("flower") ||
                name.Contains("bush") || name.Contains("shrub") || name.Contains("small_plant");
            return tree || hiddenFoliage;
        }

        private static void AssignQuestMarkers(Scene scene, Sprite[] icons)
        {
            QuestTarget[] targets = UnityEngine.Object.FindObjectsByType<QuestTarget>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (QuestTarget target in targets)
            {
                if (target == null || target.gameObject.scene != scene)
                    continue;

                MapMarker marker = target.GetComponent<MapMarker>();
                if (marker == null)
                {
                    marker = Undo.AddComponent<MapMarker>(target.gameObject);
                    marker.createdByMapSetup = true;
                }

                marker.type = target.GetComponentInParent<QuestPickup>() != null
                    ? MapMarkerType.Pickup
                    : target.GetComponentInParent<NPCInfo>() != null
                        ? MapMarkerType.NPC
                        : MapMarkerType.Quest;
                marker.displayName = string.IsNullOrWhiteSpace(target.displayName)
                    ? target.targetId
                    : target.displayName;
                marker.icon = icons[(int)marker.type];
                marker.color = MapMarkerColor(marker.type);
                marker.visibleThroughFog = marker.type == MapMarkerType.Quest;
                EditorUtility.SetDirty(marker);
            }

            NPCInfo[] npcs = UnityEngine.Object.FindObjectsByType<NPCInfo>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (NPCInfo npc in npcs)
            {
                if (npc == null || npc.gameObject.scene != scene ||
                    npc.GetComponentInParent<QuestTarget>() != null ||
                    npc.GetComponent<MapMarker>() != null)
                    continue;
                MapMarker marker = Undo.AddComponent<MapMarker>(npc.gameObject);
                marker.createdByMapSetup = true;
                marker.type = MapMarkerType.NPC;
                marker.displayName = npc.DisplayName;
                marker.icon = icons[(int)MapMarkerType.NPC];
                marker.color = MapMarkerColor(MapMarkerType.NPC);
                EditorUtility.SetDirty(marker);
            }
        }

        private static int RevertMapLayers(Scene scene)
        {
            int restored = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                MapLayerVisibility[] entries = root.GetComponentsInChildren<MapLayerVisibility>(true);
                foreach (MapLayerVisibility entry in entries)
                {
                    if (entry == null)
                        continue;
                    restored += entry.ChangedObjectCount;
                    entry.Restore();
                    Undo.DestroyObjectImmediate(entry);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Restored original layers on {restored} object(s) in scene '{scene.name}'.");
            return restored;
        }

        private static void RemoveMapSystem()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Remove Map System cannot run in Play mode.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("Open a loaded scene before removing the map system.");
                return;
            }

            RevertMapLayers(scene);
            int removedMarkers = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                MapMarker[] markers = root.GetComponentsInChildren<MapMarker>(true);
                foreach (MapMarker marker in markers)
                {
                    if (marker == null || !marker.createdByMapSetup)
                        continue;
                    Undo.DestroyObjectImmediate(marker);
                    removedMarkers++;
                }
            }

            foreach (string objectName in new[] { MapRootName, AreaObjectName })
            {
                GameObject target = FindRootObject(scene, objectName);
                if (target != null)
                    Undo.DestroyObjectImmediate(target);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Removed map objects and {removedMarkers} generated markers from '{scene.name}'. Map data assets were kept.");
        }

        private static bool EnsureLayer(string layerName, out int index)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("ProjectSettings/TagManager.asset could not be loaded; map layers cannot be configured.");
                index = -1;
                return false;
            }

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray)
            {
                Debug.LogError("TagManager layer list is unavailable.");
                index = -1;
                return false;
            }

            for (int i = 0; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (layer.stringValue == layerName)
                {
                    index = i;
                    return true;
                }
            }

            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(layer.stringValue))
                    continue;
                layer.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                index = i;
                return true;
            }

            Debug.LogError($"No free User Layer is available for '{layerName}'. Remove an unused layer and retry.");
            index = -1;
            return false;
        }

        private static bool EnsureIcons(out Sprite[] sprites)
        {
            sprites = new Sprite[8];
            for (int i = 0; i < sprites.Length; i++)
            {
                MapMarkerType type = (MapMarkerType)i;
                string assetPath = IconFolder + "/MapIcon_" + type + "_Smooth.png";
                if (!File.Exists(assetPath))
                    File.WriteAllBytes(assetPath, CreateIconPng(type));
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogError($"Could not configure map icon '{assetPath}'.");
                    return false;
                }
                ConfigureSpriteImporter(importer, Vector4.zero, 100f);
                Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (existing == null)
                {
                    Debug.LogError($"Map icon sprite was not imported at '{assetPath}'.");
                    return false;
                }
                sprites[i] = existing;
            }
            return true;
        }

        private static byte[] CreateIconPng(MapMarkerType type)
        {
            const int resolution = 64;
            const int supersampling = 4;
            int highResolution = resolution * supersampling;
            Color32[] highPixels = new Color32[highResolution * highResolution];
            Color32 fillColor = MapMarkerColor(type);
            Color32 outlineColor = new Color32(30, 25, 20, 255);
            for (int y = 0; y < highResolution; y++)
            for (int x = 0; x < highResolution; x++)
            {
                Vector2 point = new Vector2(
                    (x + 0.5f) / supersampling - resolution * 0.5f,
                    (y + 0.5f) / supersampling - resolution * 0.5f);
                bool outer = ContainsMarkerShape(type, point, 2f);
                bool inner = ContainsMarkerShape(type, point, 0f);
                Color32 color = inner ? fillColor : outer ? outlineColor : new Color32(0, 0, 0, 0);
                if (inner && type == MapMarkerType.Enemy &&
                    ((Mathf.Abs(point.x - 5f) < 2.2f && point.y > -1f && point.y < 5f) ||
                     (Mathf.Abs(point.x + 5f) < 2.2f && point.y > -1f && point.y < 5f) ||
                     (Mathf.Abs(point.x) < 2f && point.y < -3f && point.y > -8f)))
                    color = outlineColor;
                if (inner && type == MapMarkerType.Campfire && Mathf.Abs(point.y + 14f) < 2f &&
                    Mathf.Abs(point.x) < 12f)
                    color = outlineColor;
                highPixels[y * highResolution + x] = color;
            }

            Color32[] pixels = new Color32[resolution * resolution];
            for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
            {
                int red = 0;
                int green = 0;
                int blue = 0;
                int alpha = 0;
                for (int sy = 0; sy < supersampling; sy++)
                for (int sx = 0; sx < supersampling; sx++)
                {
                    Color32 sample = highPixels[
                        (y * supersampling + sy) * highResolution + x * supersampling + sx];
                    red += sample.r;
                    green += sample.g;
                    blue += sample.b;
                    alpha += sample.a;
                }
                int sampleCount = supersampling * supersampling;
                pixels[y * resolution + x] = new Color32(
                    (byte)(red / sampleCount), (byte)(green / sampleCount),
                    (byte)(blue / sampleCount), (byte)(alpha / sampleCount));
            }

            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            byte[] bytes = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            return bytes;
        }

        private static bool ContainsMarkerShape(MapMarkerType type, Vector2 point, float expansion)
        {
            float x = Mathf.Abs(point.x);
            float y = point.y;
            switch (type)
            {
                case MapMarkerType.Quest:
                    return x + Mathf.Abs(y) <= 18f + expansion * 1.414f;
                case MapMarkerType.NPC:
                    return point.sqrMagnitude <= Mathf.Pow(14f + expansion, 2f);
                case MapMarkerType.Pickup:
                    return point.sqrMagnitude <= Mathf.Pow(8f + expansion, 2f);
                case MapMarkerType.Campfire:
                    bool flame = y >= -17f - expansion && y <= 13f + expansion &&
                        x <= Mathf.Max(1f, 8f + expansion - Mathf.Abs(y + 2f) * 0.3f);
                    bool logs = Mathf.Abs(y + 14f) <= 2f + expansion && x <= 12f + expansion;
                    return flame || logs;
                case MapMarkerType.Enemy:
                    bool skull = (point - new Vector2(0f, 2f)).sqrMagnitude <=
                        Mathf.Pow(12f + expansion, 2f);
                    bool jaw = x <= 8f + expansion && y >= -15f - expansion && y <= -7f + expansion;
                    return skull || jaw;
                case MapMarkerType.Waypoint:
                    bool pinHead = (point - new Vector2(0f, 6f)).sqrMagnitude <=
                        Mathf.Pow(10f + expansion, 2f);
                    bool pinTail = y <= 5f + expansion && y >= -20f - expansion &&
                        x <= (5f + expansion) * (y + 20f + expansion) / 25f;
                    return pinHead || pinTail;
                case MapMarkerType.PlayerPing:
                    return y >= -20f - expansion && y <= 21f + expansion &&
                        x <= (21f + expansion - y) * 0.42f + expansion;
                default:
                    return point.sqrMagnitude <= Mathf.Pow(12f + expansion, 2f);
            }
        }

        private static bool EnsureMapFrame(out Sprite sprite)
        {
            sprite = null;
            string frameAssetPath = FramePath;
            if (!File.Exists(frameAssetPath))
            {
                string[] existingFrames = Directory.GetFiles(FrameFolder, "*.png");
                if (existingFrames.Length > 0)
                {
                    Array.Sort(existingFrames, StringComparer.OrdinalIgnoreCase);
                    frameAssetPath = existingFrames[0].Replace('\\', '/');
                }
            }
            if (!File.Exists(frameAssetPath))
            {
                File.WriteAllBytes(frameAssetPath, CreateMapFramePng());
            }
            AssetDatabase.ImportAsset(frameAssetPath, ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = AssetImporter.GetAtPath(frameAssetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Could not configure map frame '{frameAssetPath}'.");
                return false;
            }
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(frameAssetPath);
            if (texture == null)
            {
                Debug.LogError($"Map frame texture was not imported at '{frameAssetPath}'.");
                return false;
            }
            float border = Mathf.Min(texture.width, texture.height) * 0.0625f;
            ConfigureSpriteImporter(importer, new Vector4(border, border, border, border), 1f);
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(frameAssetPath);
            if (sprite == null)
            {
                Debug.LogError($"Map frame sprite was not imported at '{frameAssetPath}'.");
                return false;
            }
            return true;
        }

        private static void ConfigureSpriteImporter(
            TextureImporter importer,
            Vector4 border,
            float pixelsPerUnit)
        {
            bool changed = importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                !importer.alphaIsTransparency ||
                importer.filterMode != FilterMode.Bilinear ||
                importer.mipmapEnabled ||
                importer.textureCompression != TextureImporterCompression.Uncompressed ||
                !Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit) ||
                importer.spriteBorder != border;
            if (!changed)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        private static byte[] CreateMapFramePng()
        {
            const int resolution = 512;
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true);
            Color32[] pixels = new Color32[resolution * resolution];
            for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
            {
                int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(resolution - 1 - x, resolution - 1 - y));
                float grain = Mathf.Sin(x * 0.045f + Mathf.Sin(y * 0.027f)) * 0.018f;
                Color color = new Color(0.12f + grain, 0.085f + grain, 0.055f + grain, 1f);
                if (edge < 4)
                    color = new Color(0.035f, 0.028f, 0.023f, 1f);
                else if (edge < 8)
                    color = new Color(0.32f, 0.25f, 0.15f, 1f);
                else if (edge < 11)
                    color = new Color(0.58f, 0.44f, 0.24f, 1f);
                else if (edge < 15)
                    color = new Color(0.19f, 0.14f, 0.09f, 1f);
                else if (edge < 18)
                    color = new Color(0.39f, 0.29f, 0.16f, 1f);
                else if (edge < 22)
                    color = new Color(0.075f, 0.055f, 0.038f, 1f);

                int cornerX = Mathf.Min(x, resolution - 1 - x);
                int cornerY = Mathf.Min(y, resolution - 1 - y);
                if (cornerX < 62 && cornerY < 62)
                {
                    float diamond = Mathf.Abs(cornerX - 29f) + Mathf.Abs(cornerY - 29f);
                    if (diamond > 19f && diamond < 27f)
                        color = new Color(0.62f, 0.48f, 0.27f, 1f);
                    else if (diamond >= 27f && diamond < 32f)
                        color = new Color(0.17f, 0.12f, 0.075f, 1f);
                    else if (Mathf.Abs(cornerX - 29f) < 3f && Mathf.Abs(cornerY - 29f) < 3f)
                        color = new Color(0.68f, 0.53f, 0.3f, 1f);
                }
                pixels[y * resolution + x] = color;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            byte[] bytes = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            return bytes;
        }

        private static Color MapMarkerColor(MapMarkerType type)
        {
            switch (type)
            {
                case MapMarkerType.Quest: return new Color(0.95f, 0.72f, 0.28f, 1f);
                case MapMarkerType.NPC: return new Color(0.9f, 0.83f, 0.68f, 1f);
                case MapMarkerType.Pickup: return new Color(0.8f, 0.66f, 0.4f, 1f);
                case MapMarkerType.Campfire: return new Color(0.98f, 0.38f, 0.12f, 1f);
                case MapMarkerType.Enemy: return new Color(0.69f, 0.2f, 0.16f, 1f);
                case MapMarkerType.Waypoint: return new Color(0.95f, 0.78f, 0.36f, 1f);
                case MapMarkerType.PlayerPing: return new Color(1f, 0.85f, 0.38f, 1f);
                default: return new Color(0.8f, 0.73f, 0.58f, 1f);
            }
        }

        private static Material GetOrCreateMapMaterial(Shader shader)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MapMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "MapDisplay" };
                AssetDatabase.CreateAsset(material, MapMaterialPath);
            }
            else
                material.shader = shader;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static RenderTexture GetOrCreateRenderTexture(string path, int resolution)
        {
            RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
            if (texture != null)
            {
                if (texture.width != resolution || texture.height != resolution)
                {
                    Debug.LogWarning($"Existing render texture '{path}' has a custom size; preserving it.");
                    return texture;
                }
                return texture;
            }

            UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing != null)
            {
                Debug.LogError($"A non-RenderTexture asset already exists at '{path}'.");
                return null;
            }

            texture = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32)
            {
                name = Path.GetFileNameWithoutExtension(path),
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing != null)
            {
                Debug.LogError($"A non-{typeof(T).Name} asset already exists at '{path}'.", existing);
                return null;
            }

            asset = ScriptableObject.CreateInstance<T>();
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void SetAreaFromTerrain(
            MapAreaDefinition definition,
            Terrain terrain,
            bool isNewAreaDefinition)
        {
            Vector3 terrainSize = terrain.terrainData.size;
            Vector3 origin = terrain.transform.TransformPoint(Vector3.zero);
            Vector3 cornerX = terrain.transform.TransformPoint(new Vector3(terrainSize.x, 0f, 0f));
            Vector3 cornerZ = terrain.transform.TransformPoint(new Vector3(0f, 0f, terrainSize.z));
            Vector3 cornerXZ = terrain.transform.TransformPoint(new Vector3(terrainSize.x, 0f, terrainSize.z));
            float minX = Mathf.Min(origin.x, cornerX.x, cornerZ.x, cornerXZ.x);
            float maxX = Mathf.Max(origin.x, cornerX.x, cornerZ.x, cornerXZ.x);
            float minZ = Mathf.Min(origin.z, cornerX.z, cornerZ.z, cornerXZ.z);
            float maxZ = Mathf.Max(origin.z, cornerX.z, cornerZ.z, cornerXZ.z);
            float width = maxX - minX;
            float depth = maxZ - minZ;
            float height = terrainSize.y * Mathf.Abs(terrain.transform.lossyScale.y);
            definition.centerXZ = new Vector2((minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f);
            definition.sizeXZ = new Vector2(width * 1.03f, depth * 1.03f);
            if (isNewAreaDefinition || string.IsNullOrWhiteSpace(definition.areaName))
                definition.areaName = terrain.gameObject.scene.name;
            if (isNewAreaDefinition || string.IsNullOrWhiteSpace(definition.areaDescription))
                definition.areaDescription = "Jelajahi jalur, hutan, dan tempat-tempat penting di wilayah ini.";
            definition.cameraHeight = Mathf.Max(200f, height + 180f);
            definition.mapScale = 1f;
        }

        private static void SetRenderSettings(MapAreaDefinition definition)
        {
            if (definition.parchmentTint == default)
                definition.parchmentTint = new Color(0.67f, 0.53f, 0.34f, 1f);
            if (definition.accentColor == default)
                definition.accentColor = new Color(0.73f, 0.56f, 0.28f, 1f);
        }

        private static Terrain FindTerrain(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Terrain terrain in root.GetComponentsInChildren<Terrain>(true))
                    if (terrain != null && terrain.gameObject.scene == scene && terrain.terrainData != null)
                        return terrain;
            return null;
        }

        private static Transform FindPlayer(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "PlayerPrince")
                    return root.transform;
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in transforms)
                {
                    if (child.name == "PlayerPrince")
                        return child;
                    if (child.CompareTag("Player"))
                        return child;
                }
            }
            return null;
        }

        private static GameObject FindRootObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name)
                    return root;
            return null;
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (T component in root.GetComponentsInChildren<T>(true))
                    if (component != null && component.gameObject.scene == scene)
                        return component;
            return null;
        }

        private static void SetReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"MapSystem serialized reference '{propertyName}' does not exist.");
                return;
            }
            property.objectReferenceValue = value;
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.intValue = value;
            else
                Debug.LogError($"MapSystem serialized integer '{propertyName}' does not exist.");
        }

        private static void SetObjectArray(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object[] values)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                Debug.LogError($"MapSystem serialized array '{propertyName}' was not found.");
                return;
            }
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void EnsureFolder(string path)
        {
            string[] pieces = path.Split('/');
            string current = pieces[0];
            for (int i = 1; i < pieces.Length; i++)
            {
                string next = current + "/" + pieces[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, pieces[i]);
                current = next;
            }
        }

        private static string SanitizeName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value.Replace(' ', '_');
        }
    }
}
