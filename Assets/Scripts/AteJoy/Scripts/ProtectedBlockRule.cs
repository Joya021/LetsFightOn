using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Validation/ProtectedBlockRule")]
public class ProtectedBlockRule : ValidationRule
{
    public string originalTemplate;
    public List<string> protectedSnippets;

    [HideInInspector]
    public List<string> errorMessages = new();

    static readonly Regex InlineLock =
        new Regex(@"<LOCK>(.*?)</LOCK>", RegexOptions.Compiled);

    public override bool Validate(string code)
    {
        errorMessages.Clear();

        if (protectedSnippets == null || protectedSnippets.Count == 0)
            return true;

        if (string.IsNullOrEmpty(originalTemplate))
            return true;

        foreach (var snippet in protectedSnippets)
        {
            int originalCount = CountOccurrences(originalTemplate, snippet);
            int inputCount = CountOccurrences(code, snippet);

            if (inputCount < originalCount)
            {
                errorMessages.Add($"You removed protected code: '{snippet}'");
                return false;
            }
        }

        return true;
    }

    private int CountOccurrences(string source, string target)
    {
        return Regex.Matches(source, Regex.Escape(target)).Count;
    }
}
