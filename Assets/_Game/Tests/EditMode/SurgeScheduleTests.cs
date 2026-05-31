using NUnit.Framework;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    /// <summary>Mid-run surge-event cadence for Endless mode (pure schedule).</summary>
    public class SurgeScheduleTests
    {
        [Test]
        public void ShallowWaves_HaveNoSurge()
        {
            Assert.AreEqual(SurgeType.None, SurgeSchedule.For(0));
            Assert.AreEqual(SurgeType.None, SurgeSchedule.For(5));
        }

        [Test]
        public void Wave6_IsSwarmRush()
        {
            Assert.AreEqual(SurgeType.SwarmRush, SurgeSchedule.For(6));
        }

        [Test]
        public void Wave11_IsEliteAmbush()
        {
            Assert.AreEqual(SurgeType.EliteAmbush, SurgeSchedule.For(11));
        }

        [Test]
        public void Wave16_IsGauntlet()
        {
            Assert.AreEqual(SurgeType.Gauntlet, SurgeSchedule.For(16));
        }

        [Test]
        public void Wave21_CyclesBackToSwarmRush()
        {
            Assert.AreEqual(SurgeType.SwarmRush, SurgeSchedule.For(21));
        }

        [Test]
        public void NonCadenceWave_HasNoSurge()
        {
            Assert.AreEqual(SurgeType.None, SurgeSchedule.For(7));
            Assert.AreEqual(SurgeType.None, SurgeSchedule.For(10));
        }
    }
}
