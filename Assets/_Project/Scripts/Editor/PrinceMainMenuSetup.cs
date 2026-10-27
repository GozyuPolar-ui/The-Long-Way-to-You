using System;
using System.Collections.Generic;
using Prince;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Prince.Editor
{
    internal static class PrinceMainMenuSetup
    {
        private const string MenuScenePath = "Assets/_Project/Scene/Main-Menu.unity";
        private const string GameScenePath = "Assets/_Project/Scene/UNS_Demo.unity";
        private const string MenuMusicPath =
            "Assets/_Project/Audio/MAIN MENU/royaltyfreemusicstudio-medieval-fantasy-music-462199.mp3";
        private const string MenuVideoPath =
            "Assets/_Project/UI/MainMenu/gemini_generated_video_728ee3eb.mp4";
        private const string MenuFallbackPath =
            "Assets/_Project/UI/MainMenu/Gambar backup.jpeg";
        private const string MenuVideoTexturePath =
            "Assets/_Project/Rendering/MainMenu/MenuVideo.renderTexture";
        private const string MenuTextStylePath =
            "Assets/_Project/Resources/MenuTextStyle.asset";
        private const string MenuFontSourcePath =
            "Assets/_Project/Fonts/Cinzel_Decorative,Cormorant_Garamond,MedievalSharp/Cormorant_Garamond/static/CormorantGaramond-SemiBold.ttf";
        private const string TitleFontSourcePath =
            "Assets/_Project/Fonts/Cinzel_Decorative,Cormorant_Garamond,MedievalSharp/Cinzel_Decorative/CinzelDecorative-Bold.ttf";
        private const string MenuFontAssetPath =
            "Assets/_Project/Fonts/TMP/CormorantGaramond-SemiBold SDF.asset";
        private const string TitleFontAssetPath =
            "Assets/_Project/Fonts/TMP/CinzelDecorative-Bold SDF.asset";
        private const string FallbackFontPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string MainCanvasName = "MainMenuCanvas";
        private const string FaderCanvasName = "SceneFaderCanvas";
        private const string EventSystemName = "MainMenuEventSystem";
        private const string CameraName = "MainMenuCamera";
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;

        [MenuItem("Tools/Prince/Setup Main Menu")]
        private static void SetupMenu()
        {
            Setup();
        }

        private static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Setup Main Menu cannot run in Play mode.");
                return;
            }

            if (!ValidateAssets(out VideoClip menuVideo, out Texture2D fallbackImage,
                    out RenderTexture videoTexture, out AudioClip menuMusic,
                    out AudioMixerGroup musicGroup, out TMP_FontAsset font,
                    out TMP_FontAsset titleFont))
                return;

            MenuTextStyle textStyle = GetOrCreateTextStyle();
            if (textStyle == null)
            {
                Debug.LogError($"Could not create or load MenuTextStyle at '{MenuTextStylePath}'.");
                return;
            }

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene menuScene = SceneManager.GetSceneByPath(MenuScenePath);
            bool openedMenuScene = false;
            if (!menuScene.IsValid() || !menuScene.isLoaded)
            {
                if (!System.IO.File.Exists(MenuScenePath))
                {
                    Debug.LogError($"The existing main menu scene was not found at '{MenuScenePath}'.");
                    return;
                }
                menuScene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Additive);
                openedMenuScene = true;
            }

            RemoveGeneratedMenuObjects(menuScene);
            SceneManager.SetActiveScene(menuScene);
            BuildCamera(menuScene);
            BuildCanvas(menuScene, menuVideo, fallbackImage, videoTexture, menuMusic, musicGroup,
                font, titleFont, textStyle);
            SceneFader fader = BuildFader(menuScene);
            MainMenuController controller =
                FindComponentInScene<MainMenuController>(menuScene);
            if (controller != null)
                SetReference(controller, "sceneFader", fader);
            BuildEventSystem(menuScene);

            if (!EditorSceneManager.SaveScene(menuScene, MenuScenePath))
            {
                Debug.LogError($"Could not save the main menu scene to '{MenuScenePath}'.");
                RestoreEditorScenes(previousActiveScene, menuScene, openedMenuScene);
                return;
            }

            RestoreEditorScenes(previousActiveScene, menuScene, openedMenuScene);
            AssetDatabase.SaveAssets();
            PrinceMainMenuTextContrastAudit.Audit(textStyle);
            Debug.Log("Existing Main-Menu scene updated with improved text contrast and menu BGM.");
        }

        private static MenuTextStyle GetOrCreateTextStyle()
        {
            MenuTextStyle style = AssetDatabase.LoadAssetAtPath<MenuTextStyle>(MenuTextStylePath);
            if (style != null)
                return style;

            EnsureFolder("Assets/_Project/Resources");
            style = ScriptableObject.CreateInstance<MenuTextStyle>();
            style.name = "MenuTextStyle";
            AssetDatabase.CreateAsset(style, MenuTextStylePath);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<MenuTextStyle>(MenuTextStylePath);
        }

        private static bool ValidateAssets(
            out VideoClip menuVideo,
            out Texture2D fallbackImage,
            out RenderTexture videoTexture,
            out AudioClip menuMusic,
            out AudioMixerGroup musicGroup,
            out TMP_FontAsset font,
            out TMP_FontAsset titleFont)
        {
            ImportVideoWithoutTranscoding();
            ImportMenuMusicForStreaming();
            menuVideo = AssetDatabase.LoadAssetAtPath<VideoClip>(MenuVideoPath);
            fallbackImage = AssetDatabase.LoadAssetAtPath<Texture2D>(MenuFallbackPath);
            videoTexture = GetOrCreateVideoTexture();
            menuMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(MenuMusicPath);
            musicGroup = FindMusicMixerGroup();
            font = LoadOrCreateFontAsset(MenuFontSourcePath, MenuFontAssetPath);
            titleFont = LoadOrCreateFontAsset(TitleFontSourcePath, TitleFontAssetPath);
            TMP_FontAsset fallbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);
            font = font != null ? font : fallbackFont;
            titleFont = titleFont != null ? titleFont : font;
            var missing = new List<string>();
            if (!System.IO.File.Exists(GameScenePath))
                missing.Add(GameScenePath);
            if (menuVideo == null)
                Debug.LogWarning($"Menu video was not imported at '{MenuVideoPath}'. The still-image fallback will be used.");
            if (fallbackImage == null)
                Debug.LogWarning($"Menu fallback image was not imported at '{MenuFallbackPath}'. A dark background will be used.");
            if (videoTexture == null)
                missing.Add(MenuVideoTexturePath);
            if (menuMusic == null)
                Debug.LogWarning($"Main menu BGM was not found at '{MenuMusicPath}'; MenuMusic will be silent.", null);
            if (font == null)
                Debug.LogWarning($"Menu font assets could not be created from '{MenuFontSourcePath}' or '{TitleFontSourcePath}', and the TMP fallback font was not found. The runtime TMP default font will be used.");
            if (AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/_Project/Scripts/Runtime/UI/MainMenuController.cs") == null)
                missing.Add("Assets/_Project/Scripts/Runtime/UI/MainMenuController.cs");
            if (AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/_Project/Scripts/Runtime/UI/MenuVideoBackground.cs") == null)
                missing.Add("Assets/_Project/Scripts/Runtime/UI/MenuVideoBackground.cs");
            if (missing.Count == 0)
                return true;
            Debug.LogError("Main menu setup is missing required assets:\n- " + string.Join("\n- ", missing));
            return false;
        }

        private static void BuildCamera(Scene scene)
        {
            GameObject cameraObject = FindGameObjectInScene(scene, CameraName);
            Camera camera = cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
            if (camera == null)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    camera = root.GetComponentInChildren<Camera>(true);
                    if (camera != null)
                    {
                        cameraObject = camera.gameObject;
                        break;
                    }
                }
            }

            if (cameraObject == null)
            {
                cameraObject = new GameObject(CameraName, typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                camera = cameraObject.GetComponent<Camera>();
            }

            cameraObject.name = CameraName;
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            camera.enabled = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.03f, 0.032f, 1f);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;

            AudioListener keepListener = cameraObject.GetComponent<AudioListener>();
            if (keepListener == null)
            {
                keepListener = cameraObject.AddComponent<AudioListener>();
                Debug.LogWarning("MainMenu had no AudioListener on its camera; one has been added.", cameraObject);
            }
            keepListener.enabled = true;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (AudioListener listener in root.GetComponentsInChildren<AudioListener>(true))
                {
                    if (listener != keepListener)
                    {
                        Debug.LogWarning($"Removing duplicate MainMenu AudioListener from '{listener.gameObject.name}'.", listener);
                        UnityEngine.Object.DestroyImmediate(listener);
                    }
                }
            }
        }

        private static void BuildCanvas(
            Scene scene,
            VideoClip videoClip,
            Texture2D fallbackTexture,
            RenderTexture videoTexture,
            AudioClip musicClip,
            AudioMixerGroup musicGroup,
            TMP_FontAsset font,
            TMP_FontAsset titleFont,
            MenuTextStyle textStyle)
        {
            GameObject canvasObject = FindGameObjectInScene(scene, MainCanvasName);
            if (canvasObject == null)
            {
                canvasObject = new GameObject(MainCanvasName, typeof(RectTransform));
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
            }

            Canvas canvas = GetOrAddComponent<Canvas>(canvasObject);
            GetOrAddComponent<GraphicRaycaster>(canvasObject);
            CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(canvasObject);
            MainMenuController controller = GetOrAddComponent<MainMenuController>(canvasObject);
            MenuVideoBackground videoBackground = GetOrAddComponent<MenuVideoBackground>(canvasObject);
            RemoveExistingBackgroundLayers(canvasObject.transform);

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;

            RawImage fallback = CreateStretchRawImage("MenuFallbackBackground", canvasObject.transform);
            fallback.texture = fallbackTexture;
            fallback.color = fallbackTexture != null ? Color.white : new Color(0.035f, 0.045f, 0.048f, 1f);

            GameObject videoImageObject = new GameObject(
                "MenuVideoImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(CanvasGroup));
            videoImageObject.transform.SetParent(canvasObject.transform, false);
            RawImage videoImage = videoImageObject.GetComponent<RawImage>();
            videoImage.texture = videoTexture;
            videoImage.raycastTarget = false;
            RectTransform videoRect = videoImage.rectTransform;
            videoRect.anchorMin = Vector2.zero;
            videoRect.anchorMax = Vector2.one;
            videoRect.offsetMin = Vector2.zero;
            videoRect.offsetMax = Vector2.zero;
            videoRect.localScale = Vector3.one * 1.02f;
            CanvasGroup videoGroup = videoImageObject.GetComponent<CanvasGroup>();
            videoGroup.alpha = 0f;

            GameObject gradientObject = new GameObject(
                "MenuBackgroundGradient", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gradientObject.transform.SetParent(canvasObject.transform, false);
            Image gradient = gradientObject.GetComponent<Image>();
            gradient.raycastTarget = false;

            GameObject videoObject = new GameObject("MenuVideoPlayer", typeof(VideoPlayer), typeof(AudioSource));
            videoObject.transform.SetParent(canvasObject.transform, false);
            VideoPlayer videoPlayer = videoObject.GetComponent<VideoPlayer>();
            videoPlayer.clip = videoClip;
            videoPlayer.targetTexture = videoTexture;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.isLooping = true;
            videoPlayer.skipOnDrop = true;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.playOnAwake = false;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            AudioSource videoAudio = videoObject.GetComponent<AudioSource>();
            videoAudio.playOnAwake = false;
            videoAudio.spatialBlend = 0f;
            videoAudio.volume = 0f;
            videoAudio.mute = true;

            SetReference(videoBackground, "videoPlayer", videoPlayer);
            SetReference(videoBackground, "videoImage", videoImage);
            SetReference(videoBackground, "videoCanvasGroup", videoGroup);
            SetReference(videoBackground, "fallbackImage", fallback);
            SetReference(videoBackground, "gradientImage", gradient);
            SetReference(videoBackground, "videoAudioSource", videoAudio);
            SetBool(videoBackground, "useVideoAudio", false);
            SetFloat(videoBackground, "gradientAlpha", 0.6f);
            SetFloat(videoBackground, "gradientWidth", 0.55f);
            SetColor(videoBackground, "gradientColor", new Color(0.09f, 0.14f, 0.19f, 1f));

            SetReference(controller, "menuCanvas", canvas);
            SetReference(controller, "menuVideoBackground", videoBackground);
            MenuMusicPlayer musicPlayer = BuildMenuMusic(scene, musicClip, musicGroup);
            SetReference(controller, "menuMusicPlayer", musicPlayer);
            SetReference(controller, "menuFont", font);
            SetReference(controller, "titleFont", titleFont);
            SetReference(controller, "textStyle", textStyle);
            SetFloat(controller, "titleFadeDuration", 1.5f);
            SetFloat(controller, "backgroundDim", 0.6f);
            SetFloat(controller, "gradientWidth", 0.55f);
            SetColor(controller, "gradientColor", new Color(0.09f, 0.14f, 0.19f, 1f));
            SetFloat(controller, "screenMargin", 76f);
            SetString(controller, "gameplaySceneName", "UNS_Demo");
        }

        private static MenuMusicPlayer BuildMenuMusic(Scene scene, AudioClip clip, AudioMixerGroup mixerGroup)
        {
            GameObject musicObject = FindGameObjectInScene(scene, "MenuMusic");
            if (musicObject == null)
            {
                musicObject = new GameObject("MenuMusic");
                SceneManager.MoveGameObjectToScene(musicObject, scene);
            }
            else if (musicObject.transform.parent != null)
            {
                musicObject.transform.SetParent(null, true);
            }

            AudioSource source = GetOrAddComponent<AudioSource>(musicObject);
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.mute = false;
            source.spatialBlend = 0f;
            source.volume = 0.48f;
            source.outputAudioMixerGroup = mixerGroup;

            return GetOrAddComponent<MenuMusicPlayer>(musicObject);
        }

        private static void RemoveExistingBackgroundLayers(Transform canvas)
        {
            string[] names =
            {
                "MenuFallbackBackground",
                "MenuVideoImage",
                "MenuBackgroundGradient",
                "MenuVideoPlayer"
            };
            foreach (string name in names)
            {
                Transform existing = canvas.Find(name);
                if (existing != null)
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static RawImage CreateStretchRawImage(string name, Transform parent)
        {
            GameObject imageObject = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);
            RawImage image = imageObject.GetComponent<RawImage>();
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return image;
        }

        private static SceneFader BuildFader(Scene scene)
        {
            GameObject faderCanvasObject = FindGameObjectInScene(scene, FaderCanvasName);
            if (faderCanvasObject == null)
            {
                faderCanvasObject = new GameObject(FaderCanvasName, typeof(RectTransform));
                SceneManager.MoveGameObjectToScene(faderCanvasObject, scene);
            }
            Canvas canvas = GetOrAddComponent<Canvas>(faderCanvasObject);
            CanvasGroup group = GetOrAddComponent<CanvasGroup>(faderCanvasObject);
            Image image = GetOrAddComponent<Image>(faderCanvasObject);
            GetOrAddComponent<GraphicRaycaster>(faderCanvasObject);
            SceneFader fader = GetOrAddComponent<SceneFader>(faderCanvasObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            group.alpha = 1f;
            group.blocksRaycasts = true;
            image.color = Color.black;
            image.raycastTarget = true;
            RectTransform rect = faderCanvasObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return fader;
        }

        private static void BuildEventSystem(Scene scene)
        {
            EventSystem eventSystem = FindComponentInScene<EventSystem>(scene);
            if (eventSystem == null)
            {
                var eventSystemObject = new GameObject(EventSystemName, typeof(EventSystem));
                SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }
            InputSystemUIInputModule inputModule =
                eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

            if (inputModule.actionsAsset == null || inputModule.point == null ||
                inputModule.leftClick == null || inputModule.move == null ||
                inputModule.submit == null)
                inputModule.AssignDefaultActions();

            eventSystem.sendNavigationEvents = true;
            inputModule.enabled = true;
        }

        private static void RestoreEditorScenes(Scene previousActiveScene, Scene sourceScene, bool openedSourceScene)
        {
            if (openedSourceScene && sourceScene.IsValid() && sourceScene.isLoaded)
                EditorSceneManager.CloseScene(sourceScene, true);
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);
        }

        private static void RemoveGeneratedMenuObjects(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                string objectName = root.name;
                if (objectName == "Main Camera" || objectName == "Directional Light" ||
                    objectName == "MenuForestTerrain" || objectName == "MenuCameraLookTarget" ||
                    objectName == "MenuWarmLight" ||
                    objectName.StartsWith("MenuSpruce_", StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject FindGameObjectInScene(Scene scene, string objectName)
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

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }
            return null;
        }

        private static void ImportVideoWithoutTranscoding()
        {
            if (!System.IO.File.Exists(MenuVideoPath))
                return;

            AssetDatabase.ImportAsset(MenuVideoPath, ImportAssetOptions.ForceSynchronousImport);
            VideoClipImporter importer = AssetImporter.GetAtPath(MenuVideoPath) as VideoClipImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Could not configure video import settings at '{MenuVideoPath}'.");
                return;
            }

            VideoImporterTargetSettings targetSettings = importer.defaultTargetSettings;
            bool changed = targetSettings.enableTranscoding || !importer.importAudio;
            targetSettings.enableTranscoding = false;
            importer.defaultTargetSettings = targetSettings;
            importer.importAudio = true;
            if (changed)
                importer.SaveAndReimport();

            if (importer.sourceAudioTrackCount == 0)
                Debug.LogWarning($"The menu video at '{MenuVideoPath}' reports no source audio tracks.");
        }

        private static void ImportMenuMusicForStreaming()
        {
            if (!System.IO.File.Exists(MenuMusicPath))
                return;

            AssetDatabase.ImportAsset(MenuMusicPath, ImportAssetOptions.ForceSynchronousImport);
            AudioImporter importer = AssetImporter.GetAtPath(MenuMusicPath) as AudioImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Could not configure AudioClip import settings at '{MenuMusicPath}'.");
                return;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            bool changed = settings.loadType != AudioClipLoadType.Streaming ||
                !settings.preloadAudioData || !importer.loadInBackground;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = true;
            if (changed)
                importer.SaveAndReimport();
        }

        private static TMP_FontAsset LoadOrCreateFontAsset(string sourcePath, string assetPath)
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (fontAsset != null)
                return fontAsset;

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (sourceFont == null)
            {
                Debug.LogWarning($"Menu font source was not imported or could not be found at '{sourcePath}'.");
                return null;
            }

            fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
            if (fontAsset == null)
            {
                Debug.LogWarning($"Could not generate a TMP Font Asset from '{sourcePath}'.");
                return null;
            }

            EnsureFolder("Assets/_Project/Fonts/TMP");
            fontAsset.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            if (fontAsset.atlasTexture != null && !AssetDatabase.Contains(fontAsset.atlasTexture))
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);

            TMP_FontAsset generated = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (generated == null)
                Debug.LogWarning($"TMP Font Asset generation did not produce an importable asset at '{assetPath}'.");
            return generated;
        }

        private static AudioMixerGroup FindMusicMixerGroup()
        {
            string[] mixerGuids = AssetDatabase.FindAssets("t:AudioMixer");
            if (mixerGuids.Length == 0)
                return null;

            foreach (string guid in mixerGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
                if (mixer == null)
                    continue;
                foreach (AudioMixerGroup group in mixer.FindMatchingGroups("Music"))
                    if (group != null && group.name == "Music")
                        return group;
            }

            Debug.LogWarning("AudioMixer assets exist, but none contains an AudioMixerGroup named 'Music'.");
            return null;
        }

        private static RenderTexture GetOrCreateVideoTexture()
        {
            EnsureFolder("Assets/_Project/Rendering");
            EnsureFolder("Assets/_Project/Rendering/MainMenu");
            RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(MenuVideoTexturePath);
            if (texture == null)
            {
                texture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32)
                {
                    name = "MenuVideo",
                    antiAliasing = 1,
                    useMipMap = false,
                    autoGenerateMips = false,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                AssetDatabase.CreateAsset(texture, MenuVideoTexturePath);
            }

            texture.Release();
            texture.width = 1920;
            texture.height = 1080;
            texture.depth = 0;
            texture.antiAliasing = 1;
            texture.useMipMap = false;
            texture.autoGenerateMips = false;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            EditorUtility.SetDirty(texture);
            return texture;
        }

        private static void SetReference(Component component, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"Serialized reference '{propertyName}' was not found on {component.GetType().Name}.", component);
                return;
            }
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Component component, string propertyName, string value)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"Serialized string '{propertyName}' was not found on {component.GetType().Name}.", component);
                return;
            }
            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Component component, string propertyName, bool value)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"Serialized boolean '{propertyName}' was not found on {component.GetType().Name}.", component);
                return;
            }
            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Component component, string propertyName, float value)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"Serialized float '{propertyName}' was not found on {component.GetType().Name}.", component);
                return;
            }
            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetColor(Component component, string propertyName, Color value)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"Serialized color '{propertyName}' was not found on {component.GetType().Name}.", component);
                return;
            }
            property.colorValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
