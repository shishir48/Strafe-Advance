using NUnit.Framework;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    /// <summary>Bomber death-blast AoE damage falloff (pure math).</summary>
    public class BomberBlastTests
    {
        [Test]
        public void DamageAt_Center_IsMax()
        {
            Assert.AreEqual(40, BomberBlast.DamageAt(0f, 4f, 40));
        }

        [Test]
        public void DamageAt_BeyondRadius_IsZero()
        {
            Assert.AreEqual(0, BomberBlast.DamageAt(4f, 4f, 40));
            Assert.AreEqual(0, BomberBlast.DamageAt(10f, 4f, 40));
        }

        [Test]
        public void DamageAt_Midway_IsBetweenZeroAndMax()
        {
            int d = BomberBlast.DamageAt(2f, 4f, 40);
            Assert.Greater(d, 0);
            Assert.Less(d, 40);
        }

        [Test]
        public void DamageAt_NegativeDistance_IsMax()
        {
            Assert.AreEqual(40, BomberBlast.DamageAt(-1f, 4f, 40));
        }
    }
}
