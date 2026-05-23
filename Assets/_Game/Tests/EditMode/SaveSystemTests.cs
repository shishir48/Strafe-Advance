using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class SaveSystemTests
    {
        [SetUp]
        public void SetUp() => SaveSystem.Reset();

        [TearDown]
        public void TearDown() => SaveSystem.Reset();

        [Test]
        public void DefaultSave_HasSchemaVersionAndProfile()
        {
            var s = SaveSystem.Current;
            Assert.AreEqual(SaveData.CurrentSchemaVersion, s.schemaVersion);
            Assert.IsNotNull(s.profile);
            Assert.IsFalse(string.IsNullOrEmpty(s.profile.playerId));
        }

        [Test]
        public void SaveThenReload_PersistsMutations()
        {
            SaveSystem.Current.progress.bestScore     = 9001;
            SaveSystem.Current.progress.softCurrency  = 250;
            SaveSystem.Current.settings.musicVolume   = 0.3f;
            SaveSystem.Save();

            var fresh = SaveSystem.Reload();
            Assert.AreEqual(9001, fresh.progress.bestScore);
            Assert.AreEqual(250,  fresh.progress.softCurrency);
            Assert.AreEqual(0.3f, fresh.settings.musicVolume, 0.001f);
        }

        [Test]
        public void Reset_ClearsExistingSave()
        {
            SaveSystem.Current.progress.bestScore = 42;
            SaveSystem.Save();
            SaveSystem.Reset();
            Assert.AreEqual(0, SaveSystem.Current.progress.bestScore);
        }

        [Test]
        public void EncryptedFileIsNotPlaintextJson()
        {
            SaveSystem.Current.profile.displayName = "TopSecretPilot";
            SaveSystem.Save();
            string path = System.IO.Path.Combine(Application.persistentDataPath, "save.dat");
            byte[] bytes = System.IO.File.ReadAllBytes(path);
            string raw  = System.Text.Encoding.UTF8.GetString(bytes);
            Assert.IsFalse(raw.Contains("TopSecretPilot"), "Save file should be encrypted, not plaintext.");
        }
    }
}
