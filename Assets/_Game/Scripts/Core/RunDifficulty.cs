using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Run-local difficulty escalation for Endless Arcade mode. Scales by wave depth within a
    /// single run (vs the meta <see cref="DifficultyService"/>, which scales by player level).
    /// </summary>
    public static class RunDifficulty
    {
        public const float PerWaveMul = 0.06f; // +6% per wave depth
        public const float MaxMul     = 4.0f;  // ×4 run-local cap

        /// <summary>Run-local enemy stat multiplier for the given (0-based) endless wave index.</summary>
        public static float ForWave(int waveIndex)
        {
            if (waveIndex <= 0) return 1f;
            return Mathf.Min(1f + waveIndex * PerWaveMul, MaxMul);
        }
    }
}
