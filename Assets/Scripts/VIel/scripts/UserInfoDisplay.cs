using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UserInfoDisplay : MonoBehaviour
{
    [Header("Display Options")]
    public bool showDisplayName = true;
    public bool showUserId = true;
    public bool showEmail = false;

    [Header("IS THIS THE CURRENT PLAYER'S DISPLAY?")]
    public bool isCurrentPlayerDisplay = false;

    [Header("UI Text References (Optional)")]
    public Text displayNameText;
    public Text userIdText;
    public Text emailText;

    [Header("XP & Level Display (Optional)")]
    public Text levelText;
    public Text xpText;
    public Slider xpProgressBar;

    [Header("Profile Icon (Optional)")]
    public Image profileIcon;
    public Sprite defaultProfileSprite;

    [Header("Host Indicator (Optional)")]
    public GameObject hostIndicator;

    [Header("Optional: Custom Labels")]
    public string displayNamePrefix = "";
    public string userIdPrefix = "";
    public string emailPrefix = "";

    [Header("Guest User Settings")]
    public string guestDisplayName = "Guest";

    private FirebaseAuth auth;
    private DatabaseReference database;
    private bool isFirebaseReady = false;

    private string assignedPlayerNickName = "";

    void Start()
    {
        Debug.Log($"[UserInfoDisplay] START - isCurrentPlayerDisplay={isCurrentPlayerDisplay}, displayNameText={displayNameText}");

        if (hostIndicator != null)
            hostIndicator.SetActive(false);

        StartCoroutine(WaitForFirebaseAndUpdate());
    }

    void OnEnable()
    {
        Debug.Log($"[UserInfoDisplay] OnEnable called");
        if (isFirebaseReady)
        {
            SafeUpdateUserInfo();
        }
        else
        {
            StartCoroutine(WaitForFirebaseAndUpdate());
        }
    }

    IEnumerator WaitForFirebaseAndUpdate()
    {
        while (FirebaseInitializer.Instance == null || !FirebaseInitializer.Instance.IsFirebaseReady())
        {
            yield return new WaitForSeconds(0.1f);
        }

        try
        {
            auth = FirebaseAuth.DefaultInstance;
            database = FirebaseDatabase.DefaultInstance?.RootReference;
            isFirebaseReady = true;
            Debug.Log("[UserInfoDisplay] Firebase ready");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[UserInfoDisplay] Firebase initialization failed: {ex.Message}");
            yield break;
        }

        SafeUpdateUserInfo();
    }

    public void SetPlayerInfo(string nickName)
    {
        assignedPlayerNickName = nickName;
        Debug.Log($"[UserInfoDisplay.SetPlayerInfo] Called with: '{nickName}', isCurrentPlayerDisplay={isCurrentPlayerDisplay}, displayNameText={displayNameText}");

        if (!isCurrentPlayerDisplay && displayNameText != null)
        {
            // If nickname is empty or null, use the guest display name
            string displayName = string.IsNullOrEmpty(nickName) ? guestDisplayName : nickName;
            displayNameText.text = displayNamePrefix + displayName;
            Debug.Log($"[UserInfoDisplay.SetPlayerInfo] SUCCESS - Set text to: '{displayNamePrefix + displayName}'");
        }
        else
        {
            Debug.LogWarning($"[UserInfoDisplay.SetPlayerInfo] SKIPPED - isCurrentPlayerDisplay={isCurrentPlayerDisplay}, displayNameText null={displayNameText == null}");
        }
    }

    public void SafeUpdateUserInfo()
    {
        try
        {
            UpdateUserInfo();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[UserInfoDisplay] UpdateUserInfo failed: {ex.Message}");
        }
    }

    public void UpdateUserInfo()
    {
        Debug.Log($"[UserInfoDisplay.UpdateUserInfo] Called - isCurrentPlayerDisplay={isCurrentPlayerDisplay}");

        if (!isFirebaseReady || auth == null)
        {
            if (isCurrentPlayerDisplay)
                SetGuestUI();
            return;
        }

        if (!isCurrentPlayerDisplay)
        {
            Debug.Log("[UserInfoDisplay.UpdateUserInfo] Not current player - returning early");
            return;
        }

        FirebaseUser user = null;
        try
        {
            user = auth.CurrentUser;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[UserInfoDisplay] Failed to get current user: {ex.Message}");
            SetGuestUI();
            return;
        }

        if (user != null)
        {
            if (showDisplayName && displayNameText != null)
            {
                string name = string.IsNullOrEmpty(user.DisplayName) ? guestDisplayName : user.DisplayName;
                displayNameText.text = displayNamePrefix + name;
            }

            if (showUserId && userIdText != null)
            {
                userIdText.text = userIdPrefix + user.UserId;
            }

            if (showEmail && emailText != null)
            {
                string email = string.IsNullOrEmpty(user.Email) ? "No email" : user.Email;
                emailText.text = emailPrefix + email;
            }

            if (profileIcon != null)
            {
                profileIcon.sprite = defaultProfileSprite ?? profileIcon.sprite;
            }

            if (database != null)
                LoadUserProgress(user.UserId);
            else
                SetDefaultProgress();
        }
        else
        {
            SetGuestUI();
        }
    }

    void SetGuestUI()
    {
        if (displayNameText != null)
            displayNameText.text = "Not logged in";

        if (userIdText != null)
            userIdText.text = "";

        if (emailText != null)
            emailText.text = "";

        SetDefaultProgress();
    }

    void SetDefaultProgress()
    {
        if (levelText != null)
            levelText.text = "Level 1";

        if (xpText != null)
            xpText.text = "0/50 XP";

        if (xpProgressBar != null)
            xpProgressBar.value = 0;
    }

    private void LoadUserProgress(string userId)
    {
        if (database == null || string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("[UserInfoDisplay] Database or userId is null. Using default progress.");
            SetDefaultProgress();
            return;
        }

        database.Child("users").Child(userId).Child("progress").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted || task.Result == null || !task.Result.Exists)
            {
                SetDefaultProgress();
                return;
            }

            DataSnapshot snapshot = task.Result;

            int level = 1;
            int xp = 0;
            int xpPerLevel = 50;

            try
            {
                if (snapshot.Child("level").Value != null)
                    int.TryParse(snapshot.Child("level").Value.ToString(), out level);
                if (snapshot.Child("xp").Value != null)
                    int.TryParse(snapshot.Child("xp").Value.ToString(), out xp);

                if (UserProgressManager.Instance != null)
                    xpPerLevel = UserProgressManager.Instance.xpPerLevel;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UserInfoDisplay] Failed parsing XP data: {ex.Message}");
            }

            if (levelText != null)
                levelText.text = $"Level {level}";

            if (xpText != null)
                xpText.text = $"{xp}/{xpPerLevel} XP";

            if (xpProgressBar != null)
            {
                xpProgressBar.maxValue = xpPerLevel;
                xpProgressBar.value = xp;
            }
        });
    }

    public void SetHostStatus(bool isHost)
    {
        if (hostIndicator != null)
            hostIndicator.SetActive(isHost);
    }

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(assignedPlayerNickName))
            return assignedPlayerNickName;

        try
        {
            if (isFirebaseReady && auth != null && auth.CurrentUser != null)
                return string.IsNullOrEmpty(auth.CurrentUser.DisplayName) ? guestDisplayName : auth.CurrentUser.DisplayName;
        }
        catch { }

        return guestDisplayName;
    }
}