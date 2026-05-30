using NUnit.Framework;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    /// <summary>Escalating revive cost by number of revives already used this run.</summary>
    public class ReviveCostTests
    {
        [Test]
        public void FirstRevive_IsCheapest()
        {
            Assert.AreEqual(100, ReviveCost.For(0));
        }

        [Test]
        public void SecondAndThird_Escalate()
        {
            Assert.AreEqual(250, ReviveCost.For(1));
            Assert.AreEqual(500, ReviveCost.For(2));
        }

        [Test]
        public void FourthRevive_CostsMoreThanThird()
        {
            Assert.Greater(ReviveCost.For(3), ReviveCost.For(2));
        }

        [Test]
        public void NegativeCount_ClampsToFirst()
        {
            Assert.AreEqual(100, ReviveCost.For(-2));
        }

        [Test]
        public void Cost_IsStrictlyIncreasing()
        {
            for (int n = 1; n <= 6; n++)
                Assert.Greater(ReviveCost.For(n), ReviveCost.For(n - 1), $"revive {n} should cost more than {n - 1}");
        }
    }
}
