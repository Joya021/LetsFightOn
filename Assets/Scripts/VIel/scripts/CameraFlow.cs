using UnityEngine;

public class CameraFlow : MonoBehaviour
{
    public Transform player;
    public float smoothSpeed = 0.125f;

    private Vector2 minBounds;
    private Vector2 maxBounds;
    private Camera cam;
    private bool boundsSet = false;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (player == null || cam == null || !boundsSet) return;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float clampedX = Mathf.Clamp(player.position.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
        float clampedY = Mathf.Clamp(player.position.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);

        Vector3 targetPos = new Vector3(clampedX, clampedY, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed);
    }

    // 📏 Now works with maps that have multiple renderers (Tilemaps, sprites, etc.)
    public void SetBoundsFromMultipleRenderers(Renderer[] renderers)
    {
        if (renderers == null || renderers.Length == 0) return;

        Bounds combinedBounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            combinedBounds.Encapsulate(r.bounds);
        }

        minBounds = combinedBounds.min;
        maxBounds = combinedBounds.max;
        boundsSet = true;
    }
}