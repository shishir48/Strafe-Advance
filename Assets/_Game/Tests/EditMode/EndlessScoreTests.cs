using NUnit.Framework;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    /// <summary>Best-endless-score record/persist for Arcade mode.</summary>
    public class EndlessScoreTests
    {
        [SetUp]    public void SetUp()    => SaveSystem.Reset();
        [TearDown] public void TearDown() => SaveSystem.Reset();

        [Test]
        public void Record_FirstScore_SetsBest_AndReportsNewBest()
        {
            bool isBest = EndlessScore.Record(1500);
            Assert.IsTrue(isBest);
            Assert.AreEqual(1500, SaveSystem.Current.progress.bestEndlessScore);
        }

        [Test]
        public void Record_LowerScore_KeepsBest_AndReportsNotBest()
        {
            EndlessScore.Record(2000);
            bool isBest = EndlessScore.Record(900);
            Assert.IsFalse(isBest);
            Assert.AreEqual(2000, SaveSystem.Current.progress.bestEndlessScore);
        }

        [Test]
        public void Record_HigherScore_RaisesBest_AndReportsNewBest()
        {
            EndlessScore.Record(2000);
            bool isBest = EndlessScore.Record(3200);
            Assert.IsTrue(isBest);
            Assert.AreEqual(3200, SaveSystem.Current.progress.bestEndlessScore);
        }

        [Test]
        public void Record_Persists_AcrossReload()
        {
            EndlessScore.Record(4242);
            SaveSystem.Reload();
            Assert.AreEqual(4242, SaveSystem.Current.progress.bestEndlessScore);
        }
    }
}
