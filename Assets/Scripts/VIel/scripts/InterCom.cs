using UnityEngine;

public class InterCom : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Collider2D[] spawnAreas;              // Assign in Inspector
    public float minDistanceBetweenObjects = 3f; // Minimum spacing between InterComs

    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.F;      // Key to interact

    [Header("References")]
    public CodeCheckGame codeCheckGame;          // Drag your CodeCheckGame here

    private bool isPlayerNearby = false;

    void Start()
    {
        // Random spawn if spawn areas exist
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
            }
            while (!IsFarFromOtherInteractables(pos) && safety < 50);

            transform.position = pos;
        }
        else
        {
            Debug.LogWarning("⚠ No spawn area(s) assigned for InterCom!");
        }
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

    void Update()
    {
        // Open panel if player is near, presses the key, and panel is not in cooldown
        if (isPlayerNearby && Input.GetKeyDown(interactKey))
        {
            if (codeCheckGame != null && !codeCheckGame.isOnCooldown)
            {
                codeCheckGame.OpenCodePanel();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isPlayerNearby = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isPlayerNearby = false;
    }
}