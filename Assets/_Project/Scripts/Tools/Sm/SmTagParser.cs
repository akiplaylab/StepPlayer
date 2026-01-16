using System;
using System.Collections.Generic;

public static class SmTagParser
{
    const string NotesMarker = "#NOTES";

    public static Dictionary<string, List<string>> ParseAllTags(string content)
    {
        return ParseTags(content, stopAtNotes: false);
    }

    public static Dictionary<string, string> ParseHeader(string content)
    {
        var tags = ParseTags(content, stopAtNotes: true);
        var header = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, values) in tags)
        {
            if (values.Count > 0)
                header[key] = values[0].Trim();
        }

        return header;
    }

    static Dictionary<string, List<string>> ParseTags(string content, bool stopAtNotes)
    {
        var tags = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        int index = 0;
        while (index < content.Length)
        {
            var tagStart = content.IndexOf('#', index);
            if (tagStart < 0) break;

            if (stopAtNotes && MatchesNotes(content, tagStart))
                break;

            var colon = content.IndexOf(':', tagStart + 1);
            if (colon < 0) break;
            var semicolon = content.IndexOf(';', colon + 1);
            if (semicolon < 0) break;

            var tag = content.Substring(tagStart + 1, colon - tagStart - 1).Trim();
            var value = content.Substring(colon + 1, semicolon - colon - 1);

            if (!string.IsNullOrWhiteSpace(tag))
            {
                if (!tags.TryGetValue(tag, out var list))
                {
                    list = new List<string>();
                    tags[tag] = list;
                }

                list.Add(value);
            }

            index = semicolon + 1;
        }

        return tags;
    }

    static bool MatchesNotes(string content, int index)
    {
        if (index + NotesMarker.Length > content.Length) return false;
        return string.Compare(content, index, NotesMarker, 0, NotesMarker.Length, StringComparison.OrdinalIgnoreCase) == 0;
    }
}
