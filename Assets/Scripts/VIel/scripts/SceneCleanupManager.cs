using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// Cleans up scene transitions and ensures UI works properly
/// Attach to a GameObject in your login scene
/// </summary>
public class SceneCleanupManager : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");

        // Ensure EventSystem exists and is active
        EnsureEventSystem();

        // Clean up duplicate canvases if needed
        CleanupDuplicateCanvases();
    }

    void EnsureEventSystem()
    {
        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();

        if (eventSystems.Length == 0)
        {
            Debug.LogWarning("⚠️ No EventSystem found! Creating one...");
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            eventSystemObj.AddComponent<EventSystemManager>(); // Add our singleton manager
        }
        else if (eventSystems.Length > 1)
        {
            Debug.LogWarning($"⚠️ Multiple EventSystems detected ({eventSystems.Length})! Cleaning up...");

            // Keep the first one with DontDestroyOnLoad
            EventSystem keepSystem = null;
            foreach (var es in eventSystems)
            {
                if (es.GetComponent<EventSystemManager>() != null)
                {
                    keepSystem = es;
                    break;
                }
            }

            if (keepSystem == null)
                keepSystem = eventSystems[0];

            // Destroy others
            foreach (var es in eventSystems)
            {
                if (es != keepSystem)
                {
                    Debug.Log($"Destroying duplicate EventSystem: {es.gameObject.name}");
                    Destroy(es.gameObject);
                }
            }
        }
        else
        {
            Debug.Log("✅ EventSystem OK");
        }
    }

    void CleanupDuplicateCanvases()
    {
        // Optional: Clean up any duplicate UI canvases
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Debug.Log($"Found {canvases.Length} canvases in scene");
    }

    void Start()
    {
        EnsureEventSystem();
    }
}