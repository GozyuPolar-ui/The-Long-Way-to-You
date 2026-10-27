using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prince
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private enum ConfirmationAction
        {
            None,
            StartNew,
            Quit
        }

        [SerializeField] private string gameplaySceneName = "UNS_Demo";
        [SerializeField] private Canvas menuCanvas;
        [SerializeField] private SceneFader sceneFader;
        [SerializeField] private MenuVideoBackground menuVideoBackground;
        [SerializeField] private TMP_FontAsset menuFont;
        [SerializeField] private TMP_FontAsset titleFont;
        [SerializeField] private MenuMusicPlayer menuMusicPlayer;
        [SerializeField, Range(0f, 1.5f)] private float titleFadeDuration = 1.5f;
        [SerializeField, Range(0f, 1f)] private float backgroundDim = 0.6f;
        [SerializeField, Range(0.1f, 1f)] private float gradientWidth = 0.55f;
        [SerializeField] private Color gradientColor = new Color(0.09f, 0.14f, 0.19f, 1f);
        [SerializeField, Min(0f)] private float screenMargin = 76f;

        private RectTransform mainGroup;
        private RectTransform settingsRoot;
        private RectTransform confirmationRoot;
        private CanvasGroup mainGroupCanvasGroup;
        private TextMeshProUGUI continueLabel;
        private Button continueButton;
        private Button firstButton;
        private ConfirmationAction confirmationAction;
        private bool transitionStarted;

        private void Awake()
        {
            if (menuCanvas == null)
                menuCanvas = GetComponentInChildren<Canvas>(true);
            if (sceneFader == null)
                sceneFader = FindAnyObjectByType<SceneFader>();
            if (menuVideoBackground == null)
                menuVideoBackground = GetComponent<MenuVideoBackground>();
            if (menuMusicPlayer == null)
                menuMusicPlayer = GetComponentInChildren<MenuMusicPlayer>(true);
            if (menuFont == null)
                menuFont = TMP_Settings.defaultFontAsset;
            if (titleFont == null)
                titleFont = menuFont;
            if (menuCanvas == null)
            {
                Debug.LogError("MainMenuController requires a Canvas.", this);
                enabled = false;
                return;
            }

            SaveGameService.EnsureInstance();
            MenuSettings.ApplyAll();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            BuildMenu();
            if (menuMusicPlayer == null)
                Debug.LogWarning("Main menu has no assigned MenuMusic AudioSource.", this);
        }

        private void Start()
        {
            if (firstButton != null && firstButton.interactable && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
            StartCoroutine(FadeTitleIn());
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu")
                return;
            MenuSettings.ApplyAll();
        }

        private void BuildMenu()
        {
            RectTransform canvasRect = menuCanvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            Transform gradientTransform = canvasRect.Find("MenuBackgroundGradient");
            if (gradientTransform != null && menuVideoBackground != null)
                menuVideoBackground.ConfigureGradient(
                    gradientTransform.GetComponent<Image>(), backgroundDim, gradientWidth, gradientColor);

            mainGroup = MenuUIFactory.Rect("MainMenuContent", canvasRect, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            mainGroupCanvasGroup = mainGroup.gameObject.AddComponent<CanvasGroup>();
            BuildTitle(mainGroup);
            BuildMainButtons(mainGroup);
            BuildFooter(mainGroup);
            BuildSettings(canvasRect);
            BuildConfirmation(canvasRect);
        }

        private void BuildTitle(Transform parent)
        {
            RectTransform titleRect = MenuUIFactory.Rect("TitleGroup", parent,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(screenMargin, -158f), new Vector2(980f, 120f));
            TextMeshProUGUI title = MenuUIFactory.Text(
                "GameTitle", titleRect, MenuStrings.GameTitle, 78f, MenuUIFactory.TitleGold);
            title.fontWeight = FontWeight.Bold;
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 92f));
            title.characterSpacing = 1.2f;
            ApplyTitleFont(title);
            TextMeshProUGUI subtitle = MenuUIFactory.Text(
                "GameSubtitle", titleRect, MenuStrings.Subtitle, 25f, MenuUIFactory.Cream);
            subtitle.fontWeight = FontWeight.Medium;
            Place(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, -82f), new Vector2(0f, 42f));
            ApplyFont(subtitle);
        }

        private void BuildMainButtons(Transform parent)
        {
            RectTransform buttonRoot = MenuUIFactory.Rect("MainMenuButtons", parent,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(screenMargin, -365f), new Vector2(470f, 430f));

            continueButton = CreateMenuButton(buttonRoot, "ContinueButton", MenuStrings.Continue, 0f, ContinueGame);
            continueLabel = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            continueButton.interactable = SaveGameService.HasSave;
            MenuButtonFX continueFx = continueButton.GetComponent<MenuButtonFX>();
            if (continueFx != null)
                continueFx.SetInteractable(continueButton.interactable);
            if (!continueButton.interactable)
                continueLabel.color = new Color(MenuUIFactory.Muted.r, MenuUIFactory.Muted.g, MenuUIFactory.Muted.b, 0.55f);
            firstButton = continueButton.interactable
                ? continueButton
                : CreateMenuButton(buttonRoot, "NewGameButton", MenuStrings.NewGame, -78f, StartNewGame);

            if (buttonRoot.Find("NewGameButton") == null)
                CreateMenuButton(buttonRoot, "NewGameButton", MenuStrings.NewGame, -78f, StartNewGame);

            CreateMenuButton(buttonRoot, "SettingsButton", MenuStrings.Settings, -156f, OpenSettings);
            CreateMenuButton(buttonRoot, "QuitButton", MenuStrings.Quit, -234f, ConfirmQuit);
            if (firstButton != null)
            {
                Navigation navigation = firstButton.navigation;
                navigation.mode = Navigation.Mode.Automatic;
                firstButton.navigation = navigation;
            }
        }

        private Button CreateMenuButton(Transform parent, string name, string label, float y, UnityEngine.Events.UnityAction action)
        {
            Button button = MenuUIFactory.Button(name, parent, label, menuFont);
            Place(button.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(430f, 58f));
            button.onClick.AddListener(action);
            return button;
        }

        private void BuildFooter(Transform parent)
        {
            TextMeshProUGUI studio = MenuUIFactory.Text(
                "FooterStudio", parent, MenuStrings.Studio, 21f, MenuUIFactory.Cream,
                TextAlignmentOptions.BottomLeft);
            studio.fontWeight = FontWeight.SemiBold;
            Place(studio.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(screenMargin, screenMargin + 24f), new Vector2(420f, 30f));
            ApplyFont(studio);

            TextMeshProUGUI copyright = MenuUIFactory.Text(
                "FooterCopyright", parent, MenuStrings.Copyright, 14f, MenuUIFactory.Cream,
                TextAlignmentOptions.BottomLeft);
            Place(copyright.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(screenMargin, screenMargin), new Vector2(540f, 22f));
            ApplyFont(copyright);

            TextMeshProUGUI beta = MenuUIFactory.Text(
                "FooterBeta", parent, MenuStrings.Beta, 16f, MenuUIFactory.Gold,
                TextAlignmentOptions.BottomRight);
            beta.characterSpacing = 3f;
            beta.fontWeight = FontWeight.SemiBold;
            Place(beta.rectTransform, Vector2.one, Vector2.one, Vector2.one,
                new Vector2(-screenMargin, screenMargin + 24f), new Vector2(280f, 26f));
            ApplyFont(beta);

            TextMeshProUGUI version = MenuUIFactory.Text(
                "FooterVersion", parent, MenuStrings.Version, 17f, MenuUIFactory.Cream,
                TextAlignmentOptions.BottomRight);
            Place(version.rectTransform, Vector2.one, Vector2.one, Vector2.one,
                new Vector2(-screenMargin, screenMargin), new Vector2(280f, 22f));
            ApplyFont(version);
        }

        private void BuildSettings(Transform parent)
        {
            GameObject panelObject = new GameObject("SettingsPanel", typeof(RectTransform), typeof(CanvasGroup));
            panelObject.transform.SetParent(parent, false);
            settingsRoot = panelObject.GetComponent<RectTransform>();
            SettingsPanel settingsPanel = panelObject.AddComponent<SettingsPanel>();
            settingsPanel.Build(menuFont, CloseSettings);
        }

        private void BuildConfirmation(Transform parent)
        {
            confirmationRoot = MenuUIFactory.Panel("ConfirmationPanel", parent, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650f, 330f));
            TextMeshProUGUI message = MenuUIFactory.Text(
                "ConfirmationMessage", confirmationRoot, MenuStrings.StartNewConfirmation, 23f,
                MenuUIFactory.Cream, TextAlignmentOptions.Center);
            Place(message.rectTransform, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(550f, 110f));
            ApplyFont(message);

            Button confirm = MenuUIFactory.Button("ConfirmAction", confirmationRoot, MenuStrings.Confirm, menuFont);
            Place(confirm.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-145f, 42f), new Vector2(240f, 54f));
            confirm.onClick.AddListener(ExecuteConfirmation);

            Button cancel = MenuUIFactory.Button("CancelAction", confirmationRoot, MenuStrings.Cancel, menuFont);
            Place(cancel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(145f, 42f), new Vector2(240f, 54f));
            cancel.onClick.AddListener(CancelConfirmation);
            confirmationRoot.gameObject.SetActive(false);
        }

        private IEnumerator FadeTitleIn()
        {
            if (mainGroupCanvasGroup == null)
                yield break;
            mainGroupCanvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < titleFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                mainGroupCanvasGroup.alpha = titleFadeDuration > 0f
                    ? Mathf.Clamp01(elapsed / titleFadeDuration)
                    : 1f;
                yield return null;
            }
            mainGroupCanvasGroup.alpha = 1f;
        }

        private void ContinueGame()
        {
            if (!SaveGameService.TryLoadSavedGame(out string sceneName))
            {
                continueButton.interactable = false;
                MenuButtonFX buttonFx = continueButton.GetComponent<MenuButtonFX>();
                if (buttonFx != null)
                    buttonFx.SetInteractable(false);
                if (continueLabel != null)
                    continueLabel.color = MenuUIFactory.Muted;
                Debug.LogWarning("No valid save file is available to continue.");
                return;
            }
            LoadGameScene(sceneName);
        }

        private void StartNewGame()
        {
            if (SaveGameService.SaveFileExists)
            {
                confirmationAction = ConfirmationAction.StartNew;
                ShowConfirmation(MenuStrings.StartNewConfirmation, MenuStrings.Confirm);
                return;
            }
            BeginNewGame();
        }

        private void BeginNewGame()
        {
            if (!SaveGameService.DeleteSave())
                return;
            LoadGameScene(gameplaySceneName);
        }

        private void OpenSettings()
        {
            if (settingsRoot == null)
                return;
            mainGroup.gameObject.SetActive(false);
            settingsRoot.gameObject.SetActive(true);
            SelectFirstSelectable(settingsRoot);
        }

        private void CloseSettings()
        {
            if (settingsRoot != null)
                settingsRoot.gameObject.SetActive(false);
            if (mainGroup != null)
                mainGroup.gameObject.SetActive(true);
            if (firstButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }

        private void ConfirmQuit()
        {
            confirmationAction = ConfirmationAction.Quit;
            ShowConfirmation(MenuStrings.QuitConfirmation, MenuStrings.Quit);
        }

        private void ShowConfirmation(string message, string confirmLabel)
        {
            TextMeshProUGUI text = confirmationRoot.Find("ConfirmationMessage").GetComponent<TextMeshProUGUI>();
            text.text = message;
            confirmationRoot.Find("ConfirmAction/Label").GetComponent<TextMeshProUGUI>().text = confirmLabel;
            confirmationRoot.gameObject.SetActive(true);
            SelectFirstSelectable(confirmationRoot);
        }

        private void ExecuteConfirmation()
        {
            ConfirmationAction action = confirmationAction;
            CancelConfirmation();
            if (action == ConfirmationAction.StartNew)
                BeginNewGame();
            else if (action == ConfirmationAction.Quit)
                QuitGame();
        }

        private void CancelConfirmation()
        {
            confirmationAction = ConfirmationAction.None;
            if (confirmationRoot != null)
                confirmationRoot.gameObject.SetActive(false);
            if (firstButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }

        private void LoadGameScene(string sceneName)
        {
            if (transitionStarted)
                return;
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(FindScenePath(sceneName));
            if (buildIndex < 0)
            {
                Debug.LogError($"Scene '{sceneName}' is not present in the build settings.");
                return;
            }
            if (sceneFader == null)
            {
                Debug.LogError("MainMenuController has no SceneFader for scene transitions.", this);
                return;
            }
            transitionStarted = true;
            if (menuMusicPlayer != null)
                menuMusicPlayer.FadeOut(sceneFader.FadeDuration);
            if (menuVideoBackground != null)
                menuVideoBackground.FadeAudioOut(sceneFader.FadeDuration);
            SceneLoader.Load(sceneName);
        }

        private static string FindScenePath(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                    return path;
            }
            return string.Empty;
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SelectFirstSelectable(Transform root)
        {
            Selectable selectable = root.GetComponentInChildren<Selectable>(true);
            if (selectable != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }

        private void ApplyFont(TMP_Text text)
        {
            if (menuFont != null)
                text.font = menuFont;
        }

        private void ApplyTitleFont(TMP_Text text)
        {
            if (titleFont != null)
                text.font = titleFont;
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
