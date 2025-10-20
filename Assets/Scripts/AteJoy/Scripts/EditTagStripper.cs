using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class EditTagStripper
{
    /// <summary>
    /// Removes all <EDIT>...</EDIT> tags and their content from the input string.
    /// Useful for displaying clean code without editing markers.
    /// </summary>
    /// <param name="input">The original code template with <EDIT> tags.</param>
    /// <returns>The code with all <EDIT>...</EDIT> sections removed.</returns>
    public static string RemoveEditTags(string input)
    {
        // Remove only the <EDIT> and </EDIT> tags, keep the inner content
        return Regex.Replace(input, @"<\/?EDIT>", "", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Replaces all <EDIT>...</EDIT> sections with a specified placeholder.
    /// Useful if you want to show placeholders indicating editable areas.
    /// </summary>
    /// <param name="input">The original code template with <EDIT> tags.</param>
    /// <param name="placeholder">The string to replace each <EDIT>...</EDIT> section with.</param>
    /// <returns>The code with <EDIT>...</EDIT> sections replaced by the placeholder.</returns>
    public static string ReplaceEditTagsWithPlaceholder(string input, string placeholder = "")
    {
        return Regex.Replace(input, @"<EDIT>.*?</EDIT>", placeholder, RegexOptions.Singleline);
    }


    public static string ExtractEditContent(string originalTemplate, string input)
    {
        var match = Regex.Match(input, @"<EDIT>(.*?)<\/EDIT>", RegexOptions.Singleline);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return "";
    }


    public static string ReconstructCodeWithSingleInput(string originalTemplate, string editContent)
    {
        string pattern = @"(<EDIT>).*?(<\/EDIT>)"; // Capture the tags separately
        string result = Regex.Replace(originalTemplate, pattern, $"$1{editContent}$2");
        return result;
    }

    public static string ReconstructCodeWithInputs(string originalTemplate, List<string> playerInputs)
    {
        int inputIndex = 0;

        // Replace each <EDIT>...</EDIT> with the player's input
        string reconstructedCode = Regex.Replace(originalTemplate, @"<EDIT>.*?<\/EDIT>", match =>
        {
            if (inputIndex >= playerInputs.Count)
            {
                // If no more inputs, keep original or empty
                return "<EDIT></EDIT>";
            }

            string userInput = playerInputs[inputIndex];
            inputIndex++;
            return $"<EDIT>{userInput}</EDIT>";
        });

        return reconstructedCode;
    }
}