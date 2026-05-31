using System.IO;
using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    /// <summary>
    /// Sprint 7.4 — corruption fuzz. A truncated write, a bad sector, or a tampered file must never
    /// throw out of SaveSystem; it must degrade to a fresh default save. We blast random bytes of
    /// varying lengths (including degenerate &lt;16-byte blobs that break the AES IV read) into both
    /// the main and backup files, then assert Reload() returns a valid default.
    /// </summary>
    public class SaveSystemFuzzTests
    {
        static string PathMain   => Path.Combine(Application.persistentDataPath, "save.dat");
        static string PathBackup => PathMain + ".bak";

        [SetUp]
        public void SetUp() => SaveSystem.Reset();

        [TearDown]
        public void TearDown() => SaveSystem.Reset();

        [Test]
        public void RandomBytes_ReloadReturnsValidDefault([Values(0, 1, 7, 15, 16, 17, 64, 257)] int length)
        {
            var rng = new System.Random(length * 7919);
            for (int trial = 0; trial < 16; trial++)
            {
                byte[] garbage = new byte[length];
                rng.NextBytes(garbage);
                File.WriteAllBytes(PathMain, garbage);
                // Also corrupt the backup so Reload can't silently recover from a stale good copy.
                File.WriteAllBytes(PathBackup, garbage);

                SaveData data = SaveSystem.Reload();

                Assert.IsNotNull(data, $"Reload returned null for {length}-byte garbage (trial {trial}).");
                Assert.AreEqual(SaveData.CurrentSchemaVersion, data.schemaVersion,
                    "Corrupt save should fall back to a current-schema default.");
                Assert.IsNotNull(data.profile, "Default save must have a profile.");
                Assert.IsFalse(string.IsNullOrEmpty(data.profile.playerId), "Default profile must have a playerId.");
                Assert.AreEqual(0, data.progress.bestScore, "Corrupt save must not surface stale progress.");
            }
        }

        [Test]
        public void EmptyFile_ReloadReturnsDefault()
        {
            File.WriteAllBytes(PathMain, new byte[0]);
            SaveData data = SaveSystem.Reload();
            Assert.IsNotNull(data);
            Assert.AreEqual(SaveData.CurrentSchemaVersion, data.schemaVersion);
        }
    }
}
