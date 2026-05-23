using System;
using UnityEngine;

namespace StrafAdvance
{
    public enum EnemyType { Grunt, Flanker, Elite, Charger, Sniper, Shielded, Splitter, Drone, MiniBoss }

    /// <summary>One enemy group inside a wave.</summary>
    [Serializable]
    public class WaveEntry
    {
        public EnemyType enemyType;
        [Min(1)] public int count = 3;
        [Min(0f)] public float spawnInterval = 1.0f;
        /// <summary>Delay before this entry starts spawning, measured from wave start.</summary>
        [Min(0f)] public float startDelay = 0f;
    }

    [CreateAssetMenu(fileName = "WaveConfig", menuName = "StrafAdvance/WaveConfig")]
    public class WaveConfig : ScriptableObject
    {
        [Tooltip("Mixed-type groups spawned in parallel from wave start. Preferred over legacy single-type fields below.")]
        public WaveEntry[] entries = Array.Empty<WaveEntry>();

        // ── Legacy single-type fields (back-compat with pre-1.0 configs) ─────
        [Header("Legacy (used only when entries is empty)")]
        public EnemyType enemyType;
        public int count = 5;
        public float spawnInterval = 1.5f;

        /// <summary>True when the wave uses the modern <see cref="entries"/> layout.</summary>
        public bool UsesEntries => entries != null && entries.Length > 0;

        /// <summary>Total enemy count across all entries (or legacy count fallback).</summary>
        public int TotalCount
        {
            get
            {
                if (!UsesEntries) return count;
                int sum = 0;
                foreach (var e in entries) sum += e.count;
                return sum;
            }
        }
    }
}
