using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    /// <summary>EnemyBase.Heal — used by HealerEnemy's aura.</summary>
    public class EnemyHealTests
    {
        GameObject _go;
        ConcreteEnemy _enemy;
        EnemyConfig _config;

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
            if (_go != null) Object.DestroyImmediate(_go);
            if (_config != null) Object.DestroyImmediate(_config);
        }

        [Test]
        public void Heal_RestoresHp()
        {
            _enemy.TakeDamage(30); // 50 -> 20
            _enemy.Heal(15);
            Assert.AreEqual(35, _enemy.CurrentHp);
        }

        [Test]
        public void Heal_ClampsToMaxHp()
        {
            _enemy.TakeDamage(10); // 50 -> 40
            _enemy.Heal(999);
            Assert.AreEqual(50, _enemy.CurrentHp);
        }

        [Test]
        public void Heal_OnDeadEnemy_HasNoEffect()
        {
            _enemy.TakeDamage(50); // dead
            _enemy.Heal(20);
            Assert.AreEqual(0, _enemy.CurrentHp);
        }
    }
}
