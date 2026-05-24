using System;
using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class DailyLoginServiceTests
    {
        GameObject _go;
        DailyLoginService _svc;
        GameObject _currencyGO;
        CurrencyService _currency;

        [SetUp]
        public void SetUp()
        {
            SaveSystem.Reset();
            _currencyGO = new GameObject("CurrencyForDaily");
            _currency = _currencyGO.AddComponent<CurrencyService>();
            _go = new GameObject("DailyLoginTest");
            _svc = _go.AddComponent<DailyLoginService>();
            // Awake auto-called CheckIn(UtcNow). Wipe state so our deterministic CheckIn calls drive the streak from a known baseline.
            var p = SaveSystem.Current.progress;
            p.loginStreak = 0;
            p.lastLoginDateUtc = "";
            p.softCurrency = 0;
            SaveSystem.Save();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
            if (_currencyGO != null) UnityEngine.Object.DestroyImmediate(_currencyGO);
            SaveSystem.Reset();
        }

        [Test]
        public void FirstCheckIn_GrantsDay1Reward()
        {
            bool granted = _svc.CheckIn(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));
            Assert.IsTrue(granted);
            Assert.AreEqual(1, SaveSystem.Current.progress.loginStreak);
            Assert.AreEqual(DailyLoginService.RewardForDay(1), SaveSystem.Current.progress.softCurrency);
        }

        [Test]
        public void SameDayCheckIn_IsIdempotent()
        {
            var t = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _svc.CheckIn(t);
            int beforeCurrency = SaveSystem.Current.progress.softCurrency;

            bool grantedAgain = _svc.CheckIn(t.AddHours(2));
            Assert.IsFalse(grantedAgain);
            Assert.AreEqual(beforeCurrency, SaveSystem.Current.progress.softCurrency);
            Assert.AreEqual(1, SaveSystem.Current.progress.loginStreak);
        }

        [Test]
        public void ConsecutiveDays_IncrementStreak()
        {
            _svc.CheckIn(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            _svc.CheckIn(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
            _svc.CheckIn(new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));
            Assert.AreEqual(3, SaveSystem.Current.progress.loginStreak);
        }

        [Test]
        public void GapInDays_ResetsStreakToOne()
        {
            _svc.CheckIn(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            _svc.CheckIn(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
            _svc.CheckIn(new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)); // skipped 3rd + 4th
            Assert.AreEqual(1, SaveSystem.Current.progress.loginStreak);
        }

        [Test]
        public void RewardCurve_CapsAtDay7()
        {
            int day7 = DailyLoginService.RewardForDay(7);
            int day10 = DailyLoginService.RewardForDay(10);
            int day100 = DailyLoginService.RewardForDay(100);
            Assert.AreEqual(day7, day10);
            Assert.AreEqual(day7, day100);
        }

        [Test]
        public void RewardCurve_MonotonicallyIncreasing()
        {
            int prev = DailyLoginService.RewardForDay(1);
            for (int d = 2; d <= 7; d++)
            {
                int cur = DailyLoginService.RewardForDay(d);
                Assert.GreaterOrEqual(cur, prev, $"Reward at day {d} must be >= day {d - 1}.");
                prev = cur;
            }
        }
    }
}
