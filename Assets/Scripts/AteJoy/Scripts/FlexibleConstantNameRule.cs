using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(menuName = "Validation/FlexibleConstantNameRule")]
public class FlexibleConstantNameRule : ValidationRule
{
    [Tooltip("Expected semantic meaning, e.g. 'constant name'")]
    public string expectedType = "constant name";

    [Tooltip("Optional list of acceptable answers (e.g. 'MAX_SPEED', 'PLAYER_LIMIT')")]
    public List<string> acceptedAnswers;

    [HideInInspector] public List<string> editableSnippets;

    [System.NonSerialized]
    public List<string> errorMessages = new List<string>();

    private readonly HashSet<string> reservedKeywords = new HashSet<string>
    {
        "False", "None", "True", "and", "as", "assert", "async", "await", "break", "class",
        "continue", "def", "del", "elif", "else", "except", "finally", "for", "from", "global",
        "if", "import", "in", "is", "lambda", "nonlocal", "not", "or", "pass", "raise", "return",
        "try", "while", "with", "yield"
    };

    public override bool Validate(string playerInput)
    {
        errorMessages.Clear();
        string editableInput = playerInput.Trim();
        Debug.Log("🔍 Rule received editable input: " + editableInput);

        if (string.IsNullOrEmpty(editableInput))
            errorMessages.Add("» Constant name cannot be empty.");

        // 1. Cannot start with a digit
        if (Regex.IsMatch(editableInput, @"^\d"))
            errorMessages.Add("» Constant names cannot start with a digit.");

        // 2. Special characters (excluding underscore)
        if (Regex.IsMatch(editableInput, @"[^A-Z0-9_ ]"))
            errorMessages.Add("» Special characters are not allowed; use uppercase letters and underscores only.");

        // 3. Reserved keyword
        if (reservedKeywords.Contains(editableInput.ToLower()))
            errorMessages.Add("» Reserved keywords cannot be used as constant names.");

        // 4. Spaces
        if (editableInput.Contains(" "))
            errorMessages.Add("» Constant names cannot contain spaces.");

        // 5. Lowercase letters
        if (Regex.IsMatch(editableInput, @"[a-z]"))
            errorMessages.Add("» Try Again! Constants should use uppercase letters only (e.g. MAX_SPEED).");

        // Optional: check against accepted answers
        if (acceptedAnswers != null && acceptedAnswers.Count > 0)
        {
            bool matchFound = false;
            foreach (string answer in acceptedAnswers)
            {
                if (string.Equals(editableInput, answer, System.StringComparison.Ordinal))
                {
                    matchFound = true;
                    break;
                }
            }
        }

        return errorMessages.Count == 0;
    }
}