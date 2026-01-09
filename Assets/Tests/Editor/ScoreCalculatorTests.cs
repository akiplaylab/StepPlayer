using NUnit.Framework;

public class ScoreCalculatorTests
{
    [Test]
    public void Calculate_ReturnsZero_WhenTotalNotesIsZero()
    {
        var score = ScoreCalculator.Calculate(0, 10, 10, 10, 10, 10, 10);

        Assert.That(score, Is.EqualTo(0));
    }

    [TestCase(100, 100, 0, 0, 0, 0, 0, 1_000_000)]
    [TestCase(100, 0, 100, 0, 0, 0, 0, 999_000)]
    [TestCase(100, 0, 0, 100, 0, 0, 0, 599_000)]
    [TestCase(100, 0, 0, 0, 100, 0, 0, 199_000)]
    [TestCase(100, 0, 0, 0, 0, 100, 0, 0)]
    [TestCase(100, 0, 0, 0, 0, 0, 100, 0)]
    [TestCase(100, 50, 50, 0, 0, 0, 0, 999_500)]
    [TestCase(3, 1, 1, 1, 0, 0, 0, 866_638)]
    public void Calculate_UsesWeightedScores(
        int totalNotes,
        int marvelous,
        int perfect,
        int great,
        int good,
        int bad,
        int miss,
        int expected)
    {
        var score = ScoreCalculator.Calculate(totalNotes, marvelous, perfect, great, good, bad, miss);

        Assert.That(score, Is.EqualTo(expected));
    }

    [Test]
    public void GetDanceLevel_ReturnsE_WhenFailed()
    {
        var danceLevel = ScoreCalculator.GetDanceLevel(999_999, failed: true);

        Assert.That(danceLevel, Is.EqualTo("E"));
    }

    [TestCase(990_000, "AAA")]
    [TestCase(989_999, "AA+")]
    [TestCase(950_000, "AA+")]
    [TestCase(900_000, "AA")]
    [TestCase(890_000, "AA-")]
    [TestCase(850_000, "A+")]
    [TestCase(800_000, "A")]
    [TestCase(790_000, "A-")]
    [TestCase(750_000, "B+")]
    [TestCase(700_000, "B")]
    [TestCase(690_000, "B-")]
    [TestCase(650_000, "C+")]
    [TestCase(600_000, "C")]
    [TestCase(590_000, "C-")]
    [TestCase(550_000, "D+")]
    [TestCase(549_999, "D")]
    public void GetDanceLevel_UsesScoreThresholds(int score, string expected)
    {
        var danceLevel = ScoreCalculator.GetDanceLevel(score);

        Assert.That(danceLevel, Is.EqualTo(expected));
    }
}
