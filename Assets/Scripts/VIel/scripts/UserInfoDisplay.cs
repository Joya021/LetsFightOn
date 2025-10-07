using Firebase.Auth;
using UnityEngine;
using UnityEngine.UI;

public class UserInfoDisplay : MonoBehaviour
{
    [Header("Display Options")]
    public bool showDisplayName = true;
    public bool showUserId = true;
    public bool showEmail = false;

    [Header("UI Text References")]
    public Text displayNameText;
    public Text userIdText;
    public Text emailText;

    [Header("Profile Icon")]
    public Image profileIcon;
    public Sprite defaultProfileSprite; // Assign a default uniform icon in inspector

    [Header("Host Indicator")]
    public GameObject hostIndicator; // Image/Icon showing "HOST"

    [Header("Optional: Custom Labels")]
    public string displayNamePrefix = "";
    public string userIdPrefix = "";
    public string emailPrefix = "";

    [Header("Guest User Settings")]
    public string guestDisplayName = "Guest";

    private FirebaseAuth auth;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        // Hide host indicator by default
        if (hostIndicator != null)
            hostIndicator.SetActive(false);

        UpdateUserInfo();
    }

    void OnEnable()
    {
        UpdateUserInfo();
    }

    public void UpdateUserInfo()
    {
        if (auth == null)
            auth = FirebaseAuth.DefaultInstance;

        FirebaseUser user = auth.CurrentUser;

        if (user != null)
        {
            // Update Display Name
            if (showDisplayName && displayNameText != null)
            {
                string name = string.IsNullOrEmpty(user.DisplayName) ? guestDisplayName : user.DisplayName;
                displayNameText.text = displayNamePrefix + name;
            }

            // Update User ID
            if (showUserId && userIdText != null)
            {
                userIdText.text = userIdPrefix + user.UserId;
            }

            // Update Email
            if (showEmail && emailText != null)
            {
                string email = string.IsNullOrEmpty(user.Email) ? "No email" : user.Email;
                emailText.text = emailPrefix + email;
            }

            // Set Profile Icon
            if (profileIcon != null && defaultProfileSprite != null)
            {
                profileIcon.sprite = defaultProfileSprite;
            }
        }
        else
        {
            if (displayNameText != null)
                displayNameText.text = "Not logged in";

            if (userIdText != null)
                userIdText.text = "";

            if (emailText != null)
                emailText.text = "";
        }
    }

    public void SetHostStatus(bool isHost)
    {
        if (hostIndicator != null)
            hostIndicator.SetActive(isHost);
    }

    public string GetDisplayName()
    {
        if (auth != null && auth.CurrentUser != null)
        {
            return string.IsNullOrEmpty(auth.CurrentUser.DisplayName) ? guestDisplayName : auth.CurrentUser.DisplayName;
        }
        return guestDisplayName;
    }
}