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
        OnRegistration,
        OnFirstVerifiedLogin,
        OnBoth
    }

    private FirebaseAuth auth;
    private bool isPasswordVisible = false;
    private bool isConfirmPasswordVisible = false;
    private bool isFirebaseReady = false;
    private const string FIRST_LOGIN_KEY = "HasCompletedFirstLogin_";

    void Start()
    {
       
        StartCoroutine(InitializeAfterFrame());
    }

    IEnumerator InitializeAfterFrame()
    {
       
        yield return null;

        ForceEnableButtons();

       
        StartCoroutine(WaitForFirebase());
    }

    void OnEnable()
    {
        ForceEnableButtons();

        if (auth == null && FirebaseInitializer.Instance != null && FirebaseInitializer.Instance.IsFirebaseReady())
        {
            StartCoroutine(ReinitializeAuth());
        }
    }

    void ForceEnableButtons()
    {
        UnityEngine.UI.Button[] buttons = GetComponentsInChildren<UnityEngine.UI.Button>(true);
        foreach (var btn in buttons)
        {
            btn.interactable = true;
        }
    }

    IEnumerator ReinitializeAuth()
    {
        yield return new WaitForSeconds(0.1f);

        try
        {
            auth = FirebaseAuth.DefaultInstance;
            isFirebaseReady = true;

            if (statusText != null)
                statusText.text = "";

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

        if (statusText != null)
            statusText.text = "Initializing...";

        while (FirebaseInitializer.Instance == null || !FirebaseInitializer.Instance.IsFirebaseReady())
        {
            yield return new WaitForSeconds(0.1f);
        }

        try
        {
            auth = FirebaseAuth.DefaultInstance;
            isFirebaseReady = true;

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

           
            if (statusText != null)
                statusText.text = "";

            if (auth.CurrentUser != null)
            {
              
                CheckAutoLogin();
            }
            else
            {
           
                if (loginPanel != null)
                    loginPanel.SetActive(true);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to initialize Firebase Auth: {ex.Message}");
            if (statusText != null)
                statusText.text = "Failed to initialize authentication.";

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

    private void CheckAndShowNewUserPanel(FirebaseUser user)
    {
        if (newUserPanelTiming == NewUserPanelTiming.OnRegistration)
            return; 

        string userKey = FIRST_LOGIN_KEY + user.UserId;
        bool hasCompletedFirstLogin = PlayerPrefs.GetInt(userKey, 0) == 1;

        if (!hasCompletedFirstLogin)
        {
            ShowNewUserPanel();
        }
    }

   
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
    public void ShowGuestSignInPanel()
    {
        if (guestSignInPanel != null)
            guestSignInPanel.SetActive(true);
    }

    public void CloseGuestSignInPanel()
    {
        if (guestSignInPanel != null)
            guestSignInPanel.SetActive(false);
    }

    public void ShowNewUserPanel()
    {
        if (newUserPanel != null)
            newUserPanel.SetActive(true);
    }

    public void CloseNewUserPanel()
    {
        if (newUserPanel != null)
            newUserPanel.SetActive(false);

    
        MarkFirstLoginComplete();
    }

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

            CloseGuestSignInPanel();

            UpdateUserUI(user);
            SwitchToUserPanel();

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
 
    }

 
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