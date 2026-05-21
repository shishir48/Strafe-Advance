using NUnit.Framework;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class ScoreCalculatorTests
    {
        [Test]
        public void Calculate_Returns100PerKill()
        {
            Assert.AreEqual(500, ScoreCalculator.Calculate(5));
        }

        [Test]
        public void CalculateStars_Returns0_WhenBossNotKilled()
        {
            Assert.AreEqual(0, ScoreCalculator.CalculateStars(false, true, true));
        }

        [Test]
        public void CalculateStars_Returns1_WhenOnlyBossKilled()
        {
            Assert.AreEqual(1, ScoreCalculator.CalculateStars(true, false, false));
        }

        [Test]
        public void CalculateStars_Returns2_WhenBossKilledAndNoDeath()
        {
            Assert.AreEqual(2, ScoreCalculator.CalculateStars(true, true, false));
        }

        [Test]
        public void CalculateStars_Returns3_WhenAllConditionsMet()
        {
            Assert.AreEqual(3, ScoreCalculator.CalculateStars(true, true, true));
        }
    }
}
