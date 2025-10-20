using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MiniMap : MonoBehaviour
{
    [Header("References")]
    public Camera minimapCamera;
    public Transform player; // This will be auto-assigned now

    [Header("Camera Flow Reference")]
    public CameraFlow mainCameraFlow; // Assign your CameraFlow script here

    [Header("Minimap Settings")]
    public float mapScale = 1f;
    public LayerMask minimapLayers;

    [Header("Hunter Mode")]
    public bool isHunterMode = false; // Set this to true in the hunter scene

    private HashSet<GameObject> revealedIntercoms = new HashSet<GameObject>();
    private bool searchingForPlayer = false;

    void Start()
    {
        if (minimapCamera == null)
        {
            minimapCamera = GetComponent<Camera>();
        }

        // Auto-find CameraFlow if not assigned
        if (mainCameraFlow == null)
        {
            mainCameraFlow = Camera.main?.GetComponent<CameraFlow>();
            if (mainCameraFlow == null)
            {
                Debug.LogWarning("[MiniMap] CameraFlow not found on main camera!");
            }
        }

        // Start searching for the player
        StartCoroutine(FindPlayerFromCameraFlow());

        if (isHunterMode)
        {
            RevealAllIntercomsForHunter();
        }
        else
        {
            HideAllIntercomsForSurvivor();
        }
    }

    void Update()
    {
        // Follow the player
        if (player != null && minimapCamera != null)
        {
            Vector3 newPosition = player.position;
            newPosition.z = minimapCamera.transform.position.z; // Keep the camera's Z position
            minimapCamera.transform.position = newPosition;
        }
    }

    /// <summary>
    /// Continuously tries to get the player target from CameraFlow
    /// </summary>
    IEnumerator FindPlayerFromCameraFlow()
    {
        if (searchingForPlayer) yield break;
        searchingForPlayer = true;

        Debug.Log("[MiniMap] Searching for player from CameraFlow...");

        while (player == null)
        {
            // Try to get the target from CameraFlow
            if (mainCameraFlow != null)
            {
                Transform cameraTarget = mainCameraFlow.GetTarget();
                if (cameraTarget != null)
                {
                    player = cameraTarget;
                    Debug.Log($"[MiniMap] Now following player: {player.name}");
                    searchingForPlayer = false;
                    yield break;
                }
            }

            yield return new WaitForSeconds(0.5f);
        }

        searchingForPlayer = false;
    }

    public void RevealIntercom(GameObject intercomIcon)
    {
        if (intercomIcon == null) return;

        // 🔍 Only reveal if we're in survivor mode
        if (!isHunterMode && !revealedIntercoms.Contains(intercomIcon))
        {
            revealedIntercoms.Add(intercomIcon);
            intercomIcon.SetActive(true);
            Debug.Log($"[MiniMap] Revealed intercom icon: {intercomIcon.name}");
        }
    }

    private void RevealAllIntercomsForHunter()
    {
        // Find all InterCom objects in the scene
        InterCom[] allIntercoms = FindObjectsOfType<InterCom>();
        foreach (InterCom intercom in allIntercoms)
        {
            if (intercom.minimapIcon != null)
            {
                intercom.minimapIcon.SetActive(true);
                Debug.Log($"[MiniMap] Hunter mode: Revealed {intercom.minimapIcon.name}");
            }
        }
    }

    private void HideAllIntercomsForSurvivor()
    {
        // Find all InterCom objects in the scene
        InterCom[] allIntercoms = FindObjectsOfType<InterCom>();
        foreach (InterCom intercom in allIntercoms)
        {
            if (intercom.minimapIcon != null)
            {
                intercom.minimapIcon.SetActive(false);
                Debug.Log($"[MiniMap] Survivor mode: Hid {intercom.minimapIcon.name}");
            }
        }
    }

    public void HideIntercom(GameObject intercomIcon)
    {
        if (intercomIcon == null) return;
        if (revealedIntercoms.Contains(intercomIcon))
        {
            revealedIntercoms.Remove(intercomIcon);
        }
        intercomIcon.SetActive(false);
    }

    public void ResetRevealedIntercoms()
    {
        foreach (GameObject icon in revealedIntercoms)
        {
            if (icon != null)
                icon.SetActive(false);
        }
        revealedIntercoms.Clear();
    }
}