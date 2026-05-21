using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class PlayerHealthTests
    {
        private PlayerHealth _health;
        private PlayerConfig _config;
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject();
            _health = _go.AddComponent<PlayerHealth>();
            _config = ScriptableObject.CreateInstance<PlayerConfig>();
            _config.maxHp = 100;
            _health.Initialize(_config);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            if (_config != null) Object.DestroyImmediate(_config);
        }

        [Test]
        public void Initialize_SetsCurrentHpToMax()
        {
            Assert.AreEqual(100, _health.CurrentHp);
            Assert.AreEqual(100, _health.MaxHp);
        }

        [Test]
        public void TakeDamage_ReducesHp()
        {
            _health.TakeDamage(30);
            Assert.AreEqual(70, _health.CurrentHp);
        }

        [Test]
        public void TakeDamage_ClampsAtZero()
        {
            _health.TakeDamage(999);
            Assert.AreEqual(0, _health.CurrentHp);
        }

        [Test]
        public void TakeDamage_FiresDeathEvent_WhenHpReachesZero()
        {
            bool fired = false;
            _health.OnDeath += () => fired = true;
            _health.TakeDamage(100);
            Assert.IsTrue(fired);
        }

        [Test]
        public void TakeDamage_DoesNotFireDeathEvent_WhenHpAboveZero()
        {
            bool fired = false;
            _health.OnDeath += () => fired = true;
            _health.TakeDamage(50);
            Assert.IsFalse(fired);
        }

        [Test]
        public void OnHealthChanged_Fires_WithCorrectValues()
        {
            int reportedCurrent = -1, reportedMax = -1;
            _health.OnHealthChanged += (cur, max) => { reportedCurrent = cur; reportedMax = max; };
            _health.TakeDamage(25);
            Assert.AreEqual(75, reportedCurrent);
            Assert.AreEqual(100, reportedMax);
        }

        [Test]
        public void SetInvincible_PreventsDamage()
        {
            _health.SetInvincible(true);
            _health.TakeDamage(50);
            Assert.AreEqual(100, _health.CurrentHp);
        }

        [Test]
        public void SetInvincible_False_AllowsDamageAgain()
        {
            _health.SetInvincible(true);
            _health.SetInvincible(false);
            _health.TakeDamage(30);
            Assert.AreEqual(70, _health.CurrentHp);
        }
    }
}
