using DDR.Domain;
using NUnit.Framework;

namespace DDR.Domain.Tests
{
    public class ScoreCalculatorTests
    {
        [Test]
        public void Calculate_ReturnsZero_WhenTotalNotesIsZero()
        {
            int score = ScoreCalculator.Calculate(0, 1, 1, 1, 1, 1, 1);

            Assert.That(score, Is.EqualTo(0));
        }

        [Test]
        public void Calculate_UsesWeightedJudgements()
        {
            int score = ScoreCalculator.Calculate(
                totalNotes: 10,
                marvelous: 5,
                perfect: 3,
                great: 1,
                good: 1,
                bad: 0,
                miss: 0);

            // basePoint = 1_000_000 / 10 = 100_000
            // weighted = 5*1.00 + 3*0.98 + 1*0.60 + 1*0.20 = 8.74
            // score = 874_000
            Assert.That(score, Is.EqualTo(873_999));
        }

        [TestCase(990_000, "AAA")]
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
        [TestCase(0, "D")]
        public void GetDanceLevel_ReturnsExpectedGrade(int score, string expected)
        {
            string result = ScoreCalculator.GetDanceLevel(score);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GetDanceLevel_ReturnsE_WhenFailed()
        {
            string result = ScoreCalculator.GetDanceLevel(999_999, failed: true);

            Assert.That(result, Is.EqualTo("E"));
        }
    }
}
