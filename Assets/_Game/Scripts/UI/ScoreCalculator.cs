namespace StrafAdvance
{
    public static class ScoreCalculator
    {
        private const int PointsPerKill = 100;

        public static int Calculate(int enemiesKilled) => enemiesKilled * PointsPerKill;

        public static int CalculateStars(bool bossKilled, bool noDeath, bool underParTime)
        {
            if (!bossKilled) return 0;
            if (!noDeath) return 1;
            if (!underParTime) return 2;
            return 3;
        }
    }
}
