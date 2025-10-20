using UnityEngine;

/// <summary>
/// Forces game viewport to EXACTLY 1920x1080 on ANY device
/// Your 2160x1080 phone will show black bars on the sides
/// What you see at 1920x1080 in Unity editor is what players see
/// </summary>
public class Force1920x1080 : MonoBehaviour
{
    private Camera mainCam;
    private Camera bgCam;
    private const float TARGET_ASPECT = 16f / 9f; // 1920/1080 = 1.777...

    void Awake()
    {
        mainCam = GetComponent<Camera>();
        ApplyForced1080pViewport();
    }

    void ApplyForced1080pViewport()
    {
        // Get actual screen aspect ratio
        float screenAspect = (float)Screen.width / (float)Screen.height;

        Debug.Log($"Device Screen: {Screen.width}x{Screen.height} (aspect: {screenAspect:F3})");
        Debug.Log($"Target Aspect: 16:9 (1.778)");

        // Create black background camera
        CreateBackgroundCamera();

        // Calculate viewport rectangle to maintain 16:9 aspect
        if (screenAspect > TARGET_ASPECT)
        {
            // Screen is WIDER than 16:9 (like your 2160x1080 = 2:1)
            // Add black bars on LEFT and RIGHT
            float scaleWidth = TARGET_ASPECT / screenAspect;

            Rect rect = mainCam.rect;
            rect.width = scaleWidth;      // Narrower viewport
            rect.height = 1.0f;            // Full height
            rect.x = (1.0f - scaleWidth) / 2.0f;  // Center horizontally
            rect.y = 0;
            mainCam.rect = rect;

            Debug.Log($"PILLARBOXING: Viewport width = {scaleWidth:F3}, bars on sides");
        }
        else if (screenAspect < TARGET_ASPECT)
        {
            // Screen is TALLER than 16:9 (rare)
            // Add black bars on TOP and BOTTOM
            float scaleHeight = screenAspect / TARGET_ASPECT;

            Rect rect = mainCam.rect;
            rect.width = 1.0f;             // Full width
            rect.height = scaleHeight;     // Shorter viewport
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;  // Center vertically
            mainCam.rect = rect;

            Debug.Log($"LETTERBOXING: Viewport height = {scaleHeight:F3}, bars on top/bottom");
        }
        else
        {
            // Perfect 16:9 match
            Debug.Log("Perfect 16:9 match - no bars needed");
        }
    }

    void CreateBackgroundCamera()
    {
        if (bgCam != null) return;

        GameObject bgObj = new GameObject("BackgroundCamera_1080p");
        bgCam = bgObj.AddComponent<Camera>();

        // Render BEHIND main camera
        bgCam.depth = mainCam.depth - 10;
        bgCam.clearFlags = CameraClearFlags.SolidColor;
        bgCam.backgroundColor = Color.black;
        bgCam.cullingMask = 0; // Render nothing, just clear to black
        bgCam.farClipPlane = 0.1f;
        bgCam.nearClipPlane = 0.01f;
    }

    // Handle device rotation or resolution changes
    void Update()
    {
        float currentAspect = (float)Screen.width / (float)Screen.height;
        if (Mathf.Abs(currentAspect - lastAspect) > 0.01f)
        {
            lastAspect = currentAspect;
            ApplyForced1080pViewport();
        }
    }

    private float lastAspect = 0;
}