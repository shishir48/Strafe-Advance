namespace StrafAdvance
{
    /// <summary>
    /// Source of waves for <see cref="WaveSpawner"/>. Decouples the spawner from a fixed
    /// <see cref="LevelConfig"/> so Endless Arcade mode can synthesize waves procedurally.
    /// </summary>
    public interface IWaveProvider
    {
        /// <summary>Total waves; <see cref="int.MaxValue"/> for an endless source.</summary>
        int WaveCount { get; }

        /// <summary>The wave for a 0-based index.</summary>
        WaveConfig GetWave(int index);
    }
}
