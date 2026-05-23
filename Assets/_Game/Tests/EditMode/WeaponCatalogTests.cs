using NUnit.Framework;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class WeaponCatalogTests
    {
        [Test]
        public void Catalog_HasFiveWeapons()
        {
            Assert.AreEqual(5, WeaponCatalog.All.Length);
        }

        [Test]
        public void Find_UnknownId_ReturnsDefault()
        {
            var w = WeaponCatalog.Find("does_not_exist");
            Assert.AreEqual(WeaponCatalog.Default.id, w.id);
        }

        [Test]
        public void Find_KnownId_ReturnsExact()
        {
            var w = WeaponCatalog.Find("heavy_cannon");
            Assert.AreEqual("heavy_cannon", w.id);
            Assert.AreEqual(35, w.damage);
        }

        [Test]
        public void Catalog_AllIdsAreUnique()
        {
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var w in WeaponCatalog.All)
                Assert.IsTrue(ids.Add(w.id), $"duplicate weapon id: {w.id}");
        }
    }
}
