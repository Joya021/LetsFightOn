// InterCom.cs
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Photon.Pun;
using ExitGames.Client.Photon;
using Photon.Realtime;

public class InterCom : MonoBehaviour
{
    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.F;

    [Header("References")]
    public CodeCheckGame codeCheckGame;
    public GameManager gameManager;

    [Header("Minimap Icon")]
    public GameObject minimapIcon;

    private bool isPlayerNearby = false;
    private MiniMap miniMap;

    // Network sync - positions are set by MasterClient
    public static readonly string INTERCOM_POSITIONS_KEY = "IntercomPositions";

    void Start()
    {
        miniMap = FindObjectOfType<MiniMap>();
    }

    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(interactKey))
        {
            if (codeCheckGame != null && !codeCheckGame.isOnCooldown)
            {
                if (gameManager != null && gameManager.gameEnded) return;

                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayIntercomInteract();

                codeCheckGame.OpenCodePanel();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Prefer Photon detection, but fall back for offline/local players
        PhotonView pv = other.GetComponent<PhotonView>();
        PlayerMovement pm = other.GetComponent<PlayerMovement>();

        // If we're in offline mode, any local player collider should be able to interact
        if (PhotonNetwork.OfflineMode)
        {
            isPlayerNearby = true;
            return;
        }

        // Networked: only mark nearby for the *local* player's own object
        if (pv != null && pv.IsMine)
        {
            isPlayerNearby = true;
            return;
        }

        // As a safety fallback: if there's no PhotonView but it's a PlayerMovement and it appears active, allow interaction
        if (pv == null && pm != null && pm.enabled)
        {
            isPlayerNearby = true;
            return;
        }

        // otherwise don't set nearby
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PhotonView pv = other.GetComponent<PhotonView>();
        PlayerMovement pm = other.GetComponent<PlayerMovement>();

        if (PhotonNetwork.OfflineMode)
        {
            isPlayerNearby = false;
            return;
        }

        if (pv != null && pv.IsMine)
        {
            isPlayerNearby = false;
            return;
        }

        if (pv == null && pm != null && pm.enabled)
        {
            isPlayerNearby = false;
            return;
        }
    }

    public void OnInteractionComplete()
    {
        if (miniMap != null && minimapIcon != null)
        {
            miniMap.RevealIntercom(minimapIcon);
        }
    }
}
