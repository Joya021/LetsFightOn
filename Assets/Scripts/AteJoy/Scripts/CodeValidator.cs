using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class CodeValidator
{
    public static List<string> Validate(string playerCode, BrokenCode challenge, List<string> editableSnippets)
    {
        var errors = new List<string>();

        foreach (var rule in challenge.validationRules)
        {
            Debug.Log("🔍 Editable snippets passed to rule: " + string.Join(" | ", editableSnippets));

            // Pass editable snippets to EditableBlockRule
            if (rule is EditableBlockRule editableRule)
            {
                editableRule.editableSnippets = editableSnippets;
            }

            if (rule is FlexibleFunctionNameRule fnRule)
                fnRule.editableSnippets = editableSnippets;

            if (rule is FlexibleVariableNameRule varRule)
                varRule.editableSnippets = editableSnippets;

            if (rule is FlexibleConstantNameRule constRule)
                constRule.editableSnippets = editableSnippets;

            if (rule is FlexibleClassNameRule classRule)
                classRule.editableSnippets = editableSnippets;

            if (rule is FlexibleMethodNameRule methodRule)
                methodRule.editableSnippets = editableSnippets;

            if (!rule.Validate(playerCode))
                errors.Add(rule.errorMessage);
        }

        return errors;
    }
}
