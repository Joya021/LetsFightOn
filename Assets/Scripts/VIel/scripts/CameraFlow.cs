using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// CameraFlow automatically follows the local player's chosen prefab (hunter or survivor),
/// both in multiplayer (Photon) and offline modes. Works with delayed spawns.
/// </summary>
public class CameraFlow : MonoBehaviour
{
    private Transform target;
    public float smoothSpeed = 0.125f;

    private Vector2 minBounds;
    private Vector2 maxBounds;
    private Camera cam;
    private bool boundsSet = false;
    private bool searchingForTarget = false;

    void Start()
    {
        cam = Camera.main;

        // Automatically try to find local player after spawn
        StartCoroutine(FindLocalPlayerTarget());
    }

    void LateUpdate()
    {
        if (target == null)
        {
            // Don't spam every frame
            return;
        }

        if (cam == null)
        {
            Debug.LogWarning("[CameraFlow] Camera is null.");
            return;
        }

        if (!boundsSet)
        {
            // Optional: follow even without bounds
            Vector3 fallbackPos = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, fallbackPos, smoothSpeed);
            return;
        }

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float clampedX = Mathf.Clamp(target.position.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
        float clampedY = Mathf.Clamp(target.position.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);

        Vector3 targetPos = new Vector3(clampedX, clampedY, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed);
    }

    public void SetFollowTarget(Transform targetToFollow)
    {
        if (targetToFollow == null)
        {
            Debug.LogError("[CameraFlow] Trying to set null follow target!");
            return;
        }
        target = targetToFollow;
        Debug.Log($"[CameraFlow] Now following: {target.name}");
    }

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
        Debug.Log($"[CameraFlow] Bounds set: min {minBounds}, max {maxBounds}");
    }

    /// <summary>
    /// Continuously tries to find the local player object after scene load or network spawn.
    /// Works for both offline and Photon multiplayer.
    /// </summary>
    IEnumerator FindLocalPlayerTarget()
    {
        if (searchingForTarget) yield break;
        searchingForTarget = true;

        Debug.Log("[CameraFlow] Searching for local player to follow...");

        while (target == null)
        {
            if (PhotonNetwork.InRoom)
            {
                // Multiplayer mode: look for the object with PhotonView.IsMine == true
                PhotonView[] allViews = FindObjectsOfType<PhotonView>();
                foreach (var view in allViews)
                {
                    if (view.IsMine)
                    {
                        SetFollowTarget(view.transform);
                        searchingForTarget = false;
                        yield break;
                    }
                }
            }
            else
            {
                // Offline mode: just find the player-tagged object
                GameObject localPlayer = GameObject.FindGameObjectWithTag("Player");
                if (localPlayer != null)
                {
                    SetFollowTarget(localPlayer.transform);
                    searchingForTarget = false;
                    yield break;
                }
            }

            yield return new WaitForSeconds(0.5f);
        }

        searchingForTarget = false;
    }
}
