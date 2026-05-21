using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class BossControllerTests
    {
        private GameObject _go;
        private BossController _boss;
        private EnemyConfig _config;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject();
            _boss = _go.AddComponent<BossController>();
            _config = ScriptableObject.CreateInstance<EnemyConfig>();
            _config.maxHp = 200;
            _boss.Initialize(_config);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            if (_config != null) Object.DestroyImmediate(_config);
        }

        [Test]
        public void StartsInPhase1()
        {
            Assert.AreEqual(1, _boss.Phase);
        }

        [Test]
        public void TransitionsToPhase2_AtHalfHp()
        {
            _boss.TakeDamage(100);
            Assert.AreEqual(2, _boss.Phase);
        }

        [Test]
        public void DoesNotTransitionToPhase2_AboveHalfHp()
        {
            _boss.TakeDamage(99);
            Assert.AreEqual(1, _boss.Phase);
        }

        [Test]
        public void OnPhaseChanged_FiresWithPhase2()
        {
            int reportedPhase = 0;
            _boss.OnPhaseChanged += p => reportedPhase = p;
            _boss.TakeDamage(100);
            Assert.AreEqual(2, reportedPhase);
        }

        [Test]
        public void Phase2_DoesNotFireAgain_OnFurtherDamage()
        {
            int count = 0;
            _boss.OnPhaseChanged += _ => count++;
            _boss.TakeDamage(100);
            _boss.TakeDamage(50);
            Assert.AreEqual(1, count);
        }
    }
}
