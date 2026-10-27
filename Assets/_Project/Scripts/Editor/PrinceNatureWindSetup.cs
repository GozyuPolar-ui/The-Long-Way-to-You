using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Prince;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Prince.Editor
{
    public static class PrinceNatureWindSetup
    {
        private const string NatureMaterialFolder = "Assets/_Project/Materials/Nature";
        private const string NaturePrefabFolder = "Assets/_Project/Prefabs/Nature";
        private const string PackageRoot = "Assets/InnerverseInteractive";
        private const string FoliageShaderName = "Prince/Foliage Wind (URP Lit)";
        private const string WaterShaderName = "Prince/Stylized Water (URP)";

        private sealed class NatureMaterial
        {
            public string SourcePath;
            public string OutputPath;
            public Material Source;
            public bool IsWater;
        }

        [MenuItem("Tools/Prince/Apply Wind And Water", false, 10)]
        private static void ApplyWindAndWaterToScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Apply Wind And Water cannot run in Play mode.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("Apply Wind And Water requires a loaded active scene.");
                return;
            }
            if (string.IsNullOrEmpty(scene.path))
            {
                Debug.LogError("Save the active scene to an asset path before applying wind and water.");
                return;
            }

            if (!TryCreateMaterialMap(out Dictionary<Material, Material> materialMap))
                return;

            int rendererCount = ReplaceMaterials(scene, materialMap, out int materialSlots, out int distinctMaterials);
            EnsureWindController(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError($"Could not save the active scene '{scene.path}'.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Applied nature wind/water materials in scene '{scene.name}'. " +
                $"Changed renderers: {rendererCount}; material slots: {materialSlots}; " +
                $"distinct source materials used: {distinctMaterials}.");
        }

        [MenuItem("Tools/Prince/Revert Wind And Water", false, 11)]
        private static void RevertWindAndWaterInScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Revert Wind And Water cannot run in Play mode.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("Revert Wind And Water requires a loaded active scene.");
                return;
            }

            Dictionary<Material, Material> materialMap = LoadExistingMaterialMap();
            if (materialMap.Count == 0)
            {
                Debug.LogWarning("No generated nature material copies were found to revert.");
                return;
            }

            Dictionary<Material, Material> reverseMap = materialMap.ToDictionary(pair => pair.Value, pair => pair.Key);
            int rendererCount = ReplaceMaterials(scene, reverseMap, out int materialSlots, out int distinctMaterials);
            if (rendererCount == 0)
            {
                Debug.Log("No generated wind/water materials were assigned in the active scene.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError($"Could not save the active scene '{scene.path}'.");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Reverted nature materials in scene '{scene.name}'. " +
                $"Changed renderers: {rendererCount}; material slots: {materialSlots}; " +
                $"distinct material copies used: {distinctMaterials}.");
        }

        [MenuItem("Tools/Prince/Apply Wind And Water To Selected Prefab", false, 12)]
        private static void ApplyWindAndWaterToSelectedPrefab()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Apply Wind And Water To Selected Prefab cannot run in Play mode.");
                return;
            }

            if (!TryGetSelectedPrefabPath(out string sourcePath))
            {
                Debug.LogError("Select a prefab asset or a prefab instance in the Hierarchy first.");
                return;
            }

            if (!TryCreateMaterialMap(out Dictionary<Material, Material> materialMap))
                return;

            if (sourcePath.StartsWith(NaturePrefabFolder + "/", StringComparison.Ordinal))
            {
                UpdateExistingNaturePrefab(sourcePath, materialMap);
                return;
            }

            EnsureAssetFolder(NaturePrefabFolder);
            string fileName = Path.GetFileNameWithoutExtension(sourcePath);
            string outputPath = $"{NaturePrefabFolder}/{fileName}_Wind.prefab";
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (sourcePrefab == null)
            {
                Debug.LogError($"Could not load selected prefab at '{sourcePath}'.");
                return;
            }

            Scene previewScene = EditorSceneManager.NewPreviewScene();
            GameObject instance = null;
            try
            {
                instance = PrefabUtility.InstantiatePrefab(sourcePrefab, previewScene) as GameObject;
                if (instance == null)
                {
                    Debug.LogError($"Could not create a temporary instance of '{sourcePath}'.");
                    return;
                }

                int changedRenderers = ReplaceMaterials(instance, materialMap, out int materialSlots, out int distinctMaterials);
                if (changedRenderers == 0)
                {
                    Debug.LogWarning(
                        $"No Ultimate Nature source materials were found in '{sourcePath}'. " +
                        "No variant was created.");
                    return;
                }

                GameObject saved = PrefabUtility.SaveAsPrefabAssetAndConnect(
                    instance, outputPath, InteractionMode.AutomatedAction, out bool success);
                if (!success || saved == null)
                {
                    Debug.LogError($"Could not save the wind-enabled prefab variant to '{outputPath}'.");
                    return;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(
                    $"Created/updated prefab variant '{outputPath}' from '{sourcePath}'. " +
                    $"Changed renderers: {changedRenderers}; material slots: {materialSlots}; " +
                    $"distinct source materials used: {distinctMaterials}.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not create a nature prefab variant from '{sourcePath}': {exception}");
            }
            finally
            {
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
                if (previewScene.IsValid())
                    EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        [MenuItem("Tools/Prince/Apply Wind And Water To Selected Prefab", true)]
        private static bool ValidateSelectedPrefab()
        {
            return !EditorApplication.isPlaying && TryGetSelectedPrefabPath(out _);
        }

        private static bool TryCreateMaterialMap(out Dictionary<Material, Material> materialMap)
        {
            materialMap = new Dictionary<Material, Material>();
            Shader foliageShader = Shader.Find(FoliageShaderName);
            Shader waterShader = Shader.Find(WaterShaderName);
            if (foliageShader == null || waterShader == null)
            {
                Debug.LogError(
                    $"Nature shaders are not available. Expected '{FoliageShaderName}' and '{WaterShaderName}'. " +
                    "Check shader import/compilation before running this tool.");
                return false;
            }

            List<string> materialPaths = FindNatureMaterialPaths();
            if (materialPaths.Count == 0)
            {
                Debug.LogError(
                    $"No material assets were found below '{PackageRoot}' in the Ultimate Nature Trees, " +
                    "Vegetation, or Water folders.");
                return false;
            }
            WarnForMissingExpectedMaterials(materialPaths);

            EnsureAssetFolder(NatureMaterialFolder);
            List<NatureMaterial> entries = new List<NatureMaterial>();
            foreach (string sourcePath in materialPaths)
            {
                Material source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
                if (source == null)
                {
                    Debug.LogWarning($"Could not load nature material '{sourcePath}'.");
                    continue;
                }

                bool isWater = sourcePath.Contains("/Environment/Water/", StringComparison.Ordinal);
                string outputPath = $"{NatureMaterialFolder}/{Path.GetFileName(sourcePath)}";
                entries.Add(new NatureMaterial
                {
                    SourcePath = sourcePath,
                    OutputPath = outputPath,
                    Source = source,
                    IsWater = isWater
                });
            }

            Dictionary<Material, Bounds> sourceBounds = CollectMaterialBounds(entries);
            foreach (NatureMaterial entry in entries)
            {
                Shader shader = entry.IsWater ? waterShader : foliageShader;
                Material output = CreateOrUpdateMaterialCopy(entry, shader, out bool isNewCopy);
                if (output == null)
                    continue;

                if (entry.IsWater)
                    ConfigureWaterMaterial(output, entry.Source, isNewCopy);
                else
                {
                    if (isNewCopy && !sourceBounds.ContainsKey(entry.Source))
                        Debug.LogWarning(
                            $"Could not infer mesh height for '{entry.SourcePath}'. " +
                            "The generated material keeps its default 0-to-1 height range.");
                    ConfigureFoliageMaterial(
                        output,
                        entry.Source,
                        GetWindMode(entry.Source.name),
                        sourceBounds.TryGetValue(entry.Source, out Bounds bounds) ? bounds : (Bounds?)null,
                        isNewCopy);
                }

                EditorUtility.SetDirty(output);
                materialMap[entry.Source] = output;
                Debug.Log($"Nature material map: '{entry.SourcePath}' -> '{entry.OutputPath}'.");
            }

            if (materialMap.Count == 0)
            {
                Debug.LogError("No nature material copies could be created.");
                return false;
            }
            if (!entries.Any(entry =>
                    !entry.IsWater &&
                    (entry.Source.name.IndexOf("trunk", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     entry.Source.name.IndexOf("bark", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     entry.Source.name.IndexOf("wood", StringComparison.OrdinalIgnoreCase) >= 0)))
            {
                Debug.LogWarning(
                    "No separate trunk/bark/wood material was found in the package's Trees or Vegetation folders. " +
                    "Only materials that include trunk vertices can use the stiff Tree Trunk mode.");
            }

            AssetDatabase.SaveAssets();
            return true;
        }

        private static List<string> FindNatureMaterialPaths()
        {
            return AssetDatabase.FindAssets("t:Material", new[] { PackageRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path =>
                    path.Contains("Ultimate Nature", StringComparison.OrdinalIgnoreCase) &&
                    (path.Contains("/Environment/Trees/", StringComparison.Ordinal) ||
                     path.Contains("/Environment/Vegetation/", StringComparison.Ordinal) ||
                     path.Contains("/Environment/Water/", StringComparison.Ordinal)))
                .Where(path => path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
        }

        private static void WarnForMissingExpectedMaterials(List<string> materialPaths)
        {
            string[] expectedNames =
            {
                "UNS_Spruce_Tree_Branch.mat",
                "UNS_Bush_Leaves.mat",
                "UNS_Grass.mat",
                "UNS_Water.mat"
            };

            foreach (string expectedName in expectedNames)
            {
                if (!materialPaths.Any(path => path.EndsWith("/" + expectedName, StringComparison.OrdinalIgnoreCase)))
                {
                    Debug.LogWarning(
                        $"Expected Ultimate Nature material '{expectedName}' was not found. " +
                        "Other discovered materials will still be processed.");
                }
            }
        }

        private static Material CreateOrUpdateMaterialCopy(
            NatureMaterial entry,
            Shader shader,
            out bool isNewCopy)
        {
            Material output = AssetDatabase.LoadAssetAtPath<Material>(entry.OutputPath);
            if (output != null && output.shader == shader)
            {
                isNewCopy = false;
                return output;
            }

            isNewCopy = true;
            Material fresh = new Material(shader)
            {
                name = Path.GetFileNameWithoutExtension(entry.OutputPath),
                enableInstancing = true,
                renderQueue = entry.Source.renderQueue
            };
            CopySharedMaterialProperties(entry.Source, fresh);

            if (output == null)
            {
                AssetDatabase.CreateAsset(fresh, entry.OutputPath);
                output = fresh;
            }
            else
            {
                EditorUtility.CopySerialized(fresh, output);
                UnityEngine.Object.DestroyImmediate(fresh);
                EditorUtility.SetDirty(output);
            }

            return output;
        }

        private static void CopySharedMaterialProperties(Material source, Material destination)
        {
            CopyTexture(source, destination, "_BaseMap", "_MainTex");
            CopyTexture(source, destination, "_BumpMap");
            CopyTexture(source, destination, "_MetallicGlossMap");
            CopyTexture(source, destination, "_OcclusionMap");
            CopyTexture(source, destination, "_EmissionMap");

            CopyColor(source, destination, "_BaseColor", "_Color");
            CopyColor(source, destination, "_EmissionColor");
            CopyColor(source, destination, "_SpecColor");
            CopyFloat(source, destination, "_Cutoff");
            CopyFloat(source, destination, "_Metallic");
            CopyFloat(source, destination, "_Smoothness", "_Glossiness");
            CopyFloat(source, destination, "_GlossMapScale");
            CopyFloat(source, destination, "_BumpScale");
            CopyFloat(source, destination, "_OcclusionStrength");
            CopyFloat(source, destination, "_Cull");
            CopyFloat(source, destination, "_ReceiveShadows");
            CopyFloat(source, destination, "_AlphaClip");

            destination.SetFloat("_AlphaClip", source.IsKeywordEnabled("_ALPHATEST_ON") ? 1f : 0f);
            SetKeyword(destination, "_ALPHATEST_ON", source.IsKeywordEnabled("_ALPHATEST_ON"));
            SetKeyword(destination, "_NORMALMAP", source.IsKeywordEnabled("_NORMALMAP"));
            SetKeyword(destination, "_METALLICSPECGLOSSMAP", source.IsKeywordEnabled("_METALLICSPECGLOSSMAP"));
            SetKeyword(destination, "_OCCLUSIONMAP", source.IsKeywordEnabled("_OCCLUSIONMAP"));
            SetKeyword(destination, "_EMISSION", source.IsKeywordEnabled("_EMISSION"));
            bool disableSpecularHighlights = source.IsKeywordEnabled("_SPECULARHIGHLIGHTS_OFF");
            bool disableEnvironmentReflections = source.IsKeywordEnabled("_ENVIRONMENTREFLECTIONS_OFF");
            destination.SetFloat("_DisableSpecularHighlights", disableSpecularHighlights ? 1f : 0f);
            destination.SetFloat("_DisableEnvironmentReflections", disableEnvironmentReflections ? 1f : 0f);
            SetKeyword(destination, "_SPECULARHIGHLIGHTS_OFF", disableSpecularHighlights);
            SetKeyword(destination, "_ENVIRONMENTREFLECTIONS_OFF", disableEnvironmentReflections);
            float receiveShadows = source.HasProperty("_ReceiveShadows")
                ? source.GetFloat("_ReceiveShadows")
                : 1f;
            destination.SetFloat("_DisableReceiveShadows", receiveShadows < 0.5f ? 1f : 0f);
            SetKeyword(destination, "_RECEIVE_SHADOWS_OFF", receiveShadows < 0.5f);

            destination.enableInstancing = true;
        }

        private static void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }

        private static void CopyTexture(Material source, Material destination, params string[] properties)
        {
            foreach (string property in properties)
            {
                if (!source.HasProperty(property) || !destination.HasProperty(property))
                    continue;
                Texture texture = source.GetTexture(property);
                if (texture == null)
                    continue;
                destination.SetTexture(property, texture);
                destination.SetTextureScale(property, source.GetTextureScale(property));
                destination.SetTextureOffset(property, source.GetTextureOffset(property));
                return;
            }
        }

        private static void CopyColor(Material source, Material destination, params string[] properties)
        {
            foreach (string property in properties)
            {
                if (source.HasProperty(property) && destination.HasProperty(property))
                {
                    destination.SetColor(property, source.GetColor(property));
                    return;
                }
            }
        }

        private static void CopyFloat(Material source, Material destination, params string[] properties)
        {
            foreach (string property in properties)
            {
                if (source.HasProperty(property) && destination.HasProperty(property))
                {
                    destination.SetFloat(property, source.GetFloat(property));
                    return;
                }
            }
        }

        private static void ConfigureFoliageMaterial(
            Material material,
            Material source,
            int windMode,
            Bounds? bounds,
            bool isNewCopy)
        {
            if (material.shader.name != FoliageShaderName)
                return;

            if (isNewCopy)
            {
                material.SetFloat("_WindMode", windMode);
                material.SetFloat("_WindStrength", windMode == 0 ? 0.48f : windMode == 3 ? 0.035f : 0.34f);
                material.SetFloat("_WindSpeed", windMode == 0 ? 1.2f : 0.8f);
                material.SetFloat("_WindFrequency", windMode == 0 ? 1.3f : 0.75f);
                material.SetFloat("_FlutterStrength", windMode == 0 ? 0.24f : windMode == 2 ? 0.18f : 0.1f);
                material.SetFloat("_HeightMask", windMode == 0 ? 0.02f : 0.12f);
                material.SetFloat("_TrunkStiffness", 0.94f);
                material.SetFloat("_WindMaskPower", windMode == 0 ? 0.8f : windMode == 3 ? 3.2f : 1.5f);
            }

            if (isNewCopy && bounds.HasValue)
            {
                material.SetFloat("_WindHeightMin", bounds.Value.min.y);
                material.SetFloat("_WindHeightRange", Mathf.Max(bounds.Value.size.y, 0.001f));
            }

            material.SetFloat("_ReceiveShadows", source.HasProperty("_ReceiveShadows")
                ? source.GetFloat("_ReceiveShadows")
                : 1f);
            material.renderQueue = source.renderQueue;
        }

        private static void ConfigureWaterMaterial(Material material, Material source, bool isNewCopy)
        {
            if (material.shader.name != WaterShaderName)
                return;

            if (!isNewCopy)
                return;

            Color sourceColor = source.HasProperty("_BaseColor")
                ? source.GetColor("_BaseColor")
                : Color.white;
            material.SetColor("_ShallowColor", sourceColor);
            material.SetColor("_DeepColor", Color.Lerp(sourceColor, new Color(0.12f, 0.25f, 0.34f, 1f), 0.48f));
            material.SetFloat("_Alpha", sourceColor.a);
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static int GetWindMode(string materialName)
        {
            string lowerName = materialName.ToLowerInvariant();
            if (lowerName.Contains("trunk") || lowerName.Contains("bark") || lowerName.Contains("wood"))
                return 3;
            if (lowerName.Contains("grass"))
                return 0;
            if (lowerName.Contains("bush"))
                return 1;
            if (lowerName.Contains("leaf") || lowerName.Contains("branch") || lowerName.Contains("flower"))
                return 2;
            return 1;
        }

        private static Dictionary<Material, Bounds> CollectMaterialBounds(List<NatureMaterial> entries)
        {
            HashSet<Material> relevant = new HashSet<Material>(entries.Select(entry => entry.Source));
            Dictionary<Material, Bounds> boundsByMaterial = new Dictionary<Material, Bounds>();

            foreach (string prefabPath in AssetDatabase.FindAssets("t:Prefab", new[] { PackageRoot })
                         .Select(AssetDatabase.GUIDToAssetPath))
            {
                if (!prefabPath.Contains("Ultimate Nature", StringComparison.OrdinalIgnoreCase))
                    continue;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                    AccumulateRendererBounds(prefab.GetComponentsInChildren<Renderer>(true), relevant, boundsByMaterial);
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                    AccumulateRendererBounds(root.GetComponentsInChildren<Renderer>(true), relevant, boundsByMaterial);
            }

            return boundsByMaterial;
        }

        private static void AccumulateRendererBounds(
            IEnumerable<Renderer> renderers,
            HashSet<Material> relevant,
            Dictionary<Material, Bounds> boundsByMaterial)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;
                Bounds bounds = GetLocalRendererBounds(renderer);
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null || !relevant.Contains(material))
                        continue;
                    if (boundsByMaterial.TryGetValue(material, out Bounds accumulated))
                    {
                        accumulated.Encapsulate(bounds.min);
                        accumulated.Encapsulate(bounds.max);
                        boundsByMaterial[material] = accumulated;
                    }
                    else
                    {
                        boundsByMaterial.Add(material, bounds);
                    }
                }
            }
        }

        private static Bounds GetLocalRendererBounds(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
                return skinned.localBounds;
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
                return meshFilter.sharedMesh.bounds;
            return new Bounds(Vector3.zero, Vector3.one);
        }

        private static int ReplaceMaterials(
            Scene scene,
            Dictionary<Material, Material> materialMap,
            out int materialSlots,
            out int distinctMaterials)
        {
            int changedRenderers = 0;
            materialSlots = 0;
            HashSet<Material> changedSourceMaterials = new HashSet<Material>();
            foreach (GameObject root in scene.GetRootGameObjects())
                changedRenderers += ReplaceMaterials(root, materialMap, changedSourceMaterials, ref materialSlots);
            distinctMaterials = changedSourceMaterials.Count;
            return changedRenderers;
        }

        private static int ReplaceMaterials(
            GameObject root,
            Dictionary<Material, Material> materialMap,
            out int materialSlots,
            out int distinctMaterials)
        {
            HashSet<Material> changedSourceMaterials = new HashSet<Material>();
            materialSlots = 0;
            int changedRenderers = ReplaceMaterials(root, materialMap, changedSourceMaterials, ref materialSlots);
            distinctMaterials = changedSourceMaterials.Count;
            return changedRenderers;
        }

        private static int ReplaceMaterials(
            GameObject root,
            Dictionary<Material, Material> materialMap,
            HashSet<Material> changedSourceMaterials,
            ref int materialSlots)
        {
            int changedRenderers = 0;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (ReplaceRendererMaterials(renderer, materialMap, changedSourceMaterials, ref materialSlots))
                {
                    EditorUtility.SetDirty(renderer);
                    if (PrefabUtility.IsPartOfPrefabInstance(renderer))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    changedRenderers++;
                }
            }
            return changedRenderers;
        }

        private static bool ReplaceRendererMaterials(
            Renderer renderer,
            Dictionary<Material, Material> materialMap,
            HashSet<Material> changedSourceMaterials,
            ref int materialSlots)
        {
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && materialMap.TryGetValue(materials[i], out Material replacement))
                {
                    changedSourceMaterials.Add(materials[i]);
                    materials[i] = replacement;
                    changed = true;
                    materialSlots++;
                }
            }

            if (changed)
                renderer.sharedMaterials = materials;
            return changed;
        }

        private static Dictionary<Material, Material> LoadExistingMaterialMap()
        {
            Dictionary<Material, Material> materialMap = new Dictionary<Material, Material>();
            foreach (string sourcePath in FindNatureMaterialPaths())
            {
                string outputPath = $"{NatureMaterialFolder}/{Path.GetFileName(sourcePath)}";
                Material source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
                Material output = AssetDatabase.LoadAssetAtPath<Material>(outputPath);
                if (source != null && output != null)
                    materialMap[source] = output;
            }
            return materialMap;
        }

        private static void EnsureWindController(Scene scene)
        {
            foreach (WindController controller in UnityEngine.Object.FindObjectsByType<WindController>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (controller.gameObject.scene == scene)
                    return;
            }

            GameObject windObject = new GameObject("WindController");
            SceneManager.MoveGameObjectToScene(windObject, scene);
            Undo.RegisterCreatedObjectUndo(windObject, "Create Wind Controller");
            windObject.AddComponent<WindController>();
        }

        private static bool TryGetSelectedPrefabPath(out string path)
        {
            path = null;
            UnityEngine.Object selection = Selection.activeObject;
            if (selection == null)
                return false;

            path = AssetDatabase.GetAssetPath(selection);
            if (string.IsNullOrEmpty(path) && selection is GameObject selectedObject)
                path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selectedObject);

            return !string.IsNullOrEmpty(path) &&
                   path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) &&
                   AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        }

        private static void UpdateExistingNaturePrefab(
            string prefabPath,
            Dictionary<Material, Material> materialMap)
        {
            GameObject prefabRoot = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                int changedRenderers = ReplaceMaterials(prefabRoot, materialMap, out int materialSlots, out int distinctMaterials);
                if (changedRenderers == 0)
                {
                    Debug.Log($"Prefab variant '{prefabPath}' already uses wind/water material copies.");
                    return;
                }

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath, out bool success);
                if (!success || saved == null)
                {
                    Debug.LogError($"Could not update nature prefab variant '{prefabPath}'.");
                    return;
                }

                AssetDatabase.SaveAssets();
                Debug.Log(
                    $"Updated existing nature prefab variant '{prefabPath}'. " +
                    $"Changed renderers: {changedRenderers}; material slots: {materialSlots}; " +
                    $"distinct source materials used: {distinctMaterials}.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not update nature prefab variant '{prefabPath}': {exception}");
            }
            finally
            {
                if (prefabRoot != null)
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }
    }
}
