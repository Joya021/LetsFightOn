using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.UI;

public class UserRegistrationLogin : MonoBehaviour
{
    [Header("Input Fields")]
    public InputField nameInputField;
    public InputField emailInputField;
    public InputField passwordInputField;
    public InputField confirmPasswordInputField;

    [Header("UI Texts")]
    public Text statusText;
    public Text displayNameText;   // Shown in the next panel
    public Text userIdText;        // Shown in the next panel
    public Text verificationMessageText; // Text in verification message panel

    [Header("Panels")]
    public GameObject loginPanel;   // The panel with login/register inputs
    public GameObject userPanel;    // The panel shown after login
    public GameObject verificationMessagePanel; // Panel for email verification message

    [Header("Password Toggle Buttons")]
    public Button passwordToggleButton;
    public Button confirmPasswordToggleButton;

    private FirebaseAuth auth;
    private bool isPasswordVisible = false;
    private bool isConfirmPasswordVisible = false;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        // Set initial password fields to censored
        passwordInputField.contentType = InputField.ContentType.Password;
        confirmPasswordInputField.contentType = InputField.ContentType.Password;
        passwordInputField.ForceLabelUpdate();
        confirmPasswordInputField.ForceLabelUpdate();

        // Hide verification panel at start
        if (verificationMessagePanel != null)
            verificationMessagePanel.SetActive(false);

        // Auto-login if user is already logged in
        CheckAutoLogin();
    }

    // ============================
    // AUTO LOGIN
    // ============================
    private void CheckAutoLogin()
    {
        if (auth.CurrentUser != null)
        {
            FirebaseUser user = auth.CurrentUser;

            // Check if email is verified (skip for anonymous users)
            if (user.IsAnonymous || user.IsEmailVerified)
            {
                statusText.text = "Auto-login successful!";
                UpdateUserUI(user);
                SwitchToUserPanel();
            }
            else
            {
                // User is logged in but email not verified
                statusText.text = "Please verify your email to continue.";
                ShowVerificationMessage(user.Email);
            }
        }
    }

    // ============================
    // PASSWORD TOGGLE FUNCTIONS
    // ============================
    public void TogglePasswordVisibility()
    {
        isPasswordVisible = !isPasswordVisible;

        if (isPasswordVisible)
        {
            passwordInputField.contentType = InputField.ContentType.Standard;
        }
        else
        {
            passwordInputField.contentType = InputField.ContentType.Password;
        }

        passwordInputField.ForceLabelUpdate();
    }

    public void ToggleConfirmPasswordVisibility()
    {
        isConfirmPasswordVisible = !isConfirmPasswordVisible;

        if (isConfirmPasswordVisible)
        {
            confirmPasswordInputField.contentType = InputField.ContentType.Standard;
        }
        else
        {
            confirmPasswordInputField.contentType = InputField.ContentType.Password;
        }

        confirmPasswordInputField.ForceLabelUpdate();
    }

    // ============================
    // REGISTER USER
    // ============================
    public void RegisterUser()
    {
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

            // Set display name
            UserProfile profile = new UserProfile { DisplayName = name };
            newUser.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(updateTask =>
            {
                if (updateTask.IsCompleted)
                {
                    statusText.text = $"Registered as {newUser.DisplayName}";

                    // Send verification email
                    SendVerificationEmail(newUser);
                }
                else
                {
                    statusText.text = "Registered, but failed to set display name.";
                }
            });
        });
    }

    // ============================
    // SEND VERIFICATION EMAIL
    // ============================
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

            // Show verification message panel
            ShowVerificationMessage(user.Email);
        });
    }

    // ============================
    // SHOW VERIFICATION MESSAGE
    // ============================
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

    // ============================
    // CLOSE VERIFICATION PANEL
    // ============================
    public void CloseVerificationPanel()
    {
        if (verificationMessagePanel != null)
            verificationMessagePanel.SetActive(false);
    }

    // ============================
    // LOGIN USER
    // ============================
    public void LoginUser()
    {
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

            // Check if email is verified
            if (!user.IsEmailVerified)
            {
                statusText.text = "Please verify your email before logging in.";
                ShowVerificationMessage(user.Email);
                return;
            }

            // Email is verified, proceed with login
            statusText.text = $"Logged in as {user.DisplayName}";
            UpdateUserUI(user);
            SwitchToUserPanel();
        });
    }

    // ============================
    // RESEND VERIFICATION EMAIL
    // ============================
    public void ResendVerificationEmail()
    {
        if (auth.CurrentUser != null)
        {
            SendVerificationEmail(auth.CurrentUser);
            statusText.text = "Verification email resent!";
        }
        else
        {
            statusText.text = "No user logged in to resend email.";
        }
    }

    // ============================
    // CHECK VERIFICATION STATUS
    // ============================
    public void CheckVerificationStatus()
    {
        if (auth.CurrentUser != null)
        {
            // Reload user data to get updated verification status
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
                    }
                    else
                    {
                        statusText.text = "Email not yet verified. Please check your inbox.";
                    }
                }
            });
        }
    }

    // ============================
    // GUEST SIGN-IN
    // ============================
    public void GuestSignIn()
    {
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
            UpdateUserUI(user);
            SwitchToUserPanel();
        });
    }

    // ============================
    // LOGOUT USER
    // ============================
    public void LogoutUser()
    {
        auth.SignOut();
        statusText.text = "Logged out successfully.";

        if (loginPanel != null) loginPanel.SetActive(true);
        if (userPanel != null) userPanel.SetActive(false);
        if (verificationMessagePanel != null) verificationMessagePanel.SetActive(false);
    }

    // ============================
    // UPDATE UI TEXTS
    // ============================
    private void UpdateUserUI(FirebaseUser user)
    {
        if (displayNameText != null)
            displayNameText.text = string.IsNullOrEmpty(user.DisplayName) ? "Guest" : user.DisplayName;

        if (userIdText != null)
            userIdText.text = user.UserId;
    }

    // ============================
    // PANEL SWITCH
    // ============================
    private void SwitchToUserPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (userPanel != null) userPanel.SetActive(true);
        if (verificationMessagePanel != null) verificationMessagePanel.SetActive(false);
    }
}