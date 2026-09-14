using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TacticalMinimap : MonoBehaviour
{
    [Header("Tracking")]
    public Transform playerTransform;
    public Transform playerCameraTransform;
    public float heightAbovePlayer = 75f;
    public float radarRadiusInMeters = 100f;
    public bool rotateWithPlayer = false;

    [Header("Camera & Render Target")]
    public Camera minimapCamera;
    public RenderTexture minimapTexture;

    [Header("UI References")]
    public RectTransform radarContainer;
    public RawImage radarDisplay;
    public RectTransform playerBlipRect;
    public List<RectTransform> enemyBlipPool = new List<RectTransform>();

    private const int POOL_SIZE = 30;
    private const float UI_RADAR_RADIUS = 72f; // Pixel radius for 100m in UI

    void Awake()
    {
        InitializeMinimapSystem();
    }

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (playerCameraTransform == null)
        {
            if (Camera.main != null)
            {
                playerCameraTransform = Camera.main.transform;
            }
            else if (playerTransform != null)
            {
                Camera childCam = playerTransform.GetComponentInChildren<Camera>();
                if (childCam != null) playerCameraTransform = childCam.transform;
            }
        }
    }

    public void InitializeMinimapSystem()
    {
        // 1. Setup Overhead Orthographic Camera
        SetupMinimapCamera();

        // 2. Build or Attach HUD Radar Interface
        SetupRadarUI();

        // 3. Create Hostile Blips Pool
        SetupBlipPool();
    }

    void SetupMinimapCamera()
    {
        if (minimapCamera == null)
        {
            GameObject camObj = GameObject.Find("MinimapCamera");
            if (camObj == null)
            {
                camObj = new GameObject("MinimapCamera");
            }
            minimapCamera = camObj.GetComponent<Camera>();
            if (minimapCamera == null)
            {
                minimapCamera = camObj.AddComponent<Camera>();
            }
        }

        // Configure Orthographic Camera with 100m coverage
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = radarRadiusInMeters;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.04f, 0.07f, 0.12f, 1f); // Dark tactical blueprint
        minimapCamera.cullingMask = LayerMask.GetMask("Default", "Obstacle");
        minimapCamera.nearClipPlane = 0.5f;
        minimapCamera.farClipPlane = 200f;
        minimapCamera.depth = -10;

        // Create 256x256 RenderTexture for high performance WebGL rendering
        if (minimapTexture == null)
        {
            minimapTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            minimapTexture.name = "MinimapRenderTexture";
            minimapTexture.filterMode = FilterMode.Bilinear;
            minimapTexture.wrapMode = TextureWrapMode.Clamp;
        }

        minimapCamera.targetTexture = minimapTexture;

        // Disable unnecessary URP passes on overhead camera if URP additional data is available
        var additionalData = minimapCamera.GetComponent("UniversalAdditionalCameraData");
        if (additionalData != null)
        {
            // Reflection-free property toggling
            var propShadows = additionalData.GetType().GetProperty("renderShadows");
            if (propShadows != null) propShadows.SetValue(additionalData, false);

            var propPost = additionalData.GetType().GetProperty("renderPostProcessing");
            if (propPost != null) propPost.SetValue(additionalData, false);
        }
    }

    void SetupRadarUI()
    {
        // Find or create Radar Container on the HUD
        Transform targetParent = transform;
        MenuManager mm = Object.FindAnyObjectByType<MenuManager>();
        if (mm != null && mm.hudPanel != null)
        {
            targetParent = mm.hudPanel.transform;
        }

        Transform existing = targetParent.Find("RadarContainer");
        if (existing != null)
        {
            radarContainer = existing.GetComponent<RectTransform>();
            radarDisplay = radarContainer.GetComponentInChildren<RawImage>();
            Transform pb = radarContainer.Find("RadarFrame/PlayerBlip");
            if (pb != null) playerBlipRect = pb.GetComponent<RectTransform>();
            return;
        }

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 1. Radar Container
        GameObject container = new GameObject("RadarContainer");
        container.transform.SetParent(targetParent, false);
        radarContainer = container.AddComponent<RectTransform>();
        radarContainer.anchorMin = new Vector2(1f, 1f);
        radarContainer.anchorMax = new Vector2(1f, 1f);
        radarContainer.pivot = new Vector2(1f, 1f);
        radarContainer.anchoredPosition = new Vector2(-30f, -85f);
        radarContainer.sizeDelta = new Vector2(180f, 210f);

        // 2. Header Banner ("RADAR // 100M SCAN")
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(container.transform, false);
        RectTransform headerRt = headerObj.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, 0f);
        headerRt.sizeDelta = new Vector2(0f, 24f);

        Image headerBg = headerObj.AddComponent<Image>();
        headerBg.color = new Color(0.06f, 0.10f, 0.16f, 0.85f);

        GameObject headerTextObj = new GameObject("Text");
        headerTextObj.transform.SetParent(headerObj.transform, false);
        RectTransform htRt = headerTextObj.AddComponent<RectTransform>();
        htRt.anchorMin = Vector2.zero;
        htRt.anchorMax = Vector2.one;
        htRt.sizeDelta = Vector2.zero;

        Text headerTxt = headerTextObj.AddComponent<Text>();
        headerTxt.font = uiFont;
        headerTxt.fontSize = 11;
        headerTxt.fontStyle = FontStyle.Bold;
        headerTxt.color = new Color(0.12f, 0.85f, 0.55f, 1f); // Tactical Cyan/Mint
        headerTxt.alignment = TextAnchor.MiddleCenter;
        headerTxt.text = "RADAR // 100M SCAN";

        // 3. Radar Bezel Frame
        GameObject frameObj = new GameObject("RadarFrame");
        frameObj.transform.SetParent(container.transform, false);
        RectTransform frameRt = frameObj.AddComponent<RectTransform>();
        frameRt.anchorMin = new Vector2(0.5f, 0f);
        frameRt.anchorMax = new Vector2(0.5f, 0f);
        frameRt.pivot = new Vector2(0.5f, 0f);
        frameRt.anchoredPosition = new Vector2(0f, 5f);
        frameRt.sizeDelta = new Vector2(175f, 175f);

        Image frameBg = frameObj.AddComponent<Image>();
        frameBg.color = new Color(0.04f, 0.07f, 0.12f, 0.90f);

        // 4. RawImage Minimap Display
        GameObject displayObj = new GameObject("MinimapDisplay");
        displayObj.transform.SetParent(frameObj.transform, false);
        RectTransform dispRt = displayObj.AddComponent<RectTransform>();
        dispRt.anchorMin = Vector2.zero;
        dispRt.anchorMax = Vector2.one;
        dispRt.sizeDelta = new Vector2(-10f, -10f);

        radarDisplay = displayObj.AddComponent<RawImage>();
        radarDisplay.texture = minimapTexture;
        radarDisplay.color = new Color(0.85f, 0.95f, 1f, 0.95f);

        // 5. Procedural Tactical Radar Rings & Reticle Overlay
        GameObject gridObj = new GameObject("RadarGrid");
        gridObj.transform.SetParent(frameObj.transform, false);
        RectTransform gridRt = gridObj.AddComponent<RectTransform>();
        gridRt.anchorMin = Vector2.zero;
        gridRt.anchorMax = Vector2.one;
        gridRt.sizeDelta = new Vector2(-10f, -10f);

        Image gridImg = gridObj.AddComponent<Image>();
        gridImg.sprite = CreateRadarGridSprite();
        gridImg.color = Color.white;
        gridImg.raycastTarget = false;

        // 6. Compass Direction Labels (N, S, E, W)
        CreateCompassLabel(frameObj.transform, "N", new Vector2(0f, 74f), new Color(1f, 0.35f, 0.35f, 1f), uiFont);
        CreateCompassLabel(frameObj.transform, "S", new Vector2(0f, -74f), new Color(0.7f, 0.8f, 0.9f, 0.8f), uiFont);
        CreateCompassLabel(frameObj.transform, "E", new Vector2(74f, 0f), new Color(0.7f, 0.8f, 0.9f, 0.8f), uiFont);
        CreateCompassLabel(frameObj.transform, "W", new Vector2(-74f, 0f), new Color(0.7f, 0.8f, 0.9f, 0.8f), uiFont);

        // 7. Player Blip (Cyan Chevron at Center)
        GameObject playerBlipObj = new GameObject("PlayerBlip");
        playerBlipObj.transform.SetParent(frameObj.transform, false);
        playerBlipRect = playerBlipObj.AddComponent<RectTransform>();
        playerBlipRect.anchorMin = new Vector2(0.5f, 0.5f);
        playerBlipRect.anchorMax = new Vector2(0.5f, 0.5f);
        playerBlipRect.pivot = new Vector2(0.5f, 0.5f);
        playerBlipRect.anchoredPosition = Vector2.zero;
        playerBlipRect.sizeDelta = new Vector2(16f, 16f);

        Image pbImg = playerBlipObj.AddComponent<Image>();
        pbImg.sprite = CreatePlayerChevronSprite();
        pbImg.color = new Color(0.15f, 0.95f, 1f, 1f); // Bright Neon Cyan
        pbImg.raycastTarget = false;
    }

    void SetupBlipPool()
    {
        enemyBlipPool.Clear();
        Transform frame = radarContainer != null ? radarContainer.Find("RadarFrame") : transform;
        if (frame == null) return;

        Sprite dotSprite = CreateEnemyDotSprite();

        for (int i = 0; i < POOL_SIZE; i++)
        {
            GameObject dotObj = new GameObject($"EnemyBlip_{i}");
            dotObj.transform.SetParent(frame, false);

            RectTransform rt = dotObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(10f, 10f);

            Image img = dotObj.AddComponent<Image>();
            img.sprite = dotSprite;
            img.color = new Color(1f, 0.25f, 0.25f, 0.95f); // Neon Enemy Crimson
            img.raycastTarget = false;

            dotObj.SetActive(false);
            enemyBlipPool.Add(rt);
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else return;
        }

        if (playerCameraTransform == null && Camera.main != null)
        {
            playerCameraTransform = Camera.main.transform;
        }

        // 1. Position overhead camera directly above the player
        if (minimapCamera != null)
        {
            Vector3 camPos = new Vector3(playerTransform.position.x, playerTransform.position.y + heightAbovePlayer, playerTransform.position.z);
            minimapCamera.transform.position = camPos;

            if (rotateWithPlayer)
            {
                minimapCamera.transform.rotation = Quaternion.Euler(90f, playerTransform.eulerAngles.y, 0f);
            }
            else
            {
                minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        // 2. Rotate player chevron on radar to match first-person look heading
        if (playerBlipRect != null && playerCameraTransform != null)
        {
            if (rotateWithPlayer)
            {
                playerBlipRect.localRotation = Quaternion.identity;
            }
            else
            {
                // Fixed North: Chevron points in camera direction relative to North (0 deg)
                playerBlipRect.localRotation = Quaternion.Euler(0f, 0f, -playerCameraTransform.eulerAngles.y);
            }
        }

        // 3. Update Enemy Radar Blips within 100m
        UpdateEnemyContacts();
    }

    void UpdateEnemyContacts()
    {
        if (enemyBlipPool.Count == 0 || playerTransform == null) return;

        EnemyAI[] enemies = Object.FindObjectsByType<EnemyAI>();
        int blipIndex = 0;

        Vector3 playerPos = playerTransform.position;
        float playerYaw = rotateWithPlayer ? playerTransform.eulerAngles.y : 0f;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null || blipIndex >= enemyBlipPool.Count) continue;

            Vector3 enemyPos = enemies[i].transform.position;
            float dx = enemyPos.x - playerPos.x;
            float dz = enemyPos.z - playerPos.z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);

            if (dist <= radarRadiusInMeters)
            {
                // Calculate UI coordinates relative to 100m radar bounds
                Vector2 worldOffset = new Vector2(dx, dz);
                if (rotateWithPlayer)
                {
                    // Rotate world offset into player local space
                    float angleRad = -playerYaw * Mathf.Deg2Rad;
                    float cos = Mathf.Cos(angleRad);
                    float sin = Mathf.Sin(angleRad);
                    worldOffset = new Vector2(worldOffset.x * cos - worldOffset.y * sin, worldOffset.x * sin + worldOffset.y * cos);
                }

                float uiX = (worldOffset.x / radarRadiusInMeters) * UI_RADAR_RADIUS;
                float uiY = (worldOffset.y / radarRadiusInMeters) * UI_RADAR_RADIUS;

                // Clamp within circular radar perimeter
                Vector2 blipPos = new Vector2(uiX, uiY);
                if (blipPos.magnitude > UI_RADAR_RADIUS)
                {
                    blipPos = blipPos.normalized * UI_RADAR_RADIUS;
                }

                RectTransform blip = enemyBlipPool[blipIndex];
                blip.anchoredPosition = blipPos;
                blip.gameObject.SetActive(true);
                blipIndex++;
            }
        }

        // Hide remaining unused blips in pool
        for (int k = blipIndex; k < enemyBlipPool.Count; k++)
        {
            if (enemyBlipPool[k].gameObject.activeSelf)
            {
                enemyBlipPool[k].gameObject.SetActive(false);
            }
        }
    }

    private void CreateCompassLabel(Transform parent, string text, Vector2 pos, Color color, Font font)
    {
        GameObject obj = new GameObject($"Label_{text}");
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(20f, 20f);

        Text txt = obj.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = 11;
        txt.fontStyle = FontStyle.Bold;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = text;
        txt.raycastTarget = false;
    }

    private Sprite CreateRadarGridSprite()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color ring100 = new Color(0.12f, 0.85f, 0.55f, 0.70f); // 100m Perimeter ring
        Color ring50 = new Color(0.12f, 0.85f, 0.55f, 0.35f);  // 50m Mid ring
        Color crosshair = new Color(0.12f, 0.85f, 0.55f, 0.20f); // Reticle lines
        Color fill = new Color(0.04f, 0.08f, 0.14f, 0.25f);

        float center = size / 2f;
        float rOuter = 118f;
        float rInner = 59f;

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                int idx = y * size + x;

                if (r > rOuter + 2f)
                {
                    pixels[idx] = clear;
                }
                else if (Mathf.Abs(r - rOuter) <= 1.5f)
                {
                    pixels[idx] = ring100;
                }
                else if (Mathf.Abs(r - rInner) <= 1.2f)
                {
                    pixels[idx] = ring50;
                }
                else if (r < rOuter && (Mathf.Abs(dx) < 1f || Mathf.Abs(dy) < 1f))
                {
                    pixels[idx] = crosshair;
                }
                else if (r < rOuter)
                {
                    pixels[idx] = fill;
                }
                else
                {
                    pixels[idx] = clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreatePlayerChevronSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color cyan = new Color(0.15f, 0.95f, 1f, 1f);
        Color white = Color.white;

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        for (int y = 4; y < 28; y++)
        {
            float widthAtY = (28 - y) * 0.55f;
            float mid = 16f;
            for (int x = (int)(mid - widthAtY); x <= (int)(mid + widthAtY); x++)
            {
                if (x >= 0 && x < size)
                {
                    // Notched base for arrow look
                    if (y < 12 && Mathf.Abs(x - mid) < 3) continue;
                    bool isCore = Mathf.Abs(x - mid) <= 1;
                    pixels[y * size + x] = isCore ? white : cyan;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateEnemyDotSprite()
    {
        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color redGlow = new Color(1f, 0.2f, 0.2f, 0.95f);
        Color whiteCore = new Color(1f, 0.9f, 0.8f, 1f);

        float center = size / 2f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                int idx = y * size + x;
                if (dist <= 2.5f) pixels[idx] = whiteCore;
                else if (dist <= 6.0f) pixels[idx] = redGlow;
                else pixels[idx] = clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    void OnDestroy()
    {
        if (minimapTexture != null)
        {
            minimapTexture.Release();
            Destroy(minimapTexture);
        }
    }
}
