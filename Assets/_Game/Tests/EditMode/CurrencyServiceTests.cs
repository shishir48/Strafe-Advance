using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class CurrencyServiceTests
    {
        GameObject _go;
        CurrencyService _svc;

        [SetUp]
        public void SetUp()
        {
            SaveSystem.Reset();
            _go = new GameObject("CurrencyServiceTest");
            _svc = _go.AddComponent<CurrencyService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            SaveSystem.Reset();
        }

        [Test]
        public void TrySpend_DeductsWhenAffordable()
        {
            SaveSystem.Current.progress.softCurrency = 500;
            bool ok = _svc.TrySpend(200);
            Assert.IsTrue(ok);
            Assert.AreEqual(300, _svc.Balance);
        }

        [Test]
        public void TrySpend_RefusesWhenInsufficient()
        {
            SaveSystem.Current.progress.softCurrency = 100;
            bool ok = _svc.TrySpend(200);
            Assert.IsFalse(ok);
            Assert.AreEqual(100, _svc.Balance, "Balance must not change on a failed spend.");
        }

        [Test]
        public void TrySpend_RefusesZeroOrNegative()
        {
            SaveSystem.Current.progress.softCurrency = 100;
            Assert.IsFalse(_svc.TrySpend(0));
            Assert.IsFalse(_svc.TrySpend(-50));
            Assert.AreEqual(100, _svc.Balance);
        }

        [Test]
        public void Grant_AddsToBalance()
        {
            SaveSystem.Current.progress.softCurrency = 50;
            _svc.Grant(75);
            Assert.AreEqual(125, _svc.Balance);
        }

        [Test]
        public void Grant_IgnoresNonPositive()
        {
            SaveSystem.Current.progress.softCurrency = 50;
            _svc.Grant(0);
            _svc.Grant(-10);
            Assert.AreEqual(50, _svc.Balance);
        }

        [Test]
        public void TrySpend_PersistsAcrossReload()
        {
            SaveSystem.Current.progress.softCurrency = 1000;
            _svc.TrySpend(400);
            var fresh = SaveSystem.Reload();
            Assert.AreEqual(600, fresh.progress.softCurrency);
        }
    }
}
