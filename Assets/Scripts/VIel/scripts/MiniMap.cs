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

    
    private HashSet<GameObject> revealedIntercoms = new HashSet<GameObject>();

    void Start()
    {
        if (minimapCamera == null)
        {
            minimapCamera = GetComponent<Camera>();
        }

        
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