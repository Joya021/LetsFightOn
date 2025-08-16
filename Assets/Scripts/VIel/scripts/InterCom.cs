using UnityEngine;

public class InterCom : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Collider2D[] spawnAreas;
    public float minDistanceBetweenObjects = 3f;
    public LayerMask obstacleLayer; // NEW: assign obstacle/wall layer here

    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.F;

    [Header("References")]
    public CodeCheckGame codeCheckGame;
    public GameManager gameManager; // Reference to check if game ended

    private bool isPlayerNearby = false;

    void Start()
    {
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
            // ✅ Now checks: far from other InterComs AND not inside obstacle colliders
            while ((!IsFarFromOtherInteractables(pos) || Physics2D.OverlapCircle(pos, 0.5f, obstacleLayer))
                   && safety < 50);

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
        if (other.CompareTag("Player"))
            isPlayerNearby = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isPlayerNearby = false;
    }
}
