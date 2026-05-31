using NUnit.Framework;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    /// <summary>
    /// Run-local difficulty escalation for Endless Arcade mode. Distinct from the meta
    /// <see cref="DifficultyService"/> (which scales by player level). This scales by wave depth
    /// within a single run so an endless run ramps from baseline up to a hard cap.
    /// </summary>
    public class RunDifficultyTests
    {
        [Test]
        public void Wave0_IsBaselineOne()
        {
            Assert.AreEqual(1f, RunDifficulty.ForWave(0), 1e-4f);
        }

        [Test]
        public void EarlyWave_ScalesLinearlyByPerWaveStep()
        {
            // 1 + waveIndex * perWaveMul
            float expected = 1f + 10 * RunDifficulty.PerWaveMul;
            Assert.AreEqual(expected, RunDifficulty.ForWave(10), 1e-4f);
        }

        [Test]
        public void DeeperWave_IsHarderThanShallowWave()
        {
            Assert.Greater(RunDifficulty.ForWave(8), RunDifficulty.ForWave(2));
        }

        [Test]
        public void VeryDeepWave_ClampsToMaxMultiplier()
        {
            Assert.AreEqual(RunDifficulty.MaxMul, RunDifficulty.ForWave(100000), 1e-4f);
        }

        [Test]
        public void NegativeWave_ClampsToBaseline()
        {
            Assert.AreEqual(1f, RunDifficulty.ForWave(-5), 1e-4f);
        }
    }
}
