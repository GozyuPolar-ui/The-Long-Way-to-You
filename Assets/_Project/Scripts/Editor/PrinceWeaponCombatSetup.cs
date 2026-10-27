using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Prince;
using StarterAssets;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prince.Editor
{
    public static class PrinceWeaponCombatSetup
    {
        [MenuItem("Tools/Prince/Setup Loading Screen", false, 200)]
        private static void SetupLoadingScreen()
        {
            PrinceLoadingScreenSetup.Setup();
        }

        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/PlayerPrince.prefab";
        private const string StarterControllerPath =
            "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller";
        private const string PrinceControllerPath = "Assets/_Project/Animation/PlayerPrince.controller";
        private const string AnimationClipFolder = "Assets/_Project/Animation/Clips";
        private const string WeaponMaterialsFolder = "Assets/Delighton/JusticeSword/Materials";
        private const string SwordAudioFolder = "Assets/_Project/Audio/SFX/Sword";
        private const string SwordAlbedoPath =
            "Assets/Delighton/JusticeSword/Textures/JusticeSword/JusticeSword_AlbedoTransparency.png";
        private const string SwordMetallicPath =
            "Assets/Delighton/JusticeSword/Textures/JusticeSword/JusticeSword_MetallicSmoothness.png";
        private const string SwordNormalPath =
            "Assets/Delighton/JusticeSword/Textures/JusticeSword/JusticeSword_Normal.png";
        private const string ScabbardAlbedoPath =
            "Assets/Delighton/JusticeSword/Textures/Scabbard/Scabbard_AlbedoTransparency.png";
        private const string ScabbardMetallicPath =
            "Assets/Delighton/JusticeSword/Textures/Scabbard/Scabbard_MetallicSmoothness.png";
        private const string ScabbardNormalPath =
            "Assets/Delighton/JusticeSword/Textures/Scabbard/Scabbard_Normal.png";
        private const string SwordPrefabPath = "Assets/Delighton/JusticeSword/Prefabs/JusticeSword.prefab";
        private const string Attack1Path =
            "Assets/DoubleL/FBX Unity/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_1_InPlace.fbx";
        private const string Attack2Path =
            "Assets/DoubleL/FBX Unity/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_2_InPlace.fbx";
        private const string Attack3Path =
            "Assets/DoubleL/FBX Unity/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_3_InPlace.fbx";
        private const string HitPath = "Assets/DoubleL/FBX Unity/Hit/InPlace/Hit_B_4_InPlace.fbx";

        private sealed class CombatClips
        {
            public AnimationClip Draw;
            public AnimationClip Sheath;
            public string DrawSource;
            public string SheathSource;
            public AnimationClip Attack1;
            public AnimationClip Attack2;
            public AnimationClip Attack3;
            public AnimationClip Hit;
        }

        private sealed class ClipSource
        {
            public string AssetPath;
            public string ClipName;
        }

        private sealed class SwordAudioMapping
        {
            public AudioClip[] Draw = Array.Empty<AudioClip>();
            public AudioClip[] Sheath = Array.Empty<AudioClip>();
            public AudioClip[] Swing = Array.Empty<AudioClip>();
            public AudioClip[] HitFlesh = Array.Empty<AudioClip>();
            public AudioClip[] BearHit = Array.Empty<AudioClip>();
            public AudioClip[] HitHard = Array.Empty<AudioClip>();
            public string SheathFallbackName;
        }

        private readonly struct ConditionSpec
        {
            public readonly AnimatorConditionMode Mode;
            public readonly float Threshold;
            public readonly string Parameter;

            public ConditionSpec(AnimatorConditionMode mode, float threshold, string parameter)
            {
                Mode = mode;
                Threshold = threshold;
                Parameter = parameter;
            }
        }

        [MenuItem("Tools/Prince/Setup Weapon And Combat")]
        private static void SetupWeaponAndCombat()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Setup Weapon And Combat cannot run in Play mode.");
                return;
            }

            SwordAudioMapping audioMapping = SetupSwordAudioAssets();
            if (!ConvertWeaponMaterials())
                return;

            GameObject swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwordPrefabPath);
            if (swordPrefab == null)
            {
                Debug.LogError($"Weapon sword prefab was not found: '{SwordPrefabPath}'.");
                return;
            }

            if (!ValidateHumanoidClip(Attack1Path) || !ValidateHumanoidClip(Attack2Path) ||
                !ValidateHumanoidClip(Attack3Path) || !ValidateHumanoidClip(HitPath))
                return;

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Animation"))
                AssetDatabase.CreateFolder("Assets/_Project", "Animation");
            if (!AssetDatabase.IsValidFolder(AnimationClipFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Animation", "Clips");

            CombatClips clips;
            try
            {
                clips = CreateCombatClips();
                CreateOrUpdateController(clips);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not create the Prince combat controller: {exception}");
                return;
            }

            if (!AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath))
            {
                Debug.LogError($"Player prefab not found: '{PlayerPrefabPath}'.");
                return;
            }

            GameObject prefabRoot = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
                if (!ConfigureCharacter(prefabRoot, swordPrefab, clips, audioMapping, true))
                    return;

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not configure '{PlayerPrefabPath}': {exception}");
                return;
            }
            finally
            {
                if (prefabRoot != null)
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            UpdateActiveSceneInstances(swordPrefab, clips, audioMapping);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Combat clip mapping: Draw={(clips.Draw != null ? clips.Draw.name + ".anim from " + clips.DrawSource : "code-driven swap")}, " +
                $"Sheath={(clips.Sheath != null ? clips.Sheath.name + ".anim from " + clips.SheathSource : "code-driven swap")}; " +
                $"Attack1={Attack1Path} -> {clips.Attack1.name}.anim; " +
                $"Attack2={Attack2Path} -> {clips.Attack2.name}.anim; " +
                $"Attack3={Attack3Path} -> {clips.Attack3.name}.anim; " +
                $"Hit={HitPath} -> {clips.Hit.name}.anim.");
            Debug.Log(
                "Prince weapon/combat setup complete. Controller: " + PrinceControllerPath +
                ".");
        }

        [MenuItem("Tools/Prince/Convert Weapon Materials To URP", false, 3)]
        private static void ConvertWeaponMaterialsMenu()
        {
            ConvertWeaponMaterials();
        }

        private static SwordAudioMapping SetupSwordAudioAssets()
        {
            AssetDatabase.Refresh();
            SwordAudioMapping mapping = new SwordAudioMapping();
            if (!AssetDatabase.IsValidFolder(SwordAudioFolder))
            {
                Debug.LogWarning($"Sword SFX folder was not found: '{SwordAudioFolder}'.");
                return mapping;
            }

            List<AudioClip> draw = new List<AudioClip>();
            List<AudioClip> sheath = new List<AudioClip>();
            List<AudioClip> swing = new List<AudioClip>();
            List<AudioClip> hitFlesh = new List<AudioClip>();
            List<AudioClip> bearHit = new List<AudioClip>();
            List<AudioClip> hitHard = new List<AudioClip>();
            List<string> tableRows = new List<string>();
            string[] audioPaths = AssetDatabase.FindAssets("t:AudioClip", new[] { SwordAudioFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                .ToArray();

            for (int i = 0; i < audioPaths.Length; i++)
            {
                string path = audioPaths[i];
                string fileName = Path.GetFileName(path);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (importer == null || clip == null)
                {
                    Debug.LogWarning($"Could not configure or load sword audio asset '{path}'.");
                    continue;
                }

                ConfigureSwordAudioImporter(importer, clip, path);
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null)
                {
                    Debug.LogWarning($"Sword audio clip was unavailable after import: '{path}'.");
                    continue;
                }

                string normalizedName = Path.GetFileNameWithoutExtension(fileName);
                string category;
                if (ContainsAudioToken(normalizedName, "beruang") ||
                    ContainsAudioToken(normalizedName, "bear"))
                {
                    bearHit.Add(clip);
                    category = "bear hit";
                }
                else if (ContainsAudioToken(normalizedName, "unsheath") ||
                    ContainsAudioToken(normalizedName, "draw"))
                {
                    draw.Add(clip);
                    category = "draw";
                }
                else if (!ContainsAudioToken(normalizedName, "unsheath") &&
                         (ContainsAudioToken(normalizedName, "sheath") ||
                          ContainsAudioToken(normalizedName, "holster")))
                {
                    sheath.Add(clip);
                    category = "sheath";
                }
                else if (ContainsAudioToken(normalizedName, "swoosh") ||
                         ContainsAudioToken(normalizedName, "swing") ||
                         ContainsAudioToken(normalizedName, "slash"))
                {
                    swing.Add(clip);
                    category = "swing";
                }
                else if (ContainsAudioToken(normalizedName, "keras") ||
                         ContainsAudioToken(normalizedName, "hard") ||
                         ContainsAudioToken(normalizedName, "wall"))
                {
                    hitHard.Add(clip);
                    category = "hard hit";
                }
                else if (ContainsAudioToken(normalizedName, "impact") ||
                         ContainsAudioToken(normalizedName, "hit"))
                {
                    hitFlesh.Add(clip);
                    category = "flesh hit";
                }
                else
                {
                    category = "unmapped";
                }

                tableRows.Add($"{fileName} -> {category}");
            }

            mapping.Draw = draw.ToArray();
            mapping.Sheath = sheath.ToArray();
            mapping.Swing = swing.ToArray();
            mapping.HitFlesh = hitFlesh.ToArray();
            mapping.BearHit = bearHit.ToArray();
            mapping.HitHard = hitHard.ToArray();
            if (mapping.BearHit.Length == 0)
                Debug.LogWarning(
                    $"No bear-specific sword hit sound was found in '{SwordAudioFolder}'. " +
                    "Files containing 'bear' or 'beruang' are mapped separately from ordinary hits.");
            if (mapping.Sheath.Length == 0)
            {
                if (mapping.Draw.Length > 0)
                    mapping.SheathFallbackName = mapping.Draw[0].name;
                Debug.LogWarning("Suara menyarungkan belum ada, memakai klip draw sebagai cadangan");
            }

            StringBuilder report = new StringBuilder("Sword audio mapping:");
            for (int i = 0; i < tableRows.Count; i++)
                report.AppendLine().Append("  ").Append(tableRows[i]);
            if (!string.IsNullOrEmpty(mapping.SheathFallbackName))
                report.AppendLine().Append("  Sheath fallback: ")
                    .Append(mapping.SheathFallbackName)
                    .Append(" (pitch x0.85, volume x0.7)");
            report.AppendLine().Append("  Import settings: Decompress On Load, preload enabled, PCM.");
            Debug.Log(report.ToString());
            return mapping;
        }

        private static bool ContainsAudioToken(string value, string token)
        {
            return value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void ConfigureSwordAudioImporter(
            AudioImporter importer,
            AudioClip clip,
            string path)
        {
            bool forceMono = clip.channels == 1;
            if (clip.channels > 1)
                Debug.Log($"Keeping stereo sword SFX '{Path.GetFileName(path)}' stereo ({clip.channels} channels).");
            else if (clip.channels <= 0)
                Debug.LogWarning($"Could not determine channel count for sword SFX '{path}'; leaving channel mode unchanged.");

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            bool changed = settings.loadType != AudioClipLoadType.DecompressOnLoad ||
                           settings.compressionFormat != AudioCompressionFormat.PCM ||
                           !settings.preloadAudioData ||
                           (clip.channels > 0 && importer.forceToMono != forceMono);
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            if (clip.channels > 0)
                importer.forceToMono = forceMono;
            if (changed)
                importer.SaveAndReimport();
        }

        private static bool ConvertWeaponMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                Debug.LogError(
                    "Weapon material conversion failed: shader 'Universal Render Pipeline/Lit' was not found.");
                return false;
            }

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { WeaponMaterialsFolder });
            if (guids.Length == 0)
            {
                Debug.LogError($"No weapon materials were found under '{WeaponMaterialsFolder}'.");
                return false;
            }

            string[] materialPaths = guids.Select(AssetDatabase.GUIDToAssetPath).ToArray();
            foreach (string path in materialPaths)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    Debug.LogWarning($"Could not load weapon material '{path}'.");
                    continue;
                }

                Texture normal = GetTexture(material, "_BumpMap");
                if (normal == null)
                    normal = LoadWeaponTexture(path, SwordNormalPath, ScabbardNormalPath);
                if (normal != null)
                    EnsureNormalMapImport(normal, path);

                Texture metallicMap =
                    GetFirstTexture(material, "_MetallicGlossMap", "_MetallicSpecGlossMap");
                if (metallicMap == null)
                    metallicMap = LoadWeaponTexture(path, SwordMetallicPath, ScabbardMetallicPath);
                if (metallicMap != null)
                    EnsureLinearTextureImport(metallicMap, path);
            }

            int converted = 0;
            foreach (string path in materialPaths)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                    continue;

                Texture main = GetFirstTexture(material, "_MainTex", "_BaseMap");
                if (main == null)
                    main = LoadWeaponTexture(path, SwordAlbedoPath, ScabbardAlbedoPath);
                Texture normal = GetTexture(material, "_BumpMap");
                if (normal == null)
                    normal = LoadWeaponTexture(path, SwordNormalPath, ScabbardNormalPath);
                Texture metallicMap = GetFirstTexture(material, "_MetallicGlossMap", "_MetallicSpecGlossMap");
                if (metallicMap == null)
                    metallicMap = LoadWeaponTexture(path, SwordMetallicPath, ScabbardMetallicPath);
                Color color = GetColor(material, "_Color", GetColor(material, "_BaseColor", Color.white));
                float metallic = GetFloat(material, "_Metallic", 0f);
                float smoothness = GetFloat(material, "_Smoothness", GetFloat(material, "_Glossiness", 0.5f));
                float bumpScale = GetFloat(material, "_BumpScale", 1f);
                Vector2 mainScale = GetTextureScale(material, "_MainTex", "_BaseMap");
                Vector2 mainOffset = GetTextureOffset(material, "_MainTex", "_BaseMap");
                Vector2 normalScale = GetTextureScale(material, "_BumpMap");
                Vector2 normalOffset = GetTextureOffset(material, "_BumpMap");
                Vector2 metallicScale = GetTextureScale(material, "_MetallicGlossMap", "_MetallicSpecGlossMap");
                Vector2 metallicOffset = GetTextureOffset(material, "_MetallicGlossMap", "_MetallicSpecGlossMap");

                if (material.shader != urpLit)
                    material.shader = urpLit;

                SetTexture(material, "_BaseMap", main, mainScale, mainOffset);
                SetTexture(material, "_BumpMap", normal, normalScale, normalOffset);
                SetTexture(material, "_MetallicGlossMap", metallicMap, metallicScale, metallicOffset);
                SetColor(material, "_BaseColor", color);
                SetFloat(material, "_Metallic", metallic);
                SetFloat(material, "_Smoothness", smoothness);
                SetFloat(material, "_BumpScale", bumpScale);
                if (main == null)
                    Debug.LogWarning($"No base-color texture is assigned or available for '{path}'.");
                if (normal != null)
                    material.EnableKeyword("_NORMALMAP");
                else
                    material.DisableKeyword("_NORMALMAP");
                if (metallicMap != null)
                    material.EnableKeyword("_METALLICSPECGLOSSMAP");
                else
                    material.DisableKeyword("_METALLICSPECGLOSSMAP");

                EditorUtility.SetDirty(material);
                converted++;
                Debug.Log(
                    $"Weapon material '{path}': base map='{(main != null ? AssetDatabase.GetAssetPath(main) : "missing")}', " +
                    $"normal='{(normal != null ? AssetDatabase.GetAssetPath(normal) : "missing")}', " +
                    $"metallic/smoothness='{(metallicMap != null ? AssetDatabase.GetAssetPath(metallicMap) : "missing")}'.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Converted {converted} JusticeSword/scabbard material(s) to URP/Lit.");
            return converted > 0;
        }

        private static Texture LoadWeaponTexture(
            string materialPath,
            string swordTexturePath,
            string scabbardTexturePath)
        {
            bool isScabbard = Path.GetFileNameWithoutExtension(materialPath)
                .IndexOf("scabbard", StringComparison.OrdinalIgnoreCase) >= 0;
            string texturePath = isScabbard ? scabbardTexturePath : swordTexturePath;
            Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
            if (texture == null)
                Debug.LogWarning(
                    $"Expected weapon texture '{texturePath}' for material '{materialPath}' was not found.");
            return texture;
        }

        private static void EnsureLinearTextureImport(Texture texture, string materialPath)
        {
            string texturePath = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning(
                    $"Could not inspect metallic/smoothness texture '{texturePath}' for '{materialPath}'.");
                return;
            }
            if (!importer.sRGBTexture)
                return;

            importer.sRGBTexture = false;
            importer.SaveAndReimport();
        }

        private static void EnsureNormalMapImport(Texture texture, string materialPath)
        {
            string texturePath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(texturePath))
            {
                Debug.LogWarning($"Normal map on '{materialPath}' is not a project asset.");
                return;
            }

            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Could not inspect normal-map importer '{texturePath}'.");
                return;
            }
            if (importer.textureType == TextureImporterType.NormalMap)
                return;

            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }

        private static Texture GetTexture(Material material, string property)
        {
            return material.HasProperty(property) ? material.GetTexture(property) : null;
        }

        private static Texture GetFirstTexture(Material material, string first, string second)
        {
            Texture texture = GetTexture(material, first);
            return texture != null ? texture : GetTexture(material, second);
        }

        private static Color GetColor(Material material, string property, Color fallback)
        {
            return material.HasProperty(property) ? material.GetColor(property) : fallback;
        }

        private static float GetFloat(Material material, string property, float fallback)
        {
            return material.HasProperty(property) ? material.GetFloat(property) : fallback;
        }

        private static Vector2 GetTextureScale(Material material, string first, string second = null)
        {
            if (material.HasProperty(first))
                return material.GetTextureScale(first);
            return !string.IsNullOrEmpty(second) && material.HasProperty(second)
                ? material.GetTextureScale(second)
                : Vector2.one;
        }

        private static Vector2 GetTextureOffset(Material material, string first, string second = null)
        {
            if (material.HasProperty(first))
                return material.GetTextureOffset(first);
            return !string.IsNullOrEmpty(second) && material.HasProperty(second)
                ? material.GetTextureOffset(second)
                : Vector2.zero;
        }

        private static void SetTexture(
            Material material, string property, Texture texture, Vector2 scale, Vector2 offset)
        {
            if (!material.HasProperty(property))
                return;
            material.SetTexture(property, texture);
            material.SetTextureScale(property, scale);
            material.SetTextureOffset(property, offset);
        }

        private static void SetColor(Material material, string property, Color color)
        {
            if (material.HasProperty(property))
                material.SetColor(property, color);
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property))
                material.SetFloat(property, value);
        }

        private static bool ValidateHumanoidClip(string fbxPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"Animation FBX not found or not imported: '{fbxPath}'.");
                return false;
            }
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                Debug.LogError($"Animation FBX must use a Humanoid rig: '{fbxPath}'.");
                return false;
            }
            if (AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>().FirstOrDefault() == null)
            {
                Debug.LogError($"No AnimationClip subasset found in '{fbxPath}'.");
                return false;
            }
            return true;
        }

        private static CombatClips CreateCombatClips()
        {
            ClipSource drawSource = FindHumanoidAnimation("draw");
            ClipSource sheathSource = FindHumanoidAnimation("sheath");
            AnimationClip draw = drawSource != null
                ? CreateClipCopy(drawSource, "Draw", false, "OnDrawSword")
                : null;
            AnimationClip sheath = sheathSource != null
                ? CreateClipCopy(sheathSource, "Sheath", false, "OnSheathSword")
                : null;

            if (draw == null || sheath == null)
            {
                Debug.LogWarning(
                    "Mixamo draw/sheath animations were not both found as Humanoid FBX assets. " +
                    $"Draw: {(drawSource != null ? drawSource.AssetPath + " [" + drawSource.ClipName + "]" : "missing")}; " +
                    $"Sheath: {(sheathSource != null ? sheathSource.AssetPath + " [" + sheathSource.ClipName + "]" : "missing")}. " +
                    "Missing clips will use the short code-driven weapon swap.");
            }

            return new CombatClips
            {
                Draw = draw,
                Sheath = sheath,
                DrawSource = drawSource != null ? drawSource.AssetPath + " [" + drawSource.ClipName + "]" : null,
                SheathSource = sheathSource != null ? sheathSource.AssetPath + " [" + sheathSource.ClipName + "]" : null,
                Attack1 = CreateClipCopy(Attack1Path, "Attack1", true),
                Attack2 = CreateClipCopy(Attack2Path, "Attack2", true),
                Attack3 = CreateClipCopy(Attack3Path, "Attack3", true),
                Hit = CreateClipCopy(HitPath, "Hit", false)
            };
        }

        private static ClipSource FindHumanoidAnimation(string nameToken)
        {
            string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
            string[] candidates = modelGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => string.Equals(Path.GetExtension(path), ".fbx", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(path => path.IndexOf("/Mixamo/", StringComparison.OrdinalIgnoreCase) >= 0)
                .ThenByDescending(path => Path.GetFileNameWithoutExtension(path)
                    .IndexOf(nameToken, StringComparison.OrdinalIgnoreCase) >= 0)
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            for (int i = 0; i < candidates.Length; i++)
            {
                ModelImporter importer = AssetImporter.GetAtPath(candidates[i]) as ModelImporter;
                if (importer == null || importer.animationType != ModelImporterAnimationType.Human)
                {
                    bool filenameMatches = Path.GetFileNameWithoutExtension(candidates[i])
                        .IndexOf(nameToken, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (filenameMatches)
                        Debug.LogWarning(
                            $"Ignoring non-Humanoid {nameToken} animation candidate '{candidates[i]}'.");
                    continue;
                }

                AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(candidates[i])
                    .OfType<AnimationClip>()
                    .Where(clip => clip.name.IndexOf("__preview__", StringComparison.Ordinal) != 0)
                    .ToArray();
                AnimationClip matchingClip = clips.FirstOrDefault(clip =>
                    clip.name.IndexOf(nameToken, StringComparison.OrdinalIgnoreCase) >= 0);
                if (matchingClip != null)
                    return new ClipSource { AssetPath = candidates[i], ClipName = matchingClip.name };

                bool modelNameMatches = Path.GetFileNameWithoutExtension(candidates[i])
                    .IndexOf(nameToken, StringComparison.OrdinalIgnoreCase) >= 0;
                if (modelNameMatches && clips.Length > 0)
                    return new ClipSource { AssetPath = candidates[i], ClipName = clips[0].name };
            }

            return null;
        }

        private static AnimationClip CreateClipCopy(ClipSource source, string outputName, bool addEvent, string eventName)
        {
            string outputPath = AnimationClipFolder + "/" + outputName + ".anim";
            AnimationClip copiedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
            if (copiedClip == null)
            {
                AnimationClip sourceClip = AssetDatabase.LoadAllAssetsAtPath(source.AssetPath)
                    .OfType<AnimationClip>()
                    .FirstOrDefault(clip => clip.name == source.ClipName);
                if (sourceClip == null)
                    throw new InvalidOperationException(
                        $"Animation clip '{source.ClipName}' was not found in '{source.AssetPath}'.");

                copiedClip = UnityEngine.Object.Instantiate(sourceClip);
                copiedClip.name = outputName;
                copiedClip.hideFlags = HideFlags.None;
                AssetDatabase.CreateAsset(copiedClip, outputPath);
            }

            if (addEvent)
            {
                EnsureAttackEvents(copiedClip);
                EnsureAttackSwingEvent(copiedClip);
            }
            if (!string.IsNullOrEmpty(eventName))
                EnsureAnimationEvent(copiedClip, eventName, copiedClip.length * 0.5f);
            EditorUtility.SetDirty(copiedClip);
            return copiedClip;
        }

        private static AnimationClip CreateClipCopy(
            string sourcePath,
            string outputName,
            bool addHitEvents,
            string additionalEvent = null)
        {
            string outputPath = AnimationClipFolder + "/" + outputName + ".anim";
            AnimationClip copiedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
            if (copiedClip == null)
            {
                AnimationClip sourceClip = AssetDatabase.LoadAllAssetsAtPath(sourcePath)
                    .OfType<AnimationClip>()
                    .FirstOrDefault(clip => clip.name.IndexOf("__preview__", StringComparison.Ordinal) != 0);
                if (sourceClip == null)
                    throw new InvalidOperationException($"No animation clip found in '{sourcePath}'.");

                copiedClip = UnityEngine.Object.Instantiate(sourceClip);
                copiedClip.name = outputName;
                copiedClip.hideFlags = HideFlags.None;
                AssetDatabase.CreateAsset(copiedClip, outputPath);
            }

            if (addHitEvents)
            {
                EnsureAttackEvents(copiedClip);
                EnsureAttackSwingEvent(copiedClip);
            }
            if (!string.IsNullOrEmpty(additionalEvent))
                EnsureAnimationEvent(copiedClip, additionalEvent, copiedClip.length * 0.5f);
            EditorUtility.SetDirty(copiedClip);
            return copiedClip;
        }

        private static void EnsureAnimationEvent(AnimationClip clip, string functionName, float time)
        {
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            if (events.Any(animationEvent => animationEvent.functionName == functionName))
                return;

            List<AnimationEvent> updatedEvents = events.ToList();
            updatedEvents.Add(new AnimationEvent
            {
                time = Mathf.Clamp(time, 0f, clip.length),
                functionName = functionName
            });
            updatedEvents.Sort((left, right) => left.time.CompareTo(right.time));
            AnimationUtility.SetAnimationEvents(clip, updatedEvents.ToArray());
        }

        private static void EnsureAttackEvents(AnimationClip clip)
        {
            List<AnimationEvent> events = AnimationUtility.GetAnimationEvents(clip).ToList();
            if (!events.Any(animationEvent => animationEvent.functionName == "OnAttackHitStart"))
            {
                events.Add(new AnimationEvent
                {
                    time = Mathf.Clamp(clip.length * 0.28f, 0f, clip.length),
                    functionName = "OnAttackHitStart"
                });
            }
            if (!events.Any(animationEvent => animationEvent.functionName == "OnAttackHitEnd"))
            {
                events.Add(new AnimationEvent
                {
                    time = Mathf.Clamp(clip.length * 0.55f, 0f, clip.length),
                    functionName = "OnAttackHitEnd"
                });
            }

            events.Sort((left, right) => left.time.CompareTo(right.time));
            AnimationUtility.SetAnimationEvents(clip, events.ToArray());
        }

        private static void EnsureAttackSwingEvent(AnimationClip clip)
        {
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            if (events.Any(animationEvent => animationEvent.functionName == "OnAttackSwing"))
                return;

            AnimationEvent hitStart = events.FirstOrDefault(
                animationEvent => animationEvent.functionName == "OnAttackHitStart");
            float swingTime = hitStart != null ? hitStart.time * 0.9f : clip.length * 0.25f;
            EnsureAnimationEvent(clip, "OnAttackSwing", swingTime);
        }

        private static void CreateOrUpdateController(CombatClips clips)
        {
            if (!AssetDatabase.LoadAssetAtPath<AnimatorController>(PrinceControllerPath))
            {
                if (!AssetDatabase.CopyAsset(StarterControllerPath, PrinceControllerPath))
                    throw new InvalidOperationException(
                        $"Could not copy Starter Assets controller from '{StarterControllerPath}'.");
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PrinceControllerPath);
            if (controller == null)
                throw new InvalidOperationException($"Prince AnimatorController is missing: '{PrinceControllerPath}'.");

            EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Draw", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Sheath", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Hit", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "IsArmed", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "ComboIndex", AnimatorControllerParameterType.Int);

            AnimatorControllerLayer layer = controller.layers.FirstOrDefault(candidate => candidate.name == "Combat");
            if (layer == null)
            {
                controller.AddLayer("Combat");
                AnimatorControllerLayer[] layers = controller.layers;
                int addedIndex = Array.FindIndex(layers, candidate => candidate.name == "Combat");
                layers[addedIndex].defaultWeight = 1f;
                layers[addedIndex].blendingMode = AnimatorLayerBlendingMode.Override;
                controller.layers = layers;
                layer = controller.layers[addedIndex];
            }

            AnimatorStateMachine stateMachine = layer.stateMachine;
            AnimatorState empty = EnsureState(stateMachine, "Empty", null, new Vector3(250f, 0f, 0f));
            AnimatorState draw = EnsureState(stateMachine, "Draw", clips.Draw, new Vector3(0f, 0f, 0f));
            AnimatorState attack1 = EnsureState(stateMachine, "Attack1", clips.Attack1, new Vector3(0f, 100f, 0f));
            AnimatorState attack2 = EnsureState(stateMachine, "Attack2", clips.Attack2, new Vector3(250f, 100f, 0f));
            AnimatorState attack3 = EnsureState(stateMachine, "Attack3", clips.Attack3, new Vector3(500f, 100f, 0f));
            AnimatorState hit = EnsureState(stateMachine, "Hit", clips.Hit, new Vector3(500f, 0f, 0f));
            AnimatorState sheath = EnsureState(stateMachine, "Sheath", clips.Sheath, new Vector3(0f, 200f, 0f));
            draw.speed = 1.35f;
            sheath.speed = 1.35f;
            attack1.speed = 1.45f;
            attack2.speed = 1.45f;
            attack3.speed = 1.45f;
            hit.speed = 1.65f;

            if (stateMachine.defaultState == null)
                stateMachine.defaultState = empty;

            EnsureTransition(stateMachine, empty, draw, false, 0f, 0.08f,
                new ConditionSpec(AnimatorConditionMode.If, 0f, "Draw"));
            EnsureTransition(stateMachine, empty, sheath, false, 0f, 0.08f,
                new ConditionSpec(AnimatorConditionMode.If, 0f, "Sheath"));
            EnsureTransition(stateMachine, empty, attack1, false, 0f, 0.08f,
                new ConditionSpec(AnimatorConditionMode.If, 0f, "Attack"),
                new ConditionSpec(AnimatorConditionMode.Equals, 1f, "ComboIndex"));
            EnsureTransition(stateMachine, empty, attack2, false, 0f, 0.08f,
                new ConditionSpec(AnimatorConditionMode.If, 0f, "Attack"),
                new ConditionSpec(AnimatorConditionMode.Equals, 2f, "ComboIndex"));
            EnsureTransition(stateMachine, empty, attack3, false, 0f, 0.08f,
                new ConditionSpec(AnimatorConditionMode.If, 0f, "Attack"),
                new ConditionSpec(AnimatorConditionMode.Equals, 3f, "ComboIndex"));

            EnsureTransition(stateMachine, draw, empty, true, 0.95f, 0.1f);
            EnsureTransition(stateMachine, sheath, empty, true, 0.95f, 0.1f);
            EnsureTransition(stateMachine, attack1, attack2, true, 0.68f, 0.1f,
                new ConditionSpec(AnimatorConditionMode.If, 0f, "Attack"),
                new ConditionSpec(AnimatorConditionMode.Equals, 2f, "ComboIndex"));
            EnsureTransition(stateMachine, attack2, attack3, true, 0.68f, 0.1f,
                new ConditionSpec(AnimatorConditionMode.If, 0f, "Attack"),
                new ConditionSpec(AnimatorConditionMode.Equals, 3f, "ComboIndex"));
            EnsureTransition(stateMachine, attack1, empty, true, 0.96f, 0.12f);
            EnsureTransition(stateMachine, attack2, empty, true, 0.96f, 0.12f);
            EnsureTransition(stateMachine, attack3, empty, true, 0.96f, 0.12f);
            EnsureTransition(stateMachine, hit, empty, true, 0.92f, 0.1f);

            if (!stateMachine.anyStateTransitions.Any(transition => transition.destinationState == hit))
            {
                AnimatorStateTransition hitTransition = stateMachine.AddAnyStateTransition(hit);
                hitTransition.hasExitTime = false;
                hitTransition.duration = 0.08f;
                hitTransition.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter existing = controller.parameters.FirstOrDefault(
                parameter => parameter.name == name);
            if (existing != null)
            {
                if (existing.type != type)
                    throw new InvalidOperationException(
                        $"Animator parameter '{name}' exists as {existing.type}; expected {type}.");
                return;
            }

            controller.AddParameter(name, type);
        }

        private static AnimatorState EnsureState(
            AnimatorStateMachine stateMachine,
            string name,
            Motion motion,
            Vector3 position)
        {
            ChildAnimatorState existing = stateMachine.states.FirstOrDefault(
                childState => childState.state.name == name);
            if (existing.state != null)
            {
                if (existing.state.motion == null && motion != null)
                    existing.state.motion = motion;
                return existing.state;
            }

            AnimatorState state = stateMachine.AddState(name, position);
            state.motion = motion;
            return state;
        }

        private static void EnsureTransition(
            AnimatorStateMachine stateMachine,
            AnimatorState from,
            AnimatorState to,
            bool hasExitTime,
            float exitTime,
            float duration,
            params ConditionSpec[] conditions)
        {
            AnimatorStateTransition existing = from.transitions.FirstOrDefault(
                transition => transition.destinationState == to &&
                              conditions.All(condition => transition.conditions.Any(current =>
                                  current.parameter == condition.Parameter && current.mode == condition.Mode)));

            AnimatorStateTransition created = existing != null ? existing : from.AddTransition(to);
            created.hasExitTime = hasExitTime;
            created.exitTime = exitTime;
            created.duration = duration;
            created.hasFixedDuration = true;
            for (int i = 0; i < conditions.Length; i++)
            {
                bool exists = created.conditions.Any(condition =>
                    condition.parameter == conditions[i].Parameter &&
                    condition.mode == conditions[i].Mode &&
                    Mathf.Approximately(condition.threshold, conditions[i].Threshold));
                if (!exists)
                    created.AddCondition(
                        conditions[i].Mode, conditions[i].Threshold, conditions[i].Parameter);
            }
        }

        private static bool ConfigureCharacter(
            GameObject playerRoot,
            GameObject swordPrefab,
            CombatClips clips,
            SwordAudioMapping audioMapping,
            bool configureBag)
        {
            Animator animator = playerRoot.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogError($"No Animator found under '{playerRoot.name}'.", playerRoot);
                return false;
            }

            Transform backBone = FindNamedTransform(animator.transform,
                "spine_03", "upperchest", "chest", "spine_02", "spine");
            if (backBone == null)
                backBone = animator.GetBoneTransform(HumanBodyBones.Chest) ??
                           animator.GetBoneTransform(HumanBodyBones.UpperChest) ??
                           animator.GetBoneTransform(HumanBodyBones.Spine);

            Transform rightHandBone = FindNamedTransform(animator.transform,
                "hand_r", "right_hand", "righthand", "handright");
            if (rightHandBone == null)
                rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);

            if (backBone == null || rightHandBone == null)
            {
                Debug.LogError(
                    $"Could not locate a chest/spine and right-hand bone below Animator '{animator.name}'.",
                    animator);
                return false;
            }

            Transform backSocket = FindNamedTransform(animator.transform, "WeaponSocket_Back");
            bool createdBackSocket = backSocket == null;
            if (createdBackSocket)
                backSocket = CreateSocket(backBone, "WeaponSocket_Back", Vector3.zero, Vector3.zero);
            else if (backSocket.parent != backBone)
                backSocket.SetParent(backBone, false);

            WeaponHolster holster = playerRoot.GetComponent<WeaponHolster>();
            bool createdHolster = holster == null;
            if (createdHolster)
                holster = playerRoot.AddComponent<WeaponHolster>();
            bool recalibrateWeaponPose = createdBackSocket || !holster.setupPoseConfigured;

            Transform handSocket = FindNamedTransform(animator.transform, "WeaponSocket_RightHand");
            bool createdHandSocket = handSocket == null;
            if (createdHandSocket)
                handSocket = CreateSocket(rightHandBone, "WeaponSocket_RightHand", Vector3.zero, Vector3.zero);
            else if (handSocket.parent != rightHandBone)
                handSocket.SetParent(rightHandBone, true);

            holster.weaponSocketBack = backSocket;
            holster.weaponSocketRightHand = handSocket;

            Transform swordTransform = FindUniqueNamedTransform(playerRoot.transform, "PrinceSword", backSocket,
                handSocket);
            bool createdSword = swordTransform == null;
            if (createdSword)
            {
                GameObject swordObject = UnityEngine.Object.Instantiate(swordPrefab, backSocket, false);
                swordObject.name = "PrinceSword";
                swordTransform = swordObject.transform;
            }
            else if (swordTransform.parent != backSocket && swordTransform.parent != handSocket)
                swordTransform.SetParent(backSocket, false);

            Transform nestedScabbard = FindNamedTransform(swordTransform, "Scabbard");
            Transform scabbardTransform;
            if (nestedScabbard != null)
            {
                scabbardTransform = nestedScabbard;
                scabbardTransform.name = "PrinceScabbard";
                scabbardTransform.SetParent(backSocket, true);
            }
            else
            {
                GameObject sourceSword = UnityEngine.Object.Instantiate(swordPrefab);
                Transform sourceScabbard = FindNamedTransform(sourceSword.transform, "Scabbard");
                if (sourceScabbard == null)
                {
                    UnityEngine.Object.DestroyImmediate(sourceSword);
                    Debug.LogError(
                        $"The sword prefab '{SwordPrefabPath}' has no nested Scabbard child to extract.",
                        playerRoot);
                    return false;
                }

                Vector3 sourceScabbardPosition = sourceScabbard.localPosition;
                Quaternion sourceScabbardRotation = sourceScabbard.localRotation;
                Vector3 sourceScabbardScale = sourceScabbard.localScale;
                GameObject extractedScabbard = UnityEngine.Object.Instantiate(sourceScabbard.gameObject, backSocket, false);
                extractedScabbard.name = "PrinceScabbard";
                extractedScabbard.transform.localPosition = sourceScabbardPosition;
                extractedScabbard.transform.localRotation = sourceScabbardRotation;
                extractedScabbard.transform.localScale = sourceScabbardScale;
                scabbardTransform = extractedScabbard.transform;
                UnityEngine.Object.DestroyImmediate(sourceSword);
            }

            Transform[] existingScabbards = FindAllNamedTransforms(playerRoot.transform, "PrinceScabbard");
            for (int i = 0; i < existingScabbards.Length; i++)
            {
                if (existingScabbards[i] == scabbardTransform)
                    continue;
                UnityEngine.Object.DestroyImmediate(existingScabbards[i].gameObject);
            }
            if (scabbardTransform.parent != backSocket)
                scabbardTransform.SetParent(backSocket, false);

            Vector3 sourcePosition = scabbardTransform.localPosition;
            Quaternion sourceRotation = scabbardTransform.localRotation;
            Vector3 sourceScale = scabbardTransform.localScale;
            GameObject sourcePosePrefab = UnityEngine.Object.Instantiate(swordPrefab);
            Transform sourcePoseScabbard = FindNamedTransform(sourcePosePrefab.transform, "Scabbard");
            if (sourcePoseScabbard == null)
            {
                UnityEngine.Object.DestroyImmediate(sourcePosePrefab);
                Debug.LogError($"Could not read the authored Scabbard pose from '{SwordPrefabPath}'.", playerRoot);
                return false;
            }
            Vector3 authoredScabbardPosition = sourcePoseScabbard.localPosition;
            Quaternion authoredScabbardRotation = sourcePoseScabbard.localRotation;
            Vector3 authoredScabbardScale = sourcePoseScabbard.localScale;
            Bounds authoredWeaponBounds = CalculateLocalBounds(sourcePosePrefab.transform);
            UnityEngine.Object.DestroyImmediate(sourcePosePrefab);

            if (recalibrateWeaponPose)
            {
                holster.backSwordPosition = -authoredWeaponBounds.center;
                holster.backSwordEulerAngles = Vector3.zero;
                holster.swordScale = Vector3.one;
                holster.scabbardPosition = holster.backSwordPosition + authoredScabbardPosition;
                holster.scabbardEulerAngles = authoredScabbardRotation.eulerAngles;
                holster.scabbardScale = authoredScabbardScale;
            }

            Bounds swordBounds = CalculateLocalBounds(swordTransform);
            Bounds scabbardLocalBounds = CalculateLocalBounds(scabbardTransform);
            Debug.Log(
                $"Using authored sword/scabbard prefab alignment: group bounds={authoredWeaponBounds}, " +
                $"sword local position={holster.backSwordPosition}, " +
                $"rotation={Vector3.zero}, scale={holster.swordScale}; scabbard source pose was " +
                $"{sourcePosition}/{sourceRotation.eulerAngles}/{sourceScale}, authored pose is " +
                $"{holster.scabbardPosition}/{authoredScabbardRotation.eulerAngles}/{authoredScabbardScale}; " +
                $"sword bounds={swordBounds.size}, scabbard bounds={scabbardLocalBounds.size}.",
                playerRoot);

            if (recalibrateWeaponPose)
            {
                float backAngle = -Mathf.Clamp(
                    45f - Mathf.Atan2(scabbardLocalBounds.size.x, scabbardLocalBounds.size.y) * Mathf.Rad2Deg,
                    20f,
                    40f);
                Vector3 socketWorldPosition = backBone.position - animator.transform.forward * 0.18f;
                Quaternion socketWorldRotation =
                    animator.transform.rotation * Quaternion.Euler(0f, 0f, backAngle);
                backSocket.localPosition = backBone.InverseTransformPoint(socketWorldPosition);
                backSocket.localRotation = Quaternion.Inverse(backBone.rotation) * socketWorldRotation;
            }

            Vector3 gripPivot = FindGripPivot(swordTransform, swordBounds);

            Debug.Log(
                $"Weapon bounds: sword={swordBounds.size}, scabbard={scabbardLocalBounds.size}, " +
                $"gripPivot={gripPivot}.",
                playerRoot);

            Quaternion handRotation = Quaternion.FromToRotation(GetLongAxis(swordBounds), Vector3.forward);
            if (createdHolster || recalibrateWeaponPose)
            {
                holster.handSwordPosition = handRotation * (-gripPivot * holster.swordScale.x);
                holster.handSwordEulerAngles = handRotation.eulerAngles;
            }

            if (createdSword || swordTransform.parent == backSocket)
            {
                swordTransform.localPosition = holster.backSwordPosition;
                swordTransform.localRotation = Quaternion.Euler(holster.backSwordEulerAngles);
                swordTransform.localScale = holster.swordScale;
                DisableImportedColliders(swordTransform.gameObject);
            }
            if (recalibrateWeaponPose)
                holster.setupPoseConfigured = true;
            scabbardTransform.localPosition = holster.scabbardPosition;
            scabbardTransform.localRotation = Quaternion.Euler(holster.scabbardEulerAngles);
            scabbardTransform.localScale = holster.scabbardScale;
            DisableImportedColliders(scabbardTransform.gameObject);

            holster.sword = swordTransform.gameObject;
            holster.scabbard = scabbardTransform.gameObject;

            Transform hitboxTransform = FindNamedTransform(swordTransform, "PrinceSwordHitbox");
            if (hitboxTransform == null)
            {
                GameObject hitboxObject = new GameObject("PrinceSwordHitbox");
                hitboxTransform = hitboxObject.transform;
                hitboxTransform.SetParent(swordTransform, false);
                hitboxTransform.localPosition = Vector3.zero;
                hitboxTransform.localRotation = Quaternion.identity;
                hitboxTransform.localScale = Vector3.one;
            }

            WeaponHitbox hitbox = hitboxTransform.GetComponent<WeaponHitbox>();
            if (hitbox == null)
                hitbox = hitboxTransform.gameObject.AddComponent<WeaponHitbox>();
            hitbox.ownerRoot = playerRoot.transform;
            BoxCollider trigger = FindOrCreateHitboxCollider(hitboxTransform.gameObject, swordBounds);
            hitbox.hitboxCollider = trigger;
            trigger.enabled = false;
            Rigidbody weaponBody = hitboxTransform.GetComponent<Rigidbody>();
            if (weaponBody == null)
                weaponBody = hitboxTransform.gameObject.AddComponent<Rigidbody>();
            weaponBody.isKinematic = true;
            weaponBody.useGravity = false;
            holster.weaponHitbox = hitbox;

            WeaponAudio weaponAudio = playerRoot.GetComponent<WeaponAudio>();
            if (weaponAudio == null)
                weaponAudio = playerRoot.AddComponent<WeaponAudio>();
            AudioSource audioSource = playerRoot.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = playerRoot.AddComponent<AudioSource>();
            weaponAudio.audioSource = audioSource;
            weaponAudio.drawClips = audioMapping.Draw;
            weaponAudio.sheathClips = audioMapping.Sheath;
            weaponAudio.swingClips = audioMapping.Swing;
            weaponAudio.hitFleshClips = audioMapping.HitFlesh;
            weaponAudio.bearHitClips = audioMapping.BearHit;
            weaponAudio.hitHardClips = audioMapping.HitHard;
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
            holster.weaponAudio = weaponAudio;
            hitbox.weaponAudio = weaponAudio;

            PlayerCombat combat = playerRoot.GetComponent<PlayerCombat>();
            if (combat == null)
                combat = playerRoot.AddComponent<PlayerCombat>();
            combat.animator = animator;
            combat.weaponHolster = holster;
            combat.weaponHitbox = hitbox;
            combat.weaponAudio = weaponAudio;
            combat.movementController = playerRoot.GetComponent<ThirdPersonController>();
            combat.characterController = playerRoot.GetComponent<CharacterController>();
            if (combat.movementController == null)
                Debug.LogWarning(
                    $"No Starter Assets {nameof(ThirdPersonController)} found on '{playerRoot.name}'; " +
                    "combat movement locking will require an alternate controller integration.",
                    playerRoot);
            if (combat.characterController == null)
                Debug.LogWarning(
                    $"No {nameof(CharacterController)} found on '{playerRoot.name}'; aerial attack prevention " +
                    "will use the Starter Assets grounded flag.",
                    playerRoot);
            combat.attackClips = new[] { clips.Attack1, clips.Attack2, clips.Attack3 };
            combat.drawClip = clips.Draw;
            combat.sheathClip = clips.Sheath;
            combat.attackCooldown = 0.12f;
            combat.comboInputBuffer = 0.85f;
            combat.comboTransitionNormalizedTime = 0.58f;
            combat.drawAnimationSpeed = 1.35f;
            combat.sheathAnimationSpeed = 1.35f;
            combat.attackTransitionDuration = 0.07f;
            combat.fallbackHitStart = 0.18f;
            combat.fallbackHitEnd = 0.82f;
            PlayerHealth health = playerRoot.GetComponent<PlayerHealth>();
            if (health == null)
                health = playerRoot.AddComponent<PlayerHealth>();
            health.animator = animator;
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(PrinceControllerPath);
            animator.applyRootMotion = false;

            CombatAnimationEventRelay relay = animator.GetComponent<CombatAnimationEventRelay>();
            if (relay == null)
                relay = animator.gameObject.AddComponent<CombatAnimationEventRelay>();
            relay.weaponHolster = holster;
            relay.weaponHitbox = hitbox;
            relay.weaponAudio = weaponAudio;

            if (configureBag)
            {
                CharacterOutfit outfit = playerRoot.GetComponentInChildren<CharacterOutfit>(true);
                if (outfit != null && outfit.Bags)
                {
                    outfit.Bags = false;
                    if (outfit.BagsObject != null)
                        outfit.BagsObject.SetActive(false);
                    outfit.Apply();
                    Debug.Log("CharacterOutfit.Bags disabled to prevent back-weapon overlap.", outfit);
                }
            }

            if (PrefabUtility.IsPartOfPrefabInstance(playerRoot))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(holster);
                PrefabUtility.RecordPrefabInstancePropertyModifications(hitbox);
                PrefabUtility.RecordPrefabInstancePropertyModifications(combat);
                PrefabUtility.RecordPrefabInstancePropertyModifications(weaponAudio);
                PrefabUtility.RecordPrefabInstancePropertyModifications(audioSource);
                PrefabUtility.RecordPrefabInstancePropertyModifications(relay);
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            }

            Debug.Log(
                $"Sword asset='{SwordPrefabPath}' as '{swordTransform.name}', scabbard extracted from sword prefab " +
                $"as '{scabbardTransform.name}'. Weapon sockets: {backSocket.name} under {backBone.name} (local {backSocket.localPosition}, " +
                $"Euler {backSocket.localEulerAngles}); {handSocket.name} under {rightHandBone.name} " +
                $"(local {handSocket.localPosition}, Euler {handSocket.localEulerAngles}). " +
                $"Offsets: sword-back {holster.backSwordPosition}/{holster.backSwordEulerAngles}, " +
                $"sword-hand {holster.handSwordPosition}/{holster.handSwordEulerAngles}, " +
                $"scabbard {holster.scabbardPosition}/{holster.scabbardEulerAngles} " +
                $"scale {holster.scabbardScale}; sword scale {holster.swordScale}; " +
                $"sword parent '{swordTransform.parent.name}', world position {swordTransform.position}. " +
                $"Clips: Attack1={clips.Attack1.name}, Attack2={clips.Attack2.name}, " +
                $"Attack3={clips.Attack3.name}, Hit={clips.Hit.name}.",
                playerRoot);
            return true;
        }

        private static void UpdateActiveSceneInstances(
            GameObject swordPrefab,
            CombatClips clips,
            SwordAudioMapping audioMapping)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            bool changed = false;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Animator[] animators = roots[i].GetComponentsInChildren<Animator>(true);
                for (int j = 0; j < animators.Length; j++)
                {
                    Transform player = animators[j].transform;
                    while (player != null && player.name != "PlayerPrince")
                        player = player.parent;
                    if (player == null)
                        continue;

                    if (!ConfigureCharacter(player.gameObject, swordPrefab, clips, audioMapping, false))
                        continue;
                    EditorUtility.SetDirty(player.gameObject);
                    changed = true;
                }
            }

            if (changed)
                EditorSceneManager.MarkSceneDirty(scene);
        }

        private static Transform CreateSocket(
            Transform bone,
            string socketName,
            Vector3 localPosition,
            Vector3 localEulerAngles)
        {
            GameObject socket = new GameObject(socketName);
            socket.transform.SetParent(bone, false);
            socket.transform.localPosition = localPosition;
            socket.transform.localRotation = Quaternion.Euler(localEulerAngles);
            socket.transform.localScale = Vector3.one;
            return socket.transform;
        }

        private static Transform FindNamedTransform(Transform root, params string[] names)
        {
            if (root == null)
                return null;

            string[] normalizedNames = names.Select(NormalizeBoneName).ToArray();
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                string candidate = NormalizeBoneName(transforms[i].name);
                if (normalizedNames.Contains(candidate))
                    return transforms[i];
            }
            return null;
        }

        private static Transform FindUniqueNamedTransform(
            Transform root,
            string objectName,
            params Transform[] preferredParents)
        {
            Transform[] matches = FindAllNamedTransforms(root, objectName);
            if (matches.Length == 0)
                return null;

            Transform keep = matches.FirstOrDefault(candidate =>
                preferredParents.Any(parent => candidate.parent == parent));
            if (keep == null)
                keep = matches[0];

            for (int i = 0; i < matches.Length; i++)
            {
                if (matches[i] != keep)
                    UnityEngine.Object.DestroyImmediate(matches[i].gameObject);
            }
            return keep;
        }

        private static Transform[] FindAllNamedTransforms(Transform root, string objectName)
        {
            if (root == null)
                return Array.Empty<Transform>();

            string normalizedName = NormalizeBoneName(objectName);
            return root.GetComponentsInChildren<Transform>(true)
                .Where(candidate => NormalizeBoneName(candidate.name) == normalizedName)
                .ToArray();
        }

        private static string NormalizeBoneName(string value)
        {
            return value.Replace("_", "").Replace(" ", "").ToLowerInvariant();
        }

        private static void DisableImportedColliders(GameObject sword)
        {
            Collider[] colliders = sword.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
        }

        private static Bounds CalculateLocalBounds(Transform root)
        {
            return CalculateBoundsInSpace(root, root);
        }

        private static Bounds CalculateBoundsInSpace(Transform sourceRoot, Transform targetSpace)
        {
            Renderer[] renderers = sourceRoot.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds result = new Bounds(Vector3.zero, Vector3.zero);
            for (int i = 0; i < renderers.Length; i++)
            {
                Bounds rendererBounds;
                if (renderers[i] is SkinnedMeshRenderer skinnedRenderer)
                {
                    rendererBounds = skinnedRenderer.localBounds;
                }
                else
                {
                    MeshFilter meshFilter = renderers[i].GetComponent<MeshFilter>();
                    if (meshFilter == null || meshFilter.sharedMesh == null)
                        continue;
                    rendererBounds = meshFilter.sharedMesh.bounds;
                }

                Vector3 min = rendererBounds.min;
                Vector3 max = rendererBounds.max;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 rendererLocalPoint = new Vector3(
                        (corner & 1) == 0 ? min.x : max.x,
                        (corner & 2) == 0 ? min.y : max.y,
                        (corner & 4) == 0 ? min.z : max.z);
                    Vector3 worldPoint = renderers[i].transform.TransformPoint(rendererLocalPoint);
                    Vector3 localPoint = targetSpace.InverseTransformPoint(worldPoint);
                    if (!hasBounds)
                    {
                        result = new Bounds(localPoint, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        result.Encapsulate(localPoint);
                    }
                }
            }

            if (!hasBounds)
                throw new InvalidOperationException(
                    $"No renderers found under '{sourceRoot.name}' to measure bounds.");
            return result;
        }

        private static Vector3 FindGripPivot(Transform sword, Bounds bounds)
        {
            CapsuleCollider[] capsules = sword.GetComponentsInChildren<CapsuleCollider>(true);
            for (int i = 0; i < capsules.Length; i++)
            {
                CapsuleCollider candidate = capsules[i];
                if (candidate.direction != 1 || candidate.height <= candidate.radius * 4f)
                    continue;
                return sword.InverseTransformPoint(candidate.transform.TransformPoint(candidate.center));
            }

            return new Vector3(
                bounds.center.x,
                bounds.min.y + bounds.size.y * 0.12f,
                bounds.center.z);
        }

        private static Vector3 GetLongAxis(Bounds bounds)
        {
            if (bounds.size.y >= bounds.size.x && bounds.size.y >= bounds.size.z)
                return Vector3.up;
            return bounds.size.x >= bounds.size.z ? Vector3.right : Vector3.forward;
        }

        private static BoxCollider FindOrCreateHitboxCollider(GameObject hitboxObject, Bounds swordBounds)
        {
            BoxCollider[] existingColliders = hitboxObject.GetComponents<BoxCollider>();
            BoxCollider hitbox = existingColliders.FirstOrDefault(collider => collider.isTrigger);
            if (hitbox != null)
                return hitbox;

            hitbox = hitboxObject.AddComponent<BoxCollider>();
            hitbox.isTrigger = true;
            hitbox.center = new Vector3(
                swordBounds.center.x,
                swordBounds.min.y + swordBounds.size.y * 0.67f,
                swordBounds.center.z);
            hitbox.size = new Vector3(
                Mathf.Max(0.05f, swordBounds.size.x * 1.5f),
                Mathf.Max(0.2f, swordBounds.size.y * 0.7f),
                Mathf.Max(0.05f, swordBounds.size.z * 1.5f));
            return hitbox;
        }
    }
}
