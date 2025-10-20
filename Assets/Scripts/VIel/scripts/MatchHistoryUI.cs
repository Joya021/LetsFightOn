using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Displays Match History in a Scroll View
/// Attach to a GameObject with the match history UI
/// </summary>
public class MatchHistoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject matchHistoryPanel; // The panel containing the scroll view
    public Transform contentParent; // Content object inside Scroll View
    public GameObject matchEntryPrefab; // Prefab for each match entry

    [Header("Settings")]
    public int maxMatchesToShow = 20; // Show last 20 matches

    private FirebaseAuth auth;
    private DatabaseReference database;
    private List<MatchEntry> matchHistory = new List<MatchEntry>();

    [System.Serializable]
    public class MatchEntry
    {
        public string result; // "WIN" or "LOSS"
        public int xpGained;
        public string date;
        public long timestamp;
    }

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        database = FirebaseDatabase.DefaultInstance.RootReference;

        if (matchHistoryPanel != null)
            matchHistoryPanel.SetActive(false);
    }

    // Call this to open and load match history
    public void ShowMatchHistory()
    {
        if (matchHistoryPanel != null)
            matchHistoryPanel.SetActive(true);

        LoadMatchHistory();
    }

    public void HideMatchHistory()
    {
        if (matchHistoryPanel != null)
            matchHistoryPanel.SetActive(false);
    }

    private void LoadMatchHistory()
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogWarning("No user logged in. Cannot load match history.");
            return;
        }

        string userId = auth.CurrentUser.UserId;

        // Clear existing entries
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        matchHistory.Clear();

        // Load from Firebase
        database.Child("users").Child(userId).Child("matchHistory").LimitToLast(maxMatchesToShow).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    foreach (DataSnapshot matchSnapshot in snapshot.Children)
                    {
                        MatchEntry entry = new MatchEntry
                        {
                            result = matchSnapshot.Child("result").Value.ToString(),
                            xpGained = int.Parse(matchSnapshot.Child("xpGained").Value.ToString()),
                            date = matchSnapshot.Child("date").Value.ToString(),
                            timestamp = long.Parse(matchSnapshot.Child("timestamp").Value.ToString())
                        };

                        matchHistory.Add(entry);
                    }

                    // Sort by timestamp descending (newest first)
                    matchHistory = matchHistory.OrderByDescending(m => m.timestamp).ToList();

                    // Display matches
                    DisplayMatchHistory();
                }
                else
                {
                    Debug.Log("No match history found for this user");
                    DisplayNoMatches();
                }
            }
            else
            {
                Debug.LogError("Failed to load match history: " + task.Exception);
            }
        });
    }

    private void DisplayMatchHistory()
    {
        foreach (MatchEntry match in matchHistory)
        {
            GameObject matchObj = Instantiate(matchEntryPrefab, contentParent);

            // Find UI components in prefab
            Text resultText = matchObj.transform.Find("ResultText")?.GetComponent<Text>();
            Text xpText = matchObj.transform.Find("XPText")?.GetComponent<Text>();
            Text dateText = matchObj.transform.Find("DateText")?.GetComponent<Text>();
            Image bgImage = matchObj.GetComponent<Image>();

            // Set values
            if (resultText != null)
            {
                resultText.text = match.result;
                resultText.color = match.result == "WIN" ? Color.green : Color.red;
            }

            if (xpText != null)
                xpText.text = $"+{match.xpGained} XP";

            if (dateText != null)
                dateText.text = match.date;

            // Optional: Color background based on win/loss
            if (bgImage != null)
            {
                bgImage.color = match.result == "WIN" ? new Color(0.2f, 0.8f, 0.2f, 0.3f) : new Color(0.8f, 0.2f, 0.2f, 0.3f);
            }
        }

        Debug.Log($"Displayed {matchHistory.Count} match history entries");
    }

    private void DisplayNoMatches()
    {
        GameObject noMatchesObj = Instantiate(matchEntryPrefab, contentParent);
        Text resultText = noMatchesObj.transform.Find("ResultText")?.GetComponent<Text>();

        if (resultText != null)
        {
            resultText.text = "No matches played yet";
            resultText.color = Color.gray;
        }
    }

    // Call this to refresh match history
    public void RefreshMatchHistory()
    {
        LoadMatchHistory();
    }
}