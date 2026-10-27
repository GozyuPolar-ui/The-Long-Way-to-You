using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Prince;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prince.Editor
{
    internal static class PrinceNPCSetup
    {
        private const string CharacterPrefabPath = "Assets/_Project/Prefabs/PrinceCharacter.prefab";
        private const string AvatarModelPath = "Assets/Fantasy knight/Base mesh/Skin_1.fbx";
        private const string Body02PrefabPath = "Assets/Fantasy knight/Prefab/Separated_mesh/Body_02.prefab";
        private const string RoleFolder = "Assets/_Project/Data/NPCRoles";
        private const string PrefabFolder = "Assets/_Project/Prefabs/NPC";
        private const string MaterialFolder = "Assets/_Project/Materials/NPC";
        private const string ControllerFolder = "Assets/_Project/Animation";
        private const string ControllerPath = ControllerFolder + "/NPC.controller";
        private const string TestRowName = "NPC_Test_Row";
        private const string IdleStarterPath =
            "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Animations/Stand--Idle.anim.fbx";
        private const string IdleDoubleLPath =
            "Assets/DoubleL/FBX Unity/One Hand Up/Movement/Idle/Idle/1Hand_Up_Stand_Idle_A_2.fbx";
        private const int DefaultVariantCount = 3;
        private static readonly string[] DialoguePaths =
        {
            "Assets/DoubleL/FBX Unity/NPC/Dialogue/Dialogue_1.fbx",
            "Assets/DoubleL/FBX Unity/NPC/Dialogue/Dialogue_2.fbx",
            "Assets/DoubleL/FBX Unity/NPC/Dialogue/Dialogue_3.fbx",
            "Assets/DoubleL/FBX Unity/NPC/Dialogue/Dialogue_4.fbx",
            "Assets/DoubleL/FBX Unity/NPC/Dialogue/Dialogue_5.fbx"
        };

        private sealed class ClipSet
        {
            public AnimationClip[] Idle = Array.Empty<AnimationClip>();
            public AnimationClip[] Dialogue = Array.Empty<AnimationClip>();
        }

        private sealed class GeneratedController
        {
            public AnimatorController Controller;
            public Dictionary<AnimationClip, string> IdleStateNames;
            public Dictionary<AnimationClip, string> DialogueStateNames;
        }

        [MenuItem("Tools/Prince/Create NPC Prefabs")]
        private static void CreateNPCPrefabsMenu()
        {
            GenerateNPCPrefabs(EditorPrefs.GetInt("Prince.NPC.VariantCount", DefaultVariantCount));
        }

        [MenuItem("Tools/Prince/NPC Generator Settings")]
        private static void OpenGeneratorWindow()
        {
            NPCGeneratorWindow.ShowWindow();
        }

        [MenuItem("Tools/Prince/Spawn Test NPCs In Scene")]
        private static void SpawnTestNPCsMenu()
        {
            SpawnTestNPCs();
        }

        [MenuItem("Tools/Prince/Clear Test NPCs")]
        private static void ClearTestNPCsMenu()
        {
            ClearTestNPCs();
        }

        internal static void GenerateNPCPrefabs(int variantCount)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Create NPC Prefabs cannot run in Play mode.");
                return;
            }

            GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
            if (characterPrefab == null)
            {
                Debug.LogError("NPC source prefab is missing: " + CharacterPrefabPath);
                return;
            }

            CharacterOutfit sourceOutfit = characterPrefab.GetComponent<CharacterOutfit>();
            if (sourceOutfit == null)
            {
                Debug.LogError("NPC source prefab has no CharacterOutfit component: " + CharacterPrefabPath);
                return;
            }

            if (!TryFindHumanoidAvatar(out Avatar avatar))
                return;

            EnsureFolder(RoleFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(ControllerFolder);

            ClipSet clips = DiscoverClips();
            if (clips.Idle.Length == 0)
            {
                Debug.LogError("No usable Humanoid idle clips were found. NPC prefabs were not generated.");
                return;
            }

            if (clips.Dialogue.Length == 0)
                Debug.LogWarning("No imported AnimationClip subassets were found in the NPC Dialogue FBX files. NPCs will use idle only.");

            List<NPCRoleDefinition> roles = CreateOrLoadRoles(clips);
            if (roles.Count == 0)
            {
                Debug.LogError("No NPC role definitions are available.");
                return;
            }

            GeneratedController generatedController = CreateController(roles);
            if (generatedController == null)
                return;

            int generated = 0;
            foreach (NPCRoleDefinition role in roles)
            {
                if (role == null)
                    continue;

                for (int variant = 1; variant <= Mathf.Max(1, variantCount); variant++)
                {
                    if (CreateNPCPrefab(avatar, role, variant, generatedController))
                        generated++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created or updated {generated} NPC prefabs across {roles.Count} roles in '{PrefabFolder}'.");
        }

        private static List<NPCRoleDefinition> CreateOrLoadRoles(ClipSet clips)
        {
            EnsureFolder(RoleFolder);
            var result = new List<NPCRoleDefinition>();
            foreach (RoleDefaults defaults in GetRoleDefaults())
            {
                string path = RoleFolder + "/NPC_" + defaults.Name + ".asset";
                NPCRoleDefinition role = AssetDatabase.LoadAssetAtPath<NPCRoleDefinition>(path);
                if (role == null)
                {
                    role = ScriptableObject.CreateInstance<NPCRoleDefinition>();
                    defaults.Apply(role);
                    AssetDatabase.CreateAsset(role, path);
                }

                if (string.IsNullOrWhiteSpace(role.roleName))
                    role.roleName = defaults.Name;
                if (role.idleClips == null || role.idleClips.Length == 0)
                    role.idleClips = clips.Idle;
                if (role.dialogueClips == null || role.dialogueClips.Length == 0)
                    role.dialogueClips = clips.Dialogue;
                EditorUtility.SetDirty(role);
                result.Add(role);
            }

            return result;
        }

        private static bool CreateNPCPrefab(
            Avatar avatar,
            NPCRoleDefinition role,
            int variant,
            GeneratedController generatedController)
        {
            string path = PrefabFolder + "/NPC_" + Sanitize(role.roleName) + "_" + variant.ToString("00") + ".prefab";
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(CharacterPrefabPath);
                if (root == null)
                {
                    Debug.LogError("Could not load NPC source prefab contents: " + CharacterPrefabPath);
                    return false;
                }

                CharacterOutfit outfit = root.GetComponent<CharacterOutfit>();
                if (outfit == null)
                {
                    Debug.LogError("CharacterOutfit is missing from loaded NPC source prefab: " + CharacterPrefabPath);
                    return false;
                }

                int seed = StableSeed(role.roleName, variant);
                var random = new System.Random(seed);
                HashSet<NPCOutfitPiece> selected = SelectPieces(role, random);
                ApplyPieces(outfit, selected, role, random);
                if (!ApplyBodyVariant(outfit, role, random))
                    return false;

                Color skin = ChooseColor(role.skinPalette, role.roleName, variant, Color.white);
                Color clothing = ChooseColor(role.clothingPalette, role.roleName, variant, Color.white);
                Color hair = ChooseColor(role.hairPalette, role.roleName, variant, Color.white);
                ApplyTint(outfit.BodyObject, skin);
                ApplyTint(outfit.HeadObject, skin);
                if (selected.Contains(NPCOutfitPiece.Hair))
                    ApplyTint(outfit.HairObject, hair);

                foreach (NPCOutfitPiece piece in selected)
                {
                    if (piece == NPCOutfitPiece.Hair)
                        continue;
                    GameObject pieceObject = GetPieceObject(outfit, piece);
                    if (pieceObject != null)
                        ApplyTint(pieceObject, clothing);
                }

                outfit.Apply();
                float scaleMin = Mathf.Max(0.1f, Mathf.Min(role.scaleRange.x, role.scaleRange.y));
                float scaleMax = Mathf.Max(scaleMin, Mathf.Max(role.scaleRange.x, role.scaleRange.y));
                float scale = Mathf.Lerp(scaleMin, scaleMax, (float)random.NextDouble());
                root.transform.localScale = Vector3.one * scale;

                Animator animator = root.GetComponent<Animator>();
                if (animator == null)
                    animator = root.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = generatedController.Controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                NPCInfo info = root.GetComponent<NPCInfo>();
                if (info == null)
                    info = root.AddComponent<NPCInfo>();
                info.displayName = role.roleName + " " + variant.ToString("00");
                info.role = role.roleName;
                if (info.dialogueId == null)
                    info.dialogueId = string.Empty;

                NPCIdleAnimator idleAnimator = root.GetComponent<NPCIdleAnimator>();
                if (idleAnimator == null)
                    idleAnimator = root.AddComponent<NPCIdleAnimator>();
                ConfigureIdleAnimator(idleAnimator, animator, role, generatedController);

                NPCLookAtPlayer lookAt = root.GetComponent<NPCLookAtPlayer>();
                if (lookAt == null)
                    lookAt = root.AddComponent<NPCLookAtPlayer>();
                SerializedObject lookAtSerialized = new SerializedObject(lookAt);
                lookAtSerialized.FindProperty("lookAtPlayer").boolValue = role.lookAtPlayer;
                lookAtSerialized.ApplyModifiedPropertiesWithoutUndo();

                ConfigureColliders(root);
                EditorUtility.SetDirty(root);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                if (!success || saved == null)
                {
                    Debug.LogError($"Failed to save NPC prefab '{path}'.");
                    return false;
                }

                return true;
            }
            finally
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static HashSet<NPCOutfitPiece> SelectPieces(NPCRoleDefinition role, System.Random random)
        {
            var selected = new HashSet<NPCOutfitPiece>();
            if (role.requiredPieces != null)
                foreach (NPCOutfitPiece piece in role.requiredPieces)
                    selected.Add(piece);

            if (role.topChoices != null && role.topChoices.Length > 0)
                selected.Add(role.topChoices[random.Next(role.topChoices.Length)]);
            if (role.bottomChoices != null && role.bottomChoices.Length > 0)
                selected.Add(role.bottomChoices[random.Next(role.bottomChoices.Length)]);

            if (role.optionalPieces != null)
                foreach (NPCOptionalOutfitPiece optional in role.optionalPieces)
                    if (random.NextDouble() < Mathf.Clamp01(optional.chance))
                        selected.Add(optional.piece);

            return selected;
        }

        private static void ApplyPieces(
            CharacterOutfit outfit,
            HashSet<NPCOutfitPiece> selected,
            NPCRoleDefinition role,
            System.Random random)
        {
            outfit.Body = true;
            outfit.Head = true;
            outfit.Hair = selected.Contains(NPCOutfitPiece.Hair);
            outfit.Torso = selected.Contains(NPCOutfitPiece.Torso);
            outfit.Tunic = selected.Contains(NPCOutfitPiece.Tunic);
            outfit.Pants = selected.Contains(NPCOutfitPiece.Pants);
            outfit.Shorts = selected.Contains(NPCOutfitPiece.Shorts);
            outfit.Boots = selected.Contains(NPCOutfitPiece.Boots);
            outfit.Gloves = selected.Contains(NPCOutfitPiece.Gloves);
            outfit.Bracers = selected.Contains(NPCOutfitPiece.Bracers);
            outfit.Helmet = selected.Contains(NPCOutfitPiece.Helmet);
            outfit.Bags = selected.Contains(NPCOutfitPiece.Bags);

            if (outfit.Torso && outfit.Tunic)
            {
                NPCOutfitPiece[] tops = role.topChoices != null && role.topChoices.Length > 0
                    ? role.topChoices
                    : new[] { NPCOutfitPiece.Torso, NPCOutfitPiece.Tunic };
                NPCOutfitPiece keep = tops[random.Next(tops.Length)];
                outfit.Torso = keep == NPCOutfitPiece.Torso;
                outfit.Tunic = keep == NPCOutfitPiece.Tunic;
            }
        }

        private static bool ApplyBodyVariant(
            CharacterOutfit outfit,
            NPCRoleDefinition role,
            System.Random random)
        {
            if (role.bodyChoices == null || role.bodyChoices.Length == 0)
                return true;

            NPCBodyVariant variant = role.bodyChoices[random.Next(role.bodyChoices.Length)];
            if (variant == NPCBodyVariant.Body01)
                return true;

            GameObject sourceBodyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Body02PrefabPath);
            if (sourceBodyPrefab == null)
            {
                Debug.LogWarning($"Body_02 was selected for '{role.roleName}', but its prefab is missing: {Body02PrefabPath}. Body_01 will be retained.");
                return true;
            }

            SkinnedMeshRenderer sourceRenderer = sourceBodyPrefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            SkinnedMeshRenderer destinationRenderer = outfit.BodyObject != null
                ? outfit.BodyObject.GetComponentInChildren<SkinnedMeshRenderer>(true)
                : null;
            if (sourceRenderer == null || destinationRenderer == null || sourceRenderer.sharedMesh == null)
            {
                Debug.LogWarning($"Could not map Body_02 onto the character skeleton for '{role.roleName}'. Body_01 will be retained.");
                return true;
            }

            Transform[] sourceBones = sourceRenderer.bones;
            if (sourceRenderer.sharedMesh.bindposes.Length != sourceBones.Length)
            {
                Debug.LogWarning($"Body_02 bindpose count does not match the character skeleton for '{role.roleName}'. Body_01 will be retained.");
                return true;
            }

            var targetBonesByName = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (Transform targetBone in destinationRenderer.bones)
            {
                if (targetBone != null && !targetBonesByName.ContainsKey(targetBone.name))
                    targetBonesByName.Add(targetBone.name, targetBone);
            }

            var mappedBones = new Transform[sourceBones.Length];
            for (int i = 0; i < sourceBones.Length; i++)
            {
                if (sourceBones[i] == null ||
                    !targetBonesByName.TryGetValue(sourceBones[i].name, out mappedBones[i]))
                {
                    Debug.LogWarning($"Body_02 bone mapping does not match the character skeleton for '{role.roleName}'. Body_01 will be retained.");
                    return true;
                }
            }

            destinationRenderer.sharedMesh = sourceRenderer.sharedMesh;
            destinationRenderer.bones = mappedBones;
            if (sourceRenderer.rootBone != null &&
                targetBonesByName.TryGetValue(sourceRenderer.rootBone.name, out Transform mappedRoot))
                destinationRenderer.rootBone = mappedRoot;
            if (sourceRenderer.sharedMaterials.Length > 0)
                destinationRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
            return true;
        }

        private static GameObject GetPieceObject(CharacterOutfit outfit, NPCOutfitPiece piece)
        {
            switch (piece)
            {
                case NPCOutfitPiece.Hair: return outfit.HairObject;
                case NPCOutfitPiece.Torso: return outfit.TorsoObject;
                case NPCOutfitPiece.Tunic: return outfit.TunicObject;
                case NPCOutfitPiece.Pants: return outfit.PantsObject;
                case NPCOutfitPiece.Shorts: return outfit.ShortsObject;
                case NPCOutfitPiece.Boots: return outfit.BootsObject;
                case NPCOutfitPiece.Gloves: return outfit.GlovesObject;
                case NPCOutfitPiece.Bracers: return outfit.BracersObject;
                case NPCOutfitPiece.Helmet: return outfit.HelmetObject;
                case NPCOutfitPiece.Bags: return outfit.BagsObject;
                default: return null;
            }
        }

        private static void ApplyTint(GameObject piece, Color tint)
        {
            if (piece == null)
                return;

            Renderer[] renderers = piece.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] original = renderer.sharedMaterials;
                var copies = new Material[original.Length];
                for (int i = 0; i < original.Length; i++)
                {
                    if (original[i] == null)
                        continue;
                    copies[i] = GetOrCreateTintedMaterial(original[i], tint);
                }
                renderer.sharedMaterials = copies;
            }
        }

        private static Material GetOrCreateTintedMaterial(Material source, Color tint)
        {
            string sourceGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            string colorKey = ColorUtility.ToHtmlStringRGBA(tint);
            string hash = Hash128.Compute(sourceGuid + ":" + colorKey).ToString();
            string path = MaterialFolder + "/NPC_" + Sanitize(source.name) + "_" + hash + ".mat";
            Material result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null)
            {
                result = new Material(source);
                result.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(result, path);
            }
            else
                EditorUtility.CopySerialized(source, result);

            result.name = Path.GetFileNameWithoutExtension(path);
            if (result.HasProperty("_BaseColor"))
                result.SetColor("_BaseColor", tint);
            if (result.HasProperty("_Color"))
                result.SetColor("_Color", tint);
            EditorUtility.SetDirty(result);
            return result;
        }

        private static void ConfigureIdleAnimator(
            NPCIdleAnimator component,
            Animator animator,
            NPCRoleDefinition role,
            GeneratedController controller)
        {
            SerializedObject serialized = new SerializedObject(component);
            serialized.FindProperty("animator").objectReferenceValue = animator;
            AnimationClip[] idleClips = MapClips(role.idleClips, controller.IdleStateNames, out string[] idleStateNames);
            serialized.FindProperty("idleClips").arraySize = idleClips.Length;
            for (int i = 0; i < idleClips.Length; i++)
                serialized.FindProperty("idleClips").GetArrayElementAtIndex(i).objectReferenceValue = idleClips[i];
            serialized.FindProperty("idleStateNames").arraySize = idleStateNames.Length;
            for (int i = 0; i < idleStateNames.Length; i++)
                serialized.FindProperty("idleStateNames").GetArrayElementAtIndex(i).stringValue = idleStateNames[i];

            AnimationClip[] dialogueClips = MapClips(
                role.dialogueClips, controller.DialogueStateNames, out string[] dialogueStateNames);
            serialized.FindProperty("dialogueClips").arraySize = dialogueClips.Length;
            for (int i = 0; i < dialogueClips.Length; i++)
                serialized.FindProperty("dialogueClips").GetArrayElementAtIndex(i).objectReferenceValue = dialogueClips[i];
            serialized.FindProperty("dialogueStateNames").arraySize = dialogueStateNames.Length;
            for (int i = 0; i < dialogueStateNames.Length; i++)
                serialized.FindProperty("dialogueStateNames").GetArrayElementAtIndex(i).stringValue = dialogueStateNames[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AnimationClip[] MapClips(
            AnimationClip[] clips,
            Dictionary<AnimationClip, string> states,
            out string[] stateNames)
        {
            if (clips == null || clips.Length == 0)
            {
                stateNames = Array.Empty<string>();
                return Array.Empty<AnimationClip>();
            }

            var validClips = new List<AnimationClip>();
            var validStateNames = new List<string>();
            foreach (AnimationClip clip in clips)
            {
                if (clip == null || !states.TryGetValue(clip, out string stateName))
                    continue;
                validClips.Add(clip);
                validStateNames.Add(stateName);
            }

            stateNames = validStateNames.ToArray();
            return validClips.ToArray();
        }

        private static void ConfigureColliders(GameObject root)
        {
            CapsuleCollider[] existing = root.GetComponents<CapsuleCollider>();
            CapsuleCollider interaction = existing.FirstOrDefault(collider => collider.isTrigger);
            CapsuleCollider blocker = existing.FirstOrDefault(collider => !collider.isTrigger);
            if (interaction == null)
                interaction = root.AddComponent<CapsuleCollider>();
            interaction.isTrigger = true;
            interaction.radius = 1.6f;
            interaction.height = 3.2f;
            interaction.center = new Vector3(0f, 0.9f, 0f);

            if (blocker == null)
                blocker = root.AddComponent<CapsuleCollider>();
            blocker.isTrigger = false;
            blocker.radius = 0.3f;
            blocker.height = 1.8f;
            blocker.center = new Vector3(0f, 0.9f, 0f);
            blocker.direction = 1;
        }

        private static ClipSet DiscoverClips()
        {
            var clips = new ClipSet();
            var idle = new List<AnimationClip>();
            AddClipsFromPath(IdleStarterPath, idle);
            AddClipsFromPath(IdleDoubleLPath, idle);
            clips.Idle = idle
                .Where(clip => clip != null && clip.length > 0.1f)
                .GroupBy(clip => AssetDatabase.GetAssetPath(clip) + ":" + clip.name, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToArray();

            var dialogue = new List<AnimationClip>();
            foreach (string path in DialoguePaths)
                AddClipsFromPath(path, dialogue);
            clips.Dialogue = dialogue
                .Where(clip => clip != null && clip.length > 0.1f)
                .GroupBy(clip => AssetDatabase.GetAssetPath(clip) + ":" + clip.name, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToArray();
            return clips;
        }

        private static void AddClipsFromPath(string path, List<AnimationClip> clips)
        {
            UnityEngine.Object[] importedAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (importedAssets == null || importedAssets.Length == 0)
            {
                Debug.LogWarning("NPC animation file was not found or has no imported assets: " + path);
                return;
            }

            foreach (UnityEngine.Object asset in importedAssets)
            {
                AnimationClip clip = asset as AnimationClip;
                if (clip != null && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                    clips.Add(clip);
            }
        }

        private static GeneratedController CreateController(List<NPCRoleDefinition> roles)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            if (controller == null)
            {
                Debug.LogError("Could not create NPC Animator Controller at " + ControllerPath);
                return null;
            }

            AnimatorControllerLayer[] layers = controller.layers;
            if (layers == null || layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
                layers = controller.layers;
            }

            AnimatorStateMachine stateMachine = layers[0].stateMachine;
            foreach (ChildAnimatorState child in stateMachine.states)
                stateMachine.RemoveState(child.state);
            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions)
                stateMachine.RemoveAnyStateTransition(transition);

            AnimationClip[] idleClips = roles
                .Where(role => role != null && role.idleClips != null)
                .SelectMany(role => role.idleClips)
                .Where(clip => clip != null)
                .Distinct()
                .ToArray();
            AnimationClip[] dialogueClips = roles
                .Where(role => role != null && role.dialogueClips != null)
                .SelectMany(role => role.dialogueClips)
                .Where(clip => clip != null)
                .Distinct()
                .ToArray();
            var idleNames = new Dictionary<AnimationClip, string>();
            AnimatorState defaultIdle = null;
            for (int i = 0; i < idleClips.Length; i++)
            {
                string stateName = i == 0 ? "Idle" : "Idle_" + i;
                idleNames[idleClips[i]] = stateName;
                AnimatorState state = stateMachine.AddState(stateName);
                state.motion = idleClips[i];
                state.writeDefaultValues = true;
                if (i == 0)
                {
                    stateMachine.defaultState = state;
                    defaultIdle = state;
                }
            }

            var talkNames = new Dictionary<AnimationClip, string>();
            for (int i = 0; i < dialogueClips.Length; i++)
            {
                string stateName = i == 0 ? "Talk" : "Talk_" + i;
                talkNames[dialogueClips[i]] = stateName;
                AnimatorState state = stateMachine.AddState(stateName);
                state.motion = dialogueClips[i];
                state.writeDefaultValues = true;
                if (defaultIdle != null)
                {
                    AnimatorStateTransition toIdle = state.AddTransition(defaultIdle);
                    toIdle.hasExitTime = true;
                    toIdle.exitTime = 1f;
                    toIdle.duration = 0.2f;
                    toIdle.hasFixedDuration = true;
                }
            }

            EditorUtility.SetDirty(controller);
            return new GeneratedController
            {
                Controller = controller,
                IdleStateNames = idleNames,
                DialogueStateNames = talkNames
            };
        }

        private static bool TryFindHumanoidAvatar(out Avatar avatar)
        {
            avatar = null;
            UnityEngine.Object[] importedAssets = AssetDatabase.LoadAllAssetsAtPath(AvatarModelPath);
            if (importedAssets == null || importedAssets.Length == 0)
            {
                Debug.LogError("NPC Humanoid Avatar source is missing or has no imported assets: " + AvatarModelPath);
                return false;
            }

            foreach (UnityEngine.Object asset in importedAssets)
            {
                Avatar candidate = asset as Avatar;
                if (candidate != null && candidate.isValid && candidate.isHuman)
                {
                    avatar = candidate;
                    return true;
                }
            }

            Debug.LogError("No valid Humanoid Avatar subasset was found in " + AvatarModelPath);
            return false;
        }

        private static void SpawnTestNPCs()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Spawn Test NPCs cannot run in Play mode.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("Open a loaded scene before spawning test NPCs.");
                return;
            }

            Transform player = FindPlayerTransform();
            if (player == null)
            {
                Debug.LogError("Could not find a Player-tagged object or an object named PlayerPrince.");
                return;
            }

            var prefabs = new List<GameObject>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && Path.GetFileNameWithoutExtension(path).StartsWith("NPC_", StringComparison.Ordinal))
                    prefabs.Add(prefab);
            }
            prefabs.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            if (prefabs.Count == 0)
            {
                Debug.LogWarning("No NPC prefabs found. Run Tools > Prince > Create NPC Prefabs first.");
                return;
            }

            ClearTestNPCs();
            var parent = new GameObject(TestRowName);
            Undo.RegisterCreatedObjectUndo(parent, "Spawn NPC Test Row");
            SceneManager.MoveGameObjectToScene(parent, scene);

            Vector3 forward = player.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 center = player.position + forward * 5f;
            float start = -(prefabs.Count - 1) * 1f;

            for (int i = 0; i < prefabs.Count; i++)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(prefabs[i], scene) as GameObject;
                if (instance == null)
                    continue;
                Undo.RegisterCreatedObjectUndo(instance, "Spawn NPC Test Row");
                instance.transform.SetParent(parent.transform, true);
                Vector3 position = center + right * (start + i * 2f);
                position = FindGroundPosition(position, player);
                Vector3 facing = player.position - position;
                facing.y = 0f;
                if (facing.sqrMagnitude < 0.0001f)
                    facing = forward;
                instance.transform.SetPositionAndRotation(position, Quaternion.LookRotation(facing, Vector3.up));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Spawned {prefabs.Count} NPC test prefabs under '{TestRowName}'. Save the scene to keep them.");
        }

        private static Vector3 FindGroundPosition(Vector3 desired, Transform player)
        {
            RaycastHit[] hits = Physics.RaycastAll(
                desired + Vector3.up * 20f, Vector3.down, 50f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == player || hit.transform.IsChildOf(player))
                    continue;
                return hit.point;
            }

            return desired;
        }

        private static Transform FindPlayerTransform()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                player = GameObject.Find("PlayerPrince");
            return player != null ? player.transform : null;
        }

        private static void ClearTestNPCs()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Clear Test NPCs cannot run in Play mode.");
                return;
            }

            GameObject[] candidates = Resources.FindObjectsOfTypeAll<GameObject>();
            int removed = 0;
            foreach (GameObject candidate in candidates)
            {
                if (candidate.name != TestRowName || EditorUtility.IsPersistent(candidate) ||
                    candidate.scene != SceneManager.GetActiveScene())
                    continue;
                Undo.DestroyObjectImmediate(candidate);
                removed++;
            }

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Debug.Log($"Removed {removed} '{TestRowName}' object(s) from the active scene.");
            }
            else
                Debug.Log($"No '{TestRowName}' object was found in the active scene.");
        }

        private static int StableSeed(string role, int variant)
        {
            unchecked
            {
                int hash = 17;
                string value = (role ?? string.Empty) + ":" + variant;
                for (int i = 0; i < value.Length; i++)
                    hash = hash * 31 + value[i];
                return hash & 0x7fffffff;
            }
        }

        private static Color ChooseColor(Color[] palette, string roleName, int variant, Color fallback)
        {
            if (palette == null || palette.Length == 0)
                return fallback;
            int firstIndex = StableSeed(roleName, 0) % palette.Length;
            return palette[(firstIndex + variant - 1) % palette.Length];
        }

        private static Color[] Colors(params string[] htmlColors)
        {
            var result = new Color[htmlColors.Length];
            for (int i = 0; i < htmlColors.Length; i++)
            {
                if (!ColorUtility.TryParseHtmlString(htmlColors[i], out result[i]))
                    result[i] = Color.white;
            }
            return result;
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "NPC";
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value.Replace(' ', '_');
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

        private sealed class RoleDefaults
        {
            public string Name;
            public NPCOutfitPiece[] Required = Array.Empty<NPCOutfitPiece>();
            public NPCOptionalOutfitPiece[] Optional = Array.Empty<NPCOptionalOutfitPiece>();
            public NPCOutfitPiece[] Tops = Array.Empty<NPCOutfitPiece>();
            public NPCOutfitPiece[] Bottoms = Array.Empty<NPCOutfitPiece>();
            public NPCBodyVariant[] Bodies = { NPCBodyVariant.Body01 };
            public Color[] Clothing;
            public Color[] Skin = Colors("#E5B99A", "#D6A17F", "#F1CFB4");
            public Color[] Hair = Colors("#33241F", "#5B3A2E", "#241B19");
            public Vector2 Scale = new Vector2(0.95f, 1.05f);
            public bool LookAt = true;

            public void Apply(NPCRoleDefinition role)
            {
                role.roleName = Name;
                role.requiredPieces = Required;
                role.optionalPieces = Optional;
                role.topChoices = Tops;
                role.bottomChoices = Bottoms;
                role.bodyChoices = Bodies;
                role.clothingPalette = Clothing;
                role.skinPalette = Skin;
                role.hairPalette = Hair;
                role.scaleRange = Scale;
                role.lookAtPlayer = LookAt;
            }
        }

        private static IEnumerable<RoleDefaults> GetRoleDefaults()
        {
            yield return new RoleDefaults
            {
                Name = "Guard",
                Required = new[] { NPCOutfitPiece.Helmet, NPCOutfitPiece.Torso, NPCOutfitPiece.Pants,
                    NPCOutfitPiece.Boots, NPCOutfitPiece.Gloves, NPCOutfitPiece.Bracers },
                Tops = new[] { NPCOutfitPiece.Torso },
                Bottoms = new[] { NPCOutfitPiece.Pants },
                Bodies = new[] { NPCBodyVariant.Body01 },
                Optional = new[] { new NPCOptionalOutfitPiece { piece = NPCOutfitPiece.Bags, chance = 0.05f } },
                Clothing = Colors("#4D555E", "#6A6256", "#373D46"),
                Hair = Colors("#241B19", "#2F2725")
            };
            yield return new RoleDefaults
            {
                Name = "Merchant",
                Required = new[] { NPCOutfitPiece.Tunic, NPCOutfitPiece.Pants, NPCOutfitPiece.Boots,
                    NPCOutfitPiece.Bags, NPCOutfitPiece.Hair },
                Tops = new[] { NPCOutfitPiece.Tunic },
                Bottoms = new[] { NPCOutfitPiece.Pants },
                Optional = new[] { new NPCOptionalOutfitPiece { piece = NPCOutfitPiece.Bracers, chance = 0.2f } },
                Clothing = Colors("#9B3F32", "#C28B31", "#77513A")
            };
            yield return new RoleDefaults
            {
                Name = "Farmer",
                Required = new[] { NPCOutfitPiece.Tunic, NPCOutfitPiece.Hair },
                Tops = new[] { NPCOutfitPiece.Tunic },
                Bottoms = new[] { NPCOutfitPiece.Shorts, NPCOutfitPiece.Pants },
                Optional = new[]
                {
                    new NPCOptionalOutfitPiece { piece = NPCOutfitPiece.Boots, chance = 0.55f },
                    new NPCOptionalOutfitPiece { piece = NPCOutfitPiece.Bags, chance = 0.15f }
                },
                Clothing = Colors("#C7B58E", "#657247", "#78563C")
            };
            yield return new RoleDefaults
            {
                Name = "Blacksmith",
                Required = new[] { NPCOutfitPiece.Torso, NPCOutfitPiece.Gloves, NPCOutfitPiece.Bracers,
                    NPCOutfitPiece.Hair, NPCOutfitPiece.Pants, NPCOutfitPiece.Boots },
                Tops = new[] { NPCOutfitPiece.Torso },
                Bottoms = new[] { NPCOutfitPiece.Pants },
                Optional = new[] { new NPCOptionalOutfitPiece { piece = NPCOutfitPiece.Bags, chance = 0.2f } },
                Clothing = Colors("#383B3F", "#57483F", "#6A5547")
            };
            yield return new RoleDefaults
            {
                Name = "Elder",
                Required = new[] { NPCOutfitPiece.Tunic, NPCOutfitPiece.Hair,
                    NPCOutfitPiece.Pants, NPCOutfitPiece.Boots },
                Tops = new[] { NPCOutfitPiece.Tunic },
                Bottoms = new[] { NPCOutfitPiece.Pants },
                Bodies = new[] { NPCBodyVariant.Body02 },
                Optional = new[] { new NPCOptionalOutfitPiece { piece = NPCOutfitPiece.Bags, chance = 0.1f } },
                Clothing = Colors("#77756F", "#A69A82", "#635D55"),
                Hair = Colors("#D7D4CC", "#AAA9A4", "#8B8983"),
                Scale = new Vector2(0.91f, 0.99f)
            };
            yield return new RoleDefaults
            {
                Name = "Villager",
                Required = Array.Empty<NPCOutfitPiece>(),
                Tops = new[] { NPCOutfitPiece.Torso, NPCOutfitPiece.Tunic },
                Bottoms = new[] { NPCOutfitPiece.Shorts, NPCOutfitPiece.Pants },
                Bodies = new[] { NPCBodyVariant.Body01, NPCBodyVariant.Body02 },
                Optional = new[]
                {
                    new NPCOptionalOutfitPiece { piece = NPCOutfitPiece.Hair, chance = 0.8f },
                    new NPCOptionalOutfitPiece { piece = NPCOutfitPiece.Boots, chance = 0.65f },
                    new NPCOptionalOutfitPiece { piece = NPCOutfitPiece.Gloves, chance = 0.25f },
                    new NPCOptionalOutfitPiece { piece = NPCOutfitPiece.Bracers, chance = 0.2f },
                    new NPCOptionalOutfitPiece { piece = NPCOutfitPiece.Helmet, chance = 0.1f },
                    new NPCOptionalOutfitPiece { piece = NPCOutfitPiece.Bags, chance = 0.3f }
                },
                Clothing = Colors("#8C6346", "#667047", "#8C4540", "#A38D6D", "#536A72"),
                Skin = Colors("#E5B99A", "#D6A17F", "#F1CFB4", "#B98262"),
                Hair = Colors("#33241F", "#5B3A2E", "#241B19", "#A6A19A")
            };
        }
    }

    internal sealed class NPCGeneratorWindow : EditorWindow
    {
        private int variantCount = 3;

        internal static void ShowWindow()
        {
            NPCGeneratorWindow window = GetWindow<NPCGeneratorWindow>("NPC Generator");
            window.minSize = new Vector2(300f, 100f);
            window.Show();
        }

        private void OnEnable()
        {
            variantCount = EditorPrefs.GetInt("Prince.NPC.VariantCount", 3);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("NPC Prefab Generator", EditorStyles.boldLabel);
            variantCount = Mathf.Max(1, EditorGUILayout.IntField("Variants per role", variantCount));
            EditorPrefs.SetInt("Prince.NPC.VariantCount", variantCount);
            if (GUILayout.Button("Create or Update NPC Prefabs"))
                PrinceNPCSetup.GenerateNPCPrefabs(variantCount);
            EditorGUILayout.HelpBox("The Tools > Prince > Create NPC Prefabs menu uses this saved count.", MessageType.Info);
        }
    }
}
