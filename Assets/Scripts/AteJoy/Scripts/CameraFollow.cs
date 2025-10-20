using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;
    public float smoothSpeed = 0.125f;

    private float cameraHalfHeight;
    private float cameraHalfWidth;

    private Vector2 minBounds;
    private Vector2 maxBounds;

    void Start()
    {
        cameraHalfHeight = Camera.main.orthographicSize;
        cameraHalfWidth = cameraHalfHeight * Camera.main.aspect;

        // ✅ Find the specific TilemapRenderer by name
        GameObject groundTilemapGO = GameObject.Find("Ground");
        if (groundTilemapGO != null)
        {
            TilemapRenderer renderer = groundTilemapGO.GetComponent<TilemapRenderer>();
            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;

                minBounds = new Vector2(
                    bounds.min.x + cameraHalfWidth,
                    bounds.min.y + cameraHalfHeight
                );

                maxBounds = new Vector2(
                    bounds.max.x - cameraHalfWidth,
                    bounds.max.y - cameraHalfHeight
                );
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        float clampedX = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
        float clampedY = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);

        Vector3 clampedPosition = new Vector3(clampedX, clampedY, transform.position.z);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, clampedPosition, smoothSpeed);

        transform.position = smoothedPosition;
    }
}