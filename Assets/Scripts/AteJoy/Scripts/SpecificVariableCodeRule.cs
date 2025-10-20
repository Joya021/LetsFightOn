using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "Validation/SpecificVariableCodeRule")]
public class SpecificVariableCodeRule : ValidationRule
{
    [Tooltip("Specify the required variable name")]
    public string targetVariableName;

    [Tooltip("Provide the raw code template (with <LOCK> tags). This helps detect code position.")]
    [TextArea(4, 10)]
    public string rawTemplate;

    [System.NonSerialized]
    public List<string> errorMessages = new List<string>();

    private readonly HashSet<string> pythonKeywords = new HashSet<string>
    {
        "False","None","True","and","as","assert","async","await","break","class",
        "continue","def","del","elif","else","except","finally","for","from","global",
        "if","import","in","is","lambda","nonlocal","not","or","pass","raise","return",
        "try","while","with","yield"
    };

    public override bool Validate(string playerInput)
    {
        errorMessages.Clear();

        if (string.IsNullOrEmpty(rawTemplate))
        {
            errorMessages.Add("» Internal error: raw template is missing. Please assign it in the ScriptableObject.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(playerInput))
        {
            errorMessages.Add("» You didn’t enter anything. Try defining the variable 'total'.");
            return false;
        }

        string rawInput = playerInput;
        string input = playerInput.Trim();

        // 🧠 Determine where the player edited
        string context = DetectSectionContext(rawInput);
        Debug.Log($"📍 Detected section: {context}");

        // Validate based on section type
        switch (context)
        {
            case "before":
                Match match = Regex.Match(input, @"^([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.+)$");

                if (!match.Success)
                {
                    errorMessages.Add("» Expected a valid Python assignment, e.g. y = 10 or y = \"text\".");
                    return false;
                }

                string varName = match.Groups[1].Value;
                string value = match.Groups[2].Value.Trim();

                // --- 3️⃣ Variable name must be 'y' ---
                if (!string.Equals(varName, targetVariableName))
                {
                    errorMessages.Add($"» The variable name must be '{targetVariableName}', not '{varName}'.");
                }

                // --- 4️⃣ Validate variable name (Python rules) ---
                ValidatePythonVariableName(varName);

                // --- 5️⃣ Validate the value ---
                bool isString = Regex.IsMatch(value, @"^(['""])(.*?)\1$");
                bool isNumber = Regex.IsMatch(value, @"^-?\d+(\.\d+)?$");

                if (!isString && !isNumber)
                {
                    if (value.Contains("\"") || value.Contains("'"))
                        errorMessages.Add("» The string must start and end with quotes. Example: y = \"hello\"");
                    else
                        errorMessages.Add("» The value must be a number or a quoted string. Example: y = 10 or y = \"text\".");
                }

                // --- 6️⃣ Detect illegal characters outside quotes ---
                if (!isString && Regex.IsMatch(value, @"[!@#$%^&*{}\\[\\];,<>?/\\\\|`~]"))
                {
                    errorMessages.Add("» Special characters are not allowed outside quotes.");
                }

                return errorMessages.Count == 0;
                break;

            case "after":
                errorMessages.Add("» You cannot place your answer after the calculation. Define your variable before it.");
                break;

            default:
                errorMessages.Add("» Could not detect where you edited. Try defining a variable.");
                break;
        }

        return errorMessages.Count == 0;
    }

    // ---------------------------------------------------------
    // 🧩 Detect where player typed their code
    // ---------------------------------------------------------
    private string DetectSectionContext(string input)
    {
        // Simplified heuristics:
        // - If it contains '=', it's either before or after locked section

        if (input.Contains("="))
        {
            // Check indentation: indented line / new line → after
            if (input.StartsWith(" ") || input.StartsWith("\t") || input.StartsWith("  ") || input.StartsWith("   ") || input.StartsWith("    ") || input.StartsWith("\n\n"))
                return "after";
            else
                return "before";
        }

        return "unknown";
    }

    private void ValidatePythonVariableName(string varName)
    {
        if (string.IsNullOrEmpty(varName))
        {
            errorMessages.Add("» Variable name cannot be empty.");
            return;
        }

        if (pythonKeywords.Contains(varName))
        {
            errorMessages.Add($"» '{varName}' is a reserved Python keyword and cannot be used.");
            return;
        }

        if (Regex.IsMatch(varName, @"^\d"))
        {
            errorMessages.Add("» Variable names cannot start with a number.");
            return;
        }

        if (!Regex.IsMatch(varName, @"^[A-Za-z_][A-Za-z0-9_]*$"))
        {
            errorMessages.Add($"» Invalid variable name '{varName}'. Use only letters, digits, and underscores.");
        }
    }
}
