using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(menuName = "Validation/FlexibleVariableNameRule")]
public class FlexibleVariableNameRule : ValidationRule
{
    [Tooltip("Expected semantic meaning, e.g. 'function name' or 'variable name'")]
    public string expectedType;

    [Tooltip("Optional list of acceptable answers (e.g. 'my_function', 'myFunction')")]
    public List<string> acceptedAnswers;

    [HideInInspector] public List<string> editableSnippets;

    [System.NonSerialized]
    public List<string> errorMessages = new List<string>();

    // Python reserved keywords
    private readonly HashSet<string> reservedKeywords = new HashSet<string>
    {
        "False", "None", "True", "and", "as", "assert", "async", "await", "break", "class",
        "continue", "def", "del", "elif", "else", "except", "finally", "for", "from", "global",
        "if", "import", "in", "is", "lambda", "nonlocal", "not", "or", "pass", "raise", "return",
        "try", "while", "with", "yield"
    };

    public override bool Validate(string playerInput)
    {
        errorMessages.Clear(); // Reset

        string editableInput = playerInput.Trim();
        Debug.Log("🔍 Rule received editable input: " + editableInput);

        if (string.IsNullOrEmpty(editableInput))
            errorMessages.Add("» Variable name cannot be empty.");

        if (!Regex.IsMatch(editableInput, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            errorMessages.Add($"» Invalid {expectedType} name.");

        if (Regex.IsMatch(editableInput, @"^\d"))
            errorMessages.Add("» Variable names cannot start with a digit.");

        if (Regex.IsMatch(editableInput, @"[^a-zA-Z0-9_ ]"))
            errorMessages.Add("» Special characters are not allowed; use underscores instead.");

        if (editableInput.Contains(" "))
            errorMessages.Add("» Variable names cannot contain spaces.");

        if (reservedKeywords.Contains(editableInput))
            errorMessages.Add("» Reserved keywords cannot be used as variable names.");

        if (Regex.IsMatch(editableInput, @"[A-Z]"))
            errorMessages.Add("» Try Again! Though it is allowed to use uppercase letters, it is recommended to use snake_case when naming variables.");

        // If accepted answers are defined, check for match
        if (acceptedAnswers != null && acceptedAnswers.Count > 0)
        {
            bool matchFound = false;
            foreach (string answer in acceptedAnswers)
            {
                if (string.Equals(editableInput, answer, System.StringComparison.OrdinalIgnoreCase))
                {
                    matchFound = true;
                    break;
                }
            }
        }

        return errorMessages.Count == 0;
    }
}