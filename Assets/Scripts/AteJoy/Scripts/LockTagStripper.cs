using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Utils
{
    /// Utility class for handling <LOCK>...</LOCK> markup in code strings.
    public static class LockTagStripper
    {
        // Regex to match <LOCK>...</LOCK> blocks, including multiline content
        private static readonly Regex InlineLock =
            new Regex(@"<LOCK>(.*?)</LOCK>", RegexOptions.Singleline);

        /// Removes all <LOCK> and </LOCK> tags from the input string,
        /// preserving the inner content.
        /// <param name="input">The raw code string containing lock tags.</param>
        /// <returns>Cleaned code string with lock tags removed.</returns>
        public static string StripLockTags(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // This replaces ALL <LOCK>...</LOCK> blocks with just the inner content
            return InlineLock.Replace(input, match => match.Groups[1].Value);
        }

        /// Extracts the protected snippets (inner content of <LOCK> tags).
        /// <param name="input">The raw code string containing lock tags.</param>
        /// <returns>List of protected code snippets.</returns>
        public static List<string> ExtractLockedSnippets(string input)
        {
            var snippets = new List<string>();
            if (string.IsNullOrEmpty(input))
                return snippets;

            var matches = InlineLock.Matches(input);
            foreach (Match m in matches)
            {
                snippets.Add(m.Groups[1].Value);
            }

            return snippets;
        }

        public static List<string> ExtractEditableSnippets(string input)
        {
            if (string.IsNullOrEmpty(input)) return new List<string>();

            var editableSnippets = new List<string>();
            int currentIndex = 0;

            var matches = InlineLock.Matches(input);

            foreach (Match match in matches)
            {
                int lockStart = match.Index;
                string lockedText = match.Value;

                // Extract the editable part between currentIndex and start of <LOCK>
                if (lockStart > currentIndex)
                {
                    string editable = input.Substring(currentIndex, lockStart - currentIndex);
                    if (!string.IsNullOrWhiteSpace(editable))
                        editableSnippets.Add(editable);
                }

                // Move past the locked block
                currentIndex = match.Index + match.Length;
            }

            // Add any trailing editable content
            if (currentIndex < input.Length)
            {
                string trailing = input.Substring(currentIndex);
                if (!string.IsNullOrWhiteSpace(trailing))
                    editableSnippets.Add(trailing);
            }

            return editableSnippets;
        }

        public static List<string> ExtractPlayerEditableSnippets(string rawTemplate, string playerInput)
        {
            var snippets = new List<string>();
            if (string.IsNullOrEmpty(rawTemplate) || string.IsNullOrEmpty(playerInput)) return snippets;

            int offset = 0;
            int shift = 0;

            foreach (Match match in InlineLock.Matches(rawTemplate))
            {
                int lockStart = match.Index;
                int lockEnd = match.Index + match.Length;

                // Extract editable part between previous lock and current lock
                int editableStart = offset;
                int editableEnd = lockStart;

                if (editableEnd > editableStart)
                {
                    int adjustedStart = editableStart - shift;
                    int adjustedLength = editableEnd - editableStart;

                    if (adjustedStart >= 0 && adjustedStart + adjustedLength <= playerInput.Length)
                    {
                        string editable = playerInput.Substring(adjustedStart, adjustedLength);
                        if (!string.IsNullOrWhiteSpace(editable))
                            snippets.Add(editable);
                    }
                }

                offset = lockEnd;
                shift += "<LOCK>".Length + "</LOCK>".Length;
            }

            // Final trailing editable part
            if (offset < rawTemplate.Length)
            {
                int adjustedStart = offset - shift;
                int adjustedLength = rawTemplate.Length - offset;

                if (adjustedStart >= 0 && adjustedStart + adjustedLength <= playerInput.Length)
                {
                    string trailing = playerInput.Substring(adjustedStart, adjustedLength);
                    if (!string.IsNullOrWhiteSpace(trailing))
                        snippets.Add(trailing);
                }
            }

            return snippets;
        }

        public static string ExtractEditableInputFromPlayerCode(string rawTemplate, string playerInput)
        {
            if (string.IsNullOrEmpty(rawTemplate) || string.IsNullOrEmpty(playerInput))
                return string.Empty;

            // Extract locked segments from the raw template
            var lockedSegments = new List<string>();
            foreach (Match match in InlineLock.Matches(rawTemplate))
            {
                string locked = match.Groups[1].Value;
                if (!string.IsNullOrWhiteSpace(locked))
                    lockedSegments.Add(locked.Trim());
            }

            // Remove locked segments from the player's input
            string editable = playerInput;
            foreach (string locked in lockedSegments)
            {
                editable = editable.Replace(locked, "");
            }

            return editable.Trim();
        }
    }
}
