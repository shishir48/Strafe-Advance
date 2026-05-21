using NUnit.Framework;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class DamageSystemTests
    {
        [Test]
        public void Calculate_ReturnsBaseDamage_WithDefaultMultiplier()
        {
            Assert.AreEqual(10, DamageSystem.Calculate(10));
        }

        [Test]
        public void Calculate_AppliesMultiplier()
        {
            Assert.AreEqual(20, DamageSystem.Calculate(10, 2f));
        }

        [Test]
        public void Calculate_RoundsToNearestInt()
        {
            Assert.AreEqual(15, DamageSystem.Calculate(10, 1.5f));
        }
    }
}
