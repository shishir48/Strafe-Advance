using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    /// <summary>
    /// Verifies the shop-spend → unlock → equip pipeline at the data layer.
    /// Mirrors what <see cref="ShopController.OnWeaponAction"/> does without instantiating UI.
    /// </summary>
    public class WeaponShopTests
    {
        GameObject _go;
        CurrencyService _svc;

        [SetUp]
        public void SetUp()
        {
            SaveSystem.Reset();
            _go = new GameObject("WeaponShopTest");
            _svc = _go.AddComponent<CurrencyService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            SaveSystem.Reset();
        }

        static WeaponConfig PaidWeapon() =>
            System.Array.Find(WeaponCatalog.All, w => w.price > 0);

        [Test]
        public void DefaultSave_OwnsOnlyStarterBlaster()
        {
            var owned = SaveSystem.Current.progress.unlockedWeaponIds;
            Assert.AreEqual(1, owned.Count);
            Assert.Contains("blaster_default", owned);
            Assert.AreEqual("blaster_default", SaveSystem.Current.progress.equippedWeaponId);
        }

        [Test]
        public void Purchase_DeductsCurrencyAndUnlocks()
        {
            var w = PaidWeapon();
            SaveSystem.Current.progress.softCurrency = w.price + 100;

            Assert.IsTrue(_svc.TrySpend(w.price));
            SaveSystem.Current.progress.unlockedWeaponIds.Add(w.id);
            SaveSystem.Save();

            var fresh = SaveSystem.Reload();
            Assert.Contains(w.id, fresh.progress.unlockedWeaponIds);
            Assert.AreEqual(100, fresh.progress.softCurrency);
        }

        [Test]
        public void Purchase_FailsWhenBroke()
        {
            var w = PaidWeapon();
            SaveSystem.Current.progress.softCurrency = w.price - 1;

            Assert.IsFalse(_svc.TrySpend(w.price));
            Assert.IsFalse(SaveSystem.Current.progress.unlockedWeaponIds.Contains(w.id));
        }

        [Test]
        public void Equip_UpdatesPersistedEquippedWeapon()
        {
            var w = PaidWeapon();
            SaveSystem.Current.progress.unlockedWeaponIds.Add(w.id);
            SaveSystem.Current.progress.equippedWeaponId = w.id;
            SaveSystem.Save();

            var fresh = SaveSystem.Reload();
            Assert.AreEqual(w.id, fresh.progress.equippedWeaponId);
        }

        [Test]
        public void WeaponCatalog_HasPriceFieldsForAllPaidWeapons()
        {
            int paid = 0;
            foreach (var w in WeaponCatalog.All)
                if (w.id != "blaster_default")
                {
                    Assert.Greater(w.price, 0, $"Paid weapon {w.id} should have price > 0.");
                    paid++;
                }
            Assert.Greater(paid, 0, "At least one paid weapon expected in the catalog.");
        }
    }
}
