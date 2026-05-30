namespace StrafAdvance
{
    /// <summary>Mid-run spike events for Endless mode.</summary>
    public enum SurgeType { None, SwarmRush, EliteAmbush, Gauntlet }

    /// <summary>
    /// Deterministic cadence of surge events by endless wave index. Surges begin at wave 6 and
    /// recur every 5 waves (index % 5 == 1), cycling Swarm → Elite → Gauntlet. This never collides
    /// with the mini-boss cadence (index % 5 == 4).
    /// </summary>
    public static class SurgeSchedule
    {
        const int FirstSurgeWave = 6;
        const int Period         = 5;

        public static SurgeType For(int waveIndex)
        {
            if (waveIndex < FirstSurgeWave) return SurgeType.None;
            if (waveIndex % Period != FirstSurgeWave % Period) return SurgeType.None;
            int n = (waveIndex - FirstSurgeWave) / Period;
            switch (n % 3)
            {
                case 0:  return SurgeType.SwarmRush;
                case 1:  return SurgeType.EliteAmbush;
                default: return SurgeType.Gauntlet;
            }
        }
    }
}
