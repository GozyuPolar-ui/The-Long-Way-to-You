using System;
using System.IO;
using Prince;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Prince.Editor
{
    internal static class PrinceCombatFeelSetup
    {
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/PlayerPrince.prefab";
        private const string BearPrefabPath = "Assets/_Project/Prefabs/Bear_Enemy.prefab";
        private const string SettingsPath = "Assets/_Project/Data/CombatFeel/CombatFeelSettings.asset";
        private const string MaterialPath = "Assets/_Project/Materials/CombatFeelParticles.mat";
        private const string BackupFolder = "Assets/_Project/Docs/CombatBackup~";

        [MenuItem("Tools/Prince/Setup Combat Feel")]
        private static void SetupCombatFeel()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Setup Combat Feel cannot run in Play mode.");
                return;
            }

            if (!ValidatePrefab<PlayerCombat>(PlayerPrefabPath) ||
                !ValidatePrefab<BearAI>(BearPrefabPath))
                return;

            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/CombatFeel");
            EnsureFolder("Assets/_Project/Materials");
            Directory.CreateDirectory(BackupFolder);

            if (!CreateBackupIfMissing(PlayerPrefabPath) ||
                !CreateBackupIfMissing(BearPrefabPath))
                return;

            Material particlesMaterial = LoadOrCreateParticleMaterial();
            if (particlesMaterial == null)
                return;

            CombatFeelSettings settings =
                AssetDatabase.LoadAssetAtPath<CombatFeelSettings>(SettingsPath);
            bool createdSettings = settings == null;
            if (!createdSettings && settings.impactParticleMaterial == null &&
                !CreateBackupIfMissing(SettingsPath))
                return;

            if (createdSettings)
            {
                settings = ScriptableObject.CreateInstance<CombatFeelSettings>();
                settings.ApplyPreset(CombatFeelPreset.Lenient);
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            if (settings.impactParticleMaterial == null)
            {
                settings.impactParticleMaterial = particlesMaterial;
                EditorUtility.SetDirty(settings);
            }

            if (!ConfigurePlayer(settings, particlesMaterial) ||
                !ConfigureBear(settings))
                return;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Combat Feel setup complete. CombatFeelSettings uses the Lenient preset by default; " +
                "player and bear prefabs were updated without changing scenes or Animator Controllers.");
        }

        private static bool ValidatePrefab<T>(string path) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.GetComponentInChildren<T>(true) == null)
            {
                Debug.LogError($"Prefab '{path}' is missing or has no {typeof(T).Name} component.");
                return false;
            }

            return true;
        }

        private static bool ConfigurePlayer(
            CombatFeelSettings settings,
            Material particlesMaterial)
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Could not load player prefab '{PlayerPrefabPath}'.");
                return false;
            }

            try
            {
                PlayerCombat combat = prefab.GetComponentInChildren<PlayerCombat>(true);
                if (combat == null)
                {
                    Debug.LogError($"Player prefab '{PlayerPrefabPath}' lost its PlayerCombat component.");
                    return false;
                }

                GameObject actor = combat.gameObject;
                Animator animator = combat.animator != null
                    ? combat.animator
                    : actor.GetComponentInChildren<Animator>(true);
                CharacterController characterController = combat.characterController != null
                    ? combat.characterController
                    : actor.GetComponent<CharacterController>();
                WeaponHolster holster = combat.weaponHolster;
                if (animator == null || characterController == null || holster == null ||
                    holster.sword == null)
                {
                    Debug.LogError(
                        "Player combat prefab references are incomplete; Combat Feel setup left the prefab unchanged.",
                        combat);
                    return false;
                }

                AttackBuffer attackBuffer = GetOrAddComponent<AttackBuffer>(actor);
                HitStop hitStop = GetOrAddComponent<HitStop>(actor);
                AttackLunge lunge = GetOrAddComponent<AttackLunge>(actor);
                CameraImpulse cameraImpulse = GetOrAddComponent<CameraImpulse>(actor);
                CinemachineImpulseSource impulseSource =
                    GetOrAddComponent<CinemachineImpulseSource>(actor);
                CombatFeel feel = GetOrAddComponent<CombatFeel>(actor);

                TrailRenderer trailRenderer = EnsureSwordTrail(holster.sword, particlesMaterial, settings);
                SwordTrail swordTrail = GetOrAddComponent<SwordTrail>(holster.sword);

                if (!SetReference(hitStop, "targetAnimator", animator) ||
                    !SetReference(hitStop, "settings", settings) ||
                    !SetReference(lunge, "settings", settings) ||
                    !SetReference(lunge, "characterController", characterController) ||
                    !SetFloat(lunge, "groundProbeDistance", settings.lungeGroundCheckDistance) ||
                    !SetReference(cameraImpulse, "settings", settings) ||
                    !SetReference(cameraImpulse, "impulseSource", impulseSource) ||
                    !SetReference(swordTrail, "settings", settings) ||
                    !SetReference(swordTrail, "trail", trailRenderer) ||
                    !SetReference(feel, "settings", settings) ||
                    !SetReference(feel, "playerCombat", combat) ||
                    !SetReference(feel, "animator", animator) ||
                    !SetReference(feel, "characterController", characterController) ||
                    !SetBoolean(feel, "enemyMovementDust", false) ||
                    !SetReference(feel, "attackLunge", lunge) ||
                    !SetReference(feel, "hitStop", hitStop) ||
                    !SetReference(feel, "cameraImpulse", cameraImpulse) ||
                    !SetReference(feel, "swordTrail", swordTrail))
                    return false;

                return SavePrefab(prefab, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static bool ConfigureBear(CombatFeelSettings settings)
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(BearPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Could not load bear prefab '{BearPrefabPath}'.");
                return false;
            }

            try
            {
                BearAI bear = prefab.GetComponentInChildren<BearAI>(true);
                if (bear == null)
                {
                    Debug.LogError($"Bear prefab '{BearPrefabPath}' lost its BearAI component.");
                    return false;
                }

                GameObject actor = bear.gameObject;
                Animator animator = actor.GetComponentInChildren<Animator>(true);
                if (animator == null)
                {
                    Debug.LogError($"Bear prefab '{BearPrefabPath}' has no Animator.", bear);
                    return false;
                }

                HitStop hitStop = GetOrAddComponent<HitStop>(actor);
                EnemyPoise poise = GetOrAddComponent<EnemyPoise>(actor);
                HitFlash hitFlash = GetOrAddComponent<HitFlash>(actor);
                CombatFeel feel = GetOrAddComponent<CombatFeel>(actor);

                if (!SetReference(hitStop, "targetAnimator", animator) ||
                    !SetReference(hitStop, "settings", settings) ||
                    !SetReference(poise, "settings", settings) ||
                    !SetReference(poise, "animator", animator) ||
                    !SetReference(poise, "characterController", actor.GetComponent<CharacterController>()) ||
                    !SetReference(poise, "agent", actor.GetComponent<NavMeshAgent>()) ||
                    !SetReference(hitFlash, "settings", settings) ||
                    !SetReference(feel, "settings", settings) ||
                    !SetReference(feel, "animator", animator) ||
                    !SetReference(feel, "hitStop", hitStop) ||
                    !SetBoolean(feel, "enemyMovementDust", true))
                    return false;

                return SavePrefab(prefab, BearPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static TrailRenderer EnsureSwordTrail(
            GameObject sword,
            Material material,
            CombatFeelSettings settings)
        {
            Transform tip = sword.transform.Find("CombatFeelTrailTip");
            if (tip == null)
            {
                GameObject tipObject = new GameObject("CombatFeelTrailTip");
                tip = tipObject.transform;
                tip.SetParent(sword.transform, false);

                BoxCollider bladeCollider = sword.GetComponent<BoxCollider>();
                tip.localPosition = bladeCollider != null
                    ? bladeCollider.center + Vector3.up * (bladeCollider.size.y * 0.5f)
                    : Vector3.up;
            }

            TrailRenderer trail = tip.GetComponent<TrailRenderer>();
            if (trail == null)
                trail = tip.gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.22f;
            trail.startWidth = 0.075f;
            trail.endWidth = 0.012f;
            trail.minVertexDistance = 0.035f;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.material = material;
            trail.startColor = settings.trailColor;
            Color endColor = settings.trailColor;
            endColor.a = 0f;
            trail.endColor = endColor;
            trail.emitting = false;
            trail.enabled = false;
            return trail;
        }

        private static Material LoadOrCreateParticleMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
                return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                Debug.LogError(
                    "Could not find 'Universal Render Pipeline/Particles/Unlit'; no combat feel assets were saved.");
                return null;
            }

            material = new Material(shader)
            {
                name = "CombatFeelParticles"
            };
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", new Color(1f, 0.72f, 0.34f, 1f));
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", new Color(1f, 0.72f, 0.34f, 1f));
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static bool SavePrefab(GameObject prefab, string path)
        {
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefab, path, out bool success);
            if (!success || savedPrefab == null)
            {
                Debug.LogError($"Could not save configured prefab '{path}'.");
                return false;
            }

            return true;
        }

        private static bool CreateBackupIfMissing(string assetPath)
        {
            string source = Path.GetFullPath(assetPath);
            string backup = Path.Combine(
                Path.GetFullPath(BackupFolder),
                Path.GetFileName(assetPath) + ".bak");
            if (!File.Exists(source))
            {
                Debug.LogError($"Cannot back up missing asset '{assetPath}'.");
                return false;
            }
            if (File.Exists(backup))
                return true;

            try
            {
                File.Copy(source, backup, false);
                AssetDatabase.Refresh();
                Debug.Log($"Created pre-change backup at '{backup}'.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not create backup for '{assetPath}': {exception.Message}");
                return false;
            }
        }

        private static bool SetReference(
            Component component,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError(
                    $"Serialized field '{propertyName}' was not found on {component.GetType().Name}.",
                    component);
                return false;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(component);
            return true;
        }

        private static bool SetBoolean(Component component, string propertyName, bool value)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.Boolean)
            {
                Debug.LogError(
                    $"Serialized boolean '{propertyName}' was not found on {component.GetType().Name}.",
                    component);
                return false;
            }

            property.boolValue = value;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(component);
            return true;
        }

        private static bool SetFloat(Component component, string propertyName, float value)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.Float)
            {
                Debug.LogError(
                    $"Serialized float '{propertyName}' was not found on {component.GetType().Name}.",
                    component);
                return false;
            }

            property.floatValue = value;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(component);
            return true;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
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
