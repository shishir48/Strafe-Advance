using NUnit.Framework;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    /// <summary>Step-based milestone tracker — fires once per band, resettable per run.</summary>
    public class MilestoneTrackerTests
    {
        [Test]
        public void BelowStep_ReturnsZero()
        {
            var t = new MilestoneTracker(25);
            Assert.AreEqual(0, t.Check(10));
        }

        [Test]
        public void AtStep_ReturnsMilestone()
        {
            var t = new MilestoneTracker(25);
            Assert.AreEqual(25, t.Check(25));
        }

        [Test]
        public void SameBand_DoesNotRefire()
        {
            var t = new MilestoneTracker(25);
            t.Check(25);
            Assert.AreEqual(0, t.Check(30));
        }

        [Test]
        public void NextBand_ReturnsNextMilestone()
        {
            var t = new MilestoneTracker(25);
            t.Check(25);
            Assert.AreEqual(50, t.Check(50));
        }

        [Test]
        public void Reset_AllowsRefire()
        {
            var t = new MilestoneTracker(25);
            t.Check(25);
            t.Reset();
            Assert.AreEqual(25, t.Check(25));
        }
    }
}
