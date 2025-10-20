using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ObjectSpawner : MonoBehaviour
{
    public GameObject[] objectsToSpawn; // Each object will be spawned once
    public Tilemap tileMap;

    void Start()
    {
        SpawnAllObjectsOnce();
    }

    void SpawnAllObjectsOnce()
    {
        BoundsInt bounds = tileMap.cellBounds;
        int maxAttempts = 100;
        int attempts = 0;

        foreach (GameObject obj in objectsToSpawn)
        {
            bool placed = false;

            while (!placed && attempts < maxAttempts)
            {
                attempts++;

                Vector3Int randomCell = new Vector3Int(
                    Random.Range(bounds.xMin, bounds.xMax),
                    Random.Range(bounds.yMin, bounds.yMax),
                    0
                );

                if (tileMap.HasTile(randomCell))
                {
                    Vector3 spawnPos = tileMap.CellToWorld(randomCell) + new Vector3(0.5f, 0.5f, 0);
                    Instantiate(obj, spawnPos, Quaternion.identity);
                    placed = true;
                }
            }

            if (!placed)
            {
                Debug.LogWarning("Could not place object: " + obj.name);
            }
        }
    }
}
