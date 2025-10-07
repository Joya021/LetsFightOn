using UnityEngine;

public class TestRio1Load : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== TESTING RIO1 PREFAB LOADING ===");

        // Test with space
        GameObject testWithSpace = Resources.Load<GameObject>("Rio 1");
        if (testWithSpace != null)
            Debug.Log("✓ Found with space: 'Rio 1'");
        else
            Debug.LogWarning("✗ NOT found with space: 'Rio 1'");

        // Test without space
        GameObject testNoSpace = Resources.Load<GameObject>("Rio1");
        if (testNoSpace != null)
            Debug.Log("✓ Found without space: 'Rio1'");
        else
            Debug.LogError("✗ NOT found without space: 'Rio1'");

        // List all prefabs
        Debug.Log("=== ALL PREFABS IN RESOURCES ===");
        GameObject[] allPrefabs = Resources.LoadAll<GameObject>("");
        foreach (GameObject prefab in allPrefabs)
        {
            Debug.Log($"  • {prefab.name}");
        }
    }
}