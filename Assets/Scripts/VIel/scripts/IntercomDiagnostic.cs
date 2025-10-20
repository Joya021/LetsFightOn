using UnityEngine;
using Photon.Pun;

public class IntercomDiagnostic : MonoBehaviour
{
    [Header("Assign Your Intercom Prefab Here")]
    public GameObject intercomPrefab;

    [ContextMenu("Run Full Diagnostic")]
    public void RunFullDiagnostic()
    {
        Debug.Log("=== INTERCOM SPAWNING DIAGNOSTIC ===");

        // 1. Check Photon Network Status
        Debug.Log($"🌐 NETWORK STATUS:");
        Debug.Log($"  - Connected to Photon: {PhotonNetwork.IsConnected}");
        Debug.Log($"  - In Room: {PhotonNetwork.InRoom}");
        Debug.Log($"  - Is Master Client: {PhotonNetwork.IsMasterClient}");
        Debug.Log($"  - Players in Room: {PhotonNetwork.CurrentRoom?.PlayerCount ?? 0}");
        Debug.Log($"  - Network State: {PhotonNetwork.NetworkClientState}");

        // 2. Check Prefab Status
        Debug.Log($"📦 PREFAB STATUS:");
        if (intercomPrefab == null)
        {
            Debug.LogError("  ❌ No prefab assigned!");
            return;
        }

        Debug.Log($"  - Prefab Name: {intercomPrefab.name}");
        Debug.Log($"  - Prefab Active: {intercomPrefab.activeSelf}");

        // Check if in Resources
        GameObject resourceCheck = Resources.Load<GameObject>(intercomPrefab.name);
        if (resourceCheck == null)
        {
            Debug.LogError($"  ❌ Prefab '{intercomPrefab.name}' NOT FOUND in Resources folder!");
            Debug.LogError("  👉 SOLUTION: Move your prefab to Assets/Resources/ folder");
        }
        else
        {
            Debug.Log($"  ✅ Prefab found in Resources folder");
        }

        // 3. Check Prefab Components
        Debug.Log($"🔧 COMPONENT CHECK:");

        PhotonView pv = intercomPrefab.GetComponent<PhotonView>();
        if (pv == null)
        {
            Debug.LogError("  ❌ Missing PhotonView component!");
            Debug.LogError("  👉 SOLUTION: Add PhotonView component to your prefab");
        }
        else
        {
            Debug.Log($"  ✅ PhotonView found - View ID: {pv.ViewID}");
            Debug.Log($"  - Observed Components: {pv.ObservedComponents.Count}");
        }

        CodeCheckGame ccg = intercomPrefab.GetComponent<CodeCheckGame>();
        if (ccg == null)
        {
            Debug.LogError("  ❌ Missing CodeCheckGame component!");
        }
        else
        {
            Debug.Log("  ✅ CodeCheckGame found");
        }

        InterCom ic = intercomPrefab.GetComponent<InterCom>();
        if (ic == null)
        {
            Debug.LogError("  ❌ Missing InterCom component!");
        }
        else
        {
            Debug.Log("  ✅ InterCom found");
        }

        Collider2D col = intercomPrefab.GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError("  ❌ Missing Collider2D component!");
        }
        else
        {
            Debug.Log($"  ✅ Collider2D found - IsTrigger: {col.isTrigger}");
        }

        // 4. Check Existing Intercoms
        Debug.Log($"🎯 EXISTING INTERCOMS:");
        CodeCheckGame[] existing = FindObjectsOfType<CodeCheckGame>();
        Debug.Log($"  - Total in scene: {existing.Length}");

        for (int i = 0; i < existing.Length; i++)
        {
            CodeCheckGame game = existing[i];
            Debug.Log($"  - Intercom {i + 1}: '{game.name}' at {game.transform.position}");
            Debug.Log($"    Active: {game.gameObject.activeInHierarchy}, Enabled: {game.enabled}");

            PhotonView existingPV = game.GetComponent<PhotonView>();
            if (existingPV != null)
            {
                Debug.Log($"    PhotonView ID: {existingPV.ViewID}, IsMine: {existingPV.IsMine}");
            }
        }

        // 5. Test Spawn Position
        Debug.Log($"📍 SPAWN TEST:");
        IntercomSpawner spawner = FindObjectOfType<IntercomSpawner>();
        if (spawner != null)
        {
            Debug.Log("  ✅ IntercomSpawner found in scene");
            Debug.Log($"  - GameObject active: {spawner.gameObject.activeInHierarchy}");
            Debug.Log($"  - Component enabled: {spawner.enabled}");
        }
        else
        {
            Debug.LogError("  ❌ No IntercomSpawner found in scene!");
        }

        Debug.Log("=== DIAGNOSTIC COMPLETE ===");
        Debug.Log("👆 Check the messages above for any ❌ errors to fix");
    }

    [ContextMenu("Test Single Spawn")]
    public void TestSingleSpawn()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("You must be Master Client to test spawn");
            return;
        }

        if (intercomPrefab == null)
        {
            Debug.LogError("No prefab assigned!");
            return;
        }

        Debug.Log("Testing single intercom spawn...");

        GameObject testIntercom = PhotonNetwork.Instantiate(
            intercomPrefab.name,
            transform.position + Vector3.right * 2f,
            Quaternion.identity
        );

        if (testIntercom != null)
        {
            Debug.Log($"✅ Test spawn successful: {testIntercom.name}");
        }
        else
        {
            Debug.LogError("❌ Test spawn failed!");
        }
    }
}