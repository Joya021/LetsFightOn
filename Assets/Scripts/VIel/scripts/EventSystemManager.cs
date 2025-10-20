using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures only ONE EventSystem exists across all scenes
/// Attach this to your EventSystem GameObject in the FIRST scene (login scene)
/// </summary>
public class EventSystemManager : MonoBehaviour
{
    private static EventSystemManager instance;
    private EventSystem eventSystem;
    private StandaloneInputModule inputModule;

    void Awake()
    {
        // Check if an instance already exists
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Cache components
            eventSystem = GetComponent<EventSystem>();
            inputModule = GetComponent<StandaloneInputModule>();

            // Ensure components exist
            if (eventSystem == null)
                eventSystem = gameObject.AddComponent<EventSystem>();
            if (inputModule == null)
                inputModule = gameObject.AddComponent<StandaloneInputModule>();

            Debug.Log("✅ EventSystem persisted across scenes");
        }
        else
        {
            // Duplicate EventSystem found - destroy it
            Debug.LogWarning($"⚠️ Duplicate EventSystem '{gameObject.name}' detected and destroyed");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (instance != this) return;

        Debug.Log($"Scene loaded: {scene.name} - Ensuring EventSystem is active");

        // Ensure our EventSystem is enabled
        if (eventSystem != null)
            eventSystem.enabled = true;

        if (inputModule != null)
            inputModule.enabled = true;

        // Destroy any other EventSystems in the scene
        EventSystem[] allEventSystems = FindObjectsOfType<EventSystem>(true);
        foreach (EventSystem es in allEventSystems)
        {
            if (es.gameObject != this.gameObject)
            {
                Debug.Log($"Removing duplicate EventSystem: {es.gameObject.name}");
                Destroy(es.gameObject);
            }
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void Update()
    {
        // Ensure we're always the current EventSystem
        if (instance == this && EventSystem.current != eventSystem)
        {
            EventSystem.current = eventSystem;
        }
    }
}