using System.Collections.Generic;
using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Subscribes to gameplay events and re-evaluates locked achievements. Unlocks grant
    /// soft currency via <see cref="CurrencyService"/> and publish <see cref="AchievementUnlocked"/>
    /// for any HUD popup / RunSummary listener to render.
    ///
    /// Evaluation is intentionally O(locked × events) — at &lt; 20 achievements + cold-path triggers
    /// (kill, level-up, wave, state-change, daily-login) this is well under any frame budget.
    /// </summary>
    public class AchievementService : MonoBehaviour
    {
        public static AchievementService Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            EventBus<EnemyKilled>.Subscribe(_ => Reevaluate());
            EventBus<PlayerLeveledUp>.Subscribe(_ => Reevaluate());
            EventBus<WaveStarted>.Subscribe(_ => Reevaluate());
            EventBus<GameStateChanged>.Subscribe(OnState);
            EventBus<DailyLoginCheckedIn>.Subscribe(_ => Reevaluate());

            // Catch any retroactively-eligible achievements (e.g. save imported, counter already past threshold).
            Reevaluate();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void OnState(GameStateChanged s)
        {
            if (s.Current == GameState.LevelComplete) Reevaluate();
        }

        public void Reevaluate()
        {
            var save = SaveSystem.Current;
            var unlocked = save.progress.unlockedAchievementIds;
            bool changed = false;
            List<Achievement> newlyUnlocked = null;

            foreach (var a in AchievementCatalog.All)
            {
                if (unlocked.Contains(a.Id)) continue;
                if (a.IsComplete == null || !a.IsComplete(save)) continue;
                unlocked.Add(a.Id);
                changed = true;
                if (CurrencyService.Instance != null) CurrencyService.Instance.Grant(a.Reward);
                else save.progress.softCurrency += a.Reward;
                (newlyUnlocked ??= new List<Achievement>()).Add(a);
            }

            if (changed) SaveSystem.Save();
            if (newlyUnlocked != null)
                foreach (var a in newlyUnlocked)
                    EventBus<AchievementUnlocked>.Publish(new AchievementUnlocked(a.Id, a.DisplayName, a.Reward));
        }

        public bool IsUnlocked(string id) => SaveSystem.Current.progress.unlockedAchievementIds.Contains(id);
    }

    public readonly struct AchievementUnlocked
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly int    Reward;
        public AchievementUnlocked(string id, string name, int reward) { Id = id; DisplayName = name; Reward = reward; }
    }
}
