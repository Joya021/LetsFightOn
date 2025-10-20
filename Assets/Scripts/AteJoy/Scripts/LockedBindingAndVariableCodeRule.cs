using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(menuName = "Validation/LockedBindingAndVariableCodeRule")]
public class LockedBindingAndVariableCodeRule : ValidationRule
{
    [Tooltip("Allow numeric values in assignments.")]
    public bool allowNumbers = true;

    [Tooltip("Allow string values in assignments.")]
    public bool allowStrings = true;

    [System.NonSerialized]
    public List<string> errorMessages = new List<string>();

    private readonly HashSet<string> pythonKeywords = new HashSet<string>
    {
        "False","None","True","and","as","assert","async","await","break","class",
        "continue","def","del","elif","else","except","finally","for","from","global",
        "if","import","in","is","lambda","nonlocal","not","or","pass","raise","return",
        "try","while","with","yield"
    };

    public override bool Validate(string editableInput)
    {
        errorMessages.Clear();

        if (string.IsNullOrEmpty(editableInput))
        {
            errorMessages.Add("» You didn’t modify any editable section. Try adding a variable or parameter.");
            return false;
        }

        // Keep both versions
        string rawInput = editableInput;            // preserves indentation
        string trimmedInput = editableInput.Trim(); // used for syntax checking

        Debug.Log($"🔍 Raw editable input (with spaces): [{rawInput}]");
        Debug.Log($"🔍 Trimmed editable input: [{trimmedInput}]");

        // 🧠 Determine where the player edited
        string context = DetectSectionContext(rawInput);
        Debug.Log($"📍 Detected section: {context}");

        // Validate based on section type
        switch (context)
        {
            case "before":
                ValidateVariableDefinition(trimmedInput, "Before Function");
                break;

            case "inside":
                ValidateParameter(trimmedInput);
                break;

            case "between":
                ValidateVariableDefinition(trimmedInput, "Function Body");
                break;

            default:
                errorMessages.Add("» Could not detect where you edited. Try defining a variable or adding a parameter.");
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
        // - If it's a single name like "data", assume it's inside parentheses
        // - If it contains '=', it's either before function or between lines
        // - If it contains 'return', it's probably not editable (should’ve been locked)
        // We'll use heuristics to best-guess player intent

        if (input.Contains("="))
        {
            // Check indentation: indented line → inside body
            if (input.StartsWith(" ") || input.StartsWith("\t") || input.StartsWith("  ") || input.StartsWith("   ") || input.StartsWith("    ") || input.StartsWith("\n\n"))
                return "between";
            else
                return "before";
        }

        if (Regex.IsMatch(input, @"^[a-zA-Z0-9_][a-zA-Z0-9_]*$") || input.StartsWith("\n"))
            return "inside"; // single variable, likely parameter

        return "unknown";
    }

    // ---------------------------------------------------------
    // 🧩 Validate variable definition like data = "hello"
    // ---------------------------------------------------------
    private void ValidateVariableDefinition(string input, string context)
    {
        var assignMatch = Regex.Match(input, @"^(\w+)\s*=\s*(.+)$");

        if (!assignMatch.Success)
        {
            errorMessages.Add($"» ({context}) Expected syntax like: data = \"hello\" or data = 10");
            return;
        }

        string varName = assignMatch.Groups[1].Value;
        string value = assignMatch.Groups[2].Value.Trim();

        ValidatePythonVariableName(varName, context);

        bool isString = Regex.IsMatch(value, @"^(['""])(.*?)\1$");
        bool isNumber = Regex.IsMatch(value, @"^-?\d+(\.\d+)?$");

        if (isString)
        {
            if (!value.EndsWith("\"") && !value.EndsWith("'"))
                errorMessages.Add($"» ({context}) Strings must end with a closing quote mark.");
        }
        else if (isNumber)
        {
            if (!allowNumbers)
                errorMessages.Add($"» ({context}) Numbers are not allowed here.");
        }
        else
        {
            if (allowStrings)
                errorMessages.Add($"» ({context}) Invalid value. Use a string in quotes or a number.");
            else
                errorMessages.Add($"» ({context}) Invalid value. Expected a number.");
        }

        if (!isString && Regex.IsMatch(value, @"[!@#$%^&*{}[\];,<>?/\\|`~]"))
            errorMessages.Add($"» ({context}) Special characters are only allowed inside strings (use quotes).");
    }

    // ---------------------------------------------------------
    // 🧩 Validate Python variable name syntax
    // ---------------------------------------------------------
    private void ValidatePythonVariableName(string varName, string context)
    {
        if (string.IsNullOrEmpty(varName))
        {
            errorMessages.Add($"» ({context}) Variable name cannot be empty.");
            return;
        }

        if (pythonKeywords.Contains(varName))
        {
            errorMessages.Add($"» ({context}) '{varName}' is a reserved Python keyword and cannot be used.");
            return;
        }

        if (Regex.IsMatch(varName, @"^\d"))
        {
            errorMessages.Add($"» ({context}) Variable names cannot start with a number.");
            return;
        }

        if (!Regex.IsMatch(varName, @"^[A-Za-z_][A-Za-z0-9_]*$"))
        {
            errorMessages.Add($"» ({context}) Invalid variable name '{varName}'. Use only letters, digits, and underscores.");
        }
    }

    // ---------------------------------------------------------
    // 🧩 Validate parameter (inside parentheses)
    // ---------------------------------------------------------
    private void ValidateParameter(string param)
    {
        string context = "Inside Parentheses";

        if (param.Contains(","))
            errorMessages.Add("» Only one parameter is allowed inside parentheses.");

        if (param.Contains("\"") || param.Contains("'"))
            errorMessages.Add("» Do not use quotes inside parentheses. Use a plain variable name.");

        ValidatePythonVariableName(param, context);
    }
}

