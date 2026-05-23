using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// JSON + AES-256 save with atomic writes (temp + rename) and schema versioning.
    /// Senior-grade: never half-writes the player's save, survives mid-write crash,
    /// supports future migrations via <see cref="Migrate"/>.
    ///
    /// File layout: persistentDataPath/save.dat (base64-encoded { iv | ciphertext }).
    /// Backup at persistentDataPath/save.dat.bak (one rotation on successful save).
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName       = "save.dat";
        private const string BackupSuffix   = ".bak";
        private const string TempSuffix     = ".tmp";
        // Key derived from a project-scoped passphrase. Anyone with the binary can find this — encryption
        // is anti-tamper, not anti-cheat. For competitive stakes use a server-validated save.
        private const string Passphrase     = "strafe-advance-v1-do-not-rename";
        private const string Salt           = "sa-save-salt-2026";

        private static string PathMain   => Path.Combine(Application.persistentDataPath, FileName);
        private static string PathBackup => PathMain + BackupSuffix;
        private static string PathTemp   => PathMain + TempSuffix;

        private static readonly Lazy<byte[]> _key = new Lazy<byte[]>(DeriveKey);

        private static SaveData _cache;
        /// <summary>Current in-memory save. Auto-loads on first access.</summary>
        public static SaveData Current
        {
            get
            {
                if (_cache == null) _cache = Load();
                return _cache;
            }
        }

        /// <summary>Force-reload from disk (drops in-memory edits).</summary>
        public static SaveData Reload() { _cache = Load(); return _cache; }

        /// <summary>Persist <see cref="Current"/> to disk atomically.</summary>
        public static void Save()
        {
            if (_cache == null) return;
            _cache.profile.lastPlayedUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            WriteAtomic(_cache);
        }

        /// <summary>Wipe save file + reset in-memory state. Use for "delete profile" UX.</summary>
        public static void Reset()
        {
            if (File.Exists(PathMain))   File.Delete(PathMain);
            if (File.Exists(PathBackup)) File.Delete(PathBackup);
            _cache = new SaveData();
        }

        // ─── Internals ──────────────────────────────────────────────────────────

        private static SaveData Load()
        {
            var data = TryLoadFrom(PathMain) ?? TryLoadFrom(PathBackup);
            if (data == null) return new SaveData();
            return Migrate(data);
        }

        private static SaveData TryLoadFrom(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                byte[] blob = File.ReadAllBytes(path);
                string json = Decrypt(blob);
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Failed to load {path}: {e.Message}");
                return null;
            }
        }

        private static void WriteAtomic(SaveData data)
        {
            string json   = JsonUtility.ToJson(data);
            byte[] blob   = Encrypt(json);
            File.WriteAllBytes(PathTemp, blob);
            // Rotate previous main → backup, then promote temp → main. Atomic on POSIX, near-atomic on Windows.
            if (File.Exists(PathMain))
            {
                if (File.Exists(PathBackup)) File.Delete(PathBackup);
                File.Move(PathMain, PathBackup);
            }
            File.Move(PathTemp, PathMain);
        }

        private static SaveData Migrate(SaveData data)
        {
            // Add cases when bumping SaveData.CurrentSchemaVersion.
            while (data.schemaVersion < SaveData.CurrentSchemaVersion)
            {
                switch (data.schemaVersion)
                {
                    // case 1: MigrateV1ToV2(data); break;
                    default:
                        Debug.LogWarning($"[SaveSystem] No migration for schema v{data.schemaVersion}, resetting.");
                        return new SaveData();
                }
                data.schemaVersion++;
            }
            return data;
        }

        // ─── Crypto (AES-256-CBC, random IV per write) ──────────────────────────

        private static byte[] DeriveKey()
        {
            using var kdf = new Rfc2898DeriveBytes(Passphrase, Encoding.UTF8.GetBytes(Salt), 10000, HashAlgorithmName.SHA256);
            return kdf.GetBytes(32);
        }

        private static byte[] Encrypt(string plaintext)
        {
            using var aes = Aes.Create();
            aes.Key  = _key.Value;
            aes.Mode = CipherMode.CBC;
            aes.GenerateIV();
            using var enc = aes.CreateEncryptor();
            byte[] data   = Encoding.UTF8.GetBytes(plaintext);
            byte[] cipher = enc.TransformFinalBlock(data, 0, data.Length);
            byte[] result = new byte[aes.IV.Length + cipher.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipher, 0, result, aes.IV.Length, cipher.Length);
            return result;
        }

        private static string Decrypt(byte[] blob)
        {
            using var aes = Aes.Create();
            aes.Key  = _key.Value;
            aes.Mode = CipherMode.CBC;
            byte[] iv = new byte[16];
            Buffer.BlockCopy(blob, 0, iv, 0, 16);
            aes.IV = iv;
            using var dec = aes.CreateDecryptor();
            byte[] plain = dec.TransformFinalBlock(blob, 16, blob.Length - 16);
            return Encoding.UTF8.GetString(plain);
        }
    }
}
