using TargetIndicators;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prince
{
    [DefaultExecutionOrder(-800)]
    public sealed class QuestTrackingHUD : MonoBehaviour
    {
        private static readonly string[] CardinalDirections = { "N", "E", "S", "W" };

        [SerializeField] private QuestManager questManager;
        [SerializeField] private TargetIndicatorManager indicatorManager;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool showQuestList = true;
        [SerializeField, Min(0f)] private float compassWidth = 760f;

        private Canvas canvas;
        private RectTransform compass;
        private RectTransform objectiveMarker;
        private RectTransform targetMarker;
        private RectTransform offscreenArrow;
        private RectTransform questPanel;
        private TextMeshProUGUI questHeaderText;
        private TextMeshProUGUI questTitleText;
        private TextMeshProUGUI questObjectiveText;
        private Text targetLabel;
        private Text distanceText;
        private Text offscreenLabel;
        private Text targetMarkerFallbackText;
        private Image targetMarkerImage;
        private Text[] cardinalLabels;
        private QuestTarget activeTarget;
        private TargetIndicatorId activeTargetId;
        private string unresolvedTargetId;
        private bool hasTrackedTarget;
        private bool hiddenByMap;
        private float completionExpiresAt;

        private void Awake()
        {
            if (questManager == null)
                questManager = QuestManager.Instance;
            if (indicatorManager == null)
                indicatorManager = GetComponent<TargetIndicatorManager>();
            if (indicatorManager == null)
                indicatorManager = gameObject.AddComponent<TargetIndicatorManager>();
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (targetCamera != null)
                indicatorManager.Camera = targetCamera;
            indicatorManager.BoundaryType = BoundaryType.Absolute;
            indicatorManager.BoundaryShape = BoundaryShape.Rectangle;
            indicatorManager.Width = Mathf.Max(100f, Screen.width - 64f);
            indicatorManager.Height = Mathf.Max(100f, Screen.height - 64f);
            BuildHUD();
        }

        private void OnEnable()
        {
            if (questManager == null)
                questManager = QuestManager.Instance;
            if (questManager != null)
            {
                questManager.OnQuestChanged += OnQuestChanged;
                questManager.OnObjectiveChanged += OnObjectiveChanged;
            }
        }

        private void Start()
        {
            if (questManager == null)
                questManager = QuestManager.Instance;
            if (questManager != null)
            {
                questManager.OnQuestChanged -= OnQuestChanged;
                questManager.OnObjectiveChanged -= OnObjectiveChanged;
                questManager.OnQuestChanged += OnQuestChanged;
                questManager.OnObjectiveChanged += OnObjectiveChanged;
            }
            RefreshQuestText();
            RefreshActiveTarget();
        }

        private void OnDisable()
        {
            if (questManager != null)
            {
                questManager.OnQuestChanged -= OnQuestChanged;
                questManager.OnObjectiveChanged -= OnObjectiveChanged;
            }
            RemoveTrackedTarget();
        }

        private void Update()
        {
            if (completionExpiresAt > 0f && Time.unscaledTime >= completionExpiresAt)
            {
                completionExpiresAt = 0f;
                if (questHeaderText != null)
                    questHeaderText.text = "MISI AKTIF";
                if (questTitleText != null)
                    questTitleText.text = "Belum ada misi";
                if (questObjectiveText != null)
                    questObjectiveText.text = string.Empty;
                if (questPanel != null)
                    questPanel.gameObject.SetActive(false);
            }

            if (targetCamera == null)
                targetCamera = Camera.main;
            if (targetCamera == null || indicatorManager == null)
            {
                SetTargetUIVisible(false);
                return;
            }
            indicatorManager.Camera = targetCamera;

            if (activeTarget == null)
                RefreshActiveTarget();
            if (activeTarget == null || !activeTarget.trackable)
            {
                SetTargetUIVisible(false);
                return;
            }

            if (!hasTrackedTarget &&
                indicatorManager.TryAddTarget(activeTarget.transform, out TargetIndicator added))
            {
                activeTargetId = added.Id;
                hasTrackedTarget = true;
            }
            if (!hasTrackedTarget ||
                !indicatorManager.TryGetTargetIndicator(activeTargetId, out TargetIndicator indicator))
            {
                SetTargetUIVisible(false);
                return;
            }

            UpdateCompass(indicator);
            UpdateTargetMarkers(indicator);
            SetTargetUIVisible(true);
        }

        private void RefreshActiveTarget()
        {
            RemoveTrackedTarget();
            activeTarget = null;
            if (questManager == null)
                questManager = QuestManager.Instance;
            if (questManager == null)
                return;

            var objectives = questManager.GetActiveObjectives();
            if (objectives.Count == 0)
            {
                unresolvedTargetId = null;
                return;
            }

            string targetId = objectives[0].objective.targetId;
            if (questManager.TryGetTarget(targetId, out QuestTarget target))
            {
                unresolvedTargetId = null;
                activeTarget = target;
                if (indicatorManager != null &&
                    indicatorManager.TryAddTarget(target.transform, out TargetIndicator added))
                {
                    activeTargetId = added.Id;
                    hasTrackedTarget = true;
                }
            }
            else if (!string.Equals(unresolvedTargetId, targetId, System.StringComparison.Ordinal))
            {
                unresolvedTargetId = targetId;
                Debug.LogWarning($"Active quest target '{targetId}' is not registered in the scene.", this);
            }
        }

        private void RemoveTrackedTarget()
        {
            if (hasTrackedTarget && indicatorManager != null)
                indicatorManager.TryRemoveTarget(activeTargetId);
            hasTrackedTarget = false;
            activeTargetId = default;
        }

        private void UpdateCompass(TargetIndicator indicator)
        {
            if (targetCamera == null || activeTarget == null)
                return;

            Vector3 flatTargetDirection = activeTarget.transform.position - targetCamera.transform.position;
            flatTargetDirection.y = 0f;
            if (flatTargetDirection.sqrMagnitude < 0.001f)
                return;

            float targetYaw = Quaternion.LookRotation(flatTargetDirection, Vector3.up).eulerAngles.y;
            float cameraYaw = targetCamera.transform.eulerAngles.y;
            float relativeAngle = Mathf.DeltaAngle(cameraYaw, targetYaw);
            float markerX = (relativeAngle / 180f) * compassWidth * 0.5f;
            objectiveMarker.anchoredPosition = new Vector2(markerX, 0f);

            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                float bearing = i * 90f;
                float relative = Mathf.DeltaAngle(cameraYaw, bearing);
                cardinalLabels[i].rectTransform.anchoredPosition =
                    new Vector2((relative / 180f) * compassWidth * 0.5f, -30f);
            }

            targetLabel.text = activeTarget.displayName;
            distanceText.text = $"{indicator.Distance:0} m";
            targetMarkerImage.sprite = activeTarget.icon;
            targetMarkerImage.enabled = activeTarget.icon != null;
            targetMarkerFallbackText.gameObject.SetActive(activeTarget.icon == null);
        }

        private void UpdateTargetMarkers(TargetIndicator indicator)
        {
            Vector3 screenPosition = indicator.ScreenPose.position;
            bool inFront = screenPosition.z > 0f;
            bool insideScreen = inFront && screenPosition.x >= 12f && screenPosition.x <= Screen.width - 12f &&
                screenPosition.y >= 12f && screenPosition.y <= Screen.height - 12f;
            bool visible = insideScreen && !indicator.IsOutsideBoundary;

            if (visible)
            {
                targetMarker.gameObject.SetActive(true);
                offscreenArrow.gameObject.SetActive(false);
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform, screenPosition, null, out localPoint);
                targetMarker.anchoredPosition = localPoint + new Vector2(0f, 48f);
                return;
            }

            targetMarker.gameObject.SetActive(false);
            offscreenArrow.gameObject.SetActive(true);
            Vector2 edgePoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screenPosition, null, out edgePoint);
            offscreenArrow.anchoredPosition = edgePoint;

            Vector2 direction = edgePoint - Vector2.zero;
            if (!inFront)
                direction = -direction;
            if (direction.sqrMagnitude < 0.001f)
                direction = inFront ? Vector2.up : Vector2.down;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            offscreenArrow.localEulerAngles = new Vector3(0f, 0f, angle);
            offscreenLabel.text = $"{activeTarget.displayName}  {indicator.Distance:0} m";
        }

        private void SetTargetUIVisible(bool visible)
        {
            if (compass != null)
                compass.gameObject.SetActive(visible && !hiddenByMap);
            if (targetMarker != null)
                targetMarker.gameObject.SetActive(visible && !hiddenByMap && targetMarker.gameObject.activeSelf);
            if (offscreenArrow != null)
                offscreenArrow.gameObject.SetActive(visible && !hiddenByMap && offscreenArrow.gameObject.activeSelf);
        }

        internal void SetMapOpen(bool mapOpen)
        {
            hiddenByMap = mapOpen;
            if (questPanel != null)
                questPanel.gameObject.SetActive(showQuestList && !hiddenByMap);
            if (hiddenByMap)
                SetTargetUIVisible(false);
        }

        private void RefreshQuestText()
        {
            if (questHeaderText == null || questTitleText == null || questObjectiveText == null)
                return;
            if (questPanel != null)
                questPanel.gameObject.SetActive(showQuestList && !hiddenByMap);
            if (!showQuestList)
            {
                return;
            }

            questHeaderText.text = "MISI AKTIF";
            questTitleText.text = "Belum ada misi";
            questObjectiveText.text = string.Empty;
            if (questManager == null)
                questManager = QuestManager.Instance;
            if (questManager == null)
                return;

            var objectives = questManager.GetActiveObjectives();
            if (objectives.Count == 0)
                return;

            QuestObjectiveProgress active = objectives[0];
            questTitleText.text = active.quest != null ? active.quest.title : "Misi";
            questObjectiveText.text = active.objective != null
                ? "- " + active.objective.description
                : string.Empty;
        }

        private void OnQuestChanged(QuestDefinition definition, QuestProgress progress)
        {
            if (definition != null && progress != null && progress.state == QuestState.Completed)
            {
                completionExpiresAt = Time.unscaledTime + 8f;
                if (questHeaderText != null)
                    questHeaderText.text = "MISI SELESAI";
                if (questTitleText != null)
                    questTitleText.text = definition.title;
                if (questObjectiveText != null)
                    questObjectiveText.text = definition.description;
                if (questPanel != null)
                    questPanel.gameObject.SetActive(showQuestList && !hiddenByMap);
            }
            else
            {
                completionExpiresAt = 0f;
                RefreshQuestText();
            }
            RefreshActiveTarget();
        }

        private void OnObjectiveChanged(
            QuestDefinition definition,
            QuestObjectiveDefinition objective,
            bool completed)
        {
            RefreshQuestText();
        }

        private void BuildHUD()
        {
            QuestDialogueUI dialogueUI = FindAnyObjectByType<QuestDialogueUI>();
            canvas = dialogueUI != null ? dialogueUI.DialogueCanvas : null;
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("QuestHUDCanvas");
                canvasObject.transform.SetParent(transform, false);
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 80;
                CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            questPanel = QuestUIElements.CreatePanel(
                "ActiveQuestPanel", canvas.transform, new Color(0f, 0f, 0f, 0f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(468f, 126f),
                new Vector2(24f, -24f));
            questPanel.pivot = new Vector2(0f, 1f);
            questPanel.GetComponent<Image>().raycastTarget = false;
            questHeaderText = CreateQuestText(
                "ActiveQuestHeader", questPanel, "MISI AKTIF", 16,
                new Color(0.78f, 0.66f, 0.45f, 1f),
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(14f, -10f), new Vector2(-28f, 22f));
            questHeaderText.fontStyle = FontStyles.Bold;
            questHeaderText.characterSpacing = 1f;
            questTitleText = CreateQuestText(
                "ActiveQuestTitle", questPanel, "Belum ada misi", 19,
                new Color(0.91f, 0.87f, 0.77f, 1f),
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(14f, -35f), new Vector2(-28f, 28f));
            questTitleText.fontStyle = FontStyles.Bold;
            questObjectiveText = CreateQuestText(
                "ActiveQuestObjective", questPanel, string.Empty, 16,
                new Color(0.72f, 0.72f, 0.69f, 1f),
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(14f, -66f), new Vector2(-28f, 48f));
            questObjectiveText.alignment = TextAlignmentOptions.TopLeft;
            questPanel.gameObject.SetActive(showQuestList && !hiddenByMap);

            RectTransform compassPanel = QuestUIElements.CreatePanel(
                "QuestCompass", canvas.transform, new Color(0f, 0f, 0f, 0f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(compassWidth, 86f),
                new Vector2(0f, -48f));
            compassPanel.gameObject.AddComponent<RectMask2D>();
            compass = compassPanel;
            QuestUIElements.CreatePanel(
                "CompassRail", compassPanel, new Color(0.8f, 0.82f, 0.85f, 0.7f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(compassWidth - 40f, 2f));
            objectiveMarker = QuestUIElements.CreatePanel(
                "ObjectiveMarker", compassPanel, new Color(1f, 0.77f, 0.22f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(18f, 24f));
            targetLabel = QuestUIElements.CreateText(
                "TargetName", compassPanel, string.Empty, 17, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -9f), new Vector2(-30f, 28f));
            distanceText = QuestUIElements.CreateText(
                "TargetDistance", compassPanel, string.Empty, 16, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 7f), new Vector2(-30f, 24f));

            cardinalLabels = new Text[4];
            for (int i = 0; i < CardinalDirections.Length; i++)
                cardinalLabels[i] = QuestUIElements.CreateText(
                    "Compass_" + CardinalDirections[i], compassPanel, CardinalDirections[i], 15, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28f, 22f));

            targetMarker = QuestUIElements.CreatePanel(
                "VisibleTargetMarker", canvas.transform, new Color(1f, 0.72f, 0.15f, 0.9f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));
            targetMarkerImage = targetMarker.GetComponent<Image>();
            targetMarkerFallbackText = QuestUIElements.CreateText(
                "TargetMarkerIcon", targetMarker, "!", 20, TextAnchor.MiddleCenter);
            offscreenArrow = QuestUIElements.CreatePanel(
                "OffscreenTargetArrow", canvas.transform, new Color(1f, 0.68f, 0.12f, 0.92f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(36f, 40f));
            QuestUIElements.CreateText(
                "Arrow", offscreenArrow, "^", 32, TextAnchor.MiddleCenter);
            offscreenLabel = QuestUIElements.CreateText(
                "OffscreenTargetLabel", offscreenArrow, string.Empty, 16, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -34f), new Vector2(350f, 30f));
            compass.gameObject.SetActive(false);
            targetMarker.gameObject.SetActive(false);
            offscreenArrow.gameObject.SetActive(false);
        }

        private static TextMeshProUGUI CreateQuestText(
            string name,
            Transform parent,
            string content,
            float fontSize,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size)
        {
            GameObject element = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            element.transform.SetParent(parent, false);
            RectTransform rect = element.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TextMeshProUGUI text = element.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

    }
}
