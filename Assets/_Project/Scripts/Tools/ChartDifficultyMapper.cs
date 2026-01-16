using System;
using System.Collections.Generic;
using System.Linq;

public static class ChartDifficultyMapper
{
    static readonly Dictionary<ChartDifficulty, string> SmNameByDifficulty = new()
    {
        { ChartDifficulty.Beginner, "Beginner" },
        { ChartDifficulty.Easy, "Easy" },
        { ChartDifficulty.Medium, "Medium" },
        { ChartDifficulty.Hard, "Hard" },
        { ChartDifficulty.Challenge, "Challenge" },
    };

    static readonly Dictionary<string, ChartDifficulty> DifficultyBySmName =
        SmNameByDifficulty.ToDictionary(
            pair => pair.Value,
            pair => pair.Key,
            StringComparer.OrdinalIgnoreCase);

    public static string ToSmName(ChartDifficulty difficulty)
    {
        return SmNameByDifficulty.TryGetValue(difficulty, out var name)
            ? name
            : SmNameByDifficulty[ChartDifficulty.Beginner];
    }

    public static bool TryParseSmName(string difficultyName, out ChartDifficulty difficulty)
    {
        if (string.IsNullOrWhiteSpace(difficultyName))
        {
            difficulty = ChartDifficulty.Beginner;
            return false;
        }

        return DifficultyBySmName.TryGetValue(difficultyName.Trim(), out difficulty);
    }
}
