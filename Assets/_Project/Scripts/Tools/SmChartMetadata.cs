using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SmChartMetadata
{
    public static Dictionary<ChartDifficulty, int> LoadDifficultyMeters(string smFilePath)
    {
        var path = Path.Combine(Application.streamingAssetsPath, smFilePath);
        var content = File.ReadAllText(path);
        var tags = ParseSmTags(content);

        var results = new Dictionary<ChartDifficulty, int>();
        if (!tags.TryGetValue("NOTES", out var notesList))
            return results;

        foreach (var entry in notesList)
        {
            var parts = entry.Split(new[] { ':' }, 6);
            if (parts.Length < 6) continue;

            var stepType = parts[0].Trim();
            if (!stepType.Equals("dance-single", StringComparison.OrdinalIgnoreCase))
                continue;

            var difficultyName = parts[2].Trim();
            if (!TryParseDifficulty(difficultyName, out var difficulty))
                continue;

            var meterValue = parts[3].Trim();
            if (!int.TryParse(meterValue, out var meter))
                continue;

            if (!results.ContainsKey(difficulty))
                results.Add(difficulty, meter);
        }

        return results;
    }

    static Dictionary<string, List<string>> ParseSmTags(string content)
    {
        var tags = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        int index = 0;
        while (index < content.Length)
        {
            var tagStart = content.IndexOf('#', index);
            if (tagStart < 0) break;
            var colon = content.IndexOf(':', tagStart + 1);
            if (colon < 0) break;
            var semicolon = content.IndexOf(';', colon + 1);
            if (semicolon < 0) break;

            var tag = content.Substring(tagStart + 1, colon - tagStart - 1).Trim();
            var value = content.Substring(colon + 1, semicolon - colon - 1);

            if (!tags.TryGetValue(tag, out var list))
            {
                list = new List<string>();
                tags[tag] = list;
            }

            list.Add(value);
            index = semicolon + 1;
        }

        return tags;
    }

    static bool TryParseDifficulty(string difficultyName, out ChartDifficulty difficulty)
    {
        if (difficultyName.Equals("Beginner", StringComparison.OrdinalIgnoreCase))
        {
            difficulty = ChartDifficulty.Beginner;
            return true;
        }

        if (difficultyName.Equals("Easy", StringComparison.OrdinalIgnoreCase))
        {
            difficulty = ChartDifficulty.Easy;
            return true;
        }

        if (difficultyName.Equals("Medium", StringComparison.OrdinalIgnoreCase))
        {
            difficulty = ChartDifficulty.Medium;
            return true;
        }

        if (difficultyName.Equals("Hard", StringComparison.OrdinalIgnoreCase))
        {
            difficulty = ChartDifficulty.Hard;
            return true;
        }

        if (difficultyName.Equals("Challenge", StringComparison.OrdinalIgnoreCase))
        {
            difficulty = ChartDifficulty.Challenge;
            return true;
        }

        difficulty = ChartDifficulty.Beginner;
        return false;
    }
}
