using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UserRegistrationLogin : MonoBehaviour
{
    [Header("Input Fields")]
    public InputField nameInputField;
    public InputField emailInputField;
    public InputField passwordInputField;
    public InputField confirmPasswordInputField;

    [Header("UI Texts")]
    public Text statusText;
    public Text displayNameText;
    public Text userIdText;
    public Text verificationMessageText;

    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject userPanel;
    public GameObject verificationMessagePanel;
    public GameObject guestSignInPanel;
    public GameObject newUserPanel;

    [Header("Password Toggle Buttons")]
    public Button passwordToggleButton;
    public Button confirmPasswordToggleButton;

    [Header("New User Panel Settings")]
    [Tooltip("When to show the new user panel")]
    public NewUserPanelTiming newUserPanelTiming = NewUserPanelTiming.OnRegistration;

    public enum NewUserPanelTiming
    {
        OnRegistration,          // Show immediately after registration
        OnFirstVerifiedLogin,    // Show on first login after email verification
        OnBoth                   // Show on registration AND first verified login
    }

    private FirebaseAuth auth;
    private bool isPasswordVisible = false;
    private bool isConfirmPasswordVisible = false;
    private bool isFirebaseReady = false;
    private const string FIRST_LOGIN_KEY = "HasCompletedFirstLogin_";

    void Start()
    {
        // Wait a frame to ensure EventSystem is ready
        StartCoroutine(InitializeAfterFrame());
    }

    IEnumerator InitializeAfterFrame()
    {
        // Wait one frame for scene to fully load
        yield return null;

        // Force UI to be interactable
        ForceEnableButtons();

        // Wait for Firebase to initialize
        StartCoroutine(WaitForFirebase());
    }

    void OnEnable()
    {
        // Re-enable buttons when scene is reloaded
        ForceEnableButtons();

        // Re-check Firebase status if we're returning to this script
        if (auth == null && FirebaseInitializer.Instance != null && FirebaseInitializer.Instance.IsFirebaseReady())
        {
            StartCoroutine(ReinitializeAuth());
        }
    }

    void ForceEnableButtons()
    {
        // Find all buttons in the scene and ensure they're interactable
        UnityEngine.UI.Button[] buttons = GetComponentsInChildren<UnityEngine.UI.Button>(true);
        foreach (var btn in buttons)
        {
            btn.interactable = true;
        }

        // Don't force panel states here - let WaitForFirebase handle it
    }

    IEnumerator ReinitializeAuth()
    {
        yield return new WaitForSeconds(0.1f);

        try
        {
            auth = FirebaseAuth.DefaultInstance;
            isFirebaseReady = true;

            // Clear status text
            if (statusText != null)
                statusText.text = "";

            // Check if there's a logged-in user
            CheckAutoLogin();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to reinitialize Firebase Auth: {ex.Message}");
            if (statusText != null)
                statusText.text = "Failed to initialize authentication.";
            isFirebaseReady = false;
        }
    }

    IEnumerator WaitForFirebase()
    {
        // Hide all panels initially to prevent flickering
        if (loginPanel != null)
            loginPanel.SetActive(false);

        if (userPanel != null)
            userPanel.SetActive(false);

        if (verificationMessagePanel != null)
            verificationMessagePanel.SetActive(false);

        if (guestSignInPanel != null)
            guestSignInPanel.SetActive(false);

        if (newUserPanel != null)
            newUserPanel.SetActive(false);

        // Show loading message
        if (statusText != null)
            statusText.text = "Initializing...";

        // Wait until Firebase is ready
        while (FirebaseInitializer.Instance == null || !FirebaseInitializer.Instance.IsFirebaseReady())
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Firebase is ready, initialize auth
        try
        {
            auth = FirebaseAuth.DefaultInstance;
            isFirebaseReady = true;

            // Set initial password fields to censored
            if (passwordInputField != null)
            {
                passwordInputField.contentType = InputField.ContentType.Password;
                passwordInputField.ForceLabelUpdate();
            }

            if (confirmPasswordInputField != null)
            {
                confirmPasswordInputField.contentType = InputField.ContentType.Password;
                confirmPasswordInputField.ForceLabelUpdate();
            }

            // Clear status text
            if (statusText != null)
                statusText.text = "";

            // Check for existing user first, then decide which panel to show
            if (auth.CurrentUser != null)
            {
                // User exists, let CheckAutoLogin handle panel display
                CheckAutoLogin();
            }
            else
            {
                // No user logged in, show login panel
                if (loginPanel != null)
                    loginPanel.SetActive(true);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to initialize Firebase Auth: {ex.Message}");
            if (statusText != null)
                statusText.text = "Failed to initialize authentication.";

            // Show login panel on error
            if (loginPanel != null)
                loginPanel.SetActive(true);
        }
    }

    private void CheckAutoLogin()
    {
        if (!isFirebaseReady || auth == null)
            return;

        if (auth.CurrentUser != null)
        {
            FirebaseUser user = auth.CurrentUser;

            if (user.IsAnonymous)
            {
                statusText.text = "Auto-login successful!";
                UpdateUserUI(user);
                SwitchToUserPanel();
            }
            else if (user.IsEmailVerified)
            {
                statusText.text = "Auto-login successful!";
                UpdateUserUI(user);
                SwitchToUserPanel();

                // Check if this is their first login
                CheckAndShowNewUserPanel(user);
            }
            else
            {
                statusText.text = "Please verify your email to continue.";
                ShowVerificationMessage(user.Email);
            }
        }
    }

    // Check if user has completed first login and show panel if needed
    private void CheckAndShowNewUserPanel(FirebaseUser user)
    {
        if (newUserPanelTiming == NewUserPanelTiming.OnRegistration)
            return; // Only show on registration, not on login

        string userKey = FIRST_LOGIN_KEY + user.UserId;
        bool hasCompletedFirstLogin = PlayerPrefs.GetInt(userKey, 0) == 1;

        if (!hasCompletedFirstLogin)
        {
            ShowNewUserPanel();
        }
    }

    // Mark that the user has completed their first login
    public void MarkFirstLoginComplete()
    {
        if (auth != null && auth.CurrentUser != null)
        {
            string userKey = FIRST_LOGIN_KEY + auth.CurrentUser.UserId;
            PlayerPrefs.SetInt(userKey, 1);
            PlayerPrefs.Save();
        }
    }

    // Password toggle functions
    public void TogglePasswordVisibility()
    {
        if (passwordInputField == null) return;

        isPasswordVisible = !isPasswordVisible;
        passwordInputField.contentType = isPasswordVisible ?
            InputField.ContentType.Standard : InputField.ContentType.Password;
        passwordInputField.ForceLabelUpdate();
    }

    public void ToggleConfirmPasswordVisibility()
    {
        if (confirmPasswordInputField == null) return;

        isConfirmPasswordVisible = !isConfirmPasswordVisible;
        confirmPasswordInputField.contentType = isConfirmPasswordVisible ?
            InputField.ContentType.Standard : InputField.ContentType.Password;
        confirmPasswordInputField.ForceLabelUpdate();
    }

    // Show Guest Sign In Panel
    public void ShowGuestSignInPanel()
    {
        if (guestSignInPanel != null)
            guestSignInPanel.SetActive(true);
    }

    // Close Guest Sign In Panel
    public void CloseGuestSignInPanel()
    {
        if (guestSignInPanel != null)
            guestSignInPanel.SetActive(false);
    }

    // Show New User Panel
    public void ShowNewUserPanel()
    {
        if (newUserPanel != null)
            newUserPanel.SetActive(true);
    }

    // Close New User Panel
    public void CloseNewUserPanel()
    {
        if (newUserPanel != null)
            newUserPanel.SetActive(false);

        // Mark first login as complete when they close the panel
        MarkFirstLoginComplete();
    }

    // Register User
    public void RegisterUser()
    {
        if (!isFirebaseReady || auth == null)
        {
            statusText.text = "Authentication not ready. Please wait...";
            return;
        }

        string name = nameInputField.text;
        string email = emailInputField.text;
        string password = passwordInputField.text;
        string confirmPassword = confirmPasswordInputField.text;

        if (password != confirmPassword)
        {
            statusText.text = "Passwords do not match!";
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                statusText.text = "Registration canceled!";
                return;
            }

            if (task.IsFaulted)
            {
                statusText.text = "Registration failed: " + task.Exception?.Message;
                return;
            }

            FirebaseUser newUser = task.Result.User;

            UserProfile profile = new UserProfile { DisplayName = name };
            newUser.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(updateTask =>
            {
                if (updateTask.IsCompleted)
                {
                    statusText.text = $"Registered as {newUser.DisplayName}";

                    // Show new user panel based on timing setting
                    if (newUserPanelTiming == NewUserPanelTiming.OnRegistration ||
                        newUserPanelTiming == NewUserPanelTiming.OnBoth)
                    {
                        ShowNewUserPanel();
                    }

                    SendVerificationEmail(newUser);
                }
                else
                {
                    statusText.text = "Registered, but failed to set display name.";
                }
            });
        });
    }

    private void SendVerificationEmail(FirebaseUser user)
    {
        user.SendEmailVerificationAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                statusText.text = "Verification email canceled!";
                return;
            }

            if (task.IsFaulted)
            {
                statusText.text = "Failed to send verification email: " + task.Exception?.Message;
                return;
            }

            ShowVerificationMessage(user.Email);
        });
    }

    private void ShowVerificationMessage(string email)
    {
        if (verificationMessagePanel != null)
        {
            verificationMessagePanel.SetActive(true);

            if (verificationMessageText != null)
            {
                verificationMessageText.text = $"An email has been sent to {email}\nPlease verify your email.";
            }
        }
    }

    public void CloseVerificationPanel()
    {
        if (verificationMessagePanel != null)
            verificationMessagePanel.SetActive(false);
    }

    public void LoginUser()
    {
        if (!isFirebaseReady || auth == null)
        {
            statusText.text = "Authentication not ready. Please wait...";
            return;
        }

        string email = emailInputField.text;
        string password = passwordInputField.text;

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                statusText.text = "Login canceled!";
                return;
            }

            if (task.IsFaulted)
            {
                statusText.text = "Login failed: " + task.Exception?.Message;
                return;
            }

            FirebaseUser user = task.Result.User;

            if (!user.IsEmailVerified)
            {
                statusText.text = "Please verify your email before logging in.";
                ShowVerificationMessage(user.Email);
                return;
            }

            statusText.text = $"Logged in as {user.DisplayName}";
            UpdateUserUI(user);
            SwitchToUserPanel();

            // Check if this is their first verified login
            CheckAndShowNewUserPanel(user);
        });
    }

    public void ResendVerificationEmail()
    {
        if (!isFirebaseReady || auth == null || auth.CurrentUser == null)
        {
            statusText.text = "No user logged in to resend email.";
            return;
        }

        SendVerificationEmail(auth.CurrentUser);
        statusText.text = "Verification email resent!";
    }

    public void CheckVerificationStatus()
    {
        if (!isFirebaseReady || auth == null || auth.CurrentUser == null)
            return;

        auth.CurrentUser.ReloadAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                if (auth.CurrentUser.IsEmailVerified)
                {
                    statusText.text = "Email verified! Logging in...";
                    CloseVerificationPanel();
                    UpdateUserUI(auth.CurrentUser);
                    SwitchToUserPanel();

                    // Check if this is their first verified login
                    CheckAndShowNewUserPanel(auth.CurrentUser);
                }
                else
                {
                    statusText.text = "Email not yet verified. Please check your inbox.";
                }
            }
        });
    }

    public void GuestSignIn()
    {
        if (!isFirebaseReady || auth == null)
        {
            statusText.text = "Authentication not ready. Please wait...";
            return;
        }

        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                statusText.text = "Guest sign-in canceled!";
                return;
            }

            if (task.IsFaulted)
            {
                statusText.text = "Guest sign-in failed: " + task.Exception?.Message;
                return;
            }

            FirebaseUser user = task.Result.User;
            statusText.text = "Guest signed in: " + user.UserId;

            // Close the guest sign-in panel on success
            CloseGuestSignInPanel();

            UpdateUserUI(user);
            SwitchToUserPanel();

            // Check if this is their first time as guest and show new user panel
            CheckAndShowNewUserPanel(user);
        });
    }

    public void LogoutUser()
    {
        if (!isFirebaseReady || auth == null)
            return;

        auth.SignOut();

        // Clear input fields
        if (emailInputField != null) emailInputField.text = "";
        if (passwordInputField != null) passwordInputField.text = "";
        if (nameInputField != null) nameInputField.text = "";
        if (confirmPasswordInputField != null) confirmPasswordInputField.text = "";

        statusText.text = "Logged out successfully.";

        if (loginPanel != null) loginPanel.SetActive(true);
        if (userPanel != null) userPanel.SetActive(false);
        if (verificationMessagePanel != null) verificationMessagePanel.SetActive(false);
        if (guestSignInPanel != null) guestSignInPanel.SetActive(false);
        if (newUserPanel != null) newUserPanel.SetActive(false);

        // Re-enable buttons after logout
        ForceEnableButtons();
    }

    private void UpdateUserUI(FirebaseUser user)
    {
        if (displayNameText != null)
            displayNameText.text = string.IsNullOrEmpty(user.DisplayName) ? "Guest" : user.DisplayName;

        if (userIdText != null)
            userIdText.text = user.UserId;
    }

    private void SwitchToUserPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (userPanel != null) userPanel.SetActive(true);
        if (verificationMessagePanel != null) verificationMessagePanel.SetActive(false);
        if (guestSignInPanel != null) guestSignInPanel.SetActive(false);
        // Don't auto-close new user panel here, let the user close it
    }

    // Optional: Reset first login status (for testing purposes)
    public void ResetFirstLoginStatus()
    {
        if (auth != null && auth.CurrentUser != null)
        {
            string userKey = FIRST_LOGIN_KEY + auth.CurrentUser.UserId;
            PlayerPrefs.DeleteKey(userKey);
            PlayerPrefs.Save();
            Debug.Log("First login status reset for current user.");
        }
    }
}