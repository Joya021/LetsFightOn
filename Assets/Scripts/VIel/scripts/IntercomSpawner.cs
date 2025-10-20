using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class IntercomSpawner : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Spawning Settings")]
    public GameObject intercomPrefab;
    public Transform[] spawnPoints;

    [Header("Random Spawn Configuration")]
    [Tooltip("How many intercoms to spawn randomly from available spawn points")]
    public int numberOfIntercomsToSpawn = 3;

    [Header("Timing")]
    public float spawnDelay = 1f;

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

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"[IntercomSpawner] Master client - will spawn {numberOfIntercomsToSpawn} intercoms randomly from {spawnPoints.Length} spawn points");
            StartCoroutine(SpawnIntercomsWithDelay());
        }
        else
        {
            Debug.Log("[IntercomSpawner] Not master client - waiting for spawns from master");
        }
    }

    private bool ValidateIntercomPrefab()
    {
        if (intercomPrefab == null)
        {
            Debug.LogError("[IntercomSpawner] No intercom prefab assigned!");
            return false;
        }

        // Check if prefab is in Resources folder
        if (Resources.Load(intercomPrefab.name) == null)
        {
            Debug.LogError($"[IntercomSpawner] Prefab '{intercomPrefab.name}' must be in Resources folder!");
            return false;
        }

        // Check for required components
        CodeCheckGame codeCheck = intercomPrefab.GetComponent<CodeCheckGame>();
        InterCom intercom = intercomPrefab.GetComponent<InterCom>();
        PhotonView photonView = intercomPrefab.GetComponent<PhotonView>();
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

        if (photonView == null)
        {
            Debug.LogError("[IntercomSpawner] Prefab missing PhotonView component!");
            isValid = false;
        }
        else
        {
            Debug.Log("[IntercomSpawner] ✓ PhotonView component found");
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

        return isValid;
    }

    private IEnumerator SpawnIntercomsWithDelay()
    {
        Debug.Log($"[IntercomSpawner] Starting to spawn {numberOfIntercomsToSpawn} intercoms randomly with {spawnDelay}s delay");

        yield return new WaitForSeconds(spawnDelay);

        // Select random spawn points using Photon's synchronized random
        // This ensures all clients get the same random selection
        selectedSpawnIndices = GetRandomSpawnIndices(numberOfIntercomsToSpawn, spawnPoints.Length);

        Debug.Log($"[IntercomSpawner] Selected spawn point indices: {string.Join(", ", selectedSpawnIndices)}");

        // CRITICAL: Store the spawn indices in room properties for synchronization
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["IntercomSpawnIndices"] = selectedSpawnIndices.ToArray();
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        // Spawn intercoms at selected positions with deterministic IDs
        for (int i = 0; i < selectedSpawnIndices.Count; i++)
        {
            int spawnIndex = selectedSpawnIndices[i];

            if (spawnPoints[spawnIndex] != null)
            {
                Debug.Log($"[IntercomSpawner] Spawning intercom {i + 1}/{selectedSpawnIndices.Count} at spawn point index {spawnIndex} (position: {spawnPoints[spawnIndex].position})");

                // Create instantiation data to pass the deterministic ID
                object[] instantiationData = new object[] { i }; // Use spawn order as ID (0, 1, 2, etc.)

                GameObject spawnedIntercom = PhotonNetwork.Instantiate(
                    intercomPrefab.name,
                    spawnPoints[spawnIndex].position,
                    spawnPoints[spawnIndex].rotation,
                    0,
                    instantiationData
                );

                if (spawnedIntercom != null)
                {
                    spawnedIntercoms.Add(spawnedIntercom);

                    // CRITICAL: Set the intercom ID immediately after spawning
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

                    Debug.Log($"[IntercomSpawner] Successfully spawned intercom '{spawnedIntercom.name}' with PhotonView ID: {spawnedIntercom.GetComponent<PhotonView>()?.ViewID}");
                }
                else
                {
                    Debug.LogError($"[IntercomSpawner] Failed to spawn intercom at spawn point {spawnIndex}");
                }

                // Small delay between spawns to avoid overwhelming the network
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                Debug.LogWarning($"[IntercomSpawner] Spawn point {spawnIndex} is null!");
            }
        }

        Debug.Log($"[IntercomSpawner] Finished spawning. Total intercoms spawned: {spawnedIntercoms.Count}");

        // Give a moment for all objects to initialize, then do a simple verification
        yield return new WaitForSeconds(1f);

        // Simple verification without relying on GameManager methods
        CodeCheckGame[] allGames = FindObjectsOfType<CodeCheckGame>();
        Debug.Log($"[IntercomSpawner] FINAL CHECK: Found {allGames.Length} CodeCheckGame objects in scene");

        if (allGames.Length > 0)
        {
            Debug.Log("[IntercomSpawner] SUCCESS: CodeCheckGame objects are now available for GameManager to find!");
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
    /// Gets random spawn indices that are synchronized across all clients using room seed
    /// </summary>
    private List<int> GetRandomSpawnIndices(int count, int maxIndex)
    {
        // Use a deterministic seed based on room name to ensure all clients get same random selection
        // This works because all clients in the same room have the same room name
        int seed = PhotonNetwork.CurrentRoom.Name.GetHashCode();
        Random.InitState(seed);

        Debug.Log($"[IntercomSpawner] Using random seed: {seed} (from room: {PhotonNetwork.CurrentRoom.Name})");

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

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // Sync spawned intercoms count across clients if needed
        if (stream.IsWriting)
        {
            stream.SendNext(spawnedIntercoms.Count);
        }
        else
        {
            int remoteCount = (int)stream.ReceiveNext();
            Debug.Log($"[IntercomSpawner] Remote client reports {remoteCount} spawned intercoms");
        }
    }

    // Manual debug function
    [ContextMenu("Debug Spawner State")]
    public void DebugSpawnerState()
    {
        Debug.Log($"[IntercomSpawner] DEBUG STATE:");
        Debug.Log($"  - Is Master Client: {PhotonNetwork.IsMasterClient}");
        Debug.Log($"  - Prefab assigned: {(intercomPrefab != null ? intercomPrefab.name : "NULL")}");
        Debug.Log($"  - Total spawn points: {spawnPoints?.Length ?? 0}");
        Debug.Log($"  - Intercoms to spawn: {numberOfIntercomsToSpawn}");
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
}