using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(menuName = "Validation/NamingConventionRule")]
public class NamingConventionRule : ValidationRule
{

    static readonly Regex AssignRegex = new Regex(@"\b([A-Za-z_]\w*)\s*=", RegexOptions.Compiled);

    public override bool Validate(string code)
    {
        var matches = AssignRegex.Matches(code);
        foreach (Match m in matches)
        {
            string varName = m.Groups[1].Value;
            if (!Regex.IsMatch(varName, @"^[a-z_][a-z0-9_]*$"))
                return false;
        }
        return true;
    }
}