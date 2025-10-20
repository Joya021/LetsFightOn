using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Windows;

[CreateAssetMenu(menuName = "Validation/FlexibleMethodNameRule")]
public class FlexibleMethodNameRule : ValidationRule
{
    [Tooltip("Expected semantic meaning, e.g. 'method name'")]
    public string expectedType = "method name";

    [Tooltip("Optional list of acceptable answers (e.g. 'calculate_score', 'update_position')")]
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

        if (string.IsNullOrEmpty(cleanedInput))
            errorMessages.Add("» Method name cannot be empty.");

        // Reserved keyword
        if (reservedKeywords.Contains(cleanedInput))
            errorMessages.Add("» Reserved keywords cannot be used as method names.");

        if (!Regex.IsMatch(cleanedInput, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            errorMessages.Add($"» Invalid {expectedType} name.");

        // Cannot start with a digit
        if (Regex.IsMatch(cleanedInput, @"^\d"))
            errorMessages.Add("» Method names cannot start with a digit.");

        // Special characters (excluding underscore)
        if (Regex.IsMatch(cleanedInput, @"[^a-zA-Z0-9_ ]"))
        {
            if (Regex.IsMatch(cleanedInput, @"^[^():]"))
            {
                errorMessages.Add($"» Parenthesis and colon are only allowed at the end of a function name. It is needed for the function itself to work properly.");
            }
            else
            {
                errorMessages.Add("» Special characters are not allowed; use underscores instead.");
            }
        }

        // Spaces
        if (cleanedInput.Contains(" "))
            errorMessages.Add("» Method names cannot contain spaces.");

        // Uppercase letters (snake_case discourages them)
        if (Regex.IsMatch(cleanedInput, @"[A-Z]"))
            errorMessages.Add("» Try Again! Though it is allowed, it is recommended to use snake_case when naming methods.");

        // If accepted answers are defined, check for match
        if (acceptedAnswers != null && acceptedAnswers.Count > 0)
        {
            bool matchFound = false;
            foreach (string answer in acceptedAnswers)
            {
                if (string.Equals(cleanedInput, answer, System.StringComparison.OrdinalIgnoreCase))
                {
                    matchFound = true;
                    break;
                }
            }
        }

        // Validate suffix only if core name passed

        if (errorMessages.Count >= 0)
        {
            bool endsWithValidSuffix =
                editableInput.EndsWith("():");

            if (!endsWithValidSuffix)
            {
                bool hasOpen = editableInput.Contains("(");
                bool hasClose = editableInput.Contains(")");
                bool hasColon = editableInput.Contains(":");

                if (!hasOpen && !hasClose && !hasColon)
                {
                    errorMessages.Add("» Missing valid ending. Add (), ():, or : at the end.");
                }
                else
                {
                    if (hasOpen || hasClose || hasColon || !endsWithValidSuffix)
                    {
                        errorMessages.Add("» Invalid ending. Only (): is allowed at the end of a function.");

                        if (hasOpen && !hasClose && hasColon)
                            errorMessages.Add("» Missing closing parenthesis ')'.");
                        if (hasOpen && !hasClose && !hasColon)
                            errorMessages.Add("» Missing closing parenthesis ')' and colon ':'.");
                        if (!hasOpen && hasClose && !hasColon)
                            errorMessages.Add("» Missing opening parenthesis '(' and colon ':'.");
                        if (!hasOpen && hasClose && hasColon)
                            errorMessages.Add("» Missing opening parenthesis '('.");
                        if (!hasClose && !hasOpen && hasColon)
                            errorMessages.Add("» Missing opening and closing parenthesis '(' and ')'");
                        if (hasOpen && hasClose && !hasColon)
                            errorMessages.Add("» Missing colon ':' after parentheses.");
                    }
                }
            }
        }

        return errorMessages.Count == 0;
    }

}