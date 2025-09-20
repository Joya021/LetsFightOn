using UnityEngine;
using System.Collections.Generic;

public class MiniMap : MonoBehaviour
{
    [Header("References")]
    public Camera minimapCamera;
    public Transform player;

    [Header("Minimap Settings")]
    public float mapScale = 1f;
    public LayerMask minimapLayers;

    [Header("Hunter Mode")]
    public bool isHunterMode = false; // Set this to true in the hunter scene

    // ⭐ Keep track of revealed intercoms (for survivor mode)
    private HashSet<GameObject> revealedIntercoms = new HashSet<GameObject>();

    void Start()
    {
        if (minimapCamera == null)
        {
            minimapCamera = GetComponent<Camera>();
        }

        // 🎯 In hunter mode, reveal all intercoms immediately
        if (isHunterMode)
        {
            RevealAllIntercomsForHunter();
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
    /// Called by InterCom when the survivor first interacts with it
    /// </summary>
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

    /// <summary>
    /// In hunter mode, reveal all intercom icons immediately
    /// </summary>
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

    /// <summary>
    /// Hide a specific intercom (if needed)
    /// </summary>
    public void HideIntercom(GameObject intercomIcon)
    {
        if (intercomIcon == null) return;

        if (revealedIntercoms.Contains(intercomIcon))
        {
            revealedIntercoms.Remove(intercomIcon);
        }

        intercomIcon.SetActive(false);
    }

    /// <summary>
    /// Reset all revealed intercoms (useful for restarting)
    /// </summary>
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