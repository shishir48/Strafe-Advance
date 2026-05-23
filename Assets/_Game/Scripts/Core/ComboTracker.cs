using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Kill-streak combo tracker. Multiplier tiers: ×1 (default) → ×2 (5 kills) → ×4 (10) → ×8 (20).
    /// Combo resets on miss timeout (2s without a kill) or on player damage.
    /// Publishes <see cref="ComboChanged"/> for HUD widgets.
    /// </summary>
    public class ComboTracker : MonoBehaviour
    {
        public static ComboTracker Instance { get; private set; }

        [SerializeField] private float resetSeconds = 2.0f;
        [SerializeField] private int[] tierThresholds = { 0, 5, 10, 20 };
        [SerializeField] private int[] tierMultipliers = { 1, 2, 4, 8 };

        public int Streak    { get; private set; }
        public int Multiplier { get; private set; } = 1;

        private float _lastKillTime;
        private int _lastTier = 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            EventBus<EnemyKilled>.Subscribe(OnKill);
            EventBus<PlayerDamaged>.Subscribe(OnHit);
        }

        void OnDestroy()
        {
            EventBus<EnemyKilled>.Unsubscribe(OnKill);
            EventBus<PlayerDamaged>.Unsubscribe(OnHit);
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Streak > 0 && Time.time - _lastKillTime > resetSeconds) Reset();
        }

        void OnKill(EnemyKilled k)
        {
            Streak++;
            _lastKillTime = Time.time;
            RecomputeMultiplier();
            GameManager.Instance?.AddScore(k.ScoreReward * Multiplier);
        }

        void OnHit(PlayerDamaged _) => Reset();

        void Reset()
        {
            if (Streak == 0 && Multiplier == 1) return;
            Streak = 0;
            Multiplier = 1;
            _lastTier = 0;
            EventBus<ComboChanged>.Publish(new ComboChanged(0, 1));
        }

        void RecomputeMultiplier()
        {
            int tier = 0;
            for (int i = tierThresholds.Length - 1; i >= 0; i--)
                if (Streak >= tierThresholds[i]) { tier = i; break; }
            Multiplier = tierMultipliers[tier];
            if (tier != _lastTier) { _lastTier = tier; }
            EventBus<ComboChanged>.Publish(new ComboChanged(Streak, Multiplier));
        }
    }

    public readonly struct ComboChanged
    {
        public readonly int Streak;
        public readonly int Multiplier;
        public ComboChanged(int streak, int multiplier) { Streak = streak; Multiplier = multiplier; }
    }
}
