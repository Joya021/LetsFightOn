// SpawnPlayers.cs
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class SpawnPlayers : MonoBehaviour
{
    public Transform[] spawnPoints; // Assign fixed spawn points in the inspector
    private const string SPAWN_INDEX_KEY = "SpawnIndex";
    private const string PLAYER_CHARACTER = "PlayerCharacter";
    private const string PLAYER_ROLE = "PlayerRole"; // true = Hunter, false = Survivor

    // optional reference to PrefabRegistry (assign in inspector or it'll Find one at runtime)
    public PrefabRegistry prefabRegistry;

    private void Start()
    {
        if (prefabRegistry == null)
            prefabRegistry = FindObjectOfType<PrefabRegistry>();

        // Master client assigns spawn points
        if (PhotonNetwork.IsMasterClient)
        {
            AssignSpawnPoints();
        }

        StartCoroutine(WaitAndSpawn());
    }

    void AssignSpawnPoints()
    {
        Photon.Realtime.Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            Hashtable props = new Hashtable();
            props[SPAWN_INDEX_KEY] = i % Mathf.Max(1, spawnPoints.Length);
            players[i].SetCustomProperties(props);
        }
    }


    IEnumerator WaitAndSpawn()
    {
        // Wait until essential properties are present
        while (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(SPAWN_INDEX_KEY) ||
               !PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PLAYER_ROLE))
        {
            yield return null;
        }

        int spawnIndex = (int)PhotonNetwork.LocalPlayer.CustomProperties[SPAWN_INDEX_KEY];
        string characterPrefabName = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PLAYER_CHARACTER) ?
            (string)PhotonNetwork.LocalPlayer.CustomProperties[PLAYER_CHARACTER] : "";
        bool isHunter = (bool)PhotonNetwork.LocalPlayer.CustomProperties[PLAYER_ROLE];

        // Resolve final prefab name using PrefabRegistry:
        GameObject resolvedPrefab = null;

        if (!string.IsNullOrEmpty(characterPrefabName) && prefabRegistry != null)
        {
            resolvedPrefab = prefabRegistry.GetPrefabByName(characterPrefabName);
            if (resolvedPrefab == null)
            {
                Debug.LogWarning($"SpawnPlayers: prefab name '{characterPrefabName}' found in player props but not present in PrefabRegistry.");
            }
        }

        // If not resolved, pick a random assigned prefab from registry for that role
        if (resolvedPrefab == null && prefabRegistry != null)
        {
            resolvedPrefab = prefabRegistry.GetRandomPrefab(isHunter);
            if (resolvedPrefab != null)
            {
                characterPrefabName = resolvedPrefab.name;
                Debug.Log($"SpawnPlayers: Auto-picked '{characterPrefabName}' for player (isHunter={isHunter}).");
                // update player prop so UI and other clients know which prefab we used
                Hashtable props = new Hashtable();
                props[PLAYER_CHARACTER] = characterPrefabName;
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            }
        }

        // If still null or registry missing, fall back to using the name from player props (if any)
        if (resolvedPrefab == null && string.IsNullOrEmpty(characterPrefabName))
        {
            Debug.LogError("SpawnPlayers: No prefab selected and no PrefabRegistry available. Cannot spawn player.");
            yield break;
        }

        // Ensure spawnIndex in bounds
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("SpawnPlayers: No spawn points assigned!");
            yield break;
        }
        spawnIndex = Mathf.Clamp(spawnIndex, 0, spawnPoints.Length - 1);
        Vector3 spawnPosition = spawnPoints[spawnIndex].position;

        // PhotonNetwork.Instantiate requires a Resources path/prefab name by default.
        // We assume the assigned prefab asset also exists under Resources with the same name.
        // If you use a Photon PrefabPool, you can adapt this to use it instead.
        Debug.Log($"SpawnPlayers: Instantiating '{characterPrefabName}' at spawn index {spawnIndex}.");
        PhotonNetwork.Instantiate(characterPrefabName, spawnPosition, Quaternion.identity);
    }
}