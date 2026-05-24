using System;
using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Daily login streak + reward. UTC-day based so timezone-hopping cannot exploit it.
    /// First check-in per day grants currency on an escalating curve (capped at day 7).
    /// Missing a day resets the streak to 1.
    ///
    /// Designed to be called from <see cref="MainHubController"/> when the player lands on the
    /// menu (and also auto-fires on Awake so a player who skips the menu still gets credit).
    /// </summary>
    public class DailyLoginService : MonoBehaviour
    {
        public static DailyLoginService Instance { get; private set; }

        public int Streak       => SaveSystem.Current.progress.loginStreak;
        public int NextDayBonus => RewardForDay(Mathf.Max(1, Streak + 1));

        // Escalating reward curve, capped at day 7+. Tunable without code surgery.
        static readonly int[] Curve = { 50, 75, 100, 150, 200, 300, 500 };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            CheckIn(DateTime.UtcNow);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Idempotent within the same UTC day. Returns true if a reward was actually granted.</summary>
        public bool CheckIn(DateTime utcNow)
        {
            var p = SaveSystem.Current.progress;
            string today = utcNow.ToString("yyyy-MM-dd");

            if (p.lastLoginDateUtc == today) return false; // already collected today

            if (string.IsNullOrEmpty(p.lastLoginDateUtc))
            {
                p.loginStreak = 1;
            }
            else
            {
                DateTime last = ParseDateOrMin(p.lastLoginDateUtc);
                int dayGap = (utcNow.Date - last.Date).Days;
                p.loginStreak = dayGap == 1 ? p.loginStreak + 1 : 1;
            }

            p.lastLoginDateUtc = today;
            int reward = RewardForDay(p.loginStreak);
            if (CurrencyService.Instance != null) CurrencyService.Instance.Grant(reward);
            else { p.softCurrency += reward; }
            SaveSystem.Save();

            EventBus<DailyLoginCheckedIn>.Publish(new DailyLoginCheckedIn(p.loginStreak, reward));
            return true;
        }

        public static int RewardForDay(int day)
        {
            if (day <= 0) return 0;
            int idx = Mathf.Min(day - 1, Curve.Length - 1);
            return Curve[idx];
        }

        static DateTime ParseDateOrMin(string iso)
        {
            return DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var d)
                ? d
                : DateTime.MinValue;
        }
    }

    public readonly struct DailyLoginCheckedIn
    {
        public readonly int Streak;
        public readonly int Reward;
        public DailyLoginCheckedIn(int streak, int reward) { Streak = streak; Reward = reward; }
    }
}
