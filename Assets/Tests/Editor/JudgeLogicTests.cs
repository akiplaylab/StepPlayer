using NUnit.Framework;

public sealed class JudgeLogicTests
{
    [TestCase(0.0, Judgement.Marvelous)]
    [TestCase(0.0149, Judgement.Marvelous)]
    [TestCase(0.015, Judgement.Perfect)]
    [TestCase(0.2, Judgement.Bad)]
    [TestCase(0.201, Judgement.None)]
    public void Evaluate_Judgement(double dt, Judgement judgement)
    {
        var actual = JudgeLogic.Evaluate(dt);

        Assert.AreEqual(judgement, actual.Judgement);
    }

    [TestCase(0.0, true)]
    [TestCase(0.2, true)]
    [TestCase(0.201, false)]
    public void Evaluate_ShouldConsumeNote(double dt, bool shouldConsumeNote)
    {
        var actual = JudgeLogic.Evaluate(dt);

        Assert.AreEqual(shouldConsumeNote, actual.ShouldConsumeNote);
    }

    [Test]
    public void Evaluate_MissBoundary_IsInclusive()
    {
        Assert.AreEqual(Judgement.Bad, JudgeLogic.Evaluate(0.2000).Judgement);
        Assert.AreEqual(Judgement.None, JudgeLogic.Evaluate(0.2001).Judgement);
    }
}
