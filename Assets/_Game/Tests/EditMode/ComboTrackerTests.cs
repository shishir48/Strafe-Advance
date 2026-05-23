using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class ComboTrackerTests
    {
        private GameObject _go;
        private ComboTracker _combo;

        [SetUp]
        public void SetUp()
        {
            EventBus<EnemyKilled>.Clear();
            EventBus<PlayerDamaged>.Clear();
            EventBus<ComboChanged>.Clear();
            _go = new GameObject("ComboTrackerTest");
            _combo = _go.AddComponent<ComboTracker>();
            // EditMode tests don't fire Unity lifecycle methods automatically.
            var awake = typeof(ComboTracker).GetMethod("Awake",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awake?.Invoke(_combo, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            EventBus<EnemyKilled>.Clear();
            EventBus<PlayerDamaged>.Clear();
            EventBus<ComboChanged>.Clear();
        }

        [Test]
        public void FirstKill_StartsStreakAtOne()
        {
            EventBus<EnemyKilled>.Publish(new EnemyKilled(EnemyType.Grunt, 100));
            Assert.AreEqual(1, _combo.Streak);
            Assert.AreEqual(1, _combo.Multiplier);
        }

        [Test]
        public void FiveKills_RaisesMultiplierToTwo()
        {
            for (int i = 0; i < 5; i++)
                EventBus<EnemyKilled>.Publish(new EnemyKilled(EnemyType.Grunt, 100));
            Assert.AreEqual(5, _combo.Streak);
            Assert.AreEqual(2, _combo.Multiplier);
        }

        [Test]
        public void TwentyKills_RaisesMultiplierToEight()
        {
            for (int i = 0; i < 20; i++)
                EventBus<EnemyKilled>.Publish(new EnemyKilled(EnemyType.Grunt, 100));
            Assert.AreEqual(8, _combo.Multiplier);
        }

        [Test]
        public void PlayerDamaged_ResetsCombo()
        {
            for (int i = 0; i < 10; i++)
                EventBus<EnemyKilled>.Publish(new EnemyKilled(EnemyType.Grunt, 100));
            EventBus<PlayerDamaged>.Publish(new PlayerDamaged(10, 90));
            Assert.AreEqual(0, _combo.Streak);
            Assert.AreEqual(1, _combo.Multiplier);
        }

        [Test]
        public void Publishes_ComboChanged_Event()
        {
            int lastMult = -1;
            EventBus<ComboChanged>.Subscribe(c => lastMult = c.Multiplier);
            for (int i = 0; i < 10; i++)
                EventBus<EnemyKilled>.Publish(new EnemyKilled(EnemyType.Grunt, 100));
            Assert.AreEqual(4, lastMult);
        }
    }
}
