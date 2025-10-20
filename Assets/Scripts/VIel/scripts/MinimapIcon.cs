using UnityEngine;

public class MinimapIcon : MonoBehaviour
{
    public Transform target;                 // The world-space object this icon represents
    public RectTransform minimapRect;       // The minimap RawImage RectTransform
    public float mapWorldSize = 100f;       // Width/height of the world shown on the minimap

    private RectTransform iconRect;

    void Start()
    {
        iconRect = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (target == null || minimapRect == null) return;

        // Get relative world position (-0.5 to 0.5 range)
        Vector3 targetPos = target.position;
        float normalizedX = Mathf.Clamp(targetPos.x / mapWorldSize, -0.5f, 0.5f);
        float normalizedZ = Mathf.Clamp(targetPos.z / mapWorldSize, -0.5f, 0.5f); // Assuming Y-up: use Z

        // Convert to UI position inside minimap
        float x = normalizedX * minimapRect.rect.width;
        float y = normalizedZ * minimapRect.rect.height;

        // Clamp inside minimap bounds
        float halfWidth = minimapRect.rect.width / 2f;
        float halfHeight = minimapRect.rect.height / 2f;
        x = Mathf.Clamp(x, -halfWidth, halfWidth);
        y = Mathf.Clamp(y, -halfHeight, halfHeight);

        iconRect.anchoredPosition = new Vector2(x, y);
    }
}
