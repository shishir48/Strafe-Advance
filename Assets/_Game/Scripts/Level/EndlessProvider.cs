using System.Collections.Generic;
using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Procedurally synthesizes escalating waves for Endless Arcade mode. Deterministic in the
    /// wave index: deeper waves have more enemies, more type variety, tighter spawn intervals,
    /// and a mini-boss every 5th wave. Run-local stat scaling is layered separately via
    /// <see cref="RunDifficulty"/>.
    /// </summary>
    public class EndlessProvider : IWaveProvider
    {
        // Type variety unlocks by depth — index threshold each new type appears at.
        static readonly (int depth, EnemyType type)[] Unlocks =
        {
            (2, EnemyType.Flanker),
            (4, EnemyType.Charger),
            (6, EnemyType.Sniper),
            (8, EnemyType.Shielded),
            (11, EnemyType.Drone),
            (14, EnemyType.Splitter),
        };

        const int   GruntCap     = 12;
        const float MinInterval  = 0.3f;

        public int WaveCount => int.MaxValue;

        public WaveConfig GetWave(int index)
        {
            int depth = Mathf.Max(0, index);
            float interval = Mathf.Max(MinInterval, 1.0f - depth * 0.035f);

            var entries = new List<WaveEntry>
            {
                new WaveEntry
                {
                    enemyType = EnemyType.Grunt,
                    count = Mathf.Min(GruntCap, 3 + depth),
                    spawnInterval = interval,
                },
            };

            foreach (var (unlockDepth, type) in Unlocks)
            {
                if (depth < unlockDepth) continue;
                entries.Add(new WaveEntry
                {
                    enemyType = type,
                    count = 1 + (depth - unlockDepth) / 3,
                    spawnInterval = interval,
                    startDelay = 0.5f,
                });
            }

            // Mini-boss surge every 5th wave (index 4, 9, 14, ...).
            if (depth % 5 == 4)
                entries.Add(new WaveEntry { enemyType = EnemyType.MiniBoss, count = 1, startDelay = 1.5f });

            var wave = ScriptableObject.CreateInstance<WaveConfig>();
            wave.entries = entries.ToArray();
            return wave;
        }
    }
}
