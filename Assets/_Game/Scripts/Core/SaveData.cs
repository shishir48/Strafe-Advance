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
    }

    [Serializable]
    public class ProgressData
    {
        public int   highestWaveCleared;
        public int   totalKills;
        public int   bestScore;
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
