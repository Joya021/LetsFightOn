using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;
using System.Collections.Generic;

public class IntercomSpawner : MonoBehaviourPunCallbacks
{
    [Header("Spawn Settings")]
    public GameObject intercomPrefab; // Must be in Resources folder
    public int numberOfIntercoms = 5;
    public Collider2D[] spawnAreas;
    public float minDistanceBetweenObjects = 3f;
    public LayerMask obstacleLayerMask = -1;
    public float spawnCheckRadius = 0.5f;

    private List<Vector2> spawnedPositions = new List<Vector2>();

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // Master client generates and shares spawn positions
            GenerateAndShareSpawnPositions();
        }
        else
        {
            // Other clients wait for spawn positions
            StartCoroutine(WaitForSpawnPositions());
        }
    }

    void GenerateAndShareSpawnPositions()
    {
        List<Vector2> positions = new List<Vector2>();

        for (int i = 0; i < numberOfIntercoms; i++)
        {
            Vector2 pos = GenerateValidPosition();
            if (pos != Vector2.zero)
            {
                positions.Add(pos);
                spawnedPositions.Add(pos);
            }
        }

        // Convert positions to float array for Photon
        float[] posArray = new float[positions.Count * 2];
        for (int i = 0; i < positions.Count; i++)
        {
            posArray[i * 2] = positions[i].x;
            posArray[i * 2 + 1] = positions[i].y;
        }

        // Save to room properties
        Hashtable props = new Hashtable();
        props[InterCom.INTERCOM_POSITIONS_KEY] = posArray;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        // Spawn intercoms locally
        SpawnIntercoms(positions);
    }

    System.Collections.IEnumerator WaitForSpawnPositions()
    {
        // Wait for room properties to contain intercom positions
        while (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(InterCom.INTERCOM_POSITIONS_KEY))
        {
            yield return null;
        }

        // Get positions from room properties
        float[] posArray = (float[])PhotonNetwork.CurrentRoom.CustomProperties[InterCom.INTERCOM_POSITIONS_KEY];
        List<Vector2> positions = new List<Vector2>();

        for (int i = 0; i < posArray.Length; i += 2)
        {
            positions.Add(new Vector2(posArray[i], posArray[i + 1]));
        }

        // Spawn intercoms locally
        SpawnIntercoms(positions);
    }

    void SpawnIntercoms(List<Vector2> positions)
    {
        foreach (Vector2 pos in positions)
        {
            // Spawn locally (not networked object)
            GameObject intercom = Instantiate(intercomPrefab, pos, Quaternion.identity);

            // Optionally add to a list for tracking
            spawnedPositions.Add(pos);
        }

        Debug.Log($"Spawned {positions.Count} intercoms at synced positions");
    }

    Vector2 GenerateValidPosition()
    {
        if (spawnAreas == null || spawnAreas.Length == 0)
        {
            Debug.LogWarning("No spawn areas assigned!");
            return Vector2.zero;
        }

        int safety = 0;
        Vector2 pos;

        do
        {
            Collider2D selectedSpawnArea = spawnAreas[Random.Range(0, spawnAreas.Length)];
            pos = new Vector2(
            Random.Range(selectedSpawnArea.bounds.min.x, selectedSpawnArea.bounds.max.x),
            Random.Range(selectedSpawnArea.bounds.min.y, selectedSpawnArea.bounds.max.y)
            );
            safety++;
        } while ((!IsFarFromOtherIntercoms(pos) || IsPositionBlocked(pos)) && safety < 100);

        return safety < 100 ? pos : Vector2.zero;
    }

    bool IsFarFromOtherIntercoms(Vector2 pos)
    {
        foreach (Vector2 existingPos in spawnedPositions)
        {
            if (Vector2.Distance(pos, existingPos) < minDistanceBetweenObjects)
                return false;
        }
        return true;
    }

    bool IsPositionBlocked(Vector2 pos)
    {
        Collider2D[] overlapping = Physics2D.OverlapCircleAll(pos, spawnCheckRadius, obstacleLayerMask);

        foreach (Collider2D col in overlapping)
        {
            if (col != null && !col.isTrigger)
                return true;
        }

        return false;
    }
}