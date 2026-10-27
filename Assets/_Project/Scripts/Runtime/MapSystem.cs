using System;
using System.Collections.Generic;
using Prince;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Prince
{
    [DefaultExecutionOrder(-700)]
    public sealed class MapSystem : MonoBehaviour
    {
        private sealed class MarkerView
        {
            public MapMarker marker;
            public RectTransform rect;
            public Image image;
            public RectTransform fullRect;
            public Image fullImage;
        }

        [Header("Scene references")]
        [SerializeField] private MapArea area;
        [SerializeField] private MapSettings settings;
        [SerializeField] private Camera mapCamera;
        [SerializeField] private RenderTexture minimapTexture;
        [SerializeField] private RenderTexture fullMapTexture;
        [SerializeField] private Shader displayShader;
        [SerializeField] private Material displayMaterial;
        [SerializeField] private Transform player;
        [SerializeField] private QuestManager questManager;
        [SerializeField] private DialogueInputLock dialogueInputLock;
        [SerializeField] private PlayerInteractor playerInteractor;
        [SerializeField] private QuestTrackingHUD trackingHUD;
        [SerializeField] private Sprite[] markerIcons;
        [SerializeField] private Sprite mapFrameSprite;
        [SerializeField] private int hiddenLayer = -1;
        [SerializeField] private int treeLayer = -1;

        private readonly List<MarkerView> markerViews = new List<MarkerView>();
        private readonly Dictionary<MapMarker, MarkerView> markerViewByMarker =
            new Dictionary<MapMarker, MarkerView>();

        private InputAction toggleMapAction;
        private InputAction closeMapAction;
        private InputAction panAction;
        private InputAction zoomInAction;
        private InputAction zoomOutAction;
        private Canvas canvas;
        private RectTransform miniFrame;
        private RectTransform miniImageRect;
        private RawImage miniImage;
        private RectTransform miniMarkerRoot;
        private RectTransform fullPanel;
        private CanvasGroup fullPanelCanvasGroup;
        private RectTransform fullImageRect;
        private RawImage fullImage;
        private RectTransform fullMarkerRoot;
        private RectTransform legendPanel;
        private RectTransform legendRows;
        private RectTransform miniMaskRect;
        private TextMeshProUGUI areaLabel;
        private TextMeshProUGUI areaDescriptionLabel;
        private TextMeshProUGUI fieldAreaNameLabel;
        private TextMeshProUGUI fullTitle;
        private TextMeshProUGUI mapQuestText;
        private TextMeshProUGUI coordinateLabel;
        private TextMeshProUGUI compassNorth;
        private RectTransform playerIcon;
        private Button recenterButton;
        private Button revealAllButton;
        private Material miniMaterial;
        private Material fullMaterial;
        private Sprite circleSprite;
        private Texture2D circleTexture;
        private Sprite fallbackMapFrameSprite;
        private Texture2D fallbackMapFrameTexture;
        private CameraClearFlags previousClearFlags;
        private Color previousBackgroundColor;
        private int previousCullingMask;
        private float previousFarClip;
        private float miniZoom;
        private float fullZoom;
        private Vector3 fullMapCenter;
        private Vector2 pointerStart;
        private bool pointerOverMini;
        private bool draggingMap;
        private bool fullMapOpen;
        private bool wasInteractorEnabled;
        private bool hasLockOwnership;
        private bool activeQuestTargetCached;
        private QuestTarget activeQuestTarget;
        private GameObject pingObject;
        private float nextMapRenderTime;
        private float nextRevealTime;
        private bool warnedMissingReferences;
        private bool warnedMissingMarkerIcon;
        private bool ownsMinimapTexture;
        private bool ownsFullMapTexture;

        private static readonly Color[] DefaultMarkerColors =
        {
            new Color(0.91f, 0.69f, 0.28f),
            new Color(0.85f, 0.79f, 0.64f),
            new Color(0.79f, 0.66f, 0.43f),
            new Color(0.96f, 0.42f, 0.19f),
            new Color(0.65f, 0.22f, 0.18f),
            new Color(0.92f, 0.75f, 0.35f),
            new Color(0.99f, 0.83f, 0.38f),
            new Color(0.75f, 0.66f, 0.49f)
        };

        public bool IsFullMapOpen => fullMapOpen;
        public MapAreaDefinition AreaDefinition => area != null ? area.definition : null;

        private void Awake()
        {
            ResolveReferences();
            ConfigureCamera();
            BuildInterface();
            ConfigureMapDisplay();
            ConfigureCompassVisibility();
            if (questManager != null)
            {
                questManager.OnQuestChanged += OnQuestChanged;
                questManager.OnObjectiveChanged += OnObjectiveChanged;
            }
            if (area != null && area.revealState != null && settings != null)
                area.revealState.Configure(settings.revealResolution);
            miniZoom = settings != null ? Mathf.Clamp(settings.defaultZoom, settings.minimumZoom, settings.maximumZoom) : 40f;
            fullZoom = area != null && area.definition != null
                ? Mathf.Max(area.definition.sizeXZ.x, area.definition.sizeXZ.y) * 0.5f
                : 250f;
            CreateQuestMarkers();
            RefreshLabels();
            RefreshQuestList();
            RevealAroundPlayer();
        }

        private void OnEnable()
        {
            CreateInputActions();
            if (questManager == null)
                questManager = QuestManager.Instance;
            if (questManager != null)
            {
                questManager.OnQuestChanged -= OnQuestChanged;
                questManager.OnObjectiveChanged -= OnObjectiveChanged;
                questManager.OnQuestChanged += OnQuestChanged;
                questManager.OnObjectiveChanged += OnObjectiveChanged;
            }
        }

        private void Start()
        {
            if (!ValidateRuntimeReferences())
                return;
            RefreshMarkerViews();
            if (fullPanel != null)
                fullPanel.localScale = Vector3.one;
        }

        private void OnDisable()
        {
            CloseMap(false);
            if (fullPanel != null)
                fullPanel.gameObject.SetActive(false);
            DisposeInputActions();
            if (questManager != null)
            {
                questManager.OnQuestChanged -= OnQuestChanged;
                questManager.OnObjectiveChanged -= OnObjectiveChanged;
            }
        }

        private void OnDestroy()
        {
            RestoreCamera();
            ReleaseRenderTextures();
            if (miniMaterial != null)
                Destroy(miniMaterial);
            if (fullMaterial != null)
                Destroy(fullMaterial);
            if (pingObject != null)
                Destroy(pingObject);
            if (circleSprite != null)
                Destroy(circleSprite);
            if (circleTexture != null)
                Destroy(circleTexture);
            if (fallbackMapFrameSprite != null)
                Destroy(fallbackMapFrameSprite);
            if (fallbackMapFrameTexture != null)
                Destroy(fallbackMapFrameTexture);
        }

        private void Update()
        {
            UpdateMapPanelFade();
            if (player == null || area == null || area.definition == null || settings == null ||
                mapCamera == null)
                return;

            UpdateMapCamera();
            if (!fullMapOpen && Time.unscaledTime >= nextMapRenderTime)
            {
                mapCamera.Render();
                nextMapRenderTime = Time.unscaledTime + GetMapRefreshInterval();
            }
            if (Time.unscaledTime >= nextRevealTime)
            {
                RevealAroundPlayer();
                nextRevealTime = Time.unscaledTime + Mathf.Max(0.02f, settings.revealInterval);
            }
            UpdateMiniDisplay();
            UpdateMarkers();
            HandleGamepadMapControls();
            UpdateAreaDirection();
        }

        public void RevealAroundPlayer()
        {
            if (area == null || area.revealState == null || area.definition == null ||
                player == null || settings == null || !settings.fogOfWar)
                return;

            area.revealState.Reveal(
                new Vector2(player.position.x, player.position.z),
                settings.revealRadius,
                area.definition);
        }

        public void RevealAll()
        {
            if (area != null && area.revealState != null)
                area.revealState.RevealAll();
        }

        public void ToggleMinimapRotation()
        {
            if (settings == null)
                return;
            settings.rotateMinimap = !settings.rotateMinimap;
            UpdateMapCamera();
        }

        public void ToggleCompassTape()
        {
            if (settings == null)
                return;
            settings.showCompassTape = !settings.showCompassTape;
            ConfigureCompassVisibility();
        }

        public void OpenFullMap()
        {
            if (fullMapOpen || QuestDialogueUI.IsDialogueOpen || !ValidateRuntimeReferences())
                return;

            fullMapOpen = true;
            fullMapCenter = player.position;
            fullMapCenter.y = area.terrain != null ? area.terrain.transform.position.y : player.position.y;
            fullZoom = Mathf.Clamp(
                Mathf.Max(area.definition.sizeXZ.x, area.definition.sizeXZ.y) * 0.5f,
                settings.minimumZoom,
                Mathf.Max(settings.maximumZoom,
                    Mathf.Max(area.definition.sizeXZ.x, area.definition.sizeXZ.y) * 0.5f));
            if (settings.lockInputOnFullMap)
                SetPlayerInputLocked(true);
            if (trackingHUD != null)
                trackingHUD.SetMapOpen(true);
            fullPanel.gameObject.SetActive(true);
            if (settings.openMapSound != null)
                AudioSource.PlayClipAtPoint(settings.openMapSound, player.position, 0.7f);
            RenderFullMap();
            UpdateMarkers();
        }

        public void CloseFullMap()
        {
            CloseMap(true);
        }

        private void CloseMap(bool playSound)
        {
            if (!fullMapOpen)
                return;

            fullMapOpen = false;
            draggingMap = false;
            if (trackingHUD != null)
                trackingHUD.SetMapOpen(false);
            if (settings != null && settings.lockInputOnFullMap)
                SetPlayerInputLocked(false);
            if (playSound && settings != null && settings.closeMapSound != null && player != null)
                AudioSource.PlayClipAtPoint(settings.closeMapSound, player.position, 0.7f);
        }

        private void SetPlayerInputLocked(bool value)
        {
            if (value)
            {
                if (QuestDialogueUI.IsDialogueOpen)
                    return;

                hasLockOwnership = dialogueInputLock != null;
                if (dialogueInputLock != null)
                    dialogueInputLock.SetLocked(true);
                else
                    Debug.LogWarning("MapSystem has no DialogueInputLock; movement, camera, and combat may remain available.", this);

                if (playerInteractor != null)
                {
                    wasInteractorEnabled = playerInteractor.enabled;
                    playerInteractor.enabled = false;
                }
            }
            else
            {
                if (playerInteractor != null)
                    playerInteractor.enabled = wasInteractorEnabled;
                if (hasLockOwnership && dialogueInputLock != null && !QuestDialogueUI.IsDialogueOpen)
                    dialogueInputLock.SetLocked(false);
                hasLockOwnership = false;
            }
        }

        private void ResolveReferences()
        {
            if (area == null)
                area = FindInScene<MapArea>(gameObject.scene);
            if (settings == null)
                settings = Resources.Load<MapSettings>("MapSettings");
            if (mapCamera == null)
                mapCamera = GetComponentInChildren<Camera>(true);
            if (player == null)
            {
                player = FindPlayerInScene(gameObject.scene);
            }
            if (questManager == null)
                questManager = QuestManager.Instance;
            if (player != null)
            {
                dialogueInputLock = dialogueInputLock != null
                    ? dialogueInputLock
                    : player.GetComponent<DialogueInputLock>() ??
                      player.GetComponentInChildren<DialogueInputLock>(true);
                playerInteractor = playerInteractor != null
                    ? playerInteractor
                    : player.GetComponent<PlayerInteractor>() ??
                      player.GetComponentInChildren<PlayerInteractor>(true);
            }
            if (trackingHUD == null)
                trackingHUD = FindInScene<QuestTrackingHUD>(
                    area != null ? area.gameObject.scene : gameObject.scene);
            if (displayShader == null)
                displayShader = Shader.Find("Prince/Map Display");
        }

        private bool ValidateRuntimeReferences()
        {
            if (area != null && area.definition != null && settings != null && mapCamera != null &&
                minimapTexture != null && fullMapTexture != null && player != null)
                return true;
            if (!warnedMissingReferences)
            {
                Debug.LogError(
                    "MapSystem needs a MapArea with a definition, MapSettings, MapCamera, minimap/full-map textures, and PlayerPrince. Run Tools > Prince > Setup Map System.",
                    this);
                warnedMissingReferences = true;
            }
            return false;
        }

        private void ConfigureCamera()
        {
            if (mapCamera == null || settings == null)
                return;

            previousClearFlags = mapCamera.clearFlags;
            previousBackgroundColor = mapCamera.backgroundColor;
            previousCullingMask = mapCamera.cullingMask;
            previousFarClip = mapCamera.farClipPlane;

            mapCamera.enabled = false;
            mapCamera.orthographic = true;
            mapCamera.nearClipPlane = 0.1f;
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = new Color(0.018f, 0.021f, 0.025f, 1f);
            mapCamera.allowHDR = false;
            mapCamera.allowMSAA = false;
            mapCamera.farClipPlane = settings.renderDistance;
            mapCamera.cullingMask = BuildMapCullingMask();
            UniversalAdditionalCameraData additionalData =
                mapCamera.GetComponent<UniversalAdditionalCameraData>();
            if (additionalData == null)
                additionalData = mapCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            additionalData.renderPostProcessing = false;
            additionalData.volumeLayerMask = 0;
            if (minimapTexture != null)
            {
                EnsureRenderTexture(minimapTexture);
                mapCamera.targetTexture = minimapTexture;
            }
        }

        private void RestoreCamera()
        {
            if (mapCamera == null)
                return;
            mapCamera.clearFlags = previousClearFlags;
            mapCamera.backgroundColor = previousBackgroundColor;
            mapCamera.cullingMask = previousCullingMask;
            mapCamera.farClipPlane = previousFarClip;
            mapCamera.targetTexture = null;
        }

        private int BuildMapCullingMask()
        {
            int mask = ~0;
            if (hiddenLayer >= 0 && hiddenLayer < 32)
                mask &= ~(1 << hiddenLayer);
            if (!settings.showTreesOnMap && treeLayer >= 0 && treeLayer < 32)
                mask &= ~(1 << treeLayer);
            mask &= ~(1 << 5);
            return mask;
        }

        private void EnsureRenderTexture(RenderTexture texture)
        {
            if (texture != null && !texture.IsCreated())
            {
                texture.Create();
                if (texture == minimapTexture)
                    ownsMinimapTexture = true;
                if (texture == fullMapTexture)
                    ownsFullMapTexture = true;
            }
        }

        private void ReleaseRenderTextures()
        {
            if (ownsMinimapTexture && minimapTexture != null && minimapTexture.IsCreated())
                minimapTexture.Release();
            if (ownsFullMapTexture && fullMapTexture != null && fullMapTexture.IsCreated())
                fullMapTexture.Release();
            ownsMinimapTexture = false;
            ownsFullMapTexture = false;
        }

        private void UpdateMapCamera()
        {
            if (mapCamera == null || player == null || area == null || area.definition == null || settings == null)
                return;

            bool fullView = fullMapOpen;
            Vector3 center = fullView ? fullMapCenter : player.position;
            float zoom = fullView ? fullZoom : miniZoom;
            float yaw = !fullView && settings.rotateMinimap ? player.eulerAngles.y : 0f;
            float height = area.definition.cameraHeight;
            mapCamera.transform.SetPositionAndRotation(
                new Vector3(center.x, center.y + height, center.z),
                Quaternion.Euler(90f, yaw, 0f));
            mapCamera.orthographicSize = Mathf.Max(1f, zoom * area.definition.mapScale);
            mapCamera.farClipPlane = Mathf.Max(settings.renderDistance, height + 20f);
            mapCamera.cullingMask = BuildMapCullingMask();
        }

        private void RenderFullMap()
        {
            if (mapCamera == null || fullMapTexture == null)
                return;

            EnsureRenderTexture(fullMapTexture);
            RenderTexture previousTarget = mapCamera.targetTexture;
            bool previousEnabled = mapCamera.enabled;
            mapCamera.targetTexture = fullMapTexture;
            mapCamera.enabled = false;
            UpdateMapCamera();
            mapCamera.Render();
            mapCamera.targetTexture = previousTarget;
            mapCamera.enabled = previousEnabled;
            if (fullImage != null)
                fullImage.texture = fullMapTexture;
            UpdateDisplayMaterials();
        }

        private void UpdateMiniDisplay()
        {
            if (miniImage == null || miniMaterial == null)
                return;
            miniImage.color = Color.white * (settings != null ? settings.minimapOpacity : 1f);
            UpdateDisplayMaterials();
        }

        private void ConfigureMapDisplay()
        {
            if (displayMaterial == null && displayShader == null)
            {
                Debug.LogError("Shader 'Prince/Map Display' is missing. Map surfaces will use the plain RenderTexture.", this);
                return;
            }
            Material source = displayMaterial != null ? displayMaterial : new Material(displayShader);
            miniMaterial = new Material(source) { name = "Map Minimap Material" };
            fullMaterial = new Material(source) { name = "Map Fullscreen Material" };
            if (displayMaterial == null)
                Destroy(source);
            if (miniImage != null)
                miniImage.material = miniMaterial;
            if (fullImage != null)
                fullImage.material = fullMaterial;
            UpdateDisplayMaterials();
        }

        private void UpdateDisplayMaterials()
        {
            if (area == null || area.definition == null || settings == null)
                return;
            UpdateDisplayMaterial(miniMaterial, false);
            UpdateDisplayMaterial(fullMaterial, true);
        }

        private void UpdateDisplayMaterial(Material material, bool full)
        {
            if (material == null || area == null || area.definition == null)
                return;

            bool showFog = settings.fogOfWar && area.revealState != null;
            Texture fog = showFog ? area.revealState.RevealTexture : Texture2D.whiteTexture;
            float zoom = full ? fullZoom : miniZoom;
            Vector3 center = full ? fullMapCenter : player != null ? player.position : Vector3.zero;
            float yaw = !full && settings.rotateMinimap && player != null ? player.eulerAngles.y : 0f;
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.forward;
            MapAreaDefinition definition = area.definition;

            material.SetTexture("_FogTex", fog);
            material.SetColor("_Tint", definition.parchmentTint);
            material.SetColor("_FogColor", new Color(0.12f, 0.105f, 0.085f, 1f));
            material.SetFloat("_FogEnabled", showFog ? 1f : 0f);
            material.SetFloat("_CircleClip", full ? 0f : 1f);
            material.SetFloat("_SepiaStrength", 0.04f);
            material.SetFloat("_TintStrength", settings.mapTintStrength);
            material.SetFloat("_WorldSpan", Mathf.Max(1f, zoom * 2f * definition.mapScale));
            material.SetVector("_WorldCenter", new Vector4(center.x, 0f, center.z, 0f));
            material.SetVector("_WorldRight", new Vector4(right.x, 0f, right.z, 0f));
            material.SetVector("_WorldUp", new Vector4(up.x, 0f, up.z, 0f));
            material.SetVector("_AreaBounds", new Vector4(
                definition.centerXZ.x - definition.sizeXZ.x * 0.5f,
                definition.centerXZ.y - definition.sizeXZ.y * 0.5f,
                definition.sizeXZ.x,
                definition.sizeXZ.y));
            material.SetFloat("_Vignette", settings.mapVignetteStrength * (full ? 1f : 0.8f));
        }

        private void BuildInterface()
        {
            GameObject canvasObject = new GameObject("MapCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            EnsureEventSystem();

            Color gold = area != null && area.definition != null
                ? area.definition.accentColor
                : new Color(0.68f, 0.5f, 0.26f, 1f);
            float size = settings != null ? settings.minimapSize : 270f;
            float margin = settings != null ? settings.minimapEdgeMargin : 24f;
            circleSprite = CreateCircleSprite(out circleTexture);
            miniFrame = CreateRect("Minimap Metal Bezel", canvas.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-margin, -margin));
            miniFrame.pivot = Vector2.one;
            miniFrame.sizeDelta = Vector2.one * (size + 38f);
            CreateCircleImage("Minimap Outer Metal Ring", miniFrame, size + 38f,
                new Color(0.12f, 0.115f, 0.1f, 1f), circleSprite);
            CreateCircleImage("Minimap Brass Ring", miniFrame, size + 30f,
                new Color(0.48f, 0.37f, 0.22f, 1f), circleSprite);
            CreateCircleImage("Minimap Dark Inner Rim", miniFrame, size + 22f,
                new Color(0.045f, 0.04f, 0.035f, 1f), circleSprite);
            miniMaskRect = CreateRect("Minimap Circular Mask", miniFrame,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            miniMaskRect.sizeDelta = Vector2.one * size;
            Image maskImage = miniMaskRect.gameObject.AddComponent<Image>();
            maskImage.sprite = circleSprite;
            maskImage.preserveAspect = true;
            maskImage.raycastTarget = false;
            Mask circleMask = miniMaskRect.gameObject.AddComponent<Mask>();
            circleMask.showMaskGraphic = false;
            miniImageRect = CreateRect("Minimap Image", miniMaskRect, Vector2.zero, Vector2.one, Vector2.zero);
            miniImageRect.sizeDelta = Vector2.zero;
            miniImage = miniImageRect.gameObject.AddComponent<RawImage>();
            if (miniImage == null)
            {
                Debug.LogError("MapSystem could not create the minimap RawImage. Verify the Unity UI package is available.", this);
                return;
            }
            miniImage.texture = minimapTexture;
            miniImage.raycastTarget = true;
            miniImageRect.gameObject.AddComponent<MapUiSurface>().Initialize(this, MapUiSurface.Surface.Minimap);
            miniMarkerRoot = CreateRect("Minimap Markers", miniImageRect, Vector2.zero, Vector2.one, Vector2.zero);
            areaLabel = CreateText("Area Name", miniFrame, string.Empty, 16, gold,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -14f), new Vector2(size + 50f, 26f));
            areaLabel.characterSpacing = 1f;
            compassNorth = CreateText("Minimap North", miniFrame, "N", 16, gold,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(28f, 22f));
            playerIcon = CreateRect("Player Direction", miniImageRect,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            Image playerImage = playerIcon.gameObject.AddComponent<Image>();
            playerImage.sprite = GetMarkerSprite(MapMarkerType.PlayerPing);
            playerImage.color = gold;
            playerImage.raycastTarget = false;
            playerImage.preserveAspect = true;
            playerIcon.sizeDelta = new Vector2(18f, 22f);

            BuildFullMapInterface();
            if (fullPanel != null)
                fullPanel.gameObject.SetActive(false);
        }

        private void BuildFullMapInterface()
        {
            fullPanel = CreatePanel("Full Map Panel", canvas.transform,
                new Color(0.035f, 0.028f, 0.023f, 0.97f),
                Vector2.zero, Vector2.one, Vector2.zero);
            fullPanelCanvasGroup = fullPanel.gameObject.AddComponent<CanvasGroup>();
            fullPanelCanvasGroup.alpha = 0f;
            fullPanelCanvasGroup.interactable = false;
            fullPanelCanvasGroup.blocksRaycasts = false;
            fullTitle = CreateText("Map Area Title", fullPanel, string.Empty, 30,
                new Color(0.86f, 0.76f, 0.58f, 1f),
                new Vector2(0.38f, 0.94f), new Vector2(0.38f, 0.94f),
                Vector2.zero, new Vector2(780f, 48f));
            fullTitle.characterSpacing = 1.2f;
            fullTitle.fontStyle = FontStyles.Bold;
            fullTitle.alignment = TextAlignmentOptions.Center;

            RectTransform mapFrameOuter = CreatePanel("Map Frame Outer", fullPanel,
                new Color(0.09f, 0.075f, 0.06f, 1f),
                new Vector2(0.38f, 0.5f), new Vector2(0.38f, 0.5f), new Vector2(790f, 790f));
            if (mapFrameSprite == null)
                mapFrameSprite = CreateFallbackMapFrameSprite();
            Image frameImage = mapFrameOuter.GetComponent<Image>();
            frameImage.sprite = mapFrameSprite;
            frameImage.type = Image.Type.Sliced;
            frameImage.preserveAspect = false;
            frameImage.color = Color.white;
            frameImage.raycastTarget = false;
            RectTransform mapSurface = CreateRect("Full Map Surface", mapFrameOuter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            mapSurface.sizeDelta = new Vector2(730f, 730f);
            fullImageRect = mapSurface;
            fullImage = mapSurface.gameObject.AddComponent<RawImage>();
            if (fullImage == null)
            {
                Debug.LogError("MapSystem could not create the full-map RawImage. Verify the Unity UI package is available.", this);
                return;
            }
            fullImage.texture = fullMapTexture;
            fullImage.raycastTarget = true;
            fullImageRect.gameObject.AddComponent<MapUiSurface>().Initialize(this, MapUiSurface.Surface.FullMap);
            fullImageRect.gameObject.AddComponent<RectMask2D>();
            fullMarkerRoot = CreateRect("Full Map Markers", fullImageRect, Vector2.zero, Vector2.one, Vector2.zero);
            coordinateLabel = CreateText("Map Controls", fullPanel,
                "M / Esc / B: Tutup  |  Scroll / LB-RB: Zoom  |  Seret / Stick: Geser  |  Klik kanan: Tandai",
                16, new Color(0.66f, 0.63f, 0.56f, 1f),
                new Vector2(0.38f, 0.08f), new Vector2(0.38f, 0.08f),
                Vector2.zero, new Vector2(790f, 30f));

            legendPanel = CreatePanel("Field Notes Panel", fullPanel, new Color(0.055f, 0.05f, 0.043f, 0.94f),
                new Vector2(0.81f, 0.5f), new Vector2(0.81f, 0.5f), new Vector2(390f, 850f));
            Image panelImage = legendPanel.GetComponent<Image>();
            panelImage.raycastTarget = false;
            RectTransform content = CreateRect("Field Notes Content", legendPanel, Vector2.zero, Vector2.one, Vector2.zero);
            content.offsetMin = new Vector2(18f, 18f);
            content.offsetMax = new Vector2(-18f, -18f);
            VerticalLayoutGroup panelLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(0, 0, 0, 0);
            panelLayout.spacing = 8f;
            panelLayout.childAlignment = TextAnchor.UpperLeft;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            TextMeshProUGUI panelTitle = CreateLayoutText("Field Notes Title", content,
                "CATATAN LAPANGAN", 17, new Color(0.73f, 0.61f, 0.4f, 1f), 24f);
            panelTitle.fontStyle = FontStyles.Bold;
            panelTitle.characterSpacing = 1.3f;
            CreateDivider(content);

            fieldAreaNameLabel = CreateLayoutText("Field Notes Area", content,
                string.Empty, 24, new Color(0.87f, 0.79f, 0.64f, 1f), 34f);
            fieldAreaNameLabel.fontStyle = FontStyles.Bold;
            fieldAreaNameLabel.characterSpacing = 0.5f;
            areaDescriptionLabel = CreateLayoutText("Field Notes Description", content,
                string.Empty, 15, new Color(0.72f, 0.69f, 0.61f, 1f), 50f);
            areaDescriptionLabel.enableWordWrapping = true;
            areaDescriptionLabel.alignment = TextAlignmentOptions.TopLeft;
            CreateDivider(content);

            TextMeshProUGUI questHeading = CreateLayoutText("Active Quests Title", content,
                "MISI AKTIF", 18, new Color(0.83f, 0.72f, 0.52f, 1f), 28f);
            questHeading.fontStyle = FontStyles.Bold;
            mapQuestText = CreateLayoutText("Active Quest Notes", content, string.Empty,
                16, new Color(0.82f, 0.78f, 0.69f, 1f), 170f);
            mapQuestText.alignment = TextAlignmentOptions.TopLeft;
            mapQuestText.enableWordWrapping = true;
            CreateDivider(content);

            TextMeshProUGUI legendHeading = CreateLayoutText("Legend Title", content,
                "LEGENDA", 18, new Color(0.83f, 0.72f, 0.52f, 1f), 28f);
            legendHeading.fontStyle = FontStyles.Bold;
            legendRows = CreateRect("Legend Rows", content,
                new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero);
            legendRows.sizeDelta = new Vector2(0f, 204f);
            LayoutElement legendRowsLayout = legendRows.gameObject.AddComponent<LayoutElement>();
            legendRowsLayout.preferredHeight = 204f;
            VerticalLayoutGroup rowsLayout = legendRows.gameObject.AddComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 6f;
            rowsLayout.childAlignment = TextAnchor.UpperLeft;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;
            CreateLegendRow(MapMarkerType.Waypoint, "Titik Jalan", legendRows);
            CreateLegendRow(MapMarkerType.Enemy, "Musuh", legendRows);
            CreateLegendRow(MapMarkerType.Campfire, "Kemah", legendRows);
            CreateLegendRow(MapMarkerType.PlayerPing, "Pemain", legendRows);
            CreateLegendRow(MapMarkerType.NPC, "NPC", legendRows);
            CreateLegendRow(MapMarkerType.Quest, "Misi", legendRows);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            recenterButton = CreateButton("Recenter Map", "KEMBALI KE PANGERAN", content, 40f);
            recenterButton.onClick.AddListener(RecenterFullMap);
            revealAllButton = CreateButton("Reveal All", "UNGKAP SEMUA  [DEBUG]", content, 36f);
            revealAllButton.onClick.AddListener(RevealAll);
#endif
            fullPanel.localScale = Vector3.one;
        }

        private Sprite CreateFallbackMapFrameSprite()
        {
            const int resolution = 128;
            const int border = 12;
            fallbackMapFrameTexture = new Texture2D(
                resolution, resolution, TextureFormat.RGBA32, false, true)
            {
                name = "Runtime Map Frame Fallback",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32[] pixels = new Color32[resolution * resolution];
            Color32 transparent = new Color32(0, 0, 0, 0);
            Color32[] edgeColors =
            {
                new Color32(24, 19, 14, 255),
                new Color32(43, 32, 22, 255),
                new Color32(75, 57, 36, 255),
                new Color32(119, 91, 54, 255),
                new Color32(58, 44, 30, 255),
                new Color32(38, 30, 22, 255),
                new Color32(55, 43, 29, 255),
                new Color32(67, 53, 36, 255),
                new Color32(53, 42, 29, 255),
                new Color32(43, 35, 26, 255),
                new Color32(76, 59, 37, 255),
                new Color32(31, 25, 19, 255)
            };

            for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
            {
                int edge = Mathf.Min(
                    Mathf.Min(x, y),
                    Mathf.Min(resolution - 1 - x, resolution - 1 - y));
                pixels[y * resolution + x] = edge < border
                    ? edgeColors[edge]
                    : transparent;
            }

            fallbackMapFrameTexture.SetPixels32(pixels);
            fallbackMapFrameTexture.Apply(false, true);
            fallbackMapFrameSprite = Sprite.Create(
                fallbackMapFrameTexture,
                new Rect(0f, 0f, resolution, resolution),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            fallbackMapFrameSprite.name = "Runtime Map Frame Fallback";
            fallbackMapFrameSprite.hideFlags = HideFlags.HideAndDontSave;
            return fallbackMapFrameSprite;
        }

        private void CreateLegendRow(MapMarkerType type, string label, Transform parent)
        {
            RectTransform row = CreateRect("Legend Row " + label, parent,
                new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero);
            row.sizeDelta = new Vector2(0f, 28f);
            LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 28f;
            rowLayout.minHeight = 28f;
            HorizontalLayoutGroup horizontal = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 6f;
            horizontal.childAlignment = TextAnchor.MiddleLeft;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;

            RectTransform iconRect = CreateRect("Legend Icon " + label, row,
                new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero);
            iconRect.sizeDelta = new Vector2(20f, 20f);
            LayoutElement iconLayout = iconRect.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 20f;
            iconLayout.preferredHeight = 20f;
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = GetMarkerSprite(type);
            icon.color = MarkerColor(type);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            TextMeshProUGUI text = CreateText("Legend Label " + label, row, label, 17,
                new Color(0.78f, 0.72f, 0.61f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(250f, 28f));
            LayoutElement textLayout = text.gameObject.AddComponent<LayoutElement>();
            textLayout.preferredWidth = 250f;
            textLayout.preferredHeight = 28f;
            textLayout.flexibleWidth = 1f;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableWordWrapping = false;
        }

        private void CreateQuestMarkers()
        {
            if (questManager == null)
                questManager = QuestManager.Instance;

            QuestTarget[] targets = FindObjectsByType<QuestTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (QuestTarget target in targets)
            {
                if (target == null || area == null || target.gameObject.scene != area.gameObject.scene)
                    continue;
                MapMarker marker = target.GetComponent<MapMarker>();
                if (marker == null)
                    marker = target.gameObject.AddComponent<MapMarker>();
                if (target.GetComponentInParent<NPCInfo>() != null)
                    marker.type = MapMarkerType.NPC;
                else if (target.GetComponentInParent<QuestPickup>() != null)
                    marker.type = MapMarkerType.Pickup;
                marker.displayName = string.IsNullOrWhiteSpace(marker.displayName)
                    ? target.displayName
                    : marker.displayName;
                if (marker.icon == null)
                    marker.icon = GetMarkerSprite(marker.type);
                marker.color = MarkerColor(marker.type);
            }

            NPCInfo[] npcs = FindObjectsByType<NPCInfo>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (NPCInfo npc in npcs)
            {
                if (npc == null || area == null || npc.gameObject.scene != area.gameObject.scene ||
                    npc.GetComponentInChildren<MapMarker>(true) != null)
                    continue;
                MapMarker marker = npc.gameObject.AddComponent<MapMarker>();
                marker.type = MapMarkerType.NPC;
                marker.displayName = npc.DisplayName;
                marker.color = MarkerColor(MapMarkerType.NPC);
                marker.icon = GetMarkerSprite(MapMarkerType.NPC);
            }
        }

        private void RefreshMarkerViews()
        {
            IReadOnlyList<MapMarker> markers = MapMarker.Active;
            for (int i = 0; i < markers.Count; i++)
            {
                MapMarker marker = markers[i];
                if (marker == null || marker.transform == transform || marker.GetComponentInParent<MapSystem>() != null)
                    continue;
                if (area != null && marker.gameObject.scene != area.gameObject.scene)
                    continue;
                if (markerViewByMarker.ContainsKey(marker))
                    continue;
                MarkerView view = CreateMarkerView(marker);
                markerViews.Add(view);
                markerViewByMarker.Add(marker, view);
            }
        }

        private MarkerView CreateMarkerView(MapMarker marker)
        {
            GameObject iconObject = new GameObject(
                marker.DisplayName + " Map Icon",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(miniMarkerRoot, false);
            RectTransform miniRect = iconObject.GetComponent<RectTransform>();
            miniRect.anchorMin = miniRect.anchorMax = new Vector2(0.5f, 0.5f);
            miniRect.sizeDelta = new Vector2(22f, 22f);
            Image miniIcon = iconObject.GetComponent<Image>();
            miniIcon.raycastTarget = false;
            miniIcon.preserveAspect = true;
            miniIcon.sprite = marker.icon != null ? marker.icon : GetMarkerSprite(marker.type);
            miniIcon.color = marker.color == Color.white ? MarkerColor(marker.type) : marker.color;

            GameObject fullObject = new GameObject(
                marker.DisplayName + " Map Icon",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fullObject.transform.SetParent(fullMarkerRoot, false);
            RectTransform fullRect = fullObject.GetComponent<RectTransform>();
            fullRect.anchorMin = fullRect.anchorMax = new Vector2(0.5f, 0.5f);
            fullRect.sizeDelta = new Vector2(26f, 26f);
            Image fullIcon = fullObject.GetComponent<Image>();
            fullIcon.raycastTarget = true;
            fullIcon.preserveAspect = true;
            fullIcon.sprite = marker.icon != null ? marker.icon : GetMarkerSprite(marker.type);
            fullIcon.color = miniIcon.color;
            fullObject.SetActive(false);

            TextMeshProUGUI label = CreateText(
                marker.DisplayName + " Tooltip", fullObject.transform, marker.DisplayName, 15,
                new Color(0.95f, 0.87f, 0.7f, 1f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, -20f), new Vector2(220f, 26f));
            label.gameObject.SetActive(false);
            MapMarkerTooltip markerTooltip = fullObject.AddComponent<MapMarkerTooltip>();
            markerTooltip.Initialize(this, marker, label);

            return new MarkerView
            {
                marker = marker,
                rect = miniRect,
                image = miniIcon,
                fullRect = fullRect,
                fullImage = fullIcon
            };
        }

        private void UpdateMarkers()
        {
            if (miniMarkerRoot == null || fullMarkerRoot == null || area == null ||
                area.definition == null || settings == null)
                return;
            RefreshMarkerViewsIfNeeded();
            float minimapSpan = miniZoom * 2f * area.definition.mapScale;
            float minimapRadius = settings.minimapSize * 0.5f - 14f;
            Quaternion miniRotation = Quaternion.Euler(0f,
                settings.rotateMinimap && player != null ? player.eulerAngles.y : 0f, 0f);
            Vector3 miniRight = miniRotation * Vector3.right;
            Vector3 miniUp = miniRotation * Vector3.forward;
            Vector3 miniCenter = player != null ? player.position : Vector3.zero;
            Vector3 fullRight = Vector3.right;
            Vector3 fullUp = Vector3.forward;
            float fullSpan = fullZoom * 2f * area.definition.mapScale;
            Vector2 fullSize = fullImageRect != null ? fullImageRect.rect.size : Vector2.one;
            QuestTarget activeQuestTarget = GetActiveQuestTarget();

            for (int i = 0; i < markerViews.Count; i++)
            {
                MarkerView view = markerViews[i];
                if (view.marker == null)
                {
                    view.rect.gameObject.SetActive(false);
                    continue;
                }

                MapMarker marker = view.marker;
                if (!marker.isActiveAndEnabled)
                {
                    view.rect.gameObject.SetActive(false);
                    continue;
                }
                bool isQuestTarget = activeQuestTarget != null &&
                    marker.transform == activeQuestTarget.transform;
                bool discovered = !settings.fogOfWar || marker.visibleThroughFog ||
                    (area.revealState != null &&
                     area.revealState.IsPositionRevealed(
                         new Vector2(marker.transform.position.x, marker.transform.position.z),
                         area.definition));
                bool visible = discovered || isQuestTarget || marker.type == MapMarkerType.PlayerPing;

                Vector3 miniDelta = marker.transform.position - miniCenter;
                float miniX = Vector3.Dot(miniDelta, miniRight) / Mathf.Max(1f, minimapSpan) * settings.minimapSize;
                float miniY = Vector3.Dot(miniDelta, miniUp) / Mathf.Max(1f, minimapSpan) * settings.minimapSize;
                Vector2 miniPosition = new Vector2(miniX, miniY);
                bool beyondRange = miniPosition.magnitude > minimapRadius;
                if (beyondRange)
                    miniPosition = miniPosition.normalized * minimapRadius;
                view.rect.anchoredPosition = miniPosition;
                view.rect.sizeDelta = Vector2.one * (isQuestTarget ? 18f : beyondRange ? 14f : 16f);
                Sprite displayIcon = GetMarkerSprite(isQuestTarget ? MapMarkerType.Quest : marker.type);
                Color displayColor = isQuestTarget
                    ? new Color(1f, 0.78f, 0.28f, 1f)
                    : marker.color == Color.white ? MarkerColor(marker.type) : marker.color;
                view.image.sprite = marker.icon != null && !isQuestTarget
                    ? marker.icon
                    : displayIcon;
                Color miniColor = displayColor;
                if (beyondRange)
                    miniColor.a = 0.52f;
                view.image.color = miniColor;
                view.rect.gameObject.SetActive(visible);
                if (isQuestTarget)
                {
                    float pulse = 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.12f;
                    view.rect.localScale = Vector3.one * pulse;
                    if (view.fullRect != null)
                        view.fullRect.localScale = Vector3.one * pulse;
                }
                else
                {
                    view.rect.localScale = Vector3.one;
                    if (view.fullRect != null)
                        view.fullRect.localScale = Vector3.one;
                }

                Vector3 fullDelta = marker.transform.position - fullMapCenter;
                float fullX = Vector3.Dot(fullDelta, fullRight) / Mathf.Max(1f, fullSpan) * fullSize.x;
                float fullY = Vector3.Dot(fullDelta, fullUp) / Mathf.Max(1f, fullSpan) * fullSize.y;
                RectTransform fullRect = view.fullRect;
                if (fullRect != null)
                {
                    fullRect.anchoredPosition = new Vector2(fullX, fullY);
                    fullRect.sizeDelta = Vector2.one * (isQuestTarget ? 28f : 24f);
                    fullRect.gameObject.SetActive(fullMapOpen && visible);
                    if (view.fullImage != null)
                    {
                        view.fullImage.sprite = view.image.sprite;
                        Color fullColor = displayColor;
                        fullColor.a = discovered || isQuestTarget ? 1f : 0.7f;
                        view.fullImage.color = fullColor;
                    }
                }
            }

            if (playerIcon != null && player != null)
            {
                playerIcon.localEulerAngles = new Vector3(
                    0f, 0f,
                    -Mathf.DeltaAngle(settings.rotateMinimap ? player.eulerAngles.y : 0f, player.eulerAngles.y));
            }
        }

        private void RefreshMarkerViewsIfNeeded()
        {
            IReadOnlyList<MapMarker> currentMarkers = MapMarker.Active;
            for (int i = 0; i < currentMarkers.Count; i++)
            {
                MapMarker marker = currentMarkers[i];
                if (marker != null && marker.transform != transform &&
                    (area == null || marker.gameObject.scene == area.gameObject.scene) &&
                    !markerViewByMarker.ContainsKey(marker))
                {
                    MarkerView view = CreateMarkerView(marker);
                    markerViews.Add(view);
                    markerViewByMarker.Add(marker, view);
                }
            }
        }

        private QuestTarget GetActiveQuestTarget()
        {
            if (activeQuestTargetCached)
                return activeQuestTarget;

            activeQuestTargetCached = true;
            if (questManager == null)
                questManager = QuestManager.Instance;
            if (questManager == null)
                return null;

            IReadOnlyList<QuestObjectiveProgress> objectives = questManager.GetActiveObjectives();
            for (int i = 0; i < objectives.Count; i++)
                if (questManager.TryGetTarget(objectives[i].objective.targetId, out QuestTarget target))
                {
                    activeQuestTarget = target;
                    break;
                }
            return activeQuestTarget;
        }

        internal string GetMarkerTooltip(MapMarker marker)
        {
            if (marker == null)
                return string.Empty;

            var lines = new List<string> { marker.DisplayName };
            if (player != null)
            {
                int distance = Mathf.RoundToInt(Vector3.Distance(player.position, marker.transform.position));
                lines.Add(distance + " m");
            }
            if (questManager == null)
                questManager = QuestManager.Instance;
            if (questManager != null && !string.IsNullOrWhiteSpace(marker.TargetId))
            {
                IReadOnlyList<QuestObjectiveProgress> objectives = questManager.GetActiveObjectives();
                for (int i = 0; i < objectives.Count; i++)
                {
                    QuestObjectiveProgress active = objectives[i];
                    if (active.objective.targetId != marker.TargetId)
                        continue;
                    lines.Add(active.quest.title + "\n" + active.objective.description);
                    break;
                }
            }
            return string.Join("\n", lines);
        }

        private static Transform FindPlayerInScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "PlayerPrince" || root.CompareTag("Player"))
                    return root.transform;
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == "PlayerPrince" || child.CompareTag("Player"))
                        return child;
            }
            return null;
        }

        private void RefreshLabels()
        {
            string name = area != null && area.definition != null
                ? area.definition.areaName
                : gameObject.scene.name;
            if (string.IsNullOrWhiteSpace(name))
                name = gameObject.scene.name;
            if (string.IsNullOrWhiteSpace(name))
                name = "Wilayah";
            string description = area != null && area.definition != null &&
                !string.IsNullOrWhiteSpace(area.definition.areaDescription)
                ? area.definition.areaDescription
                : "Wilayah ini menanti untuk dijelajahi.";
            if (areaLabel != null)
                areaLabel.text = name.ToUpperInvariant();
            if (fullTitle != null)
                fullTitle.text = name.ToUpperInvariant();
            if (fieldAreaNameLabel != null)
                fieldAreaNameLabel.text = name;
            if (areaDescriptionLabel != null)
                areaDescriptionLabel.text = description;
        }

        private void RefreshQuestList()
        {
            if (mapQuestText == null)
                return;

            if (questManager == null)
                questManager = QuestManager.Instance;
            IReadOnlyList<QuestObjectiveProgress> objectives = questManager != null
                ? questManager.GetActiveObjectives()
                : Array.Empty<QuestObjectiveProgress>();
            if (objectives.Count == 0)
            {
                mapQuestText.text = "Belum ada misi";
                return;
            }

            var entries = new List<string>();
            for (int i = 0; i < objectives.Count; i++)
            {
                QuestObjectiveProgress active = objectives[i];
                string title = active.quest != null ? active.quest.title : "Misi";
                string objective = active.objective != null ? active.objective.description : string.Empty;
                entries.Add($"<b><color=#E5C77D>{title}</color></b>\n<color=#CFC7B7>- {objective}</color>");
            }
            mapQuestText.text = string.Join("\n\n", entries);
        }

        private void UpdateAreaDirection()
        {
            if (compassNorth == null || player == null)
                return;
            compassNorth.rectTransform.localEulerAngles = new Vector3(
                0f, 0f, settings != null && settings.rotateMinimap ? player.eulerAngles.y : 0f);
        }

        private float GetMapRefreshInterval()
        {
            if (settings == null)
                return 0.1f;
            switch (settings.quality)
            {
                case MapQuality.Low: return 0.16f;
                case MapQuality.High: return 0.04f;
                default: return 0.08f;
            }
        }

        private void UpdateMapPanelFade()
        {
            if (fullPanel == null || fullPanelCanvasGroup == null)
                return;

            float targetAlpha = fullMapOpen ? 1f : 0f;
            fullPanelCanvasGroup.alpha = Mathf.MoveTowards(
                fullPanelCanvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 6f);
            fullPanelCanvasGroup.interactable = fullMapOpen && fullPanelCanvasGroup.alpha >= 0.98f;
            fullPanelCanvasGroup.blocksRaycasts = fullMapOpen && fullPanelCanvasGroup.alpha > 0.02f;
            if (!fullMapOpen && fullPanelCanvasGroup.alpha <= 0.001f && fullPanel.gameObject.activeSelf)
                fullPanel.gameObject.SetActive(false);
        }

        private static T FindInScene<T>(UnityEngine.SceneManagement.Scene scene)
            where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            T[] components = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (T component in components)
                if (component != null && component.gameObject.scene == scene)
                    return component;
            return null;
        }

        private void ConfigureCompassVisibility()
        {
            if (settings == null)
                return;
            QuestTrackingHUD hud = trackingHUD != null
                ? trackingHUD
                : FindInScene<QuestTrackingHUD>(area != null ? area.gameObject.scene : gameObject.scene);
            if (hud == null)
                return;
            Transform[] children = hud.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
                if (child.name == "QuestCompass")
                    child.gameObject.SetActive(settings.showCompassTape);
        }

        private void RecenterFullMap()
        {
            if (player == null)
                return;
            fullMapCenter = player.position;
            RenderFullMap();
            UpdateMarkers();
        }

        private void ChangeZoom(float amount, bool fullMap)
        {
            if (settings == null)
                return;
            if (fullMap && fullMapOpen)
            {
                float areaZoomLimit = area != null && area.definition != null
                    ? Mathf.Max(area.definition.sizeXZ.x, area.definition.sizeXZ.y) * 0.5f
                    : settings.maximumZoom;
                fullZoom = Mathf.Clamp(
                    fullZoom - amount,
                    settings.minimumZoom,
                    Mathf.Max(settings.maximumZoom, areaZoomLimit));
                RenderFullMap();
            }
            else if (!fullMap && pointerOverMini)
                miniZoom = Mathf.Clamp(miniZoom - amount, settings.minimumZoom, settings.maximumZoom);
        }

        private void HandleGamepadMapControls()
        {
            if (!fullMapOpen || panAction == null)
                return;

            Vector2 pan = panAction.ReadValue<Vector2>();
            if (pan.sqrMagnitude > 0.01f)
            {
                Vector3 movement = new Vector3(pan.x, 0f, pan.y) *
                    fullZoom * 1.15f * Time.unscaledDeltaTime;
                fullMapCenter += movement;
                ClampFullMapCenter();
                RenderFullMap();
            }
        }

        private void ClampFullMapCenter()
        {
            if (area == null || area.definition == null)
                return;
            float halfX = Mathf.Max(0f, area.definition.sizeXZ.x * 0.5f - fullZoom);
            float halfZ = Mathf.Max(0f, area.definition.sizeXZ.y * 0.5f - fullZoom);
            fullMapCenter.x = Mathf.Clamp(
                fullMapCenter.x, area.definition.centerXZ.x - halfX, area.definition.centerXZ.x + halfX);
            fullMapCenter.z = Mathf.Clamp(
                fullMapCenter.z, area.definition.centerXZ.y - halfZ, area.definition.centerXZ.y + halfZ);
        }

        internal void OnPointerEnterMap(MapUiSurface.Surface surface)
        {
            pointerOverMini = surface == MapUiSurface.Surface.Minimap;
        }

        internal void OnPointerExitMap(MapUiSurface.Surface surface)
        {
            if (surface == MapUiSurface.Surface.Minimap)
                pointerOverMini = false;
        }

        internal void OnMapScroll(PointerEventData eventData, MapUiSurface.Surface surface)
        {
            if (settings == null)
                return;
            float scroll = eventData.scrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f)
                return;
            ChangeZoom(Mathf.Sign(scroll) * settings.zoomStep, surface == MapUiSurface.Surface.FullMap);
            if (surface == MapUiSurface.Surface.FullMap)
                UpdateMarkers();
        }

        internal void OnMapPointerDown(PointerEventData eventData, MapUiSurface.Surface surface)
        {
            if (surface == MapUiSurface.Surface.FullMap && fullMapOpen &&
                eventData.button == PointerEventData.InputButton.Left)
            {
                draggingMap = true;
                pointerStart = eventData.position;
            }
            else if (surface == MapUiSurface.Surface.FullMap && fullMapOpen &&
                eventData.button == PointerEventData.InputButton.Right)
                TogglePlayerPing(eventData);
        }

        internal void OnMapDrag(PointerEventData eventData, MapUiSurface.Surface surface)
        {
            if (!fullMapOpen || !draggingMap || surface != MapUiSurface.Surface.FullMap ||
                fullImageRect == null || area == null || area.definition == null)
                return;

            Vector2 delta = eventData.position - pointerStart;
            pointerStart = eventData.position;
            float worldPerPixel = fullZoom * 2f * area.definition.mapScale /
                Mathf.Max(1f, fullImageRect.rect.width);
            fullMapCenter.x -= delta.x * worldPerPixel;
            fullMapCenter.z -= delta.y * worldPerPixel;
            ClampFullMapCenter();
            RenderFullMap();
            UpdateMarkers();
        }

        internal void OnMapPointerUp(PointerEventData eventData, MapUiSurface.Surface surface)
        {
            if (surface == MapUiSurface.Surface.FullMap)
                draggingMap = false;
        }

        private void TogglePlayerPing(PointerEventData eventData)
        {
            if (fullImageRect == null || area == null || area.definition == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    fullImageRect, eventData.position, eventData.pressEventCamera, out Vector2 local))
                return;

            Rect rect = fullImageRect.rect;
            Vector2 uv = new Vector2(
                Mathf.InverseLerp(rect.xMin, rect.xMax, local.x),
                Mathf.InverseLerp(rect.yMin, rect.yMax, local.y));
            Vector3 world = new Vector3(
                fullMapCenter.x + (uv.x - 0.5f) * fullZoom * 2f * area.definition.mapScale,
                player != null ? player.position.y : 0f,
                fullMapCenter.z + (uv.y - 0.5f) * fullZoom * 2f * area.definition.mapScale);

            if (pingObject != null)
            {
                Vector2 prior = new Vector2(pingObject.transform.position.x, pingObject.transform.position.z);
                Vector2 clicked = new Vector2(world.x, world.z);
                if (Vector2.Distance(prior, clicked) < fullZoom * 0.04f)
                {
                    Destroy(pingObject);
                    pingObject = null;
                    RefreshMarkerViewsIfNeeded();
                    return;
                }
                Destroy(pingObject);
            }

            pingObject = new GameObject("Map Player Ping");
            pingObject.transform.position = world;
            MapMarker marker = pingObject.AddComponent<MapMarker>();
            marker.type = MapMarkerType.PlayerPing;
            marker.displayName = "Player Marker";
            marker.color = MarkerColor(MapMarkerType.PlayerPing);
            marker.visibleThroughFog = true;
            marker.icon = GetMarkerSprite(MapMarkerType.PlayerPing);
            RefreshMarkerViewsIfNeeded();
            UpdateMarkers();
        }

        private void OnQuestChanged(QuestDefinition definition, QuestProgress progress)
        {
            activeQuestTargetCached = false;
            RefreshQuestList();
            UpdateMarkers();
        }

        private void OnObjectiveChanged(
            QuestDefinition definition,
            QuestObjectiveDefinition objective,
            bool completed)
        {
            activeQuestTargetCached = false;
            RefreshQuestList();
            UpdateMarkers();
        }

        private void CreateInputActions()
        {
            DisposeInputActions();
            toggleMapAction = new InputAction("ToggleMap", InputActionType.Button);
            toggleMapAction.AddBinding("<Keyboard>/m");
            toggleMapAction.AddBinding("<Gamepad>/select");
            toggleMapAction.performed += OnToggleMap;
            toggleMapAction.Enable();

            closeMapAction = new InputAction("CloseMap", InputActionType.Button);
            closeMapAction.AddBinding("<Keyboard>/escape");
            closeMapAction.AddBinding("<Keyboard>/b");
            closeMapAction.AddBinding("<Gamepad>/buttonEast");
            closeMapAction.performed += OnCloseMap;
            closeMapAction.Enable();

            panAction = new InputAction("MapPan", InputActionType.Value);
            panAction.AddBinding("<Gamepad>/leftStick");
            panAction.Enable();

            zoomInAction = new InputAction("MapZoomIn", InputActionType.Button);
            zoomInAction.AddBinding("<Keyboard>/equals");
            zoomInAction.AddBinding("<Keyboard>/numpadPlus");
            zoomInAction.AddBinding("<Gamepad>/rightShoulder");
            zoomInAction.performed += OnZoomIn;
            zoomInAction.Enable();

            zoomOutAction = new InputAction("MapZoomOut", InputActionType.Button);
            zoomOutAction.AddBinding("<Keyboard>/minus");
            zoomOutAction.AddBinding("<Keyboard>/numpadMinus");
            zoomOutAction.AddBinding("<Gamepad>/leftShoulder");
            zoomOutAction.performed += OnZoomOut;
            zoomOutAction.Enable();
        }

        private void DisposeInputActions()
        {
            DisposeAction(ref toggleMapAction, OnToggleMap);
            DisposeAction(ref closeMapAction, OnCloseMap);
            DisposeAction(ref panAction, null);
            DisposeAction(ref zoomInAction, OnZoomIn);
            DisposeAction(ref zoomOutAction, OnZoomOut);
        }

        private static void DisposeAction(
            ref InputAction action,
            Action<InputAction.CallbackContext> handler)
        {
            if (action == null)
                return;
            if (handler != null)
                action.performed -= handler;
            action.Dispose();
            action = null;
        }

        private void OnToggleMap(InputAction.CallbackContext context)
        {
            if (fullMapOpen)
                CloseFullMap();
            else
                OpenFullMap();
        }

        private void OnCloseMap(InputAction.CallbackContext context)
        {
            if (fullMapOpen)
                CloseFullMap();
        }

        private void OnZoomIn(InputAction.CallbackContext context)
        {
            if (settings == null)
                return;
            ChangeZoom(settings.zoomStep, fullMapOpen);
            if (fullMapOpen)
                UpdateMarkers();
        }

        private void OnZoomOut(InputAction.CallbackContext context)
        {
            if (settings == null)
                return;
            ChangeZoom(-settings.zoomStep, fullMapOpen);
            if (fullMapOpen)
                UpdateMarkers();
        }

        private Sprite GetMarkerSprite(MapMarkerType type)
        {
            int index = Mathf.Clamp((int)type, 0, markerIcons != null ? markerIcons.Length - 1 : 0);
            if (markerIcons != null && markerIcons.Length > index && markerIcons[index] != null)
                return markerIcons[index];
            if (!warnedMissingMarkerIcon)
            {
                Debug.LogWarning($"Map icon for '{type}' is missing. Run Tools > Prince > Setup Map System to generate and assign the map icons.", this);
                warnedMissingMarkerIcon = true;
            }
            return null;
        }

        private static Color MarkerColor(MapMarkerType type)
        {
            int index = Mathf.Clamp((int)type, 0, DefaultMarkerColors.Length - 1);
            return DefaultMarkerColors[index];
        }

        private static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position)
        {
            GameObject element = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            element.transform.SetParent(parent, false);
            RectTransform rect = element.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = (anchorMin + anchorMax) * 0.5f;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = position;
            return rect;
        }

        private static RectTransform CreateCircleImage(
            string name,
            Transform parent,
            float diameter,
            Color color,
            Sprite sprite)
        {
            RectTransform rect = CreateRect(
                name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            rect.sizeDelta = Vector2.one * diameter;
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return rect;
        }

        private static TextMeshProUGUI CreateLayoutText(
            string name,
            Transform parent,
            string content,
            float fontSize,
            Color color,
            float height)
        {
            TextMeshProUGUI text = CreateText(
                name, parent, content, fontSize, color,
                new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, height));
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            text.alignment = TextAlignmentOptions.TopLeft;
            return text;
        }

        private static void CreateDivider(Transform parent)
        {
            RectTransform rect = CreateRect(
                "Field Notes Divider", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero);
            rect.sizeDelta = new Vector2(0f, 1f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.42f, 0.35f, 0.24f, 0.55f);
            image.raycastTarget = false;
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 1f;
            layout.preferredHeight = 1f;
        }

        private static Sprite CreateCircleSprite(out Texture2D texture)
        {
            const int resolution = 256;
            const int samples = 4;
            texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true)
            {
                name = "Runtime Minimap Circle Mask",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] pixels = new Color32[resolution * resolution];
            float radius = resolution * 0.5f - 1f;
            for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
            {
                int covered = 0;
                for (int sy = 0; sy < samples; sy++)
                for (int sx = 0; sx < samples; sx++)
                {
                    float px = x + (sx + 0.5f) / samples - resolution * 0.5f;
                    float py = y + (sy + 0.5f) / samples - resolution * 0.5f;
                    if (px * px + py * py <= radius * radius)
                        covered++;
                }
                byte alpha = (byte)(covered * 255 / (samples * samples));
                pixels[y * resolution + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, resolution, resolution),
                new Vector2(0.5f, 0.5f), resolution);
        }

        private static RectTransform CreatePanel(
            string name,
            Transform parent,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 size,
            Vector2? position = null)
        {
            GameObject element = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            element.transform.SetParent(parent, false);
            RectTransform rect = element.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = (anchorMin + anchorMax) * 0.5f;
            rect.sizeDelta = size;
            rect.anchoredPosition = position ?? Vector2.zero;
            element.GetComponent<Image>().color = color;
            return rect;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string text,
            float fontSize,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size)
        {
            GameObject element = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            element.transform.SetParent(parent, false);
            RectTransform rect = element.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = (anchorMin + anchorMax) * 0.5f;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            TextMeshProUGUI label = element.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.enableWordWrapping = true;
            return label;
        }

        private static Button CreateButton(string name, string text, Transform parent, float height)
        {
            RectTransform rect = CreatePanel(
                name, parent, new Color(0.22f, 0.17f, 0.11f, 0.98f),
                Vector2.zero, Vector2.one, new Vector2(280f, height));
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            Button button = rect.gameObject.AddComponent<Button>();
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;
            layout.flexibleWidth = 1f;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.42f, 0.35f, 0.24f, 1f);
            colors.pressedColor = new Color(0.55f, 0.43f, 0.26f, 1f);
            button.colors = colors;
            TextMeshProUGUI label = CreateText(
                "Label", rect, text, 15f, new Color(0.88f, 0.82f, 0.69f, 1f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.fontStyle = FontStyles.Bold;
            return button;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("MapEventSystem", typeof(EventSystem));
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() != null)
                return;
            foreach (BaseInputModule module in eventSystem.GetComponents<BaseInputModule>())
                module.enabled = false;
            InputSystemUIInputModule inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }
    }
}
