using UnityEngine;
using UnityEngine.UI;


public class PasswordToggle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public InputField passwordInputField;
    [SerializeField] public Button toggleButton;

    [Header("Optional: Button Icon/Text")]
    [SerializeField] private Image buttonIcon;
    [SerializeField] private Sprite showIcon;
    [SerializeField] private Sprite hideIcon;

    private bool isPasswordVisible = false;

    void Start()
    {
        // Make sure the input field starts as password
        if (passwordInputField != null)
        {
            passwordInputField.contentType = InputField.ContentType.Password;
            passwordInputField.ForceLabelUpdate();
        }

        // Add listener to button
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(TogglePasswordVisibility);
        }

        UpdateButtonIcon();
    }

    public void TogglePasswordVisibility()
    {
        if (passwordInputField == null) return;

        isPasswordVisible = !isPasswordVisible;

        if (isPasswordVisible)
        {
            // Show password
            passwordInputField.contentType = InputField.ContentType.Standard;
        }
        else
        {
            // Hide password
            passwordInputField.contentType = InputField.ContentType.Password;
        }

        // Force the input field to update
        passwordInputField.ForceLabelUpdate();

        UpdateButtonIcon();
    }

    void UpdateButtonIcon()
    {
        // Optional: Update button icon if you have one
        if (buttonIcon != null && showIcon != null && hideIcon != null)
        {
            buttonIcon.sprite = isPasswordVisible ? hideIcon : showIcon;
        }
    }
}