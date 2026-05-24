using System;

namespace StrafAdvance
{
    /// <summary>Single achievement entry — pure data + a predicate against <see cref="SaveData"/>.</summary>
    public class Achievement
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public int    Reward;
        public Func<SaveData, bool> IsComplete;
    }

    /// <summary>Hard-coded achievement catalogue. Will move to ScriptableObject when count > 20.</summary>
    public static class AchievementCatalog
    {
        public static readonly Achievement[] All =
        {
            new Achievement { Id = "first_blood",  DisplayName = "First Blood",  Description = "Kill 1 enemy.",          Reward = 25,  IsComplete = s => s.progress.totalKills >= 1 },
            new Achievement { Id = "centurion",    DisplayName = "Centurion",    Description = "Kill 100 enemies.",      Reward = 200, IsComplete = s => s.progress.totalKills >= 100 },
            new Achievement { Id = "slayer",       DisplayName = "Slayer",       Description = "Kill 1,000 enemies.",    Reward = 1500, IsComplete = s => s.progress.totalKills >= 1000 },
            new Achievement { Id = "level_5",      DisplayName = "Veteran",      Description = "Reach player level 5.",  Reward = 150, IsComplete = s => s.progress.playerLevel >= 5 },
            new Achievement { Id = "level_10",     DisplayName = "Hardened",     Description = "Reach player level 10.", Reward = 500, IsComplete = s => s.progress.playerLevel >= 10 },
            new Achievement { Id = "first_win",    DisplayName = "First Win",    Description = "Complete a level.",      Reward = 250, IsComplete = s => s.progress.completedLevels.Count >= 1 },
            new Achievement { Id = "wave_10",      DisplayName = "Survivor",     Description = "Reach wave 10.",         Reward = 150, IsComplete = s => s.progress.highestWaveCleared >= 9 },
            new Achievement { Id = "streak_7",     DisplayName = "Daily Driver", Description = "7-day login streak.",    Reward = 750, IsComplete = s => s.progress.loginStreak >= 7 },
        };

        public static Achievement Find(string id)
        {
            foreach (var a in All) if (a.Id == id) return a;
            return null;
        }
    }
}
