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

    [Header("Panels")]
    public GameObject loginPanel;   // The panel with login/register inputs
    public GameObject userPanel;    // The panel shown after login

    private FirebaseAuth auth;

    void Update()
    {
        auth = FirebaseAuth.DefaultInstance;

        // Make password fields censored (***)
        passwordInputField.contentType = InputField.ContentType.Password;
        confirmPasswordInputField.contentType = InputField.ContentType.Password;

        passwordInputField.ForceLabelUpdate();
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
                    UpdateUserUI(newUser);
                    SwitchToUserPanel();
                }
                else
                {
                    statusText.text = "Registered, but failed to set display name.";
                }
            });
        });
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
            statusText.text = $"Logged in as {user.DisplayName}";
            UpdateUserUI(user);
            SwitchToUserPanel();
        });
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
    }
}
