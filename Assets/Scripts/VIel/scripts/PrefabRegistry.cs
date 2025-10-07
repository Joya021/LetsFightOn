// PrefabRegistry.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central registry to assign hunter & survivor prefabs (and optional icons) via the Inspector.
/// Use this to avoid hardcoded prefab name strings in other scripts.
/// NOTE: PhotonNetwork.Instantiate still requires the prefab to exist in Resources if you spawn by name.
/// To keep things simple and backward-compatible we still use prefab.name when spawning; ensure the assigned prefabs
/// in this registry are the same prefab assets that live under Resources (or your Photon Prefab Pool).
/// </summary>
public class PrefabRegistry : MonoBehaviour
{
    [System.Serializable]
    public class PrefabEntry
    {
        public GameObject prefab;     // assign the prefab (preferably the same asset in Resources)
        public Sprite icon;           // optional icon for UI
    }

    [Header("Survivor Prefabs")]
    public PrefabEntry[] survivorPrefabs;

    [Header("Hunter Prefabs")]
    public PrefabEntry[] hunterPrefabs;

    // quick lookup dictionaries created at runtime
    private Dictionary<string, GameObject> prefabLookup = new Dictionary<string, GameObject>();
    private Dictionary<string, Sprite> iconLookup = new Dictionary<string, Sprite>();

    void Awake()
    {
        BuildLookup();
    }

    void OnValidate()
    {
        // keep lookup up-to-date in edit mode when possible
        BuildLookup();
    }

    private void BuildLookup()
    {
        prefabLookup.Clear();
        iconLookup.Clear();

        if (survivorPrefabs != null)
        {
            foreach (var e in survivorPrefabs)
            {
                if (e != null && e.prefab != null)
                {
                    string key = e.prefab.name;
                    if (!prefabLookup.ContainsKey(key))
                        prefabLookup[key] = e.prefab;
                    if (e.icon != null && !iconLookup.ContainsKey(key))
                        iconLookup[key] = e.icon;
                }
            }
        }

        if (hunterPrefabs != null)
        {
            foreach (var e in hunterPrefabs)
            {
                if (e != null && e.prefab != null)
                {
                    string key = e.prefab.name;
                    if (!prefabLookup.ContainsKey(key))
                        prefabLookup[key] = e.prefab;
                    if (e.icon != null && !iconLookup.ContainsKey(key))
                        iconLookup[key] = e.icon;
                }
            }
        }
    }

    /// <summary>
    /// Returns the prefab reference by name, or null if not found.
    /// </summary>
    public GameObject GetPrefabByName(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return null;
        prefabLookup.TryGetValue(prefabName, out var p);
        return p;
    }

    /// <summary>
    /// Returns an icon for the prefab name if available.
    /// </summary>
    public Sprite GetIconByName(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return null;
        iconLookup.TryGetValue(prefabName, out var s);
        return s;
    }

    /// <summary>
    /// Returns a random prefab appropriate for the role.
    /// </summary>
    public GameObject GetRandomPrefab(bool isHunter)
    {
        if (isHunter)
        {
            if (hunterPrefabs != null && hunterPrefabs.Length > 0)
            {
                int i = Random.Range(0, hunterPrefabs.Length);
                return hunterPrefabs[i].prefab;
            }
        }
        else
        {
            if (survivorPrefabs != null && survivorPrefabs.Length > 0)
            {
                int i = Random.Range(0, survivorPrefabs.Length);
                return survivorPrefabs[i].prefab;
            }
        }
        return null;
    }
}
