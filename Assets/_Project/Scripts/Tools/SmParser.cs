using System;
using System.Collections.Generic;

// Parses .sm header tags before the NOTES section.
public static class SmParser
{
    public static Dictionary<string, string> ParseHeader(string content)
    {
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int index = 0;

        while (index < content.Length)
        {
            var tagStart = content.IndexOf('#', index);
            if (tagStart < 0) break;

            if (MatchesNotes(content, tagStart))
                break;

            var colon = content.IndexOf(':', tagStart + 1);
            if (colon < 0) break;
            var semicolon = content.IndexOf(';', colon + 1);
            if (semicolon < 0) break;

            var tag = content.Substring(tagStart + 1, colon - tagStart - 1).Trim();
            var value = content.Substring(colon + 1, semicolon - colon - 1).Trim();

            if (!string.IsNullOrWhiteSpace(tag))
                tags[tag] = value;

            index = semicolon + 1;
        }

        return tags;
    }

    static bool MatchesNotes(string content, int index)
    {
        const string marker = "#NOTES";
        if (index + marker.Length > content.Length) return false;
        return string.Compare(content, index, marker, 0, marker.Length, StringComparison.OrdinalIgnoreCase) == 0;
    }
}
