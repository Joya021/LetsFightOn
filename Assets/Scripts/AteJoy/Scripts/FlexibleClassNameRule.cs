using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(menuName = "Validation/FlexibleClassNameRule")]
public class FlexibleClassNameRule : ValidationRule
{
    [Tooltip("Expected semantic meaning, e.g. 'class name'")]
    public string expectedType = "class name";

    [Tooltip("Optional list of acceptable answers (e.g. 'MyClass', 'PlayerStats')")]
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

        string cleanedInput = editableInput;

        // Strip valid suffix
        if (editableInput.EndsWith("():")) cleanedInput = editableInput.Substring(0, editableInput.Length - 4);
        else if (editableInput.EndsWith(":")) cleanedInput = editableInput.Substring(0, editableInput.Length - 1);

        if (string.IsNullOrEmpty(cleanedInput))
            errorMessages.Add("» Class name cannot be empty.");

        // Cannot start with a digit
        if (Regex.IsMatch(cleanedInput, @"^\d"))
            errorMessages.Add("» Class names cannot start with a digit.");

        // Special characters (excluding underscore)
        if (Regex.IsMatch(cleanedInput, @"[^a-zA-Z0-9_ ():]"))
            errorMessages.Add("» Special characters are not allowed; use letters or underscores only.");

        // Reserved keyword
        if (reservedKeywords.Contains(cleanedInput))
            errorMessages.Add("» Reserved keywords cannot be used as class names.");

        // Spaces
        if (cleanedInput.Contains(" "))
            errorMessages.Add("» Class names cannot contain spaces.");

        // PascalCase check
        if(!Regex.IsMatch(cleanedInput, @"^([A-Z][a-z0-9]+)(_[A-Z][a-z0-9]+)*$"))
            errorMessages.Add("» Try Again! Class names should use PascalCase (e.g. MyClass or My_Class).");

        // check against accepted answers
        if (acceptedAnswers != null && acceptedAnswers.Count > 0)
        {
            bool matchFound = false;
            foreach (string answer in acceptedAnswers)
            {
                if (string.Equals(cleanedInput, answer, System.StringComparison.Ordinal))
                {
                    matchFound = true;
                    break;
                }
            }
        }

        // Validate suffix only if core name passed
        if (errorMessages.Count == 0)
        {
            bool endsWithValidSuffix =
                editableInput.EndsWith("():") ||
                editableInput.EndsWith(":");

            if ((editableInput.Contains("(") || editableInput.Contains(")") || editableInput.Contains(":")) && !endsWithValidSuffix)
            {
                errorMessages.Add("» Try Again! Only (): or : are allowed at the end of a class name.");
            }
        }


        return errorMessages.Count == 0;
    }
}