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
        var tags = SmTagParser.ParseAllTags(content);

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
