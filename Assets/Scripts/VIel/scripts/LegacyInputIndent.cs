using UnityEngine;
using UnityEngine.UI;

public class LegacyInputIndent : MonoBehaviour
{
    [Header("UI")]
    public InputField codeInputField;

    [Header("Indent Settings")]
    public string indent = "   "; // 4 spaces

    void Update()
    {
        if (codeInputField != null && codeInputField.isFocused)
        {
            // Detect Enter key
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                InsertIndentAtCaret();
            }
        }
    }

    void InsertIndentAtCaret()
    {
        int caretPos = codeInputField.caretPosition;

        // Get the current text before and after the caret
        string before = codeInputField.text.Substring(0, caretPos);
        string after = codeInputField.text.Substring(caretPos);

        // Build new text with newline and indent
        string newText = before + " " + indent + after;

        // Set the new text
        codeInputField.text = newText;

        // Move caret to after the indent
        codeInputField.caretPosition = caretPos + 1 + indent.Length;
        codeInputField.selectionAnchorPosition = codeInputField.caretPosition;
        codeInputField.selectionFocusPosition = codeInputField.caretPosition;

        // Re-focus the input field so player can continue typing
        codeInputField.ActivateInputField();
    }
}
