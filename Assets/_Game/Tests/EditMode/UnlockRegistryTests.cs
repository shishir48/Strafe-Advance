using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class UnlockRegistryTests
    {
        private UnlockRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteAll();
            _registry = new UnlockRegistry();
        }

        [TearDown]
        public void TearDown() => PlayerPrefs.DeleteAll();

        [Test]
        public void IsUnlocked_ReturnsFalse_ForUnknownProduct()
        {
            Assert.IsFalse(_registry.IsUnlocked("level_pack_2"));
        }

        [Test]
        public void Unlock_MakesProductUnlocked()
        {
            _registry.Unlock("level_pack_2");
            Assert.IsTrue(_registry.IsUnlocked("level_pack_2"));
        }

        [Test]
        public void Unlock_Persists_AcrossInstances()
        {
            _registry.Unlock("level_pack_2");
            var registry2 = new UnlockRegistry();
            Assert.IsTrue(registry2.IsUnlocked("level_pack_2"));
        }

        [Test]
        public void IsUnlocked_ReturnsFalse_WhenOnlyOtherProductUnlocked()
        {
            _registry.Unlock("level_pack_2");
            Assert.IsFalse(_registry.IsUnlocked("level_pack_3"));
        }
    }
}
