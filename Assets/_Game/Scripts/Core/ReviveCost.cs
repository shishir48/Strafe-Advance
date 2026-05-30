namespace StrafAdvance
{
    /// <summary>Escalating soft-currency cost to revive, by revives already used this run.</summary>
    public static class ReviveCost
    {
        static readonly int[] Tiers = { 100, 250, 500 };

        public static int For(int reviveCount)
        {
            if (reviveCount < 0) reviveCount = 0;
            if (reviveCount < Tiers.Length) return Tiers[reviveCount];
            // Beyond the table, keep climbing: 500x2, 500x3, ...
            return Tiers[Tiers.Length - 1] * (reviveCount - Tiers.Length + 2);
        }
    }
}
