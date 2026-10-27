using System.IO;
using Prince;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Prince.Editor
{
    public static class PrinceLoadingScreenSetup
    {
        private const string LoadingImagePath = "Assets/_Project/UI/MainMenu/LoadingIMG.jpeg";
        private const string DataPath = "Assets/_Project/Resources/LoadingScreen/LoadingLoreData.asset";
        private const string PrefabPath = "Assets/_Project/Resources/LoadingScreen/SceneLoader.prefab";
        private const string MenuFontPath =
            "Assets/_Project/Fonts/TMP/CormorantGaramond-SemiBold SDF.asset";
        private const string FallbackFontPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string InitialIndonesianText =
            "Hutan mimpi. Saking indahnya, siapa pun yang masuk tidak akan bisa keluar.";
        private const string InitialEnglishText =
            "A forest of dreams. So beautiful that no one who enters can ever leave.";
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;

        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Setup Loading Screen cannot run in Play mode.");
                return;
            }

            if (!File.Exists(LoadingImagePath))
            {
                Debug.LogError(
                    $"The expected EDEN image was not found at '{LoadingImagePath}'. " +
                    "The requested 'UI-Main Menu' folder does not exist in Assets; no other image will be substituted.");
                return;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (!ConfigureLoadingImageImport())
                return;
            Texture2D edenImage = AssetDatabase.LoadAssetAtPath<Texture2D>(LoadingImagePath);
            if (edenImage == null)
            {
                Debug.LogError(
                    $"Unity could not import the EDEN image at '{LoadingImagePath}'. Check its importer and Console.");
                return;
            }

            EnsureFolder("Assets/_Project/Resources");
            EnsureFolder("Assets/_Project/Resources/LoadingScreen");
            LoadingLoreData data = CreateOrUpdateData(edenImage);
            if (data == null)
                return;

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MenuFontPath);
            if (font == null)
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);
            if (font == null)
                Debug.LogWarning("No TMP font asset was found; Unity's default TMP font will be used.");

            if (!CreateOrUpdatePrefab(data, font))
                return;

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Loading screen setup complete. Lore image: {LoadingImagePath}. " +
                "The existing Main Menu and Build Settings were not modified.");
        }

        private static bool ConfigureLoadingImageImport()
        {
            AssetImporter assetImporter = AssetImporter.GetAtPath(LoadingImagePath);
            TextureImporter importer = assetImporter as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Could not configure texture importer for '{LoadingImagePath}'.");
                return false;
            }

            Debug.Log(
                $"Loading image importer before setup: type={importer.textureType}, " +
                $"maxSize={importer.maxTextureSize}, compression={importer.textureCompression}, " +
                $"sRGB={importer.sRGBTexture}, mipmaps={importer.mipmapEnabled}.");

            bool changed = importer.textureType != TextureImporterType.Default ||
                importer.maxTextureSize != 2048 ||
                importer.textureCompression != TextureImporterCompression.Compressed ||
                !importer.sRGBTexture || importer.mipmapEnabled;
            importer.textureType = TextureImporterType.Default;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            if (changed)
                importer.SaveAndReimport();

            Debug.Log(
                $"Loading image importer after setup: type={importer.textureType}, " +
                $"maxSize={importer.maxTextureSize}, compression={importer.textureCompression}, " +
                $"sRGB={importer.sRGBTexture}, mipmaps={importer.mipmapEnabled}.");
            return true;
        }

        private static LoadingLoreData CreateOrUpdateData(Texture2D edenImage)
        {
            LoadingLoreData data = AssetDatabase.LoadAssetAtPath<LoadingLoreData>(DataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<LoadingLoreData>();
                AssetDatabase.CreateAsset(data, DataPath);
            }

            SerializedObject serialized = new SerializedObject(data);
            SerializedProperty entries = serialized.FindProperty("entries");
            if (entries == null)
            {
                Debug.LogError($"LoadingLoreData has no serialized 'entries' field at '{DataPath}'.", data);
                return null;
            }

            int edenIndex = -1;
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty title = entries.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("titleIndonesian");
                if (title != null && title.stringValue == "EDEN")
                {
                    edenIndex = i;
                    break;
                }
            }

            if (edenIndex < 0)
            {
                edenIndex = entries.arraySize;
                entries.InsertArrayElementAtIndex(edenIndex);
                SerializedProperty entry = entries.GetArrayElementAtIndex(edenIndex);
                entry.FindPropertyRelative("image").objectReferenceValue = edenImage;
                entry.FindPropertyRelative("titleIndonesian").stringValue = "EDEN";
                entry.FindPropertyRelative("textIndonesian").stringValue = InitialIndonesianText;
                entry.FindPropertyRelative("titleEnglish").stringValue = "EDEN";
                entry.FindPropertyRelative("textEnglish").stringValue = InitialEnglishText;
                entry.FindPropertyRelative("displayDuration").floatValue = 7f;
            }
            else
            {
                SerializedProperty image = entries.GetArrayElementAtIndex(edenIndex)
                    .FindPropertyRelative("image");
                image.objectReferenceValue = edenImage;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static bool CreateOrUpdatePrefab(LoadingLoreData data, TMP_FontAsset font)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return UpdateExistingPrefab(data, font);

            GameObject loaderRoot = new GameObject("SceneLoader");
            try
            {
                SceneLoader sceneLoader = loaderRoot.AddComponent<SceneLoader>();
                GameObject canvasObject = new GameObject(
                    "LoadingScreenCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(CanvasGroup),
                    typeof(LoadingScreenController),
                    typeof(LoreSlideView));
                canvasObject.transform.SetParent(loaderRoot.transform, false);

                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 2000;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
                scaler.matchWidthOrHeight = 0.5f;
                CanvasGroup canvasGroup = canvasObject.GetComponent<CanvasGroup>();
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
                CreateBackground("LoreImageA", canvasRect);
                CreateBackground("LoreImageB", canvasRect);
                CreateBottomGradient(canvasRect);

                GameObject textObject = new GameObject(
                    "LoreText", typeof(RectTransform), typeof(CanvasGroup));
                textObject.transform.SetParent(canvasRect, false);
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                SetRect(textRect, new Vector2(0.075f, 0.105f), new Vector2(0.68f, 0.31f),
                    new Vector2(0f, 0f), Vector2.zero, Vector2.zero);
                CanvasGroup textGroup = textObject.GetComponent<CanvasGroup>();

                TMP_Text title = CreateText("LoreTitle", textRect, "EDEN", 20f,
                    new Color(0.82f, 0.69f, 0.48f, 1f), font);
                RectTransform titleRect = title.rectTransform;
                SetRect(titleRect, new Vector2(0f, 0.68f), new Vector2(1f, 1f),
                    new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
                title.fontStyle = FontStyles.Bold;
                title.characterSpacing = 3f;

                TMP_Text body = CreateText("LoreBody", textRect, string.Empty, 27f,
                    new Color(0.91f, 0.87f, 0.78f, 1f), font);
                RectTransform bodyRect = body.rectTransform;
                SetRect(bodyRect, Vector2.zero, new Vector2(1f, 0.64f),
                    new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
                body.textWrappingMode = TextWrappingModes.Normal;

                RectTransform dotsRoot = CreateDotsRoot(canvasRect);
                CreateProgressBar(canvasRect, out GameObject progressRoot, out Image progressFill);
                TMP_Text prompt = CreateText(
                    "ContinuePrompt", canvasRect, string.Empty, 20f,
                    new Color(0.83f, 0.75f, 0.61f, 1f), font);
                RectTransform promptRect = prompt.rectTransform;
                SetRect(promptRect, new Vector2(0.58f, 0.065f), new Vector2(0.925f, 0.12f),
                    new Vector2(1f, 0f), Vector2.zero, Vector2.zero);
                prompt.alignment = TextAlignmentOptions.BottomRight;
                prompt.gameObject.SetActive(false);

                LoreSlideView view = canvasObject.GetComponent<LoreSlideView>();
                view.Configure(
                    canvasRect.Find("LoreImageA").GetComponent<RawImage>(),
                    canvasRect.Find("LoreImageB").GetComponent<RawImage>(),
                    textGroup, title, body, progressFill, progressRoot, prompt, dotsRoot);
                LoadingScreenController screen = canvasObject.GetComponent<LoadingScreenController>();
                screen.Configure(view);
                sceneLoader.Configure(data, screen);

                PrefabUtility.SaveAsPrefabAsset(loaderRoot, PrefabPath, out bool success);
                if (!success)
                {
                    Debug.LogError($"Could not save the Loading Screen prefab at '{PrefabPath}'.");
                    return false;
                }
                return true;
            }
            finally
            {
                Object.DestroyImmediate(loaderRoot);
            }
        }

        private static bool UpdateExistingPrefab(LoadingLoreData data, TMP_FontAsset font)
        {
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabContents == null)
            {
                Debug.LogError($"Could not load the existing Loading Screen prefab at '{PrefabPath}'.");
                return false;
            }

            try
            {
                SceneLoader sceneLoader = prefabContents.GetComponent<SceneLoader>();
                LoadingScreenController screen =
                    prefabContents.GetComponentInChildren<LoadingScreenController>(true);
                LoreSlideView view = prefabContents.GetComponentInChildren<LoreSlideView>(true);
                if (sceneLoader == null || screen == null || view == null)
                {
                    Debug.LogError(
                        $"The existing prefab at '{PrefabPath}' is missing required loading screen components. " +
                        "It was left unchanged; restore the generated prefab or remove it before setup.",
                        prefabContents);
                    return false;
                }

                sceneLoader.Configure(data, screen);
                screen.Configure(view);
                ApplyMinimalLayout(prefabContents, font);

                PrefabUtility.SaveAsPrefabAsset(prefabContents, PrefabPath, out bool success);
                if (!success)
                    Debug.LogError($"Could not update the Loading Screen prefab at '{PrefabPath}'.");
                return success;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        private static void ApplyMinimalLayout(GameObject prefabRoot, TMP_FontAsset font)
        {
            Canvas canvas = prefabRoot.GetComponentInChildren<Canvas>(true);
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 2000;
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
                    scaler.matchWidthOrHeight = 0.5f;
                }
            }

            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null)
            {
                Debug.LogError("The loading prefab has no Canvas RectTransform.", prefabRoot);
                return;
            }

            RectTransform gradientRect = FindRect(canvasRect, "BottomGradient");
            if (gradientRect != null)
                SetRect(gradientRect, Vector2.zero, new Vector2(1f, 0.36f),
                    Vector2.zero, Vector2.zero, Vector2.zero);

            RectTransform textRect = FindRect(canvasRect, "LoreText");
            if (textRect != null)
                SetRect(textRect, new Vector2(0.075f, 0.105f), new Vector2(0.68f, 0.31f),
                    new Vector2(0f, 0f), Vector2.zero, Vector2.zero);

            TMP_Text title = FindText(canvasRect, "LoreText/LoreTitle");
            if (title != null)
            {
                SetRect(title.rectTransform, new Vector2(0f, 0.68f), Vector2.one,
                    new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
                title.fontSize = 20f;
                title.fontStyle = FontStyles.Bold;
                title.characterSpacing = 3f;
                title.color = new Color(0.82f, 0.69f, 0.48f, 1f);
                if (font != null)
                    title.font = font;
            }

            TMP_Text body = FindText(canvasRect, "LoreText/LoreBody");
            if (body != null)
            {
                SetRect(body.rectTransform, Vector2.zero, new Vector2(1f, 0.64f),
                    new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
                body.fontSize = 27f;
                body.color = new Color(0.91f, 0.87f, 0.78f, 1f);
                body.textWrappingMode = TextWrappingModes.Normal;
                if (font != null)
                    body.font = font;
            }

            RectTransform dotsRect = FindRect(canvasRect, "LoreDots");
            if (dotsRect != null)
                SetRect(dotsRect, new Vector2(0.075f, 0.065f), new Vector2(0.32f, 0.12f),
                    new Vector2(0f, 0f), Vector2.zero, Vector2.zero);

            RectTransform progressRect = FindRect(canvasRect, "LoadingProgress");
            if (progressRect != null)
                SetRect(progressRect, new Vector2(0.075f, 0.028f),
                    new Vector2(0.925f, 0.028f), new Vector2(0f, 0.5f),
                    Vector2.zero, new Vector2(0f, 2f));

            TMP_Text prompt = FindText(canvasRect, "ContinuePrompt");
            if (prompt != null)
            {
                SetRect(prompt.rectTransform, new Vector2(0.58f, 0.065f),
                    new Vector2(0.925f, 0.12f), new Vector2(1f, 0f),
                    Vector2.zero, Vector2.zero);
                prompt.fontSize = 17f;
                prompt.color = new Color(0.82f, 0.78f, 0.69f, 0.9f);
                prompt.alignment = TextAlignmentOptions.BottomRight;
                if (font != null)
                    prompt.font = font;
            }
        }

        private static RectTransform FindRect(Transform root, string path)
        {
            Transform target = root.Find(path);
            if (target == null)
            {
                Debug.LogWarning($"Loading prefab is missing layout object '{path}'.", root);
                return null;
            }

            RectTransform rect = target as RectTransform;
            if (rect == null)
                Debug.LogWarning($"Loading prefab object '{path}' has no RectTransform.", target);
            return rect;
        }

        private static TMP_Text FindText(Transform root, string path)
        {
            Transform target = root.Find(path);
            if (target == null)
            {
                Debug.LogWarning($"Loading prefab is missing text object '{path}'.", root);
                return null;
            }

            TMP_Text text = target.GetComponent<TMP_Text>();
            if (text == null)
                Debug.LogWarning($"Loading prefab object '{path}' has no TMP_Text component.", target);
            return text;
        }

        private static void CreateBackground(string name, RectTransform parent)
        {
            GameObject imageObject = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage),
                typeof(AspectRatioFitter));
            imageObject.transform.SetParent(parent, false);
            RawImage image = imageObject.GetComponent<RawImage>();
            image.raycastTarget = false;
            image.color = Color.white;
            RectTransform rect = image.rectTransform;
            SetRect(rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            AspectRatioFitter fitter = imageObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
        }

        private static void CreateBottomGradient(RectTransform parent)
        {
            GameObject gradientObject =
                new GameObject("BottomGradient", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gradientObject.transform.SetParent(parent, false);
            Image gradient = gradientObject.GetComponent<Image>();
            gradient.color = Color.white;
            gradient.raycastTarget = false;
            SetRect(gradient.rectTransform, Vector2.zero, new Vector2(1f, 0.36f),
                Vector2.zero, Vector2.zero, Vector2.zero);
        }

        private static RectTransform CreateDotsRoot(RectTransform parent)
        {
            GameObject root = new GameObject(
                "LoreDots", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.075f, 0.065f), new Vector2(0.32f, 0.12f),
                new Vector2(0f, 0f), Vector2.zero, Vector2.zero);
            HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return rect;
        }

        private static void CreateProgressBar(
            RectTransform parent, out GameObject root, out Image fill)
        {
            root = new GameObject(
                "LoadingProgress", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(parent, false);
            Image background = root.GetComponent<Image>();
            background.color = new Color(0.84f, 0.76f, 0.6f, 0.22f);
            background.raycastTarget = false;
            SetRect(background.rectTransform, new Vector2(0.075f, 0.028f),
                new Vector2(0.925f, 0.028f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(0f, 2f));

            GameObject fillObject =
                new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.transform.SetParent(root.transform, false);
            fill = fillObject.GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            fill.color = new Color(0.72f, 0.59f, 0.4f, 0.96f);
            fill.raycastTarget = false;
            SetRect(fill.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        private static TMP_Text CreateText(
            string name, Transform parent, string value, float fontSize, Color color, TMP_FontAsset font)
        {
            GameObject textObject =
                new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.BottomLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            text.outlineColor = new Color(0.04f, 0.035f, 0.03f, 0.8f);
            text.outlineWidth = 0.12f;
            Shadow shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.02f, 0.02f, 0.02f, 0.88f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
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
