using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


public class AchievementsUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject achievementsPanel; 
    public Transform contentParent;
    public GameObject achievementEntryPrefab; 

    private FirebaseAuth auth;
    private DatabaseReference database;

    // Define all possible achievements
    private List<AchievementDefinition> allAchievements = new List<AchievementDefinition>
    {
        new AchievementDefinition
        {
            id = "firstWin",
            name = "First Victory!",
            description = "Win your first game",
            icon = "🏆"
        },
        new AchievementDefinition
        {
            id = "win5Games",
            name = "Champion",
            description = "Win 5 games",
            icon = "🥇"
        },
        new AchievementDefinition
        {
            id = "win10Games",
            name = "Legendary",
            description = "Win 10 games",
            icon = "⭐"
        },
        new AchievementDefinition
        {
            id = "reachLevel5",
            name = "Rising Star",
            description = "Reach Level 5",
            icon = "🌟"
        },
        new AchievementDefinition
        {
            id = "reachLevel10",
            name = "Expert",
            description = "Reach Level 10",
            icon = "💎"
        }
    };

    [System.Serializable]
    public class AchievementDefinition
    {
        public string id;
        public string name;
        public string description;
        public string icon;
    }

    [System.Serializable]
    public class UnlockedAchievement
    {
        public string name;
        public string description;
        public string unlockedAt;
        public bool isUnlocked;
    }

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        database = FirebaseDatabase.DefaultInstance.RootReference;

        if (achievementsPanel != null)
            achievementsPanel.SetActive(false);
    }

    // Call this to open and load achievements
    public void ShowAchievements()
    {
        if (achievementsPanel != null)
            achievementsPanel.SetActive(true);

        LoadAchievements();
    }

    public void HideAchievements()
    {
        if (achievementsPanel != null)
            achievementsPanel.SetActive(false);
    }

    private void LoadAchievements()
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogWarning("No user logged in. Cannot load achievements.");
            return;
        }

        string userId = auth.CurrentUser.UserId;

        // Clear existing entries
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // Load unlocked achievements from Firebase
        database.Child("users").Child(userId).Child("achievements").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                // Display all achievements (locked and unlocked)
                foreach (AchievementDefinition achievement in allAchievements)
                {
                    GameObject achievementObj = Instantiate(achievementEntryPrefab, contentParent);

                    // Find UI components in prefab
                    Text iconText = achievementObj.transform.Find("IconText")?.GetComponent<Text>();
                    Text nameText = achievementObj.transform.Find("NameText")?.GetComponent<Text>();
                    Text descText = achievementObj.transform.Find("DescText")?.GetComponent<Text>();
                    Text statusText = achievementObj.transform.Find("StatusText")?.GetComponent<Text>();
                    Image bgImage = achievementObj.GetComponent<Image>();

                    // Check if achievement is unlocked
                    bool isUnlocked = snapshot.HasChild(achievement.id);

                    // Set values
                    if (iconText != null)
                        iconText.text = achievement.icon;

                    if (nameText != null)
                    {
                        nameText.text = achievement.name;
                        nameText.color = isUnlocked ? Color.white : Color.gray;
                    }

                    if (descText != null)
                    {
                        descText.text = achievement.description;
                        descText.color = isUnlocked ? Color.white : Color.gray;
                    }

                    if (statusText != null)
                    {
                        if (isUnlocked)
                        {
                            string unlockedDate = snapshot.Child(achievement.id).Child("unlockedAt").Value.ToString();
                            statusText.text = $"Unlocked: {unlockedDate}";
                            statusText.color = Color.green;
                        }
                        else
                        {
                            statusText.text = "🔒 Locked";
                            statusText.color = Color.gray;
                        }
                    }

                    // Set background color
                    if (bgImage != null)
                    {
                        bgImage.color = isUnlocked ? new Color(1f, 0.84f, 0f, 0.3f) : new Color(0.5f, 0.5f, 0.5f, 0.2f);
                    }
                }

                Debug.Log($"Displayed {allAchievements.Count} achievements");
            }
            else
            {
                Debug.LogError("Failed to load achievements: " + task.Exception);
            }
        });
    }

    public void RefreshAchievements()
    {
        LoadAchievements();
    }
}