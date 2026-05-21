using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class WaveSpawnerTests
    {
        private WaveSpawner _spawner;
        private LevelConfig _level;
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject();
            _spawner = _go.AddComponent<WaveSpawner>();

            WaveConfig w1 = ScriptableObject.CreateInstance<WaveConfig>();
            w1.count = 3; w1.spawnInterval = 0f;
            WaveConfig w2 = ScriptableObject.CreateInstance<WaveConfig>();
            w2.count = 2; w2.spawnInterval = 0f;

            _level = ScriptableObject.CreateInstance<LevelConfig>();
            _level.waves = new[] { w1, w2 };
            _spawner.LoadLevel(_level);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void LoadLevel_SetsWaveIndexToZero()
        {
            Assert.AreEqual(0, _spawner.CurrentWaveIndex);
        }

        [Test]
        public void ReportKill_AdvancesWave_WhenAllEnemiesDead()
        {
            _spawner.ReportEnemySpawned(3);
            for (int i = 0; i < 3; i++) _spawner.ReportKill();
            Assert.AreEqual(1, _spawner.CurrentWaveIndex);
        }

        [Test]
        public void ReportKill_FiresAllWavesComplete_AfterLastWave()
        {
            bool fired = false;
            _spawner.OnAllWavesComplete += () => fired = true;
            _spawner.ReportEnemySpawned(3);
            for (int i = 0; i < 3; i++) _spawner.ReportKill();
            _spawner.ReportEnemySpawned(2);
            for (int i = 0; i < 2; i++) _spawner.ReportKill();
            Assert.IsTrue(fired);
        }
    }
}
