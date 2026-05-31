namespace StrafAdvance
{
    /// <summary>Records the best Endless Arcade score, persisting via <see cref="SaveSystem"/>.</summary>
    public static class EndlessScore
    {
        public static int Best => SaveSystem.Current.progress.bestEndlessScore;

        /// <summary>Record a finished run's score. Returns true if it set a new personal best.</summary>
        public static bool Record(int score)
        {
            var progress = SaveSystem.Current.progress;
            if (score <= progress.bestEndlessScore) return false;
            progress.bestEndlessScore = score;
            SaveSystem.Save();
            return true;
        }
    }
}
