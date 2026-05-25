using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class CosmeticSkinTests
    {
        GameObject _currencyGO;
        CurrencyService _currency;

        [SetUp]
        public void SetUp()
        {
            SaveSystem.Reset();
            _currencyGO = new GameObject("CurrencyForSkins");
            _currency = _currencyGO.AddComponent<CurrencyService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_currencyGO != null) UnityEngine.Object.DestroyImmediate(_currencyGO);
            SaveSystem.Reset();
        }

        [Test]
        public void Catalog_ContainsAtLeastOneEntryPerSlot()
        {
            int player = 0, bullet = 0, trail = 0;
            foreach (var s in CosmeticCatalog.All)
            {
                if (s.Slot == CosmeticSlot.Player) player++;
                else if (s.Slot == CosmeticSlot.Bullet) bullet++;
                else if (s.Slot == CosmeticSlot.Trail) trail++;
            }
            Assert.Greater(player, 0);
            Assert.Greater(bullet, 0);
            Assert.Greater(trail, 0);
        }

        [Test]
        public void Catalog_HasFreeStarterPerSlot()
        {
            foreach (CosmeticSlot slot in System.Enum.GetValues(typeof(CosmeticSlot)))
            {
                bool hasFree = false;
                foreach (var s in CosmeticCatalog.WhereSlot(slot))
                    if (s.Price == 0) { hasFree = true; break; }
                Assert.IsTrue(hasFree, $"Slot {slot} has no free starter skin.");
            }
        }

        [Test]
        public void Find_ReturnsNullForUnknownId()
        {
            Assert.IsNull(CosmeticCatalog.Find("__nope__"));
        }

        [Test]
        public void Purchase_DeductsCurrencyAndUnlocks()
        {
            var paid = FindFirstPaid();
            SaveSystem.Current.progress.softCurrency = paid.Price + 50;

            Assert.IsTrue(_currency.TrySpend(paid.Price));
            SaveSystem.Current.progress.unlockedSkins.Add(paid.Id);
            SaveSystem.Save();

            var fresh = SaveSystem.Reload();
            Assert.Contains(paid.Id, fresh.progress.unlockedSkins);
            Assert.AreEqual(50, fresh.progress.softCurrency);
        }

        [Test]
        public void EquipPersists_PerSlot()
        {
            SaveSystem.Current.progress.equippedPlayerSkinId = "player_crimson";
            SaveSystem.Current.progress.equippedBulletSkinId = "bullet_amber";
            SaveSystem.Save();
            var fresh = SaveSystem.Reload();
            Assert.AreEqual("player_crimson", fresh.progress.equippedPlayerSkinId);
            Assert.AreEqual("bullet_amber",   fresh.progress.equippedBulletSkinId);
        }

        static CosmeticSkin FindFirstPaid()
        {
            foreach (var s in CosmeticCatalog.All) if (s.Price > 0) return s;
            return null;
        }
    }
}
