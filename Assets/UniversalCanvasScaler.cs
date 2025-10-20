using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Makes Canvas automatically adapt to ANY screen size and aspect ratio
/// Attach this to every Canvas in your game
/// </summary>
public class UniversalCanvasScaler : MonoBehaviour
{
    [Header("Reference Resolution")]
    [Tooltip("Your design resolution (keep as 1920x1080)")]
    public Vector2 referenceResolution = new Vector2(1920, 1080);
    [Header("Scaling Strategy")]
    [Tooltip("Auto: Automatically chooses best method for each device")]
    public ScalingMode scalingMode = ScalingMode.Auto;

    [Header("Safe Area")]
    [Tooltip("Enable to handle notches and punch holes")]
    public bool useSafeArea = true;

    [Tooltip("Padding from screen edges (in pixels at reference resolution)")]
    public Vector4 safeAreaPadding = new Vector4(0, 0, 0, 0); // left, right, top, bottom

    private CanvasScaler canvasScaler;
    private RectTransform canvasRect;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    public enum ScalingMode
    {
        Auto,           // Automatically adapt based on aspect ratio
        MatchWidth,     // Always scale based on width (best for ultra-wide)
        MatchHeight,    // Always scale based on height (best for tall screens)
        Balanced        // Equal weight to width and height
    }

    void Awake()
    {
        canvasScaler = GetComponent<CanvasScaler>();
        canvasRect = GetComponent<RectTransform>();

        if (canvasScaler == null)
        {
            Debug.LogError("UniversalCanvasScaler requires a CanvasScaler component!");
            return;
        }

        SetupCanvasScaler();
        ApplyScaling();
    }

    void Start()
    {
        ApplyScaling();
    }

    void Update()
    {
        // Check if screen size or safe area changed (handles rotation, folding screens, etc)
        if (HasScreenChanged())
        {
            ApplyScaling();
        }
    }

    void SetupCanvasScaler()
    {
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
    }

    void ApplyScaling()
    {
        if (canvasScaler == null) return;

        float currentAspect = (float)Screen.width / Screen.height;
        float referenceAspect = referenceResolution.x / referenceResolution.y;

        // Determine match value based on scaling mode
        float matchValue = CalculateMatchValue(currentAspect, referenceAspect);
        canvasScaler.matchWidthOrHeight = matchValue;

        // Apply safe area if enabled
        if (useSafeArea)
        {
            ApplySafeArea();
        }

        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastSafeArea = Screen.safeArea;

        Debug.Log($"Canvas scaled for {Screen.width}x{Screen.height}, Aspect: {currentAspect:F2}, Match: {matchValue:F2}");
    }

    float CalculateMatchValue(float currentAspect, float referenceAspect)
    {
        switch (scalingMode)
        {
            case ScalingMode.MatchWidth:
                return 0f;

            case ScalingMode.MatchHeight:
                return 1f;

            case ScalingMode.Balanced:
                return 0.5f;

            case ScalingMode.Auto:
            default:
                // Wider than reference (like 21:9, 18:9)
                if (currentAspect > referenceAspect * 1.05f)
                {
                    return 0f; // Match width for ultra-wide
                }
                // Taller than reference (like 4:3, tablets)
                else if (currentAspect < referenceAspect * 0.95f)
                {
                    return 1f; // Match height for tall screens
                }
                // Close to reference aspect ratio
                else
                {
                    return 0.5f; // Balanced
                }
        }
    }

    void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;

        // Convert safe area to anchors
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        // Apply custom padding
        anchorMin.x += safeAreaPadding.x;
        anchorMin.y += safeAreaPadding.w;
        anchorMax.x -= safeAreaPadding.y;
        anchorMax.y -= safeAreaPadding.z;

        // Normalize to 0-1 range
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // Clamp values
        anchorMin.x = Mathf.Clamp01(anchorMin.x);
        anchorMin.y = Mathf.Clamp01(anchorMin.y);
        anchorMax.x = Mathf.Clamp01(anchorMax.x);
        anchorMax.y = Mathf.Clamp01(anchorMax.y);

        canvasRect.anchorMin = anchorMin;
        canvasRect.anchorMax = anchorMax;
    }

    bool HasScreenChanged()
    {
        return Screen.width != lastScreenSize.x ||
               Screen.height != lastScreenSize.y ||
               Screen.safeArea != lastSafeArea;
    }

    // Call this if you manually change settings at runtime
    public void RefreshScaling()
    {
        ApplyScaling();
    }
}
/// <summary>
/// OPTIONAL: Attach to individual UI panels for extra control
/// Keeps UI elements within safe boundaries
/// </summary>
public class SafeAreaPanel : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea;
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        ApplySafeArea();
    }

    void Update()
    {
        if (Screen.safeArea != lastSafeArea)
        {
            ApplySafeArea();
        }
    }

    void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        lastSafeArea = safeArea;
    }
}
/// <summary>
/// OPTIONAL: Advanced version with per-device overrides
/// Attach to Canvas if you need specific behavior for certain devices
/// </summary>
public class AdvancedCanvasScaler : MonoBehaviour
{
    [System.Serializable]
    public class AspectRatioRule
    {
        public string name;
        public float minAspect;
        public float maxAspect;
        public float matchValue;
    }
    public Vector2 referenceResolution = new Vector2(1920, 1080);
    public AspectRatioRule[] customRules;

    private CanvasScaler canvasScaler;

    void Awake()
    {
        canvasScaler = GetComponent<CanvasScaler>();
        SetupScaler();
    }

    void SetupScaler()
    {
        if (canvasScaler == null) return;

        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        float aspect = (float)Screen.width / Screen.height;
        float matchValue = 0.5f; // Default

        // Check custom rules
        if (customRules != null)
        {
            foreach (var rule in customRules)
            {
                if (aspect >= rule.minAspect && aspect <= rule.maxAspect)
                {
                    matchValue = rule.matchValue;
                    Debug.Log($"Applied custom rule: {rule.name} for aspect {aspect:F2}");
                    break;
                }
            }
        }

        canvasScaler.matchWidthOrHeight = matchValue;
    }
}