using System;
using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class ConcreteEnemy : EnemyBase { }

    public class EnemyBaseTests
    {
        private GameObject _go;
        private ConcreteEnemy _enemy;
        private EnemyConfig _config;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject();
            _enemy = _go.AddComponent<ConcreteEnemy>();
            _config = ScriptableObject.CreateInstance<EnemyConfig>();
            _config.maxHp = 50;
            _enemy.Initialize(_config);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
            if (_config != null) UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void Initialize_SetsCurrentHp()
        {
            Assert.AreEqual(50, _enemy.CurrentHp);
        }

        [Test]
        public void TakeDamage_ReducesHp()
        {
            _enemy.TakeDamage(20);
            Assert.AreEqual(30, _enemy.CurrentHp);
        }

        [Test]
        public void TakeDamage_FiresOnDeath_AtZeroHp()
        {
            bool fired = false;
            _enemy.OnDeath += _ => fired = true;
            _enemy.TakeDamage(50);
            Assert.IsTrue(fired);
        }

        [Test]
        public void TakeDamage_PassesSelfToDeathEvent()
        {
            EnemyBase reported = null;
            _enemy.OnDeath += e => reported = e;
            _enemy.TakeDamage(50);
            Assert.AreEqual(_enemy, reported);
        }
    }
}
