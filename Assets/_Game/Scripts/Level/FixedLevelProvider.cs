namespace StrafAdvance
{
    /// <summary>Wraps a designed <see cref="LevelConfig"/> — preserves campaign behavior.</summary>
    public class FixedLevelProvider : IWaveProvider
    {
        private readonly LevelConfig _level;

        public FixedLevelProvider(LevelConfig level)
        {
            _level = level;
        }

        public int WaveCount => _level.waves.Length;

        public WaveConfig GetWave(int index) => _level.waves[index];
    }
}
