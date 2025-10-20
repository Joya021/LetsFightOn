using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class CodeHighlighter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField answerInput;

    // Python 3.8+ keywords
    static readonly string[] Keywords = new[]
    {
        "False","None","True","and","as","assert","break","class","continue",
        "def","del","elif","else","except","finally","for","from","global",
        "if","import","in","is","lambda","nonlocal","not","or","pass","raise",
        "return","try","while","with","yield"
    };

    const string CommentColor = "#6A9955";
    const string StringColor = "#CE9178";
    const string NumberColor = "#B5CEA8";
    const string KeywordColor = "#569CD6";

    private static string StripInlineLockers(string template)
    {
        return template.Replace("<LOCK>", "").Replace("</LOCK>", "");
    }

    /// <summary>
    /// Removes <LOCK> markers and applies syntax highlighting to visible text.
    /// </summary>
    public void SetCodeWithHighlight(string template)
    {
        if (answerInput == null)
        {
            Debug.LogWarning("CodeHighlighter: TMP_InputField reference missing.");
            return;
        }

        string cleaned = StripInlineLockers(template);

        if (string.IsNullOrEmpty(cleaned))
        {
            answerInput.text = "";
            return;
        }

        string code = cleaned;

        // Comments
        code = Regex.Replace(code, @"(#.*?$)", $"<color={CommentColor}>$1</color>", RegexOptions.Multiline);
        // Strings
        code = Regex.Replace(code, @"(""[^""\n]*""|'[^'\n]*')", $"<color={StringColor}>$1</color>");
        // Numbers
        code = Regex.Replace(code, @"\b\d+(\.\d+)?\b", $"<color={NumberColor}>$0</color>");
        // Keywords
        foreach (var kw in Keywords)
            code = Regex.Replace(code, $@"\b{kw}\b", $"<color={KeywordColor}>{kw}</color>");

        // Assign the colored text
        answerInput.text = code;
    }
}
