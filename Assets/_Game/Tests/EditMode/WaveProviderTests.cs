using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    /// <summary>
    /// Wave-source abstraction behind WaveSpawner. FixedLevelProvider preserves campaign behavior;
    /// EndlessProvider procedurally escalates for Arcade mode.
    /// </summary>
    public class WaveProviderTests
    {
        static HashSet<EnemyType> DistinctTypes(WaveConfig w)
        {
            var set = new HashSet<EnemyType>();
            if (w.UsesEntries)
                foreach (var e in w.entries) set.Add(e.enemyType);
            else
                set.Add(w.enemyType);
            return set;
        }

        static float MinInterval(WaveConfig w)
        {
            float min = float.MaxValue;
            foreach (var e in w.entries) min = Mathf.Min(min, e.spawnInterval);
            return min;
        }

        // ── EndlessProvider ───────────────────────────────────────────────

        [Test]
        public void Endless_WaveCount_IsEffectivelyInfinite()
        {
            Assert.AreEqual(int.MaxValue, new EndlessProvider().WaveCount);
        }

        [Test]
        public void Endless_FirstWave_HasEnemiesViaEntries()
        {
            var w = new EndlessProvider().GetWave(0);
            Assert.IsTrue(w.UsesEntries);
            Assert.Greater(w.TotalCount, 0);
        }

        [Test]
        public void Endless_DeeperWave_HasMoreEnemiesThanFirst()
        {
            var p = new EndlessProvider();
            Assert.Greater(p.GetWave(10).TotalCount, p.GetWave(0).TotalCount);
        }

        [Test]
        public void Endless_FirstWave_IsGruntOnly()
        {
            var types = DistinctTypes(new EndlessProvider().GetWave(0));
            Assert.AreEqual(1, types.Count);
            Assert.IsTrue(types.Contains(EnemyType.Grunt));
        }

        [Test]
        public void Endless_DeepWave_AddsTypeVariety()
        {
            Assert.Greater(DistinctTypes(new EndlessProvider().GetWave(6)).Count, 1);
        }

        [Test]
        public void Endless_EveryFifthWave_IncludesMiniBoss()
        {
            // index 4 == 5th wave
            Assert.IsTrue(DistinctTypes(new EndlessProvider().GetWave(4)).Contains(EnemyType.MiniBoss));
        }

        [Test]
        public void Endless_SpawnInterval_TightensWithDepth()
        {
            var p = new EndlessProvider();
            Assert.Less(MinInterval(p.GetWave(20)), MinInterval(p.GetWave(0)));
        }

        [Test]
        public void Endless_IsDeterministic_SameIndexSameTotal()
        {
            var p = new EndlessProvider();
            Assert.AreEqual(p.GetWave(7).TotalCount, p.GetWave(7).TotalCount);
        }

        // ── FixedLevelProvider ────────────────────────────────────────────

        [Test]
        public void Fixed_WaveCount_MatchesLevelWaveArray()
        {
            var level = ScriptableObject.CreateInstance<LevelConfig>();
            level.waves = new[] { MakeWave(), MakeWave(), MakeWave() };
            Assert.AreEqual(3, new FixedLevelProvider(level).WaveCount);
        }

        [Test]
        public void Fixed_GetWave_ReturnsTheLevelWaveInstance()
        {
            var level = ScriptableObject.CreateInstance<LevelConfig>();
            var w0 = MakeWave();
            level.waves = new[] { w0, MakeWave() };
            Assert.AreSame(w0, new FixedLevelProvider(level).GetWave(0));
        }

        static WaveConfig MakeWave()
        {
            var w = ScriptableObject.CreateInstance<WaveConfig>();
            w.entries = new[] { new WaveEntry { enemyType = EnemyType.Grunt, count = 3 } };
            return w;
        }
    }
}
