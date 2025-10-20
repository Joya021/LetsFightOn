using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class LockRangeParser
{
    // Regex to match <LOCK>...</LOCK> blocks
    private static readonly Regex InlineLock =
        new Regex(@"<LOCK>(.*?)</LOCK>", RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// Returns the start and end positions of each locked snippet
    /// after removing the <LOCK> tags.
    /// </summary>
    /// <param name="input">Raw code string with <LOCK> tags</param>
    /// <returns>List of (start, end) index pairs for locked segments</returns>
    public static List<(int start, int end, string content)> GetLockedRangesWithContent(string input)
    {
        var ranges = new List<(int, int, string)>();
        if (string.IsNullOrEmpty(input)) return ranges;

        int offset = 0;

        foreach (Match match in InlineLock.Matches(input))
        {
            string lockedText = match.Groups[1].Value;
            int lockedLength = lockedText.Length;
            int start = match.Index - offset;
            int end = start + lockedLength;

            ranges.Add((start, end, lockedText));

            offset += "<LOCK>".Length + "</LOCK>".Length;
        }

        return ranges;
    }
}