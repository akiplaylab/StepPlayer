public static class ScoreCalculator
{
    public static int Calculate(int totalNotes, int marvelous, int perfect, int great, int good, int bad, int miss)
    {
        return DDR.Domain.ScoreCalculator.Calculate(totalNotes, marvelous, perfect, great, good, bad, miss);
    }

    public static string GetDanceLevel(int score, bool failed = false)
    {
        return DDR.Domain.ScoreCalculator.GetDanceLevel(score, failed);
    }
}
