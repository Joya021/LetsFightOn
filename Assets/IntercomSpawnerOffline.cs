using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntercomSpawner_Offline : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject intercomPrefab;
    public Transform[] spawnPoints;

    [Header("Random Spawn Configuration")]
    [Tooltip("How many intercoms to spawn randomly from available spawn points")]
    public int numberOfIntercomsToSpawn = 3;

    [Header("Timing")]
    public float spawnDelay = 1f;

    [Header("Random Seed (Optional)")]
    [Tooltip("Leave at 0 for random seed, or set a specific value for deterministic spawns")]
    public int randomSeed = 0;

    private List<GameObject> spawnedIntercoms = new List<GameObject>();
    private List<int> selectedSpawnIndices = new List<int>();

    void Start()
    {
        Debug.Log("[IntercomSpawner] === INTERCOM SPAWNER STARTING ===");

        // Validate prefab before attempting to spawn
        if (!ValidateIntercomPrefab())
        {
            Debug.LogError("[IntercomSpawner] Intercom prefab validation failed! Check prefab components.");
            return;
        }

        // Validate spawn configuration
        if (numberOfIntercomsToSpawn > spawnPoints.Length)
        {
            Debug.LogWarning($"[IntercomSpawner] Requested {numberOfIntercomsToSpawn} intercoms but only {spawnPoints.Length} spawn points available. Clamping to {spawnPoints.Length}");
            numberOfIntercomsToSpawn = spawnPoints.Length;
        }

        if (numberOfIntercomsToSpawn <= 0)
        {
            Debug.LogError("[IntercomSpawner] numberOfIntercomsToSpawn must be greater than 0!");
            return;
        }

        Debug.Log("[IntercomSpawner] Prefab validation passed. Starting spawn process...");
        Debug.Log($"[IntercomSpawner] Will spawn {numberOfIntercomsToSpawn} intercoms randomly from {spawnPoints.Length} spawn points");

        StartCoroutine(SpawnIntercomsWithDelay());
    }

    private bool ValidateIntercomPrefab()
    {
        if (intercomPrefab == null)
        {
            Debug.LogError("[IntercomSpawner] No intercom prefab assigned!");
            return false;
        }

        // Check for required components
        CodeCheckGame codeCheck = intercomPrefab.GetComponent<CodeCheckGame>();
        InterCom intercom = intercomPrefab.GetComponent<InterCom>();
        Collider2D collider = intercomPrefab.GetComponent<Collider2D>();

        bool isValid = true;

        if (codeCheck == null)
        {
            Debug.LogError("[IntercomSpawner] Prefab missing CodeCheckGame component!");
            isValid = false;
        }
        else
        {
            Debug.Log("[IntercomSpawner] ✓ CodeCheckGame component found");
        }

        if (intercom == null)
        {
            Debug.LogError("[IntercomSpawner] Prefab missing InterCom component!");
            isValid = false;
        }
        else
        {
            Debug.Log("[IntercomSpawner] ✓ InterCom component found");
        }

        if (collider == null)
        {
            Debug.LogError("[IntercomSpawner] Prefab missing Collider2D component!");
            isValid = false;
        }
        else
        {
            Debug.Log($"[IntercomSpawner] ✓ Collider2D found - IsTrigger: {collider.isTrigger}");
            if (!collider.isTrigger)
            {
                Debug.LogWarning("[IntercomSpawner] Collider2D should be set as Trigger for player interaction");
            }
        }

        // VISIBILITY CHECKS
        Debug.Log("[IntercomSpawner] === VISIBILITY VALIDATION ===");

        // Check for SpriteRenderer
        SpriteRenderer spriteRenderer = intercomPrefab.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning("[IntercomSpawner] ⚠ Prefab missing SpriteRenderer! Objects may be invisible!");
        }
        else
        {
            Debug.Log($"[IntercomSpawner] ✓ SpriteRenderer found - Enabled: {spriteRenderer.enabled}, Sprite: {(spriteRenderer.sprite != null ? spriteRenderer.sprite.name : "NULL")}");
            if (spriteRenderer.sprite == null)
            {
                Debug.LogError("[IntercomSpawner] SpriteRenderer has no sprite assigned!");
                isValid = false;
            }
        }

        // Check if prefab is active
        if (!intercomPrefab.activeSelf)
        {
            Debug.LogWarning("[IntercomSpawner] ⚠ Prefab is INACTIVE! Spawned objects will be invisible. Set prefab to active in the inspector.");
        }
        else
        {
            Debug.Log("[IntercomSpawner] ✓ Prefab is active");
        }

        // Check layer
        Debug.Log($"[IntercomSpawner] Prefab layer: {LayerMask.LayerToName(intercomPrefab.layer)} (ID: {intercomPrefab.layer})");

        // Check scale
        Debug.Log($"[IntercomSpawner] Prefab local scale: {intercomPrefab.transform.localScale}");
        if (intercomPrefab.transform.localScale == Vector3.zero)
        {
            Debug.LogError("[IntercomSpawner] Prefab scale is ZERO! Objects will be invisible!");
            isValid = false;
        }

        return isValid;
    }

    private IEnumerator SpawnIntercomsWithDelay()
    {
        Debug.Log($"[IntercomSpawner] Starting to spawn {numberOfIntercomsToSpawn} intercoms randomly with {spawnDelay}s delay");

        yield return new WaitForSeconds(spawnDelay);

        // Select random spawn points
        selectedSpawnIndices = GetRandomSpawnIndices(numberOfIntercomsToSpawn, spawnPoints.Length);

        Debug.Log($"[IntercomSpawner] Selected spawn point indices: {string.Join(", ", selectedSpawnIndices)}");

        // Spawn intercoms at selected positions with deterministic IDs
        for (int i = 0; i < selectedSpawnIndices.Count; i++)
        {
            int spawnIndex = selectedSpawnIndices[i];

            if (spawnPoints[spawnIndex] != null)
            {
                Debug.Log($"[IntercomSpawner] Spawning intercom {i + 1}/{selectedSpawnIndices.Count} at spawn point index {spawnIndex} (position: {spawnPoints[spawnIndex].position})");

                // Instantiate the intercom
                GameObject spawnedIntercom = Instantiate(
                    intercomPrefab,
                    spawnPoints[spawnIndex].position,
                    spawnPoints[spawnIndex].rotation
                );

                if (spawnedIntercom != null)
                {
                    spawnedIntercoms.Add(spawnedIntercom);

                    // Set the intercom ID immediately after spawning
                    CodeCheckGame codeGame = spawnedIntercom.GetComponent<CodeCheckGame>();
                    if (codeGame != null)
                    {
                        // Use reflection to set the private intercomID field
                        var field = typeof(CodeCheckGame).GetField("intercomID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            field.SetValue(codeGame, i); // Set deterministic ID based on spawn order
                            Debug.Log($"[IntercomSpawner] Set intercom ID to {i} for {spawnedIntercom.name}");
                        }
                    }

                    Debug.Log($"[IntercomSpawner] Successfully spawned intercom '{spawnedIntercom.name}'");
                }
                else
                {
                    Debug.LogError($"[IntercomSpawner] Failed to spawn intercom at spawn point {spawnIndex}");
                }

                // Small delay between spawns
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                Debug.LogWarning($"[IntercomSpawner] Spawn point {spawnIndex} is null!");
            }
        }

        Debug.Log($"[IntercomSpawner] Finished spawning. Total intercoms spawned: {spawnedIntercoms.Count}");

        // Give a moment for all objects to initialize, then do verification
        yield return new WaitForSeconds(1f);

        // Verification
        CodeCheckGame[] allGames = FindObjectsOfType<CodeCheckGame>();
        Debug.Log($"[IntercomSpawner] FINAL CHECK: Found {allGames.Length} CodeCheckGame objects in scene");

        if (allGames.Length > 0)
        {
            Debug.Log("[IntercomSpawner] SUCCESS: CodeCheckGame objects are now available!");
            for (int i = 0; i < allGames.Length; i++)
            {
                Debug.Log($"[IntercomSpawner] - CodeCheckGame {i + 1}: {allGames[i].name}, ID: {allGames[i].GetIntercomID()}");
            }
        }
        else
        {
            Debug.LogError("[IntercomSpawner] PROBLEM: No CodeCheckGame objects found after spawning!");
        }
    }

    /// <summary>
    /// Gets random spawn indices using optional seed for deterministic results
    /// </summary>
    private List<int> GetRandomSpawnIndices(int count, int maxIndex)
    {
        // Use seed if provided, otherwise use Unity's default random state
        if (randomSeed != 0)
        {
            Random.InitState(randomSeed);
            Debug.Log($"[IntercomSpawner] Using custom random seed: {randomSeed}");
        }
        else
        {
            Debug.Log($"[IntercomSpawner] Using Unity's default random state");
        }

        List<int> availableIndices = new List<int>();
        for (int i = 0; i < maxIndex; i++)
        {
            availableIndices.Add(i);
        }

        List<int> selectedIndices = new List<int>();

        // Fisher-Yates shuffle and select first 'count' elements
        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, availableIndices.Count);
            selectedIndices.Add(availableIndices[randomIndex]);
            availableIndices.RemoveAt(randomIndex);
        }

        // Sort for cleaner logging (optional)
        selectedIndices.Sort();

        return selectedIndices;
    }

    // Manual debug function
    [ContextMenu("Debug Spawner State")]
    public void DebugSpawnerState()
    {
        Debug.Log($"[IntercomSpawner] DEBUG STATE:");
        Debug.Log($"  - Prefab assigned: {(intercomPrefab != null ? intercomPrefab.name : "NULL")}");
        Debug.Log($"  - Total spawn points: {spawnPoints?.Length ?? 0}");
        Debug.Log($"  - Intercoms to spawn: {numberOfIntercomsToSpawn}");
        Debug.Log($"  - Random seed: {(randomSeed == 0 ? "Random" : randomSeed.ToString())}");
        Debug.Log($"  - Selected spawn indices: {string.Join(", ", selectedSpawnIndices)}");
        Debug.Log($"  - Spawned intercoms: {spawnedIntercoms.Count}");

        for (int i = 0; i < spawnedIntercoms.Count; i++)
        {
            if (spawnedIntercoms[i] != null)
            {
                Debug.Log($"    - Intercom {i}: {spawnedIntercoms[i].name}");
            }
        }

        // Also check all CodeCheckGames in scene
        CodeCheckGame[] allGames = FindObjectsOfType<CodeCheckGame>();
        Debug.Log($"  - Total CodeCheckGames in scene: {allGames.Length}");
    }

    // Public method to get spawned intercoms (useful for other systems)
    public List<GameObject> GetSpawnedIntercoms()
    {
        return new List<GameObject>(spawnedIntercoms);
    }

    // Public method to get selected spawn indices
    public List<int> GetSelectedSpawnIndices()
    {
        return new List<int>(selectedSpawnIndices);
    }
}