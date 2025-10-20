using UnityEngine;
using System.Collections;

public class InternetChecker : MonoBehaviour
{
    public GameObject noInternetPanel; // Assign this in inspector

    [Header("Settings")]
    [Tooltip("Delay before first check (allows time for network initialization)")]
    public float initialCheckDelay = 1f;

    [Tooltip("How often to recheck internet (in seconds)")]
    public float recheckInterval = 5f;

    private bool hasShownPanel = false; // Track if panel has been shown

    void Start()
    {
        // Hide the panel initially - only show if internet check fails
        ShowNoInternetPanel(false);

        // Wait a moment before checking to allow network to initialize
        StartCoroutine(DelayedInternetCheck());
    }

    IEnumerator DelayedInternetCheck()
    {
        // Wait for initial delay
        yield return new WaitForSeconds(initialCheckDelay);

        // Perform the check only once
        yield return StartCoroutine(CheckInternetConnection());

        // Don't start periodic rechecking - only check once
    }

    IEnumerator CheckInternetConnection()
    {
        // Method 1: Check Unity's internet reachability
        bool hasInternet = Application.internetReachability != NetworkReachability.NotReachable;

        // Method 2: Try to ping a reliable server (more reliable than Application.internetReachability)
        if (hasInternet)
        {
            // Try to actually connect to a server to verify
            UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Head("https://www.google.com");
            www.timeout = 5; // 5 second timeout

            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log("Internet connection verified.");
                // Don't show panel - internet is good
            }
            else
            {
                Debug.LogWarning("No internet connection detected via ping test.");
                ShowNoInternetPanel(true);
            }

            www.Dispose();
        }
        else
        {
            Debug.LogWarning("No internet connection detected via reachability check.");
            ShowNoInternetPanel(true);
        }
    }

    void ShowNoInternetPanel(bool show)
    {
        // Only show the panel once
        if (show && hasShownPanel)
        {
            Debug.Log("Panel already shown once. Not showing again.");
            return;
        }

        if (noInternetPanel != null && show)
        {
            noInternetPanel.SetActive(true);
            hasShownPanel = true; // Mark that we've shown it
            Debug.Log("No Internet Panel: SHOWN (once)");
        }
        else if (noInternetPanel != null && !show)
        {
            noInternetPanel.SetActive(false);
            Debug.Log("No Internet Panel: HIDDEN");
        }
        else if (noInternetPanel == null)
        {
            Debug.LogWarning("No Internet Panel is not assigned in InternetChecker!");
        }
    }

    // Remove periodic checking method since we only check once
    // IEnumerator PeriodicInternetCheck() - REMOVED

    // Public method to manually trigger a check (useful for retry buttons)
    public void ManualCheckInternet()
    {
        StopAllCoroutines();
        StartCoroutine(DelayedInternetCheck());
    }
}