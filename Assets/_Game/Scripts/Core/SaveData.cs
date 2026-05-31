using System;
using System.Collections.Generic;

namespace StrafAdvance
{
    /// <summary>Versioned root save payload. Bump <see cref="SchemaVersion"/> + add a migration when fields change.</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public PlayerProfile profile = new PlayerProfile();
        public ProgressData progress = new ProgressData();
        public SettingsData settings = new SettingsData();
    }

    [Serializable]
    public class PlayerProfile
    {
        public string playerId   = Guid.NewGuid().ToString("N");
        public string displayName = "Pilot";
        public long createdUtcMs  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public long lastPlayedUtcMs;
        // Set true after the player finishes (or skips) the in-run tutorial.
        public bool tutorialCompleted;
        // First-impression nudges — fire once ever, then stay quiet.
        public bool firstBloodSeen;
        public bool firstDeathCoached;
    }

    [Serializable]
    public class ProgressData
    {
        public int   highestWaveCleared;
        public int   totalKills;
        public int   bestScore;
        public int   bestEndlessScore;
        public int   softCurrency;
        public int   hardCurrency;
        public int   playerLevel = 1;
        public int   playerXp;
        public List<string> unlockedSkins   = new List<string>();
        public List<int>    completedLevels = new List<int>();
        public List<string> unlockedPerkIds = new List<string>();
        public List<string> equippedPerkIds = new List<string>();
        public string equippedWeaponId = "blaster_default";
        public List<string> unlockedWeaponIds = new List<string> { "blaster_default" };

        // Daily login streak (P6.3). Stored as ISO date YYYY-MM-DD in UTC; empty = never logged in.
        public string lastLoginDateUtc = "";
        public int    loginStreak;

        // Achievement progress (P6.4). Just the set of completed IDs — counters live in their catalog.
        public List<string> unlockedAchievementIds = new List<string>();

        // Battle Pass (P6.5). XP accumulates per kill, tier crosses thresholds; rewards claimed
        // separately per lane so premium upgrade retroactively unlocks previously-passed tiers.
        public int  battlePassXp;
        public int  battlePassTier;             // 0..maxTier; 0 = pre-tier-1
        public bool premiumPassOwned;
        public List<int> claimedFreeTiers    = new List<int>();
        public List<int> claimedPremiumTiers = new List<int>();

        // Cosmetic skins (P6.7). Discrete per-slot equip — JsonUtility-friendly (no Dictionary).
        public string equippedPlayerSkinId = "";
        public string equippedBulletSkinId = "";
        public string equippedTrailSkinId  = "";
    }

    [Serializable]
    public class SettingsData
    {
        public float musicVolume = 0.7f;
        public float sfxVolume   = 1.0f;
        public float uiVolume    = 1.0f;
        public bool  vibration   = true;
        public bool  invertY     = false;
        public float aimSensitivity = 1.0f;
        public string language   = "en";
        public bool  colorblindMode = false;
    }
}
