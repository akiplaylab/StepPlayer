using NUnit.Framework;

public class ScoreCalculatorUnityAdapterTests
{
    [Test]
    public void Wrapper_DelegatesToDomainCalculator()
    {
        int score = ScoreCalculator.Calculate(
            totalNotes: 10,
            marvelous: 5,
            perfect: 3,
            great: 1,
            good: 1,
            bad: 0,
            miss: 0);

        Assert.That(score, Is.EqualTo(873_999));
        Assert.That(ScoreCalculator.GetDanceLevel(score), Is.EqualTo("A"));
    }
}
