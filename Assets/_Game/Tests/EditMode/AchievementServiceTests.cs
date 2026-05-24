using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class AchievementServiceTests
    {
        GameObject _currencyGO;
        GameObject _achGO;
        CurrencyService _currency;
        AchievementService _ach;

        [SetUp]
        public void SetUp()
        {
            SaveSystem.Reset();
            _currencyGO = new GameObject("CurrencyForAch");
            _currency = _currencyGO.AddComponent<CurrencyService>();
            _achGO = new GameObject("AchTest");
            _ach = _achGO.AddComponent<AchievementService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_achGO != null) Object.DestroyImmediate(_achGO);
            if (_currencyGO != null) Object.DestroyImmediate(_currencyGO);
            SaveSystem.Reset();
        }

        [Test]
        public void DefaultSave_NothingUnlocked()
        {
            Assert.AreEqual(0, SaveSystem.Current.progress.unlockedAchievementIds.Count);
        }

        [Test]
        public void FirstBlood_UnlocksOnFirstKill()
        {
            SaveSystem.Current.progress.totalKills = 1;
            _ach.Reevaluate();
            Assert.IsTrue(_ach.IsUnlocked("first_blood"));
        }

        [Test]
        public void Reevaluate_DoesNotDoubleGrantReward()
        {
            SaveSystem.Current.progress.totalKills = 1;
            _ach.Reevaluate();
            int currencyAfterFirst = SaveSystem.Current.progress.softCurrency;
            _ach.Reevaluate();
            _ach.Reevaluate();
            Assert.AreEqual(currencyAfterFirst, SaveSystem.Current.progress.softCurrency,
                "Already-unlocked achievement must not grant currency on re-evaluation.");
        }

        [Test]
        public void Centurion_RequiresHundredKills()
        {
            SaveSystem.Current.progress.totalKills = 99;
            _ach.Reevaluate();
            Assert.IsFalse(_ach.IsUnlocked("centurion"));

            SaveSystem.Current.progress.totalKills = 100;
            _ach.Reevaluate();
            Assert.IsTrue(_ach.IsUnlocked("centurion"));
        }

        [Test]
        public void RetroactiveUnlock_FromExistingProgress()
        {
            // Simulate a save imported with progress already past the threshold.
            SaveSystem.Current.progress.playerLevel = 12;
            _ach.Reevaluate();
            Assert.IsTrue(_ach.IsUnlocked("level_5"));
            Assert.IsTrue(_ach.IsUnlocked("level_10"));
        }

        [Test]
        public void Unlock_GrantsCurrencyEqualToReward()
        {
            int before = SaveSystem.Current.progress.softCurrency;
            SaveSystem.Current.progress.totalKills = 1;
            _ach.Reevaluate();
            int gained = SaveSystem.Current.progress.softCurrency - before;
            var firstBlood = AchievementCatalog.Find("first_blood");
            Assert.AreEqual(firstBlood.Reward, gained);
        }
    }
}
