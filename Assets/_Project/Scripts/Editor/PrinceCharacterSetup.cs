using System;
using System.Collections.Generic;
using System.Linq;
using Prince;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using StarterAssets;

namespace Prince.Editor
{
    [CustomEditor(typeof(CharacterOutfit))]
    internal sealed class CharacterOutfitInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!GUILayout.Button("Apply Outfit"))
                return;

            foreach (UnityEngine.Object selected in targets)
            {
                var outfit = selected as CharacterOutfit;
                if (outfit == null)
                    continue;

                outfit.Apply();
                EditorUtility.SetDirty(outfit);
            }
        }
    }

    internal static class PrinceCharacterSetup
    {
        private const string KnightRoot = "Assets/Fantasy knight";
        private const string MaterialsFolder = KnightRoot + "/Materials";
        private const string BaseModelPath = KnightRoot + "/Base mesh/Skin_1.fbx";
        private const string OutfitPrefabFolder = KnightRoot + "/Prefab/Separated_mesh";
        private const string StarterAssetsRoot = "Assets/Starter Assets/Runtime/ThirdPersonController";
        private const string PlayerPrefabPath = StarterAssetsRoot + "/Prefabs/PlayerArmature.prefab";
        private const string CharacterFolder = "Assets/_Project";
        private const string CharacterPrefabFolder = CharacterFolder + "/Prefabs";
        private const string ScriptsFolder = CharacterFolder + "/Scripts";
        private const string RuntimeFolder = ScriptsFolder + "/Runtime";
        private const string EditorFolder = ScriptsFolder + "/Editor";
        private const string CharacterPrefabPath = CharacterPrefabFolder + "/PrinceCharacter.prefab";
        private const string PlayerPrincePrefabPath = CharacterPrefabFolder + "/PlayerPrince.prefab";
        private const string DemoScenePath =
            "Assets/InnerverseInteractive/Ultimate Nature – Starter/Demo/Scenes/Forest/UNS_Demo.unity";

        private static readonly OutfitPiece[] OutfitPieces =
        {
            new OutfitPiece("Body", "Body_01"),
            new OutfitPiece("Head", "Head"),
            new OutfitPiece("Hair", "Hair"),
            new OutfitPiece("Torso", "Torso"),
            new OutfitPiece("Tunic", "Tunic"),
            new OutfitPiece("Pants", "Pants"),
            new OutfitPiece("Shorts", "shorts"),
            new OutfitPiece("Boots", "Boots"),
            new OutfitPiece("Gloves", "Gloves"),
            new OutfitPiece("Bracers", "Bracers"),
            new OutfitPiece("Helmet", "Helmet"),
            new OutfitPiece("Bags", "Bags")
        };

        private static readonly string[] RequiredSkeletonBones =
        {
            "pelvis", "spine_01", "spine_02", "spine_03", "neck_01", "head",
            "clavicle_l", "upperarm_l", "lowerarm_l", "hand_l",
            "clavicle_r", "upperarm_r", "lowerarm_r", "hand_r",
            "thigh_l", "calf_l", "foot_l", "ball_l",
            "thigh_r", "calf_r", "foot_r", "ball_r"
        };

        [MenuItem("Tools/Prince/Convert Knight Materials To URP", false, 1)]
        private static void ConvertKnightMaterialsMenu()
        {
            ConvertKnightMaterials();
        }

        [MenuItem("Tools/Prince/Setup Player Character", false, 2)]
        private static void SetupPlayerCharacterMenu()
        {
            if (!ValidateRequiredAssets(true))
                return;

            if (!ConvertKnightMaterials())
                return;
            if (!ConfigureHumanoidAvatar(out Avatar avatar))
                return;

            if (!EnsureProjectFolders())
                return;

            GameObject characterPrefab = BuildPrinceCharacter(avatar);
            if (characterPrefab == null)
                return;

            GameObject playerPrefab = BuildPlayerPrincePrefab(characterPrefab, avatar);
            if (playerPrefab == null)
                return;

            ReplaceScenePlayer(playerPrefab);
        }

        [MenuItem("Tools/Prince/Validate Project Assets", false, 20)]
        private static void ValidateProjectAssetsMenu()
        {
            ValidateRequiredAssets(true);
        }

        private static bool ValidateRequiredAssets(bool logSuccess)
        {
            var missing = new List<string>();
            if (!AssetDatabase.IsValidFolder(KnightRoot))
                missing.Add(KnightRoot);
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
                missing.Add(MaterialsFolder);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BaseModelPath) == null)
                missing.Add(BaseModelPath);
            if (!AssetDatabase.IsValidFolder(OutfitPrefabFolder))
                missing.Add(OutfitPrefabFolder);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
                missing.Add(PlayerPrefabPath);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DemoScenePath) == null)
                missing.Add(DemoScenePath);

            foreach (OutfitPiece piece in OutfitPieces)
            {
                string path = GetOutfitPrefabPath(piece.AssetName);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                    missing.Add(path);
            }

            if (missing.Count > 0)
            {
                Debug.LogError("[Prince Setup] Required Starter Assets or Fantasy knight assets are missing:\n- "
                    + string.Join("\n- ", missing));
                return false;
            }

            if (logSuccess)
                Debug.Log("[Prince Setup] Starter Assets and Fantasy knight dependencies were found.");
            return true;
        }

        private static bool ConvertKnightMaterials()
        {
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                Debug.LogError("[Prince Setup] Knight material folder was not found: " + MaterialsFolder + ".");
                return false;
            }

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                Debug.LogError("[Prince Setup] Shader 'Universal Render Pipeline/Lit' was not found. "
                    + "Confirm that the URP package is installed and active.");
                return false;
            }

            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder });
            if (materialGuids.Length == 0)
            {
                Debug.LogWarning("[Prince Setup] No materials were found in " + MaterialsFolder + ".");
                return false;
            }

            string[] materialPaths = materialGuids.Select(AssetDatabase.GUIDToAssetPath).ToArray();
            foreach (string path in materialPaths)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    Debug.LogWarning("[Prince Setup] Could not load material at " + path + ".");
                    continue;
                }

                Texture normalTexture = GetTextureIfPresent(material, "_BumpMap");
                if (normalTexture != null)
                    EnsureNormalMapImport(normalTexture, path);
            }

            int converted = 0;
            foreach (string path in materialPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    Debug.LogWarning("[Prince Setup] Could not load material at " + path + ".");
                    continue;
                }

                Texture mainTexture = GetFirstTexture(material, "_MainTex", "_BaseMap");
                Texture normalTexture = GetTextureIfPresent(material, "_BumpMap");
                Texture metallicTexture = GetFirstTexture(material, "_MetallicGlossMap", "_MetallicSpecGlossMap");
                Color baseColor = GetColorIfPresent(material, "_Color", "_BaseColor", Color.white);
                float metallic = GetFloatIfPresent(material, "_Metallic", 0f);
                float smoothness = GetFloatIfPresent(
                    material, "_Glossiness", GetFloatIfPresent(material, "_Smoothness", 0.2f));
                float bumpScale = GetFloatIfPresent(material, "_BumpScale", 1f);
                float cutoff = GetFloatIfPresent(material, "_Cutoff", 0.5f);
                bool alphaClip = material.IsKeywordEnabled("_ALPHATEST_ON")
                    || Mathf.Approximately(GetFloatIfPresent(material, "_Mode", 0f), 1f);
                bool transparent = material.IsKeywordEnabled("_ALPHABLEND_ON")
                    || material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON")
                    || material.renderQueue == (int)RenderQueue.Transparent
                    || Mathf.Approximately(GetFloatIfPresent(material, "_Mode", 0f), 2f)
                    || Mathf.Approximately(GetFloatIfPresent(material, "_Mode", 0f), 3f);

                Vector2 mainScale = GetTextureScaleIfPresent(material, "_MainTex", "_BaseMap");
                Vector2 mainOffset = GetTextureOffsetIfPresent(material, "_MainTex", "_BaseMap");
                Vector2 normalScale = GetTextureScaleIfPresent(material, "_BumpMap");
                Vector2 normalOffset = GetTextureOffsetIfPresent(material, "_BumpMap");
                Vector2 metallicScale = GetTextureScaleIfPresent(
                    material, "_MetallicGlossMap", "_MetallicSpecGlossMap");
                Vector2 metallicOffset = GetTextureOffsetIfPresent(
                    material, "_MetallicGlossMap", "_MetallicSpecGlossMap");

                if (mainTexture == null)
                    Debug.LogWarning("[Prince Setup] Albedo texture is unassigned or unresolved on " + path + ".");
                else if (AssetDatabase.GetAssetPath(mainTexture).Length == 0)
                    Debug.LogWarning("[Prince Setup] The albedo texture on " + path
                        + " is not a project asset and may not be included in the build.");

                if (normalTexture == null)
                    Debug.LogWarning("[Prince Setup] Normal texture is unassigned or unresolved on " + path + ".");

                if (material.shader != urpLit)
                    material.shader = urpLit;

                SetTextureIfPresent(material, "_BaseMap", mainTexture, mainScale, mainOffset);
                SetTextureIfPresent(material, "_BumpMap", normalTexture, normalScale, normalOffset);
                SetTextureIfPresent(material, "_MetallicGlossMap", metallicTexture, metallicScale, metallicOffset);
                SetColorIfPresent(material, "_BaseColor", baseColor);
                SetFloatIfPresent(material, "_BumpScale", bumpScale);
                SetFloatIfPresent(material, "_AlphaClip", alphaClip ? 1f : 0f);
                SetFloatIfPresent(material, "_Cutoff", cutoff);

                if (metallicTexture == null)
                {
                    SetFloatIfPresent(material, "_Metallic", 0f);
                    SetFloatIfPresent(material, "_Smoothness", 0.2f);
                }
                else
                {
                    SetFloatIfPresent(material, "_Metallic", metallic);
                    SetFloatIfPresent(material, "_Smoothness", smoothness);
                }

                ConfigureSurface(material, alphaClip, transparent);
                if (normalTexture != null)
                    material.EnableKeyword("_NORMALMAP");
                else
                    material.DisableKeyword("_NORMALMAP");

                if (metallicTexture != null)
                    material.EnableKeyword("_METALLICSPECGLOSSMAP");
                else
                    material.DisableKeyword("_METALLICSPECGLOSSMAP");

                EditorUtility.SetDirty(material);
                converted++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Prince Setup] Converted " + converted + " knight material(s) to URP/Lit.");
            return converted > 0;
        }

        private static void EnsureNormalMapImport(Texture texture, string materialPath)
        {
            string texturePath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(texturePath))
            {
                Debug.LogWarning("[Prince Setup] Normal texture for " + materialPath
                    + " could not be resolved to an asset path.");
                return;
            }

            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[Prince Setup] Could not inspect normal texture importer at "
                    + texturePath + ".");
                return;
            }

            if (importer.textureType == TextureImporterType.NormalMap)
                return;

            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }

        private static void ConfigureSurface(Material material, bool alphaClip, bool transparent)
        {
            SetFloatIfPresent(material, "_Surface", transparent ? 1f : 0f);
            SetFloatIfPresent(material, "_Blend", 0f);
            SetFloatIfPresent(material, "_AlphaClip", alphaClip ? 1f : 0f);
            SetFloatIfPresent(material, "_SrcBlend", transparent ? (float)BlendMode.SrcAlpha : (float)BlendMode.One);
            SetFloatIfPresent(
                material, "_DstBlend", transparent ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.Zero);
            SetFloatIfPresent(material, "_ZWrite", transparent ? 0f : 1f);

            if (transparent)
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = alphaClip
                    ? (int)RenderQueue.AlphaTest
                    : (int)RenderQueue.Geometry;
            }

            if (alphaClip)
                material.EnableKeyword("_ALPHATEST_ON");
            else
                material.DisableKeyword("_ALPHATEST_ON");

            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        private static bool ConfigureHumanoidAvatar(out Avatar avatar)
        {
            avatar = null;
            var importer = AssetImporter.GetAtPath(BaseModelPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError("[Prince Setup] Skin_1.fbx does not have a ModelImporter.");
                return false;
            }

            Avatar importedAvatar = LoadImportedAvatar();
            bool avatarValid = importedAvatar != null && importedAvatar.isValid && importedAvatar.isHuman;
            bool importerSkipped = importer.animationType == ModelImporterAnimationType.Human && avatarValid;
            bool importerChanged = false;

            if (importerSkipped)
            {
                avatar = importedAvatar;
            }
            else
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
                importerChanged = true;

                importer = AssetImporter.GetAtPath(BaseModelPath) as ModelImporter;
                if (importer == null)
                {
                    Debug.LogError("[Prince Setup] Skin_1.fbx ModelImporter could not be reloaded after reimport.");
                    return false;
                }

                avatar = LoadImportedAvatar();
            }

            avatarValid = avatar != null && avatar.isValid && avatar.isHuman;
            string importerStatus = importerSkipped ? "skipped (already Human with a valid Humanoid Avatar)"
                : importerChanged ? "changed and reimported"
                : "unchanged";
            Debug.Log("[Prince Setup] Skin_1.fbx final rig type=" + importer.animationType
                + ", Avatar valid=" + (avatar != null && avatar.isValid)
                + ", Avatar Humanoid=" + (avatar != null && avatar.isHuman)
                + ", importer=" + importerStatus + ".");

            if (avatarValid)
            {
                return true;
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(BaseModelPath);
            var missingBones = new List<string>();
            if (model != null)
            {
                Transform[] transforms = model.GetComponentsInChildren<Transform>(true);
                foreach (string expectedBone in RequiredSkeletonBones)
                {
                    if (!TryFindTransform(transforms, expectedBone, out _))
                        missingBones.Add(expectedBone);
                }
            }

            string boneDiagnostics = model == null
                ? "Skin_1.fbx hierarchy could not be loaded for bone inspection"
                : missingBones.Count > 0
                    ? "missing transforms: " + string.Join(", ", missingBones)
                    : "all expected transforms exist; review this Humanoid bone mapping and pose: "
                        + string.Join(", ", RequiredSkeletonBones);
            Debug.LogError("[Prince Setup] Skin_1.fbx did not produce a valid Humanoid Avatar. "
                + "Problematic skeleton bones: " + boneDiagnostics
                + ". Buka Skin_1.fbx > Rig > Configure > Mapping > Automap, Pose > Enforce T-Pose > Done.");
            return false;
        }

        private static Avatar LoadImportedAvatar()
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(BaseModelPath);
            Animator modelAnimator = model != null ? model.GetComponent<Animator>() : null;
            if (modelAnimator != null && modelAnimator.avatar != null)
                return modelAnimator.avatar;

            UnityEngine.Object[] importedAssets = AssetDatabase.LoadAllAssetsAtPath(BaseModelPath);
            return importedAssets != null
                ? importedAssets.OfType<Avatar>().FirstOrDefault()
                : null;
        }

        private static GameObject BuildPrinceCharacter(Avatar avatar)
        {
            GameObject baseModel = AssetDatabase.LoadAssetAtPath<GameObject>(BaseModelPath);
            if (baseModel == null || avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                Debug.LogError("[Prince Setup] Cannot build PrinceCharacter without a valid Humanoid base model.");
                return null;
            }

            GameObject characterRoot = new GameObject("PrinceCharacter");
            Animator characterAnimator = characterRoot.AddComponent<Animator>();
            characterAnimator.avatar = avatar;
            characterAnimator.applyRootMotion = false;
            GameObject skeletonInstance = null;
            try
            {
                skeletonInstance = PrefabUtility.InstantiatePrefab(baseModel) as GameObject;
                if (skeletonInstance == null)
                {
                    Debug.LogError("[Prince Setup] Could not instantiate " + BaseModelPath + ".");
                    return null;
                }

                skeletonInstance.name = "KnightSkeleton";
                skeletonInstance.transform.SetParent(characterRoot.transform, false);

                SkinnedMeshRenderer[] baseRenderers =
                    skeletonInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (baseRenderers.Length == 0)
                {
                    Debug.LogError("[Prince Setup] Skin_1.fbx contains no SkinnedMeshRenderer; "
                        + "its skeleton cannot be used as the shared rig.");
                    return null;
                }

                var rigBones = new List<Transform>();
                Transform baseRootBone = null;
                foreach (SkinnedMeshRenderer renderer in baseRenderers)
                {
                    if (renderer.rootBone != null && baseRootBone == null)
                        baseRootBone = renderer.rootBone;
                    foreach (Transform bone in renderer.bones)
                    {
                        if (bone != null && !rigBones.Contains(bone))
                            rigBones.Add(bone);
                    }
                }

                if (rigBones.Count == 0)
                {
                    Debug.LogError("[Prince Setup] Skin_1.fbx renderers contain no bone references.");
                    return null;
                }

                Dictionary<string, Transform> rigByName = CreateBoneLookup(rigBones, BaseModelPath);
                if (rigByName == null)
                    return null;

                foreach (Renderer renderer in skeletonInstance.GetComponentsInChildren<Renderer>(true))
                    UnityEngine.Object.DestroyImmediate(renderer);
                foreach (Animator nestedAnimator in skeletonInstance.GetComponentsInChildren<Animator>(true))
                    UnityEngine.Object.DestroyImmediate(nestedAnimator);

                var outfit = characterRoot.AddComponent<CharacterOutfit>();
                ConfigureDefaultOutfit(outfit);

                foreach (OutfitPiece piece in OutfitPieces)
                {
                    string prefabPath = GetOutfitPrefabPath(piece.AssetName);
                    GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (sourcePrefab == null)
                    {
                        Debug.LogError("[Prince Setup] Outfit prefab was not found: " + prefabPath + ".");
                        return null;
                    }

                    GameObject sourceInstance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
                    if (sourceInstance == null)
                    {
                        Debug.LogError("[Prince Setup] Could not instantiate outfit prefab " + prefabPath + ".");
                        return null;
                    }

                    sourceInstance.transform.SetParent(characterRoot.transform, false);
                    if (PrefabUtility.IsPartOfPrefabInstance(sourceInstance))
                    {
                        PrefabUtility.UnpackPrefabInstance(
                            sourceInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                        if (PrefabUtility.IsPartOfPrefabInstance(sourceInstance))
                        {
                            UnityEngine.Object.DestroyImmediate(sourceInstance);
                            Debug.LogError("[Prince Setup] Could not unpack outfit prefab " + prefabPath
                                + "; its duplicate skeleton cannot be safely removed.");
                            return null;
                        }
                    }

                    SkinnedMeshRenderer[] renderers =
                        sourceInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    if (renderers.Length == 0)
                    {
                        UnityEngine.Object.DestroyImmediate(sourceInstance);
                        Debug.LogError("[Prince Setup] Outfit prefab has no SkinnedMeshRenderer: " + prefabPath);
                        return null;
                    }

                    var group = new GameObject(piece.AssetName);
                    group.transform.SetParent(characterRoot.transform, false);
                    bool pieceValid = true;

                    foreach (SkinnedMeshRenderer renderer in renderers)
                    {
                        if (!RebindRenderer(renderer, rigByName, baseRootBone, prefabPath))
                        {
                            pieceValid = false;
                            break;
                        }

                        if (renderer.transform == sourceInstance.transform)
                        {
                            Debug.LogError("[Prince Setup] Outfit root itself contains a skinned renderer in "
                                + prefabPath + "; refusing to leave its original skeleton in the shared rig.");
                            pieceValid = false;
                            break;
                        }

                        renderer.transform.SetParent(group.transform, true);
                    }

                    UnityEngine.Object.DestroyImmediate(sourceInstance);
                    if (!pieceValid)
                        return null;

                    SetOutfitObject(characterRoot, piece.FieldName, group);
                }

                outfit.Apply();
                if (!AlignCharacterFeet(characterRoot.transform, out float feetOffset))
                    return null;
                EditorUtility.SetDirty(outfit);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(characterRoot, CharacterPrefabPath, out bool success);
                if (!success || saved == null)
                {
                    Debug.LogError("[Prince Setup] Could not save " + CharacterPrefabPath + ".");
                    return null;
                }

                Debug.Log("[Prince Setup] Created " + CharacterPrefabPath
                    + " with one Skin_1 skeleton and re-bound outfit meshes. Applied internal feet offset "
                    + feetOffset.ToString("F3") + "m; PrinceCharacter root remains at the origin.");
                return saved;
            }
            finally
            {
                if (characterRoot != null)
                    UnityEngine.Object.DestroyImmediate(characterRoot);
            }
        }

        private static bool RebindRenderer(
            SkinnedMeshRenderer renderer,
            Dictionary<string, Transform> rigByName,
            Transform fallbackRootBone,
            string sourcePath)
        {
            if (renderer.sharedMesh == null)
            {
                Debug.LogError("[Prince Setup] SkinnedMeshRenderer has no mesh in " + sourcePath + ".");
                return false;
            }

            Transform[] sourceBones = renderer.bones;
            if (sourceBones == null || sourceBones.Length == 0)
            {
                Debug.LogError("[Prince Setup] SkinnedMeshRenderer has no bones in " + sourcePath + ".");
                return false;
            }

            var reboundBones = new Transform[sourceBones.Length];
            var missing = new List<string>();
            for (int i = 0; i < sourceBones.Length; i++)
            {
                Transform sourceBone = sourceBones[i];
                if (sourceBone == null || !TryMatchBone(sourceBone.name, rigByName, out Transform targetBone))
                {
                    missing.Add(sourceBone != null ? sourceBone.name : "(null at index " + i + ")");
                    continue;
                }

                reboundBones[i] = targetBone;
                if (!RestPoseMatches(sourceBone, targetBone))
                {
                    Debug.LogError("[Prince Setup] Bone '" + sourceBone.name + "' in " + sourcePath
                        + " has a different bind pose from Skin_1.fbx. The outfit cannot be safely re-bound.");
                    return false;
                }
            }

            if (missing.Count > 0)
            {
                Debug.LogError("[Prince Setup] Could not map bones in " + sourcePath + ": "
                    + string.Join(", ", missing) + ". No outfit skeleton will be kept.");
                return false;
            }

            Transform sourceRootBone = renderer.rootBone;
            Transform targetRootBone = null;
            if (sourceRootBone != null)
                TryMatchBone(sourceRootBone.name, rigByName, out targetRootBone);

            if (targetRootBone == null)
                targetRootBone = fallbackRootBone;
            if (targetRootBone == null)
            {
                Debug.LogError("[Prince Setup] Could not map rootBone in " + sourcePath + ".");
                return false;
            }

            if (renderer.sharedMesh.bindposes.Length != reboundBones.Length)
            {
                Debug.LogError("[Prince Setup] Bind-pose count does not match the bone count in "
                    + sourcePath + " (bind poses: " + renderer.sharedMesh.bindposes.Length
                    + ", bones: " + reboundBones.Length + ").");
                return false;
            }

            renderer.bones = reboundBones;
            renderer.rootBone = targetRootBone;
            return true;
        }

        private static bool RestPoseMatches(Transform sourceBone, Transform targetBone)
        {
            return Vector3.Distance(sourceBone.localPosition, targetBone.localPosition) <= 0.02f
                && Quaternion.Angle(sourceBone.localRotation, targetBone.localRotation) <= 2f
                && Vector3.Distance(sourceBone.localScale, targetBone.localScale) <= 0.02f;
        }

        private static Dictionary<string, Transform> CreateBoneLookup(
            IEnumerable<Transform> bones, string context)
        {
            var lookup = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (Transform bone in bones)
            {
                string key = NormalizeBoneName(bone.name);
                if (string.IsNullOrEmpty(key))
                    continue;

                if (lookup.TryGetValue(key, out Transform existing) && existing != bone)
                {
                    Debug.LogError("[Prince Setup] Multiple shared-skeleton bones normalize to '"
                        + key + "' in " + context + " (" + existing.name + " and " + bone.name
                        + "); bone names are ambiguous.");
                    return null;
                }

                lookup[key] = bone;
            }

            return lookup;
        }

        private static bool TryMatchBone(
            string sourceName, Dictionary<string, Transform> targetBones, out Transform target)
        {
            string key = NormalizeBoneName(sourceName);
            if (targetBones.TryGetValue(key, out target))
                return true;

            var candidates = targetBones
                .Where(pair => pair.Key.Length >= 4
                    && (key.StartsWith(pair.Key, StringComparison.Ordinal)
                        || pair.Key.StartsWith(key, StringComparison.Ordinal)
                        || key.EndsWith(pair.Key, StringComparison.Ordinal)
                        || pair.Key.EndsWith(key, StringComparison.Ordinal)))
                .OrderBy(pair => Math.Abs(pair.Key.Length - key.Length))
                .ToArray();

            if (candidates.Length == 0
                || (candidates.Length > 1
                    && Math.Abs(candidates[0].Key.Length - key.Length)
                    == Math.Abs(candidates[1].Key.Length - key.Length)))
            {
                target = null;
                return false;
            }

            target = candidates[0].Value;
            return true;
        }

        private static bool TryFindTransform(
            IEnumerable<Transform> transforms, string expectedName, out Transform match)
        {
            string expectedKey = NormalizeBoneName(expectedName);
            Transform[] candidates = transforms
                .Where(transform => NormalizeBoneName(transform.name) == expectedKey)
                .ToArray();
            if (candidates.Length == 1)
            {
                match = candidates[0];
                return true;
            }

            Transform[] exactName = candidates
                .Where(transform => string.Equals(
                    transform.name, expectedName, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (exactName.Length == 1)
            {
                match = exactName[0];
                return true;
            }

            match = null;
            return false;
        }

        private static string NormalizeBoneName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            var characters = name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray();
            string normalized = new string(characters);
            string[] prefixes = { "mixamorig", "armature", "skeleton", "rig" };
            foreach (string prefix in prefixes)
            {
                if (normalized.StartsWith(prefix, StringComparison.Ordinal) && normalized.Length > prefix.Length)
                    normalized = normalized.Substring(prefix.Length);
            }

            return normalized;
        }

        private static GameObject BuildPlayerPrincePrefab(GameObject characterPrefab, Avatar avatar)
        {
            GameObject sourcePlayer = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (sourcePlayer == null)
            {
                Debug.LogError("[Prince Setup] Starter Assets PlayerArmature prefab could not be loaded.");
                return null;
            }

            GameObject variantAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrincePrefabPath);
            if (variantAsset == null)
            {
                GameObject sourceInstance = PrefabUtility.InstantiatePrefab(sourcePlayer) as GameObject;
                if (sourceInstance == null)
                {
                    Debug.LogError("[Prince Setup] Could not instantiate the Starter Assets player prefab "
                        + "to create a variant.");
                    return null;
                }

                variantAsset = PrefabUtility.SaveAsPrefabAsset(
                    sourceInstance, PlayerPrincePrefabPath, out bool variantSaved);
                UnityEngine.Object.DestroyImmediate(sourceInstance);
                if (!variantSaved || variantAsset == null)
                {
                    Debug.LogError("[Prince Setup] Could not create a prefab variant at "
                        + PlayerPrincePrefabPath + ".");
                    return null;
                }
            }

            GameObject playerRoot = PrefabUtility.LoadPrefabContents(PlayerPrincePrefabPath);
            if (playerRoot == null)
            {
                Debug.LogError("[Prince Setup] Could not load PlayerPrince prefab contents.");
                return null;
            }

            try
            {
                playerRoot.name = "PlayerPrince";
                RemoveChildNamed(playerRoot.transform, "Geometry");
                RemoveChildNamed(playerRoot.transform, "Skeleton");

                Transform existingCharacter = playerRoot.transform.Find("PrinceCharacter");
                if (existingCharacter != null)
                    UnityEngine.Object.DestroyImmediate(existingCharacter.gameObject);

                GameObject nestedCharacter = PrefabUtility.InstantiatePrefab(characterPrefab) as GameObject;
                if (nestedCharacter == null)
                {
                    Debug.LogError("[Prince Setup] Could not instantiate the PrinceCharacter prefab.");
                    return null;
                }

                nestedCharacter.name = "PrinceCharacter";
                nestedCharacter.transform.SetParent(playerRoot.transform, false);
                nestedCharacter.transform.localPosition = Vector3.zero;
                nestedCharacter.transform.localRotation = Quaternion.identity;
                nestedCharacter.transform.localScale = Vector3.one;
                foreach (Animator nestedAnimator in nestedCharacter.GetComponentsInChildren<Animator>(true))
                    UnityEngine.Object.DestroyImmediate(nestedAnimator);

                Animator animator = playerRoot.GetComponent<Animator>();
                if (animator == null)
                {
                    Debug.LogError("[Prince Setup] PlayerArmature has no root Animator; refusing to remove its rig.");
                    return null;
                }

                if (animator.runtimeAnimatorController == null)
                {
                    Debug.LogError("[Prince Setup] PlayerArmature Animator has no Starter Assets controller.");
                    return null;
                }

                animator.avatar = avatar;
                animator.applyRootMotion = false;

                ThirdPersonController controller = playerRoot.GetComponent<ThirdPersonController>();
                if (controller == null)
                {
                    Debug.LogError("[Prince Setup] PlayerArmature has no ThirdPersonController component.");
                    return null;
                }

                Transform cameraTarget = playerRoot.transform.Find("PlayerCameraRoot");
                if (cameraTarget == null)
                {
                    Debug.LogError("[Prince Setup] PlayerCameraRoot was not found in PlayerArmature.");
                    return null;
                }

                controller.CinemachineCameraTarget = cameraTarget.gameObject;
                if (controller.GroundLayers.value == 0)
                    Debug.LogWarning("[Prince Setup] ThirdPersonController GroundLayers is empty; "
                        + "the original controller settings were preserved.");

                CharacterController characterController = playerRoot.GetComponent<CharacterController>();
                if (characterController == null)
                {
                    Debug.LogError("[Prince Setup] PlayerArmature has no CharacterController.");
                    return null;
                }

                float oldHeight = characterController.height;
                float oldRadius = characterController.radius;
                Vector3 oldCenter = characterController.center;
                float oldGroundedRadius = controller.GroundedRadius;
                if (!SetControllerBounds(characterController, nestedCharacter.transform, out Bounds meshBounds))
                    return null;
                controller.GroundedRadius = characterController.radius;

                EditorUtility.SetDirty(animator);
                EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(characterController);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(playerRoot, PlayerPrincePrefabPath, out bool success);
                if (!success || saved == null)
                {
                    Debug.LogError("[Prince Setup] Could not save " + PlayerPrincePrefabPath + ".");
                    return null;
                }

                Debug.Log("[Prince Setup] PlayerPrince CharacterController: old height=" + oldHeight.ToString("F3")
                    + ", radius=" + oldRadius.ToString("F3") + ", center=" + oldCenter
                    + "; new height=" + characterController.height.ToString("F3")
                    + ", radius=" + characterController.radius.ToString("F3")
                    + ", center=" + characterController.center
                    + ". GroundedRadius " + oldGroundedRadius.ToString("F3") + " -> "
                    + controller.GroundedRadius.ToString("F3")
                    + ". Knight local mesh bounds=" + meshBounds + ".");
                Debug.Log("[Prince Setup] ThirdPersonController, Animator Controller, GroundLayers, "
                    + "PlayerCameraRoot, and PlayerInput were preserved. Animation events OnFootstep/OnLand "
                    + "are implemented by the existing ThirdPersonController.");
                return saved;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(playerRoot);
            }
        }

        private static bool SetControllerBounds(
            CharacterController characterController, Transform characterRoot, out Bounds bounds)
        {
            if (!TryGetSkinnedMeshBounds(characterRoot, out bounds))
            {
                Debug.LogError("[Prince Setup] Active character outfit has no skinned mesh bounds; "
                    + "CharacterController was not changed.");
                return false;
            }

            float height = Mathf.Max(bounds.size.y, 0.1f);
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.5f;
            radius = Mathf.Max(radius, 0.05f);
            radius = Mathf.Min(radius, height * 0.49f);
            height = Mathf.Max(height, radius * 2f);

            characterController.height = height;
            characterController.radius = radius;
            characterController.center = new Vector3(bounds.center.x, height * 0.5f, bounds.center.z);
            return true;
        }

        private static bool AlignCharacterFeet(Transform characterRoot, out float feetOffset)
        {
            feetOffset = 0f;
            if (!TryGetSkinnedMeshBounds(characterRoot, out Bounds bounds))
            {
                Debug.LogError("[Prince Setup] Active character outfit has no skinned mesh bounds; "
                    + "cannot align its feet to the root origin.");
                return false;
            }

            feetOffset = -bounds.min.y;
            if (Mathf.Abs(feetOffset) < 0.0001f)
                return true;

            for (int i = 0; i < characterRoot.childCount; i++)
            {
                Transform child = characterRoot.GetChild(i);
                child.localPosition += Vector3.up * feetOffset;
            }

            return true;
        }

        private static bool TryGetSkinnedMeshBounds(Transform characterRoot, out Bounds bounds)
        {
            bool hasPoint = false;
            Vector3 minimum = Vector3.zero;
            Vector3 maximum = Vector3.zero;
            SkinnedMeshRenderer[] renderers = characterRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                if (!renderer.gameObject.activeInHierarchy || renderer.sharedMesh == null)
                    continue;

                Matrix4x4 matrix = characterRoot.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                var bakedMesh = new Mesh
                {
                    name = renderer.name + " Bounds",
                    hideFlags = HideFlags.HideAndDontSave
                };
                try
                {
                    renderer.BakeMesh(bakedMesh, false);
                    var vertices = new List<Vector3>();
                    bakedMesh.GetVertices(vertices);
                    if (vertices.Count == 0)
                    {
                        Debug.LogError("[Prince Setup] Could not bake skinned mesh bounds for "
                            + renderer.name + ".");
                        bounds = default;
                        return false;
                    }

                    foreach (Vector3 vertex in vertices)
                    {
                        Vector3 point = matrix.MultiplyPoint3x4(vertex);
                        if (!hasPoint)
                        {
                            minimum = point;
                            maximum = point;
                            hasPoint = true;
                        }
                        else
                        {
                            minimum = Vector3.Min(minimum, point);
                            maximum = Vector3.Max(maximum, point);
                        }
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(bakedMesh);
                }
            }

            if (!hasPoint)
            {
                bounds = default;
                return false;
            }

            bounds = new Bounds((minimum + maximum) * 0.5f, maximum - minimum);
            return true;
        }

        private static void ReplaceScenePlayer(GameObject playerPrefab)
        {
            Scene targetScene = SceneManager.GetSceneByPath(DemoScenePath);
            if (!targetScene.IsValid() || !targetScene.isLoaded)
            {
                targetScene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Additive);
            }
            SceneManager.SetActiveScene(targetScene);

            var controllers = targetScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ThirdPersonController>(true))
                .ToArray();
            if (controllers.Length != 1)
            {
                Debug.LogError("[Prince Setup] Expected one ThirdPersonController in UNS_Demo but found "
                    + controllers.Length + ". The scene was not modified.");
                return;
            }

            GameObject currentPlayer = controllers[0].gameObject;
            bool isAlreadyVariant =
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(currentPlayer) == PlayerPrincePrefabPath;
            Transform oldCameraRoot = currentPlayer.transform.Find("PlayerCameraRoot");
            CinemachineCamera[] sceneCameras = targetScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<CinemachineCamera>(true))
                .ToArray();
            var camerasToRetarget = new List<CinemachineCamera>();
            foreach (CinemachineCamera camera in sceneCameras)
            {
                bool targetsCurrentPlayer = oldCameraRoot != null
                    && (camera.Target.TrackingTarget == oldCameraRoot
                        || camera.Target.LookAtTarget == oldCameraRoot);
                if (targetsCurrentPlayer || camera.gameObject.name == "PlayerFollowCamera")
                    camerasToRetarget.Add(camera);
            }

            GameObject scenePlayer = currentPlayer;

            if (!isAlreadyVariant)
            {
                Transform oldTransform = currentPlayer.transform;
                Transform parent = oldTransform.parent;
                Vector3 localPosition = oldTransform.localPosition;
                Quaternion localRotation = oldTransform.localRotation;
                Vector3 localScale = oldTransform.localScale;
                int siblingIndex = oldTransform.GetSiblingIndex();
                int layer = currentPlayer.layer;
                string tag = currentPlayer.tag;

                var instance = PrefabUtility.InstantiatePrefab(playerPrefab, targetScene) as GameObject;
                if (instance == null)
                {
                    Debug.LogError("[Prince Setup] Could not instantiate PlayerPrince in UNS_Demo.");
                    return;
                }

                instance.name = "PlayerPrince";
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = localPosition;
                instance.transform.localRotation = localRotation;
                instance.transform.localScale = localScale;
                instance.transform.SetSiblingIndex(siblingIndex);
                instance.layer = layer;
                instance.tag = tag;
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);

                UnityEngine.Object.DestroyImmediate(currentPlayer);
                scenePlayer = instance;
            }

            Transform newCameraRoot = scenePlayer.transform.Find("PlayerCameraRoot");
            if (newCameraRoot == null)
            {
                Debug.LogError("[Prince Setup] New player is missing PlayerCameraRoot; "
                    + "the scene player was created but camera references could not be fixed.");
                return;
            }

            ThirdPersonController sceneController = scenePlayer.GetComponent<ThirdPersonController>();
            if (sceneController == null)
            {
                Debug.LogError("[Prince Setup] New player is missing ThirdPersonController.");
                return;
            }

            sceneController.CinemachineCameraTarget = newCameraRoot.gameObject;
            PrefabUtility.RecordPrefabInstancePropertyModifications(sceneController);
            bool followCameraUpdated = false;
            foreach (CinemachineCamera camera in camerasToRetarget)
            {
                camera.Target.TrackingTarget = newCameraRoot;
                camera.Target.LookAtTarget = newCameraRoot;
                camera.Target.CustomLookAtTarget = true;
                EditorUtility.SetDirty(camera);
                PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
                followCameraUpdated |= camera.gameObject.name == "PlayerFollowCamera";
            }

            if (!followCameraUpdated)
                Debug.LogWarning("[Prince Setup] No CinemachineCamera named PlayerFollowCamera was found. "
                    + "Existing cameras that referenced the old player were retargeted when possible.");

            EditorSceneManager.MarkSceneDirty(targetScene);
            if (!EditorSceneManager.SaveScene(targetScene))
            {
                Debug.LogError("[Prince Setup] Could not save scene " + DemoScenePath + ".");
                return;
            }

            Debug.Log("[Prince Setup] Replaced the robot with PlayerPrince in UNS_Demo at the original "
                + "position/rotation, preserving Player tag and layer. PlayerFollowCamera Follow and LookAt "
                + "now target the new PlayerCameraRoot.");
        }

        private static bool EnsureProjectFolders()
        {
            string[] folders = { CharacterFolder, CharacterPrefabFolder, ScriptsFolder, RuntimeFolder, EditorFolder };
            foreach (string folder in folders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                    continue;

                int separator = folder.LastIndexOf('/');
                string parent = folder.Substring(0, separator);
                string name = folder.Substring(separator + 1);
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    Debug.LogError("[Prince Setup] Cannot create folder " + folder
                        + " because parent folder " + parent + " does not exist.");
                    return false;
                }

                if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, name)))
                {
                    Debug.LogError("[Prince Setup] Could not create folder " + folder + ".");
                    return false;
                }
            }

            return true;
        }

        private static string GetOutfitPrefabPath(string assetName)
        {
            return OutfitPrefabFolder + "/" + assetName + ".prefab";
        }

        private static void SetOutfitObject(GameObject root, string fieldName, GameObject piece)
        {
            var serialized = new SerializedObject(root.GetComponent<CharacterOutfit>());
            SerializedProperty property = serialized.FindProperty(fieldName + "Object");
            if (property == null)
            {
                Debug.LogError("[Prince Setup] CharacterOutfit field was not found: " + fieldName + "Object.");
                return;
            }

            property.objectReferenceValue = piece;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureDefaultOutfit(CharacterOutfit outfit)
        {
            outfit.Body = true;
            outfit.Head = true;
            outfit.Hair = true;
            outfit.Torso = false;
            outfit.Tunic = true;
            outfit.Pants = true;
            outfit.Shorts = false;
            outfit.Boots = true;
            outfit.Gloves = true;
            outfit.Bracers = false;
            outfit.Helmet = false;
            outfit.Bags = false;
        }

        private static void RemoveChildNamed(Transform parent, string childName)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static Texture GetFirstTexture(Material material, string first, string second)
        {
            Texture texture = GetTextureIfPresent(material, first);
            return texture != null ? texture : GetTextureIfPresent(material, second);
        }

        private static Texture GetTextureIfPresent(Material material, string property)
        {
            return material.HasProperty(property) ? material.GetTexture(property) : null;
        }

        private static Color GetColorIfPresent(
            Material material, string first, string second, Color fallback)
        {
            if (material.HasProperty(first))
                return material.GetColor(first);
            return material.HasProperty(second) ? material.GetColor(second) : fallback;
        }

        private static float GetFloatIfPresent(Material material, string property, float fallback)
        {
            return material.HasProperty(property) ? material.GetFloat(property) : fallback;
        }

        private static Vector2 GetTextureScaleIfPresent(Material material, params string[] properties)
        {
            foreach (string property in properties)
            {
                if (material.HasProperty(property))
                    return material.GetTextureScale(property);
            }

            return Vector2.one;
        }

        private static Vector2 GetTextureOffsetIfPresent(Material material, params string[] properties)
        {
            foreach (string property in properties)
            {
                if (material.HasProperty(property))
                    return material.GetTextureOffset(property);
            }

            return Vector2.zero;
        }

        private static void SetTextureIfPresent(
            Material material, string property, Texture texture, Vector2 scale, Vector2 offset)
        {
            if (!material.HasProperty(property))
                return;

            material.SetTexture(property, texture);
            material.SetTextureScale(property, scale);
            material.SetTextureOffset(property, offset);
        }

        private static void SetColorIfPresent(Material material, string property, Color color)
        {
            if (material.HasProperty(property))
                material.SetColor(property, color);
        }

        private static void SetFloatIfPresent(Material material, string property, float value)
        {
            if (material.HasProperty(property))
                material.SetFloat(property, value);
        }

        private readonly struct OutfitPiece
        {
            public readonly string FieldName;
            public readonly string AssetName;

            public OutfitPiece(string fieldName, string assetName)
            {
                FieldName = fieldName;
                AssetName = assetName;
            }
        }

    }
}
