using System;

public static class ChartDifficultyUtil
{
    public static string ToSmName(ChartDifficulty difficulty)
    {
        return difficulty switch
        {
            ChartDifficulty.Beginner => "Beginner",
            ChartDifficulty.Easy => "Easy",
            ChartDifficulty.Medium => "Medium",
            ChartDifficulty.Hard => "Hard",
            ChartDifficulty.Challenge => "Challenge",
            _ => "Beginner",
        };
    }

    public static bool TryParseSmName(string difficultyName, out ChartDifficulty difficulty)
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
