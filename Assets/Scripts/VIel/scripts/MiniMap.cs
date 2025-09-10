using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 🎯 New script for controlling the minimap camera and revealing interactable icons
public class MiniMap : MonoBehaviour
{
    // The main player object to follow
    public Transform playerTransform;

    // The camera used for the minimap
    public Camera minimapCamera;

    // ⚙️ This variable controls how smoothly the minimap camera follows the player.
    public float smoothSpeed = 0.125f;

    // A list of all intercoms in the scene. We will set this up in the Inspector.
    public List<GameObject> intercoms;

    // A reference to the player's minimap icon
    public GameObject playerIcon;

    void Start()
    {
        // 🔍 Get the minimap camera if it's not assigned
        if (minimapCamera == null)
        {
            minimapCamera = GetComponent<Camera>();
        }

        if (minimapCamera == null)
        {
            Debug.LogError("Minimap camera is not assigned or found!");
        }

        // 👻 Hide all intercoms at the start of the game
        HideAllIntercoms();
    }

    void LateUpdate()
    {
        // 🚶‍♂️ If the player and minimap camera exist, follow the player
        if (playerTransform != null && minimapCamera != null)
        {
            Vector3 targetPosition = new Vector3(playerTransform.position.x, playerTransform.position.y, transform.position.z);
            // ✨ Use Vector3.Lerp to smoothly move the camera towards the target position
            minimapCamera.transform.position = Vector3.Lerp(minimapCamera.transform.position, targetPosition, smoothSpeed);
        }
    }

    // 🔒 Make all intercom icons invisible
    private void HideAllIntercoms()
    {
        foreach (GameObject intercom in intercoms)
        {
            if (intercom != null)
            {
                intercom.SetActive(false);
            }
        }
    }

    // 🔓 Called by the CodeCheckGame script to reveal a specific intercom's icon
    public void RevealIntercom(GameObject intercomObject)
    {
        if (intercomObject != null)
        {
            // Activate the icon so it shows up on the minimap
            intercomObject.SetActive(true);
        }
    }
}
