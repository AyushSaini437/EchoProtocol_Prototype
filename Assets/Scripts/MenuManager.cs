using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("The translucent Main Menu panel")]
    public GameObject mainMenuPanel;

    [Tooltip("The translucent Pause Menu panel")]
    public GameObject pauseMenuPanel;

    [Tooltip("The HUD panel shown during gameplay")]
    public GameObject hudPanel;

    [Header("End Game Panels")]
    [Tooltip("The Mission Accomplished panel")]
    public GameObject victoryPanel;
    public Text victoryStatsText;

    [Tooltip("The Mission Failed panel")]
    public GameObject defeatPanel;
    public Text defeatStatsText;

    [Header("Settings")]
    [Tooltip("If true, starts on the Main Menu. If false, starts directly in gameplay.")]
    public bool startInMainMenu = true;

    // Static flag accessible by PlayerMovement and PlayerShooting to pause inputs
    public static bool isGamePaused = false;
    private static bool shouldStartDirectly = false;

    void Awake()
    {
        Time.timeScale = 1f;
        isGamePaused = false;
        EnsureEndGamePanels();
        WireButtons();
    }

    void Start()
    {
        if (shouldStartDirectly)
        {
            shouldStartDirectly = false;
            StartGame();
            return;
        }

        if (startInMainMenu && mainMenuPanel != null)
        {
            ShowMainMenu();
        }
        else
        {
            StartGame();
        }
    }

    void Update()
    {
        // Toggle Pause with Escape key (only when not on the main menu)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (mainMenuPanel != null && mainMenuPanel.activeSelf)
            {
                return;
            }

            if (isGamePaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // Connects button click events to their corresponding methods automatically
    void WireButtons()
    {
        if (mainMenuPanel != null)
        {
            foreach (Button btn in mainMenuPanel.GetComponentsInChildren<Button>(true))
            {
                btn.onClick.RemoveAllListeners();
                string n = btn.name.ToLower();
                if (n.Contains("start") || n.Contains("play")) btn.onClick.AddListener(StartGame);
                else if (n.Contains("quit")) btn.onClick.AddListener(QuitGame);
            }
        }

        if (pauseMenuPanel != null)
        {
            foreach (Button btn in pauseMenuPanel.GetComponentsInChildren<Button>(true))
            {
                btn.onClick.RemoveAllListeners();
                string n = btn.name.ToLower();
                if (n.Contains("resume")) btn.onClick.AddListener(ResumeGame);
                else if (n.Contains("restart") || n.Contains("retry") || n.Contains("play")) btn.onClick.AddListener(RestartGame);
                else if (n.Contains("main")) btn.onClick.AddListener(ShowMainMenu);
                else if (n.Contains("quit")) btn.onClick.AddListener(QuitGame);
            }
        }

        if (victoryPanel != null)
        {
            foreach (Button btn in victoryPanel.GetComponentsInChildren<Button>(true))
            {
                btn.onClick.RemoveAllListeners();
                string n = btn.name.ToLower();
                if (n.Contains("restart") || n.Contains("retry") || n.Contains("play")) btn.onClick.AddListener(RestartGame);
                else if (n.Contains("main")) btn.onClick.AddListener(ShowMainMenu);
                else if (n.Contains("quit")) btn.onClick.AddListener(QuitGame);
            }
        }

        if (defeatPanel != null)
        {
            foreach (Button btn in defeatPanel.GetComponentsInChildren<Button>(true))
            {
                btn.onClick.RemoveAllListeners();
                string n = btn.name.ToLower();
                if (n.Contains("restart") || n.Contains("retry") || n.Contains("play")) btn.onClick.AddListener(RestartGame);
                else if (n.Contains("main")) btn.onClick.AddListener(ShowMainMenu);
                else if (n.Contains("quit")) btn.onClick.AddListener(QuitGame);
            }
        }
    }

    void EnsureEndGamePanels()
    {
        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Victory Panel
        if (victoryPanel == null)
        {
            Transform t = transform.Find("VictoryPanel");
            if (t != null)
            {
                victoryPanel = t.gameObject;
                if (victoryStatsText == null) victoryStatsText = victoryPanel.GetComponentInChildren<Text>();
            }
            else
            {
                victoryPanel = CreateEndGamePanel(
                    "VictoryPanel",
                    "MISSION ACCOMPLISHED",
                    new Color(0.12f, 0.85f, 0.55f, 1f), // Tactical Mint/Cyan Green
                    "ALL HOSTILE TARGETS NEUTRALIZED",
                    "PLAY AGAIN",
                    uiFont,
                    out victoryStatsText
                );
            }
        }

        // Defeat Panel
        if (defeatPanel == null)
        {
            Transform t = transform.Find("DefeatPanel");
            if (t != null)
            {
                defeatPanel = t.gameObject;
                if (defeatStatsText == null) defeatStatsText = defeatPanel.GetComponentInChildren<Text>();
            }
            else
            {
                defeatPanel = CreateEndGamePanel(
                    "DefeatPanel",
                    "MISSION FAILED",
                    new Color(0.95f, 0.25f, 0.25f, 1f), // Tactical Crimson Red
                    "OPERATIVE KILLED IN ACTION",
                    "RETRY MISSION",
                    uiFont,
                    out defeatStatsText
                );
            }
        }

        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
    }

    private GameObject CreateEndGamePanel(string panelName, string title, Color titleColor, string subtitle, string restartBtnLabel, Font font, out Text statsTextOut)
    {
        GameObject panel = new GameObject(panelName);
        panel.transform.SetParent(transform, false);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.06f, 0.1f, 0.94f);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.anchoredPosition = new Vector2(0, 180);
        titleRt.sizeDelta = new Vector2(800, 60);

        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = font;
        titleText.fontSize = 34;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = titleColor;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.text = title;

        // Subtitle
        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(panel.transform, false);
        RectTransform subRt = subObj.AddComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.5f, 0.5f);
        subRt.anchorMax = new Vector2(0.5f, 0.5f);
        subRt.pivot = new Vector2(0.5f, 0.5f);
        subRt.anchoredPosition = new Vector2(0, 135);
        subRt.sizeDelta = new Vector2(600, 30);

        Text subText = subObj.AddComponent<Text>();
        subText.font = font;
        subText.fontSize = 15;
        subText.color = new Color(0.7f, 0.75f, 0.82f, 1f);
        subText.alignment = TextAnchor.MiddleCenter;
        subText.text = subtitle;

        // Stats Card Container
        GameObject cardObj = new GameObject("StatsCard");
        cardObj.transform.SetParent(panel.transform, false);
        RectTransform cardRt = cardObj.AddComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.anchoredPosition = new Vector2(0, 15);
        cardRt.sizeDelta = new Vector2(500, 170);

        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.color = new Color(0.08f, 0.12f, 0.18f, 0.85f);

        GameObject statsObj = new GameObject("StatsText");
        statsObj.transform.SetParent(cardObj.transform, false);
        RectTransform statsRt = statsObj.AddComponent<RectTransform>();
        statsRt.anchorMin = Vector2.zero;
        statsRt.anchorMax = Vector2.one;
        statsRt.sizeDelta = new Vector2(-40, -20);

        Text stats = statsObj.AddComponent<Text>();
        stats.font = font;
        stats.fontSize = 18;
        stats.lineSpacing = 1.3f;
        stats.color = new Color(0.9f, 0.94f, 0.98f, 1f);
        stats.alignment = TextAnchor.MiddleCenter;
        statsTextOut = stats;

        // Restart/Play Again Button
        Button restartBtn = CreateMenuButton(panel.transform, restartBtnLabel, "RestartButton", new Vector2(0, -115), new Vector2(280, 48), font, new Color(0.18f, 0.42f, 0.65f, 1f));
        restartBtn.onClick.AddListener(RestartGame);

        // Main Menu Button
        Button mainMenuBtn = CreateMenuButton(panel.transform, "MAIN MENU", "MainMenuButton", new Vector2(0, -175), new Vector2(280, 48), font, new Color(0.2f, 0.25f, 0.32f, 1f));
        mainMenuBtn.onClick.AddListener(ShowMainMenu);

        return panel;
    }

    private Button CreateMenuButton(Transform parent, string label, string gameObjName, Vector2 anchoredPos, Vector2 size, Font font, Color normalColor)
    {
        GameObject btnObj = new GameObject(gameObjName);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = normalColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = normalColor;
        cb.highlightedColor = normalColor * 1.25f;
        cb.pressedColor = normalColor * 0.85f;
        btn.colors = cb;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform trt = textObj.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;

        Text txt = textObj.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = 17;
        txt.fontStyle = FontStyle.Bold;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = label;

        return btn;
    }

    // Called when clicking "START MISSION" or entering gameplay
    public void StartGame()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);

        Time.timeScale = 1f;
        isGamePaused = false;

        // Lock cursor for first-person gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartMission();
        }
    }

    // Called when pressing Escape during gameplay
    public void PauseGame()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);

        Time.timeScale = 0f;
        isGamePaused = true;

        // Free cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Called when clicking "RESUME" button
    public void ResumeGame()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);

        Time.timeScale = 1f;
        isGamePaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Called on start or when clicking "MAIN MENU" button
    public void ShowMainMenu()
    {
        shouldStartDirectly = false;

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);

        Time.timeScale = 0f;
        isGamePaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Called when all hostiles are neutralized
    public void ShowVictory(string timeStr, int totalKills, int stealthKills, int score, string rank)
    {
        if (victoryPanel != null) victoryPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);

        if (victoryStatsText != null)
        {
            victoryStatsText.text = $"TIME: {timeStr}\nHOSTILES ELIMINATED: {totalKills} / {totalKills}\nSTEALTH KILLS: {stealthKills}\nSCORE: {score:N0} PTS\n\n{rank}";
        }

        Time.timeScale = 0f;
        isGamePaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Called when player dies
    public void ShowDefeat(string timeStr, int kills, int score)
    {
        if (defeatPanel != null) defeatPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);

        if (defeatStatsText != null)
        {
            defeatStatsText.text = $"TIME SURVIVED: {timeStr}\nHOSTILES ELIMINATED: {kills}\nSCORE: {score:N0} PTS";
        }

        Time.timeScale = 0f;
        isGamePaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Called when clicking "RESTART MISSION", "PLAY AGAIN", or "RETRY MISSION" button
    public void RestartGame()
    {
        Time.timeScale = 1f;
        isGamePaused = false;
        shouldStartDirectly = true; // Reload directly into gameplay without stopping at main menu
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Called when clicking "QUIT GAME" button
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #elif UNITY_WEBGL
        // In WebGL, Application.Quit() halts the WebAssembly runtime and permanently freezes the canvas.
        // If within game, pause, or end game, safely return to the Main Menu.
        // If already on Main Menu, reload the web page to cleanly reset the session.
        if (mainMenuPanel != null && mainMenuPanel.activeSelf)
        {
            #if !UNITY_EDITOR
            Application.OpenURL(Application.absoluteURL);
            #endif
        }
        else
        {
            ShowMainMenu();
        }
        #else
        Application.Quit();
        #endif
    }
}

