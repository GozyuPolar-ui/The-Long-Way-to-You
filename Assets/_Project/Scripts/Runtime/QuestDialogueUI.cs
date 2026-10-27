using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Prince
{
    [DefaultExecutionOrder(-900)]
    public sealed class QuestDialogueUI : MonoBehaviour
    {
        private sealed class BranchFrame
        {
            public bool parentActive;
            public bool branchTaken;
            public bool currentActive;
        }

        private sealed class DialogueChoice
        {
            public string text;
            public readonly List<string> commands = new List<string>();
        }

        [SerializeField] private TextAsset dialogueSource;
        [SerializeField] private QuestManager questManager;
        [SerializeField] private DialogueInputLock inputLock;
        [SerializeField] private string defaultSpeaker = "Warga";

        private readonly Dictionary<string, List<string>> nodes =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, Func<string, string>> functions =
            new Dictionary<string, Func<string, string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, Action<string[]>> commands =
            new Dictionary<string, Action<string[]>>(StringComparer.Ordinal);
        private readonly List<BranchFrame> branchStack = new List<BranchFrame>();
        private readonly List<DialogueChoice> currentChoices = new List<DialogueChoice>();
        private readonly List<Button> choiceButtons = new List<Button>();
        private readonly Queue<string> postChoiceLines = new Queue<string>();

        private Canvas canvas;
        private GameObject dialoguePanel;
        private GameObject choiceContainer;
        private Text speakerText;
        private Text messageText;
        private Text promptText;
        private Text continuePromptText;
        private NPCInfo currentNPC;
        private string currentSpeaker;
        private List<string> currentNode;
        private int lineIndex;
        private int selectedChoice;
        private bool isShowingChoices;
        private bool hasDisplayedDialogueLine;
        private InputAction continueAction;
        private float continueInputBlockedUntil;

        public static QuestDialogueUI Instance { get; private set; }
        public static bool IsDialogueOpen => Instance != null && Instance.currentNode != null;
        public Canvas DialogueCanvas => canvas;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (questManager == null)
                questManager = QuestManager.Instance;
            if (inputLock == null)
                inputLock = FindAnyObjectByType<DialogueInputLock>();

            continueAction = new InputAction("Continue Dialogue", InputActionType.Button);
            continueAction.AddBinding("<Keyboard>/e");
            continueAction.performed += OnContinuePressed;

            RegisterCommandsAndFunctions();
            ParseDialogueSource();
            BuildCanvas();
        }

        private void OnEnable()
        {
            continueAction?.Enable();
        }

        private void OnDisable()
        {
            continueAction?.Disable();
            CloseDialogue();
        }

        private void OnDestroy()
        {
            if (continueAction != null)
            {
                continueAction.performed -= OnContinuePressed;
                continueAction.Dispose();
            }
            if (Instance == this)
                Instance = null;
        }

        public void StartDialogue(NPCInfo npc)
        {
            if (npc == null)
                return;
            if (currentNode != null)
            {
                Debug.LogWarning("A dialogue is already in progress.", this);
                return;
            }
            if (string.IsNullOrWhiteSpace(npc.dialogueId) ||
                !nodes.TryGetValue(npc.dialogueId, out List<string> nodeLines))
            {
                Debug.LogWarning($"No dialogue node '{npc.dialogueId}' was found for NPC '{npc.DisplayName}'.", npc);
                return;
            }
            if (questManager == null)
                questManager = QuestManager.Instance;
            if (questManager == null)
            {
                Debug.LogError("Dialogue cannot start because QuestManager is missing.", this);
                return;
            }
            if (inputLock == null)
                inputLock = FindAnyObjectByType<DialogueInputLock>();

            currentNPC = npc;
            currentSpeaker = npc.DisplayName;
            currentNode = nodeLines;
            lineIndex = 0;
            branchStack.Clear();
            currentChoices.Clear();
            postChoiceLines.Clear();
            isShowingChoices = false;
            hasDisplayedDialogueLine = false;
            dialoguePanel.SetActive(true);
            UISoundPlayer.Play(UISoundType.Open);
            continueInputBlockedUntil = Time.unscaledTime + 0.12f;
            continuePromptText.gameObject.SetActive(true);
            ClearChoiceButtons();
            SelectUIElement(null);
            if (inputLock != null)
                inputLock.SetLocked(true);
            AdvanceDialogue();
        }

        public static void SetPrompt(string value)
        {
            if (Instance == null || Instance.promptText == null)
                return;
            Instance.promptText.text = GameLocalization.Get(value ?? string.Empty);
            Instance.promptText.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(value) &&
                !IsDialogueOpen);
        }

        public void ContinueDialogue()
        {
            if (currentNode == null || isShowingChoices ||
                Time.unscaledTime < continueInputBlockedUntil)
                return;
            AdvanceDialogue();
        }

        private void OnContinuePressed(InputAction.CallbackContext context)
        {
            ContinueDialogue();
        }

        private void AdvanceDialogue()
        {
            if (postChoiceLines.Count > 0)
            {
                ShowDialogueLine(postChoiceLines.Dequeue());
                continuePromptText.gameObject.SetActive(true);
                SelectUIElement(null);
                return;
            }

            while (currentNode != null && lineIndex < currentNode.Count)
            {
                string rawLine = currentNode[lineIndex++];
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
                    continue;

                if (TryProcessConditional(line))
                    continue;
                if (!IsCurrentBranchActive())
                    continue;

                if (line.StartsWith("->", StringComparison.Ordinal))
                {
                    ShowChoices(rawLine);
                    return;
                }

                if (TryGetCommand(line, out string commandText))
                {
                    ExecuteCommand(commandText);
                    continue;
                }

                if (line.StartsWith("title:", StringComparison.OrdinalIgnoreCase) ||
                    line == "---" || line == "===")
                    continue;

                ShowDialogueLine(line);
                continuePromptText.gameObject.SetActive(true);
                SelectUIElement(null);
                return;
            }

            CloseDialogue();
        }

        private void ShowDialogueLine(string line)
        {
            if (hasDisplayedDialogueLine)
                UISoundPlayer.Play(UISoundType.DialogueAdvance);
            else
                hasDisplayedDialogueLine = true;

            int separator = line.IndexOf(':');
            if (separator > 0 && separator < 32)
            {
                currentSpeaker = line.Substring(0, separator).Trim();
                line = line.Substring(separator + 1).Trim();
            }
            messageText.text = GameLocalization.Get(line);
            speakerText.text = GameLocalization.Get(currentSpeaker);
        }

        private void ShowChoices(string firstRawLine)
        {
            currentChoices.Clear();
            isShowingChoices = true;
            continuePromptText.gameObject.SetActive(false);
            choiceContainer.SetActive(true);
            ReadChoice(firstRawLine);

            while (lineIndex < currentNode.Count)
            {
                string raw = currentNode[lineIndex];
                string trimmed = raw.Trim();
                if (trimmed.Length == 0)
                {
                    lineIndex++;
                    continue;
                }
                if (trimmed.StartsWith("->", StringComparison.Ordinal))
                {
                    lineIndex++;
                    ReadChoice(raw);
                    continue;
                }
                if (char.IsWhiteSpace(raw[0]))
                {
                    lineIndex++;
                    if (TryGetCommand(trimmed, out string command))
                    {
                        if (currentChoices.Count > 0)
                            currentChoices[currentChoices.Count - 1].commands.Add(command);
                    }
                    else if (currentChoices.Count > 0)
                        currentChoices[currentChoices.Count - 1].commands.Add("line " + trimmed);
                    continue;
                }
                break;
            }

            RebuildChoiceButtons();
            if (currentChoices.Count == 0)
            {
                Debug.LogWarning("The current dialogue choice block contains no options.", this);
                isShowingChoices = false;
                choiceContainer.SetActive(false);
                AdvanceDialogue();
            }
        }

        private void ReadChoice(string raw)
        {
            string choiceText = raw.Trim().Substring(2).Trim();
            if (choiceText.StartsWith("[[", StringComparison.Ordinal) && choiceText.EndsWith("]]", StringComparison.Ordinal))
                choiceText = choiceText.Substring(2, choiceText.Length - 4).Trim();
            currentChoices.Add(new DialogueChoice { text = choiceText });
        }

        private void RebuildChoiceButtons()
        {
            ClearChoiceButtons();
            selectedChoice = 0;
            for (int i = 0; i < currentChoices.Count; i++)
            {
                int index = i;
                Button button = QuestUIElements.CreateButton(
                    "Choice_" + i, currentChoices[i].text, choiceContainer.transform, 48f);
                button.onClick.AddListener(() => SelectChoice(index));
                choiceButtons.Add(button);
            }
            UpdateChoiceSelection();
            if (choiceButtons.Count > 0)
                SelectUIElement(choiceButtons[0].gameObject);
        }

        private void SelectChoice(int index)
        {
            if (!isShowingChoices || index < 0 || index >= currentChoices.Count)
                return;

            DialogueChoice choice = currentChoices[index];
            isShowingChoices = false;
            choiceContainer.SetActive(false);
            SelectUIElement(null);
            foreach (string action in choice.commands)
            {
                if (action.StartsWith("line ", StringComparison.Ordinal))
                    postChoiceLines.Enqueue(action.Substring(5));
                else
                    ExecuteCommand(action);
            }
            if (currentNode != null)
                AdvanceDialogue();
        }

        private bool TryProcessConditional(string line)
        {
            if (TryGetCommand(line, out string command))
            {
                if (command.StartsWith("if ", StringComparison.Ordinal))
                {
                    bool parentActive = IsCurrentBranchActive();
                    bool condition = parentActive && EvaluateCondition(command.Substring(3).Trim());
                    branchStack.Add(new BranchFrame
                    {
                        parentActive = parentActive,
                        branchTaken = condition,
                        currentActive = condition
                    });
                    return true;
                }
                if (command.StartsWith("elseif ", StringComparison.Ordinal))
                {
                    if (branchStack.Count == 0)
                    {
                        Debug.LogWarning("Dialogue has an elseif without a matching if.", this);
                        return true;
                    }
                    BranchFrame frame = branchStack[branchStack.Count - 1];
                    bool condition = frame.parentActive && !frame.branchTaken &&
                        EvaluateCondition(command.Substring(7).Trim());
                    frame.currentActive = condition;
                    frame.branchTaken |= condition;
                    return true;
                }
                if (command == "else")
                {
                    if (branchStack.Count == 0)
                    {
                        Debug.LogWarning("Dialogue has an else without a matching if.", this);
                        return true;
                    }
                    BranchFrame frame = branchStack[branchStack.Count - 1];
                    frame.currentActive = frame.parentActive && !frame.branchTaken;
                    frame.branchTaken = true;
                    return true;
                }
                if (command == "endif")
                {
                    if (branchStack.Count == 0)
                        Debug.LogWarning("Dialogue has an endif without a matching if.", this);
                    else
                        branchStack.RemoveAt(branchStack.Count - 1);
                    return true;
                }
            }
            return false;
        }

        private bool IsCurrentBranchActive()
        {
            return branchStack.Count == 0 || branchStack[branchStack.Count - 1].currentActive;
        }

        private bool EvaluateCondition(string condition)
        {
            string[] operators = { "==", "!=" };
            foreach (string operation in operators)
            {
                int split = condition.IndexOf(operation, StringComparison.Ordinal);
                if (split < 0)
                    continue;

                string left = condition.Substring(0, split).Trim();
                string right = Unquote(condition.Substring(split + operation.Length).Trim());
                string actual = EvaluateFunction(left);
                bool equal = string.Equals(actual, right, StringComparison.OrdinalIgnoreCase);
                return operation == "==" ? equal : !equal;
            }

            Debug.LogWarning($"Unsupported dialogue condition '{condition}'.", this);
            return false;
        }

        private string EvaluateFunction(string expression)
        {
            Match match = Regex.Match(expression, @"^([A-Za-z_][A-Za-z0-9_]*)\s*\((.*)\)$");
            if (!match.Success || !functions.TryGetValue(match.Groups[1].Value, out Func<string, string> function))
            {
                Debug.LogWarning($"Unregistered dialogue function in '{expression}'.", this);
                return string.Empty;
            }
            return function(Unquote(match.Groups[2].Value.Trim()));
        }

        private void RegisterCommandsAndFunctions()
        {
            functions["quest_state"] = questId =>
                questManager != null ? questManager.GetState(questId).ToString() : QuestState.NotStarted.ToString();
            functions["bear_alive"] = _ =>
            {
                foreach (QuestPickup backpack in FindObjectsByType<QuestPickup>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    QuestTarget target = backpack.GetComponent<QuestTarget>();
                    if (target != null && target.targetId == "bram_backpack")
                        return backpack.IsGuardBearAlive ? "true" : "false";
                }

                return "false";
            };
            commands["start_quest"] = arguments =>
            {
                if (arguments.Length == 0)
                {
                    Debug.LogWarning("The start_quest dialogue command requires a quest ID.", this);
                    return;
                }
                if (!TryGetQuestManager(out QuestManager manager))
                    return;
                if (!manager.StartQuest(arguments[0]))
                    Debug.LogWarning($"Quest '{arguments[0]}' could not be started.", this);
            };
            commands["complete_quest"] = arguments =>
            {
                if (arguments.Length == 0)
                {
                    Debug.LogWarning("The complete_quest dialogue command requires a quest ID.", this);
                    return;
                }
                if (!TryGetQuestManager(out QuestManager manager))
                    return;
                if (!manager.CompleteQuest(arguments[0]))
                    Debug.LogWarning($"Quest '{arguments[0]}' is not ready to complete.", this);
            };
            commands["set_objective_done"] = arguments =>
            {
                if (arguments.Length < 2)
                {
                    Debug.LogWarning("The set_objective_done dialogue command requires a quest ID and objective ID.", this);
                    return;
                }
                if (!TryGetQuestManager(out QuestManager manager))
                    return;
                if (!manager.SetObjectiveDone(arguments[0], arguments[1]))
                    Debug.LogWarning($"Objective '{arguments[1]}' for quest '{arguments[0]}' could not be updated.", this);
            };
            commands["goto"] = arguments =>
            {
                if (arguments.Length != 1 || !nodes.TryGetValue(arguments[0], out List<string> destination))
                {
                    Debug.LogWarning("The goto dialogue command requires the ID of an existing dialogue node.", this);
                    return;
                }

                currentNode = destination;
                lineIndex = 0;
                branchStack.Clear();
                currentSpeaker = currentNPC != null ? currentNPC.DisplayName : defaultSpeaker;
            };
        }

        private bool TryGetQuestManager(out QuestManager manager)
        {
            if (questManager == null)
                questManager = QuestManager.Instance;
            manager = questManager;
            if (manager != null)
                return true;

            Debug.LogWarning("A dialogue quest command could not run because QuestManager is missing.", this);
            return false;
        }

        private void ExecuteCommand(string commandText)
        {
            string[] tokens = commandText.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                return;
            if (!commands.TryGetValue(tokens[0], out Action<string[]> command))
            {
                Debug.LogWarning($"Unregistered dialogue command '{tokens[0]}'.", this);
                return;
            }
            var arguments = new string[tokens.Length - 1];
            Array.Copy(tokens, 1, arguments, 0, arguments.Length);
            command(arguments);
        }

        private void ParseDialogueSource()
        {
            nodes.Clear();
            if (dialogueSource == null)
            {
                Debug.LogWarning("QuestDialogueUI has no dialogue source TextAsset.", this);
                return;
            }

            string currentTitle = null;
            List<string> currentLines = null;
            bool inNodeBody = false;
            foreach (string rawLine in dialogueSource.text.Replace("\r\n", "\n").Split('\n'))
            {
                string trimmed = rawLine.Trim();
                if (trimmed.StartsWith("title:", StringComparison.OrdinalIgnoreCase))
                {
                    currentTitle = trimmed.Substring(6).Trim();
                    currentLines = new List<string>();
                    inNodeBody = false;
                    continue;
                }
                if (trimmed == "---")
                {
                    inNodeBody = currentTitle != null;
                    continue;
                }
                if (trimmed == "===")
                {
                    if (currentTitle != null && currentLines != null)
                        nodes[currentTitle] = currentLines;
                    currentTitle = null;
                    currentLines = null;
                    inNodeBody = false;
                    continue;
                }
                if (inNodeBody)
                    currentLines.Add(rawLine);
            }

            if (nodes.Count == 0)
                Debug.LogWarning($"No Yarn-style dialogue nodes were parsed from '{dialogueSource.name}'.", dialogueSource);
        }

        private static bool TryGetCommand(string line, out string command)
        {
            command = null;
            if (!line.StartsWith("<<", StringComparison.Ordinal) ||
                !line.EndsWith(">>", StringComparison.Ordinal) || line.Length < 4)
                return false;
            command = line.Substring(2, line.Length - 4).Trim();
            return true;
        }

        private static string Unquote(string value)
        {
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[value.Length - 1] == '"') ||
                 (value[0] == '\'' && value[value.Length - 1] == '\'')))
                return value.Substring(1, value.Length - 2);
            return value;
        }

        private void CloseDialogue()
        {
            bool wasOpen = currentNode != null;
            currentNode = null;
            currentNPC = null;
            isShowingChoices = false;
            currentChoices.Clear();
            postChoiceLines.Clear();
            branchStack.Clear();
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
            if (choiceContainer != null)
                choiceContainer.SetActive(false);
            if (inputLock != null)
                inputLock.SetLocked(false);
            if (wasOpen)
                UISoundPlayer.Play(UISoundType.Close);
            if (promptText != null)
                promptText.transform.parent.gameObject.SetActive(false);
            if (continuePromptText != null)
                continuePromptText.gameObject.SetActive(false);
            if (canvas != null && EventSystem.current != null)
            {
                GameObject selected = EventSystem.current.currentSelectedGameObject;
                if (selected != null && selected.transform.IsChildOf(canvas.transform))
                    SelectUIElement(null);
            }
        }

        private static void SelectUIElement(GameObject selected)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(selected);
        }

        private void ClearChoiceButtons()
        {
            foreach (Button button in choiceButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }
            choiceButtons.Clear();
        }

        private void UpdateChoiceSelection()
        {
            for (int i = 0; i < choiceButtons.Count; i++)
            {
                ColorBlock colors = choiceButtons[i].colors;
                colors.normalColor = i == selectedChoice
                    ? new Color(0.88f, 0.68f, 0.28f)
                    : Color.white;
                choiceButtons[i].colors = colors;
            }
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("DialogueCanvas");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            RectTransform promptPanel = QuestUIElements.CreatePanel(
                "InteractionPrompt", canvas.transform, new Color(0.04f, 0.04f, 0.04f, 0.75f),
                new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.56f), new Vector2(650f, 64f));
            promptText = QuestUIElements.CreateText(
                "PromptText", promptPanel, string.Empty, 24, TextAnchor.MiddleCenter);
            promptPanel.gameObject.SetActive(false);

            RectTransform panel = QuestUIElements.CreatePanel(
                "DialoguePanel", canvas.transform, new Color(0.035f, 0.025f, 0.018f, 0.88f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1040f, 320f));
            panel.anchoredPosition = new Vector2(0f, 48f);
            dialoguePanel = panel.gameObject;
            speakerText = QuestUIElements.CreateText(
                "Speaker", panel, defaultSpeaker, 26, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -42f), new Vector2(-64f, 52f));
            speakerText.color = new Color(1f, 0.82f, 0.45f);
            messageText = QuestUIElements.CreateText(
                "Line", panel, string.Empty, 22, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -112f), new Vector2(-64f, 112f));

            RectTransform choices = QuestUIElements.CreatePanel(
                "Choices", panel, new Color(0f, 0f, 0f, 0f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-64f, 130f));
            choices.anchoredPosition = new Vector2(0f, 20f);
            choiceContainer = choices.gameObject;
            VerticalLayoutGroup layout = choices.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            continuePromptText = QuestUIElements.CreateText(
                "ContinuePrompt", panel, GameLocalization.Get("Tekan E untuk melanjutkan"),
                18, TextAnchor.MiddleCenter);
            RectTransform continueRect = continuePromptText.rectTransform;
            continueRect.anchorMin = new Vector2(1f, 0f);
            continueRect.anchorMax = new Vector2(1f, 0f);
            continueRect.pivot = new Vector2(1f, 0f);
            continueRect.anchoredPosition = new Vector2(-28f, 20f);
            continueRect.sizeDelta = new Vector2(310f, 48f);

            dialoguePanel.SetActive(false);
            continuePromptText.gameObject.SetActive(false);
            choiceContainer.SetActive(false);
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
                DontDestroyOnLoad(eventSystemObject);
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                foreach (BaseInputModule module in eventSystem.GetComponents<BaseInputModule>())
                    module.enabled = false;
                InputSystemUIInputModule inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                inputModule.AssignDefaultActions();
            }
        }
    }

    internal static class QuestUIElements
    {
        private static Font cachedFont;

        public static RectTransform CreatePanel(
            string name,
            Transform parent,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 size,
            Vector2? position = null)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, anchorMin.y == 0f ? 0f : 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position ?? Vector2.zero;
            gameObject.GetComponent<Image>().color = color;
            return rect;
        }

        public static Text CreateText(
            string name,
            Transform parent,
            string content,
            int fontSize,
            TextAnchor alignment,
            Vector2? anchorMin = null,
            Vector2? anchorMax = null,
            Vector2? position = null,
            Vector2? size = null)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin ?? Vector2.zero;
            rect.anchorMax = anchorMax ?? Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size ?? Vector2.zero;
            rect.anchoredPosition = position ?? Vector2.zero;
            Text text = gameObject.GetComponent<Text>();
            if (cachedFont == null)
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = cachedFont;
            text.text = GameLocalization.Get(content);
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(string name, string label, Transform parent, float height)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, height);
            Image image = gameObject.GetComponent<Image>();
            image.color = new Color(0.22f, 0.18f, 0.12f, 0.96f);
            Button button = gameObject.GetComponent<Button>();
            UIButtonSound.Attach(gameObject);
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.8f, 0.62f, 0.28f);
            colors.pressedColor = new Color(0.65f, 0.46f, 0.17f);
            button.colors = colors;
            LayoutElement layout = gameObject.GetComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.flexibleWidth = 1f;
            CreateText("Label", rect, label, 20, TextAnchor.MiddleCenter);
            return button;
        }
    }
}
