    using Firebase.Database;
    using Firebase.Extensions;
    using UnityEngine;
    using UnityEngine.UI;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Displays Leaderboard in a Scroll View
    /// Attach to a GameObject with the leaderboard UI
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject leaderboardPanel; // The panel containing the scroll view
        public Transform contentParent; // Content object inside Scroll View
        public GameObject leaderboardEntryPrefab; // Prefab for each entry

        [Header("Settings")]
        public int maxEntriesToShow = 50; // Top 50 players

        private DatabaseReference database;
        private List<LeaderboardEntry> leaderboardData = new List<LeaderboardEntry>();

        [System.Serializable]
        public class LeaderboardEntry
        {
            public string userId;
            public string displayName;
            public int level;
            public int xp;
            public int totalXP;
        }

        void Start()
        {
            database = FirebaseDatabase.DefaultInstance.RootReference;

            if (leaderboardPanel != null)
                leaderboardPanel.SetActive(false);
        }

        // Call this to open and load leaderboard
        public void ShowLeaderboard()
        {
            if (leaderboardPanel != null)
                leaderboardPanel.SetActive(true);

            LoadLeaderboard();
        }

        public void HideLeaderboard()
        {
            if (leaderboardPanel != null)
                leaderboardPanel.SetActive(false);
        }

    private void LoadLeaderboard()
    {
        // Clear existing entries
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        leaderboardData.Clear();

        // Load from Firebase - get entries with 50+ total XP
        database.Child("leaderboard").OrderByChild("totalXP").StartAt(50).LimitToLast(maxEntriesToShow).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                foreach (DataSnapshot userSnapshot in snapshot.Children)
                {
                    try
                    {
                        int totalXP = int.Parse(userSnapshot.Child("totalXP").Value.ToString());

                        // CRITICAL FIX: Only show players with 50+ total XP
                        if (totalXP >= 50)
                        {
                            LeaderboardEntry entry = new LeaderboardEntry
                            {
                                userId = userSnapshot.Key,
                                displayName = userSnapshot.Child("displayName").Value.ToString(),
                                level = int.Parse(userSnapshot.Child("level").Value.ToString()),
                                xp = int.Parse(userSnapshot.Child("xp").Value.ToString()),
                                totalXP = totalXP
                            };

                            leaderboardData.Add(entry);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[LeaderboardUI] Failed to parse entry: {ex.Message}");
                    }
                }

                // Sort by totalXP descending
                leaderboardData = leaderboardData.OrderByDescending(e => e.totalXP).ToList();

                // Display entries
                DisplayLeaderboard();

                Debug.Log($"[LeaderboardUI] Loaded {leaderboardData.Count} players with 50+ XP");
            }
            else
            {
                Debug.LogError("Failed to load leaderboard: " + task.Exception);
            }
        });
    }

    private void DisplayLeaderboard()
        {
            for (int i = 0; i < leaderboardData.Count; i++)
            {
                LeaderboardEntry entry = leaderboardData[i];

                GameObject entryObj = Instantiate(leaderboardEntryPrefab, contentParent);

                // Find UI components in prefab
                Text rankText = entryObj.transform.Find("RankText")?.GetComponent<Text>();
                Text nameText = entryObj.transform.Find("NameText")?.GetComponent<Text>();
                Text levelText = entryObj.transform.Find("LevelText")?.GetComponent<Text>();
                Text xpText = entryObj.transform.Find("XPText")?.GetComponent<Text>();

                // Set values
                if (rankText != null)
                    rankText.text = $"#{i + 1}";

                if (nameText != null)
                    nameText.text = entry.displayName;

                if (levelText != null)
                    levelText.text = $"Level {entry.level}";

                if (xpText != null)
                    xpText.text = $"{entry.totalXP} XP";
            }

            Debug.Log($"Displayed {leaderboardData.Count} leaderboard entries");
        }

        // Call this to refresh leaderboard
        public void RefreshLeaderboard()
        {
            LoadLeaderboard();
        }
    }