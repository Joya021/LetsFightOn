/*
 * Individual Survivor Death System - FIXED
 * - Shows "You are dead!" panel only to the specific dead survivor
 * - Game continues for other players
 * - Works with your actual PlayerMovement script
 */
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;
public class SurvivorDeathPanel : MonoBehaviourPunCallbacks
{
    [Header("Death UI - Individual Survivor Only")]
    public GameObject deathPanel;
    public Text deathMessageText;
    public Button respawnButton; // Optional: if you want respawn functionality
    [Header("Settings")]
    public float checkInterval = 0.5f; // How often to check if this survivor is dead

    private bool isDead = false;
    private bool wasAliveLastFrame = true;
    private PlayerMovement playerMovement;
    private HunterController hunterController;
    private PhotonView photonView;
    private GameManager gameManager;
    void Start()
    {
        // Get components
        playerMovement = GetComponent<PlayerMovement>();
        hunterController = GetComponent<HunterController>();
        photonView = GetComponent<PhotonView>();
        gameManager = FindObjectOfType<GameManager>();
        // Only set up death detection for survivors (not hunters)
        bool isSurvivor = (playerMovement != null && hunterController == null);

        if (!isSurvivor)
        {
            // This is a hunter, disable this script
            this.enabled = false;
            return;
        }
        // Only show death UI for LOCAL player
        if (photonView != null && !photonView.IsMine && PhotonNetwork.IsConnected)
        {
            // This is not the local player, disable UI
            if (deathPanel != null)
                deathPanel.SetActive(false);
            this.enabled = false;
            return;
        }
        // Setup death panel
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
        if (respawnButton != null)
        {
            respawnButton.onClick.AddListener(RequestRespawn);
        }
        // Start monitoring this survivor's health
        StartCoroutine(MonitorIndividualHealth());
        Debug.Log($"[SurvivorDeathPanel] Setup complete for LOCAL survivor: {gameObject.name}");
    }
    IEnumerator MonitorIndividualHealth()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            CheckIndividualSurvivorStatus();
        }
    }
    void CheckIndividualSurvivorStatus()
    {
        if (playerMovement == null) return;
        // Check if THIS specific survivor is dead
        bool isCurrentlyDead = CheckIfThisSurvivorIsDead();

        // If survivor just died (transition from alive to dead)
        if (isCurrentlyDead && !isDead)
        {
            // Survivor just died
            OnSurvivorDied();
        }
        // If survivor just came back to life (if you have respawn mechanics)
        else if (!isCurrentlyDead && isDead)
        {
            // Survivor respawned/revived
            OnSurvivorRevived();
        }
        isDead = isCurrentlyDead;
        wasAliveLastFrame = !isCurrentlyDead;
    }
    bool CheckIfThisSurvivorIsDead()
    {
        if (playerMovement == null) return false;
        // Method 1: Check GameManager HP (main death condition)
        if (gameManager != null && gameManager.currentHP <= 0)
        {
            return true;
        }
        // Method 2: Check if survivor is permanently stunned (optional death condition)
        if (playerMovement.isStunned)
        {
            // You can add additional logic here if needed
            // For example: stunned for more than X seconds = dead
            return false; // For now, being stunned doesn't mean dead
        }
        // Method 3: Check if player object is inactive
        if (!gameObject.activeInHierarchy)
        {
            return true;
        }
        // Method 4: Check if player can't move (optional)
        if (!playerMovement.canMove)
        {
            // You can add additional "elimination" conditions here
            // For example: can't move for more than X seconds, etc.
            return false; // For now, can't move doesn't mean dead
        }
        // Add your specific death conditions here
        // Example: check if HP is 0 or below
        // Default: survivor is alive
        return false;
    }
    void OnSurvivorDied()
    {
        isDead = true;

        Debug.Log($"[SurvivorDeathPanel] {gameObject.name} has died! Showing death panel.");

        // Show death panel ONLY to this specific survivor
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);

            if (deathMessageText != null)
            {
                deathMessageText.text = $"You are dead!\nWait for other survivors or respawn.";
            }
        }
        // Optional: Disable player controls while dead
        if (playerMovement != null)
        {
            playerMovement.LockMovement();
        }
        // Game continues for everyone else - NO game ending logic
    }
    void OnSurvivorRevived()
    {
        isDead = false;

        Debug.Log($"[SurvivorDeathPanel] {gameObject.name} has been revived!");

        // Hide death panel
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
        // Re-enable player controls
        if (playerMovement != null)
        {
            playerMovement.UnlockMovement();
        }
    }
    void RequestRespawn()
    {
        Debug.Log($"[SurvivorDeathPanel] {gameObject.name} requesting respawn...");

        // Implement respawn logic here
        // Example: reset health, remove stun, teleport to spawn point, etc.

        if (playerMovement != null)
        {
            // Unlock movement
            playerMovement.UnlockMovement();

            // Reset health via GameManager if available
            if (gameManager != null)
            {
                // Restore some HP (you can adjust this)
                gameManager.HealPlayer(gameManager.maxHP); // Full heal
            }

            // Optional: teleport to spawn point
            // transform.position = GetRandomSpawnPoint();
        }

        // This will trigger OnSurvivorRevived() in the next check
    }
    // Optional: Method to manually kill this survivor (for testing or game mechanics)
    public void KillThisSurvivor()
    {
        if (gameManager != null)
        {
            // Deal damage to kill the survivor
            while (gameManager.currentHP > 0)
            {
                gameManager.TakeDamage(false);
            }
        }
    }
    // Optional: Method to manually revive this survivor
    public void ReviveThisSurvivor()
    {
        if (playerMovement != null)
        {
            playerMovement.UnlockMovement();
        }

        if (gameManager != null)
        {
            // Restore full health
            gameManager.HealPlayer(gameManager.maxHP);
        }
    }
    // Public method to check if this survivor is dead (for other scripts)
    public bool IsThisSurvivorDead()
    {
        return isDead;
    }
    // Optional: Context menu methods for testing in editor
    [ContextMenu("Test Death")]
    void TestDeath()
    {
        KillThisSurvivor();
    }
    [ContextMenu("Test Revive")]
    void TestRevive()
    {
        ReviveThisSurvivor();
    }
    void OnDisable()
    {
        // Hide death panel when script is disabled
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
    }
}