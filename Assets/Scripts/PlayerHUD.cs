using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("Player References")]
    public PlayerHealth playerHealth;
    public PlayerMovement playerMovement;

    [Header("Health Bar UI")]
    public Image healthFill;
    public Text healthText;
    public Color healthyColor = new Color(0.12f, 0.85f, 0.55f, 1f); // Tactical Green/Cyan
    public Color warningColor = new Color(1f, 0.8f, 0.15f, 1f);      // Amber/Yellow
    public Color dangerColor = new Color(0.95f, 0.2f, 0.2f, 1f);      // Red

    [Header("Stealth / Acoustic UI")]
    public Text stealthStatusText;

    [Header("Mission Status UI")]
    public Text hostilesText;
    public Text timerText;
    public Text scoreText;

    [Header("Damage Flash")]
    public Image damageFlashImage;
    public float flashFadeSpeed = 3f;

    private int previousHealth;

    void Start()
    {
        // Auto-find components if not assigned in Inspector
        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        if (playerMovement == null)
        {
            playerMovement = FindAnyObjectByType<PlayerMovement>();
        }

        if (playerHealth != null)
        {
            previousHealth = playerHealth.CurrentHealth;
        }

        if (damageFlashImage != null)
        {
            Color c = damageFlashImage.color;
            c.a = 0f;
            damageFlashImage.color = c;
        }

        EnsureMissionHUD();
        EnsureTacticalMinimap();
    }

    void EnsureTacticalMinimap()
    {
        if (GetComponent<TacticalMinimap>() == null)
        {
            gameObject.AddComponent<TacticalMinimap>();
        }
    }

    void EnsureMissionHUD()
    {
        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        if (hostilesText == null)
        {
            Transform t = transform.Find("HostilesPill");
            if (t != null) hostilesText = t.GetComponentInChildren<Text>();
            else hostilesText = CreateTopPill("HostilesPill", new Vector2(30, -30), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), TextAnchor.MiddleLeft, uiFont, 16, Color.white, new Vector2(240, 42));
        }

        if (timerText == null)
        {
            Transform t = transform.Find("TimerPill");
            if (t != null) timerText = t.GetComponentInChildren<Text>();
            else timerText = CreateTopPill("TimerPill", new Vector2(0, -30), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), TextAnchor.MiddleCenter, uiFont, 20, new Color(0.12f, 0.85f, 0.55f, 1f), new Vector2(200, 42));
        }

        if (scoreText == null)
        {
            Transform t = transform.Find("ScorePill");
            if (t != null) scoreText = t.GetComponentInChildren<Text>();
            else scoreText = CreateTopPill("ScorePill", new Vector2(-30, -30), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), TextAnchor.MiddleRight, uiFont, 16, new Color(1f, 0.85f, 0.2f, 1f), new Vector2(240, 42));
        }
    }

    private Text CreateTopPill(string objName, Vector2 anchoredPos, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, TextAnchor alignment, Font font, int fontSize, Color textColor, Vector2 size)
    {
        GameObject pill = new GameObject(objName);
        pill.transform.SetParent(transform, false);

        RectTransform rt = pill.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image bg = pill.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.08f, 0.14f, 0.75f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(pill.transform, false);

        RectTransform trt = textObj.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = new Vector2(-20, -10);

        Text txt = textObj.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.color = textColor;
        txt.alignment = alignment;
        txt.raycastTarget = false;

        return txt;
    }

    void Update()
    {
        UpdateHealthUI();
        UpdateStealthUI();
        UpdateMissionUI();
        UpdateDamageFlash();
    }

    void UpdateMissionUI()
    {
        if (GameManager.Instance == null) return;

        if (hostilesText != null)
        {
            hostilesText.text = $"HOSTILES: {GameManager.Instance.remainingHostiles} / {GameManager.Instance.totalHostiles}";
        }

        if (timerText != null)
        {
            timerText.text = $"TIME: {GameManager.FormatTime(GameManager.Instance.missionTime)}";
        }

        if (scoreText != null)
        {
            scoreText.text = $"SCORE: {GameManager.Instance.currentScore:N0} PTS";
        }
    }

    void UpdateHealthUI()
    {
        if (playerHealth == null || healthFill == null) return;

        int current = Mathf.Max(0, playerHealth.CurrentHealth);
        int max = playerHealth.maxHealth;

        // Detect damage taken to trigger red vignette flash
        if (current < previousHealth)
        {
            TriggerDamageFlash();
        }
        previousHealth = current;

        // Calculate fill percentage (0.0 to 1.0)
        float fillPct = max > 0 ? (float)current / max : 0f;
        healthFill.fillAmount = fillPct;

        // Dynamic color changes as health drops
        if (fillPct > 0.5f)
        {
            healthFill.color = healthyColor;
        }
        else if (fillPct > 0.25f)
        {
            healthFill.color = warningColor;
        }
        else
        {
            healthFill.color = dangerColor;
        }

        // Update numerical readout
        if (healthText != null)
        {
            healthText.text = $"{current} / {max} HP";
        }
    }

    void UpdateStealthUI()
    {
        if (stealthStatusText == null) return;

        // Check if player is sprinting (holding Left Shift and moving)
        bool isMoving = Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && isMoving;

        if (isSprinting)
        {
            stealthStatusText.text = "ACOUSTIC: SPRINTING (NOISY)";
            stealthStatusText.color = dangerColor;
        }
        else if (isMoving)
        {
            stealthStatusText.text = "ACOUSTIC: WALKING (STEALTH)";
            stealthStatusText.color = healthyColor;
        }
        else
        {
            stealthStatusText.text = "ACOUSTIC: SILENT";
            stealthStatusText.color = new Color(0.7f, 0.85f, 1f, 0.8f);
        }
    }

    public void TriggerDamageFlash()
    {
        if (damageFlashImage != null)
        {
            Color c = damageFlashImage.color;
            c.a = 0.4f; // Flash red overlay
            damageFlashImage.color = c;
        }
    }

    void UpdateDamageFlash()
    {
        if (damageFlashImage == null) return;

        // Smoothly fade red vignette back to transparent
        if (damageFlashImage.color.a > 0f)
        {
            Color c = damageFlashImage.color;
            c.a = Mathf.MoveTowards(c.a, 0f, flashFadeSpeed * Time.unscaledDeltaTime);
            damageFlashImage.color = c;
        }
    }
}
