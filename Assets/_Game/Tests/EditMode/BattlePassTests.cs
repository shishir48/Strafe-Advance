using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class BattlePassTests
    {
        GameObject _currencyGO;
        GameObject _svcGO;
        BattlePassService _bp;
        CurrencyService _currency;

        [SetUp]
        public void SetUp()
        {
            SaveSystem.Reset();
            _currencyGO = new GameObject("CurrencyForBP");
            _currency = _currencyGO.AddComponent<CurrencyService>();
            _svcGO = new GameObject("BPTest");
            _bp = _svcGO.AddComponent<BattlePassService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_svcGO != null) Object.DestroyImmediate(_svcGO);
            if (_currencyGO != null) Object.DestroyImmediate(_currencyGO);
            SaveSystem.Reset();
        }

        [Test]
        public void FreshSave_TierZero()
        {
            Assert.AreEqual(0, _bp.Tier);
            Assert.AreEqual(0, _bp.Xp);
            Assert.IsFalse(_bp.PremiumOwned);
        }

        [Test]
        public void AddXp_CrossingThreshold_RaisesTier()
        {
            _bp.AddXp(500);
            Assert.AreEqual(1, _bp.Tier);
        }

        [Test]
        public void AddXp_MultipleThresholds_RaisesMultipleTiers()
        {
            _bp.AddXp(1500);
            Assert.AreEqual(3, _bp.Tier);
        }

        [Test]
        public void Claim_FreeLane_GrantsReward()
        {
            _bp.AddXp(500);
            int before = SaveSystem.Current.progress.softCurrency;
            bool claimed = _bp.Claim(1, premiumLane: false);
            Assert.IsTrue(claimed);
            Assert.AreEqual(before + 100, SaveSystem.Current.progress.softCurrency);
        }

        [Test]
        public void Claim_IsIdempotent()
        {
            _bp.AddXp(500);
            _bp.Claim(1, premiumLane: false);
            bool claimedAgain = _bp.Claim(1, premiumLane: false);
            Assert.IsFalse(claimedAgain);
        }

        [Test]
        public void Claim_PremiumLane_RequiresPremiumPass()
        {
            _bp.AddXp(500);
            bool claimedWithoutPremium = _bp.Claim(1, premiumLane: true);
            Assert.IsFalse(claimedWithoutPremium);

            _bp.UnlockPremium();
            bool claimedWithPremium = _bp.Claim(1, premiumLane: true);
            Assert.IsTrue(claimedWithPremium);
        }

        [Test]
        public void Claim_BeyondReachedTier_Refused()
        {
            _bp.AddXp(500); // tier 1
            Assert.IsFalse(_bp.Claim(2, premiumLane: false));
        }

        [Test]
        public void Claim_TierZeroOrOverMax_Refused()
        {
            _bp.AddXp(99999);
            Assert.IsFalse(_bp.Claim(0, premiumLane: false));
            Assert.IsFalse(_bp.Claim(BattlePassCatalog.MaxTier + 1, premiumLane: false));
        }

        [Test]
        public void WeaponReward_AddsToUnlockedWeapons()
        {
            _bp.AddXp(BattlePassCatalog.ByIndex(3).XpRequired);
            _bp.UnlockPremium();
            bool claimed = _bp.Claim(3, premiumLane: true); // tier 3 premium is rapid_smg
            Assert.IsTrue(claimed);
            CollectionAssert.Contains(SaveSystem.Current.progress.unlockedWeaponIds, "rapid_smg");
        }

        [Test]
        public void TierFromXp_MapsCorrectly()
        {
            Assert.AreEqual(0,  BattlePassCatalog.TierFromXp(0));
            Assert.AreEqual(0,  BattlePassCatalog.TierFromXp(499));
            Assert.AreEqual(1,  BattlePassCatalog.TierFromXp(500));
            Assert.AreEqual(5,  BattlePassCatalog.TierFromXp(2500));
            Assert.AreEqual(10, BattlePassCatalog.TierFromXp(999_999));
        }
    }
}
