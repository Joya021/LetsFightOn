using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class C1BrokenCodesManager : MonoBehaviour
{
    public static C1BrokenCodesManager Instance;

    [Header("Broken Code Pool")]
    public List<BrokenCode> allBrokenCodes = new List<BrokenCode>();

    [Header("Computer Station Settings")]
    public GameObject computerStationPrefab;
    public int stationCount = 10;
    public Transform stationParent;

    [Header("Ground Tilemap Reference")]
    public Tilemap groundTilemap;

    public ObjectLocator objectLocator;
    public GameObject chapterOneClearedPanel;

    private List<ComputerStation> spawnedStations = new List<ComputerStation>();

    private bool allStationsSolved = false; // ✅ Track separately
    private bool panelShown = false;        // ✅ Prevent showing twice

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SpawnComputerStationsWithRandomCodes();
        if (chapterOneClearedPanel != null)
            chapterOneClearedPanel.SetActive(false);
    }

    void SpawnComputerStationsWithRandomCodes()
    {
        List<Vector3> spawnPositions = GetValidGroundPositions();
        List<BrokenCode> codePool = new List<BrokenCode>(allBrokenCodes);

        for (int i = 0; i < stationCount; i++)
        {
            if (spawnPositions.Count == 0 || codePool.Count == 0)
                break;

            int posIndex = Random.Range(0, spawnPositions.Count);
            Vector3 spawnPos = spawnPositions[posIndex];
            spawnPositions.RemoveAt(posIndex);

            int codeIndex = Random.Range(0, codePool.Count);
            BrokenCode codeToAssign = codePool[codeIndex];
            codePool.RemoveAt(codeIndex);

            GameObject stationObj = Instantiate(computerStationPrefab, spawnPos, Quaternion.identity, stationParent);
            var stationScript = stationObj.GetComponent<ComputerStation>();
            if (stationScript != null)
            {
                stationScript.AssignCode(codeToAssign);
                spawnedStations.Add(stationScript);
            }

            if (objectLocator != null)
                objectLocator.RegisterTarget(stationObj.transform);
        }
    }

    // ✅ Called when a station is solved
    public void CheckAllStationsCleared()
    {
        if (allStationsSolved) return;

        foreach (var station in spawnedStations)
        {
            if (!station.IsSolved())
                return;
        }

        // ✅ Mark that all are solved (but don’t show the panel yet)
        allStationsSolved = true;
        Debug.Log("✅ All ComputerStations solved! Waiting for CorrectAnswerPanel to close...");
    }

    // ✅ Called by CorrectAnswerPanel when player closes the last popup
    public void ShowChapterClearedPanelIfReady()
    {
        if (allStationsSolved && !panelShown)
        {
            panelShown = true;
            if (chapterOneClearedPanel != null)
            {
                chapterOneClearedPanel.SetActive(true);
                Debug.Log("🎉 Chapter One Cleared Panel Shown!");
            }
        }
    }

    List<Vector3> GetValidGroundPositions()
    {
        var validPositions = new List<Vector3>();
        var blockers = new HashSet<Vector3Int>();

        foreach (var obj in GameObject.FindGameObjectsWithTag("LearnableObject"))
        {
            Vector3Int cell = groundTilemap.WorldToCell(obj.transform.position);
            blockers.Add(cell);
        }

        BoundsInt bounds = groundTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                if (!groundTilemap.HasTile(cellPos)) continue;
                if (blockers.Contains(cellPos)) continue;

                Vector3 worldPos = groundTilemap.CellToWorld(cellPos)
                                 + new Vector3(groundTilemap.cellSize.x, groundTilemap.cellSize.y) * 0.5f;
                validPositions.Add(worldPos);
            }
        }

        return validPositions;
    }
}
