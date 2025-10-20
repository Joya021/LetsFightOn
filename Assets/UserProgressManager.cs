using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

/// <summary>
/// Complete XP, Level, Leaderboard, Match History, and Achievements System
/// Attach this to a GameObject in your scene
/// </summary>
public class UserProgressManager : MonoBehaviour
{
    public static UserProgressManager Instance { get; private set; }

    [Header("Firebase References")]
    private FirebaseAuth auth;
    private DatabaseReference database;

    [Header("XP & Level Settings")]
    public int xpPerLevel = 50;
    public int startingLevel = 1;

    [Header("XP Rewards")]
    public int winXP = 10;
    public int loseXP = 2;

    [Header("Current User Data - Runtime")]
    private int currentUserXP = 0;
    private int currentUserLevel = 1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        database = FirebaseDatabase.DefaultInstance.RootReference;

        // Load user progress when they're logged in
        if (auth.CurrentUser != null)
        {
            LoadUserProgress(auth.CurrentUser.UserId);
        }
    }

    // ============================
    // LOAD USER PROGRESS
    // ============================
    public void LoadUserProgress(string userId)
    {
        database.Child("users").Child(userId).Child("progress").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    // Load existing data
                    currentUserXP = int.Parse(snapshot.Child("xp").Value.ToString());
                    currentUserLevel = int.Parse(snapshot.Child("level").Value.ToString());

                    Debug.Log($"User progress loaded: Level {currentUserLevel}, XP {currentUserXP}");
                }
                else
                {
                    // Initialize new user
                    currentUserXP = 0;
                    currentUserLevel = startingLevel;
                    SaveUserProgress(userId);
                    Debug.Log("New user initialized with default progress");
                }
            }
            else
            {
                Debug.LogError("Failed to load user progress: " + task.Exception);
            }
        });
    }

    // ============================
    // SAVE USER PROGRESS
    // ============================
    public void SaveUserProgress(string userId)
    {
        Dictionary<string, object> progressData = new Dictionary<string, object>
        {
            { "xp", currentUserXP },
            { "level", currentUserLevel }
        };

        database.Child("users").Child(userId).Child("progress").UpdateChildrenAsync(progressData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("User progress saved successfully");
            }
            else
            {
                Debug.LogError("Failed to save user progress: " + task.Exception);
            }
        });
    }

    // ============================
    // ADD XP (Called when game ends)
    // ============================
    public void AddXP(int xpAmount, bool won, int bonusXP = 0)
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogWarning("No user logged in. Cannot add XP.");
            return;
        }

        string userId = auth.CurrentUser.UserId;
        string displayName = auth.CurrentUser.DisplayName ?? "Player";

        // Calculate total XP gained
        int totalXPGained = xpAmount + bonusXP;

        // Store old total XP for leaderboard check
        int oldTotalXP = (currentUserLevel - 1) * xpPerLevel + currentUserXP;

        currentUserXP += totalXPGained;

        // Check for level up
        while (currentUserXP >= xpPerLevel)
        {
            currentUserXP -= xpPerLevel;
            currentUserLevel++;
            Debug.Log($"LEVEL UP! Now level {currentUserLevel}");
        }

        // Calculate new total XP
        int newTotalXP = (currentUserLevel - 1) * xpPerLevel + currentUserXP;

        // Save progress
        SaveUserProgress(userId);

        // Record match history
        RecordMatchHistory(userId, won, totalXPGained);

        // Check for achievements
        CheckAchievements(userId, won);

        // CRITICAL FIX: Update leaderboard when player reaches 50+ total XP
        if (oldTotalXP < 50 && newTotalXP >= 50)
        {
            Debug.Log($"[UserProgressManager] Player reached 50 XP milestone! Adding to leaderboard.");
            UpdateLeaderboard(userId, displayName);
        }
        else if (newTotalXP >= 50)
        {
            // Always update leaderboard if player already has 50+ XP
            UpdateLeaderboard(userId, displayName);
        }

        Debug.Log($"Added {totalXPGained} XP. Current: Level {currentUserLevel}, XP {currentUserXP}/{xpPerLevel}. Total XP: {newTotalXP}");
    }
    // ============================
    // RECORD MATCH HISTORY
    // ============================
    private void RecordMatchHistory(string userId, bool won, int xpGained)
    {
        string matchId = database.Child("users").Child(userId).Child("matchHistory").Push().Key;

        Dictionary<string, object> matchData = new Dictionary<string, object>
        {
            { "timestamp", ServerValue.Timestamp },
            { "result", won ? "WIN" : "LOSS" },
            { "xpGained", xpGained },
            { "date", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") }
        };

        database.Child("users").Child(userId).Child("matchHistory").Child(matchId).SetValueAsync(matchData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Match history recorded");
            }
            else
            {
                Debug.LogError("Failed to record match: " + task.Exception);
            }
        });
    }

    // ============================
    // CHECK ACHIEVEMENTS
    // ============================
    private void CheckAchievements(string userId, bool won)
    {
        if (won)
        {
            // Grant "First Win" achievement
            database.Child("users").Child(userId).Child("achievements").Child("firstWin").GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    if (!task.Result.Exists)
                    {
                        // First time winning
                        Dictionary<string, object> achievementData = new Dictionary<string, object>
                        {
                            { "name", "First Victory!" },
                            { "description", "Win your first game" },
                            { "unlockedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") },
                            { "timestamp", ServerValue.Timestamp }
                        };

                        database.Child("users").Child(userId).Child("achievements").Child("firstWin").SetValueAsync(achievementData);
                        Debug.Log("🏆 Achievement Unlocked: First Victory!");
                    }
                }
            });

            // Increment total wins
            database.Child("users").Child(userId).Child("stats").Child("totalWins").RunTransaction(mutableData =>
            {
                int currentWins = mutableData.Value != null ? int.Parse(mutableData.Value.ToString()) : 0;
                mutableData.Value = currentWins + 1;
                return TransactionResult.Success(mutableData);
            });
        }
        else
        {
            // Increment total losses
            database.Child("users").Child(userId).Child("stats").Child("totalLosses").RunTransaction(mutableData =>
            {
                int currentLosses = mutableData.Value != null ? int.Parse(mutableData.Value.ToString()) : 0;
                mutableData.Value = currentLosses + 1;
                return TransactionResult.Success(mutableData);
            });
        }
    }

    // ============================
    // UPDATE LEADERBOARD
    // ============================
    public void UpdateLeaderboard(string userId, string displayName)
    {
        int totalXP = (currentUserLevel - 1) * xpPerLevel + currentUserXP;

        Dictionary<string, object> leaderboardEntry = new Dictionary<string, object>
    {
        { "displayName", displayName },
        { "level", currentUserLevel },
        { "xp", currentUserXP },
        { "totalXP", totalXP },
        { "lastUpdated", ServerValue.Timestamp }
    };

        database.Child("leaderboard").Child(userId).SetValueAsync(leaderboardEntry).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"[UserProgressManager] Leaderboard updated for {displayName}. Total XP: {totalXP}");
            }
            else
            {
                Debug.LogError($"[UserProgressManager] Failed to update leaderboard: {task.Exception}");
            }
        });
    }

    // ============================
    // GET CURRENT USER DATA
    // ============================
    public int GetCurrentLevel()
    {
        return currentUserLevel;
    }

    public int GetCurrentXP()
    {
        return currentUserXP;
    }

    public int GetXPForNextLevel()
    {
        return xpPerLevel;
    }
}