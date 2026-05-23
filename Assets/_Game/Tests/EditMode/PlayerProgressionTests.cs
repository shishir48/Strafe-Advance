using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class PlayerProgressionTests
    {
        private GameObject _go;
        private PlayerProgression _prog;

        [SetUp]
        public void SetUp()
        {
            EventBus<EnemyKilled>.Clear();
            EventBus<XpGained>.Clear();
            EventBus<PlayerLeveledUp>.Clear();
            EventBus<PerkUnlocked>.Clear();
            SaveSystem.Reset();
            _go = new GameObject("ProgressionTest");
            _prog = _go.AddComponent<PlayerProgression>();
            var awake = typeof(PlayerProgression).GetMethod("Awake",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awake?.Invoke(_prog, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            SaveSystem.Reset();
        }

        [Test]
        public void XpRequiredFor_Quadratic()
        {
            Assert.AreEqual(100,  PlayerProgression.XpRequiredFor(1));
            Assert.AreEqual(400,  PlayerProgression.XpRequiredFor(2));
            Assert.AreEqual(900,  PlayerProgression.XpRequiredFor(3));
        }

        [Test]
        public void EnemyKilled_GrantsXp()
        {
            EventBus<EnemyKilled>.Publish(new EnemyKilled(EnemyType.Grunt, 100));
            Assert.AreEqual(10, _prog.Xp);
        }

        [Test]
        public void EnoughXp_LevelsUp_AndUnlocksPerk()
        {
            int levelUps = 0;
            EventBus<PlayerLeveledUp>.Subscribe(_ => levelUps++);
            // Need 400 XP for level 2. Elite = 75 XP. 6 kills = 450 → level up.
            for (int i = 0; i < 6; i++)
                EventBus<EnemyKilled>.Publish(new EnemyKilled(EnemyType.Elite, 500));
            Assert.GreaterOrEqual(_prog.Level, 2);
            Assert.GreaterOrEqual(levelUps, 1);
            Assert.IsNotEmpty(SaveSystem.Current.progress.unlockedPerkIds);
        }

        [Test]
        public void GetEquippedStats_AppliesPerkMultipliers()
        {
            SaveSystem.Current.progress.equippedPerkIds.Add("dmg_up_1");
            SaveSystem.Current.progress.equippedPerkIds.Add("firerate_up_1");
            var stats = _prog.GetEquippedStats();
            Assert.AreEqual(1.25f, stats.damageMultiplier, 0.001f);
            Assert.AreEqual(0.83f, stats.fireRateMultiplier, 0.001f);
        }
    }
}
