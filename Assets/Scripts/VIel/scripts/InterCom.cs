using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class InterCom : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Collider2D[] spawnAreas;
    public float minDistanceBetweenObjects = 3f;
    public LayerMask obstacleLayerMask = -1; // Layers to avoid when spawning
    public float spawnCheckRadius = 0.5f;

    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.F;
    public string playerTag = "Player"; // Specify which tag should trigger interaction

    [Header("References")]
    public CodeCheckGame codeCheckGame;
    public GameManager gameManager; // Reference to check if game ended

    // The GameObject that represents the icon to show on the minimap
    [Header("Minimap Icon")]
    [Tooltip("Assign the child GameObject that has a SpriteRenderer (your minimap dot).")]
    public GameObject minimapIcon;

    private bool isPlayerNearby = false;

    // The minimap controller
    private MiniMap minimapController;

    void Start()
    {
        // Random spawn (your original logic)
        if (spawnAreas != null && spawnAreas.Length > 0)
        {
            Vector2 pos;
            int safety = 0;
            do
            {
                Collider2D selectedSpawnArea = spawnAreas[Random.Range(0, spawnAreas.Length)];
                pos = new Vector2(
                    Random.Range(selectedSpawnArea.bounds.min.x, selectedSpawnArea.bounds.max.x),
                    Random.Range(selectedSpawnArea.bounds.min.y, selectedSpawnArea.bounds.max.y)
                );
                safety++;
            } while (!IsFarFromOtherInteractables(pos) && safety < 50);
            transform.position = pos;
        }
        else
        {
            Debug.LogWarning("⚠️ [InterCom] No spawn area(s) assigned!");
        }

        // Find the minimap controller in the scene
        minimapController = FindObjectOfType<MiniMap>();
    }

    private bool IsFarFromOtherInteractables(Vector2 pos)
    {
        foreach (var other in FindObjectsOfType<InterCom>())
        {
            if (other != this && Vector2.Distance(pos, other.transform.position) < minDistanceBetweenObjects)
                return false;
        }
        return true;
    }

    private bool IsPositionBlocked(Vector2 pos)
    {
        // Check if there are any colliders at this position that we should avoid
        Collider2D[] overlapping = Physics2D.OverlapCircleAll(pos, spawnCheckRadius, obstacleLayerMask);

        // Filter out trigger colliders (we only care about solid obstacles)
        foreach (Collider2D col in overlapping)
        {
            if (!col.isTrigger)
            {
                return true; // Position is blocked by a solid collider
            }
        }

        return false; // Position is clear
    }

    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(interactKey))
        {
            if (codeCheckGame != null && !codeCheckGame.isOnCooldown)
            {
                if (gameManager != null && gameManager.gameEnded) return; // disable after game ends
                codeCheckGame.OpenCodePanel();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Only respond to objects with the specific player tag
        if (other.CompareTag(playerTag))
        {
            isPlayerNearby = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // Only respond to objects with the specific player tag
        if (other.CompareTag(playerTag))
        {
            isPlayerNearby = false;
        }
    }

    // This method is called by CodeCheckGame when the player correctly solves the code
    public void OnInteractionComplete()
    {
        // Tell the minimap to reveal this icon
        if (minimapController != null && minimapIcon != null)
        {
            minimapController.RevealIntercom(minimapIcon);
        }
    }
}