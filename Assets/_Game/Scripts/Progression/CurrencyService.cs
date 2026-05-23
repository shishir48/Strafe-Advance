using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Awards soft currency on kills + persists via <see cref="SaveSystem"/>.
    /// Drop amounts table-driven so designers can tune without code touch.
    /// Publishes <see cref="CurrencyEarned"/> for HUD popups (Phase 4 polish).
    /// </summary>
    public class CurrencyService : MonoBehaviour
    {
        public static CurrencyService Instance { get; private set; }

        [SerializeField] private int gruntDrop    = 5;
        [SerializeField] private int flankerDrop  = 10;
        [SerializeField] private int eliteDrop    = 25;
        [SerializeField] private int chargerDrop  = 8;
        [SerializeField] private int sniperDrop   = 20;
        [SerializeField] private int shieldedDrop = 18;
        [SerializeField] private int splitterDrop = 15;
        [SerializeField] private int droneDrop    = 3;
        [SerializeField] private int miniBossDrop = 200;

        /// <summary>Soft currency earned this run (resets when run ends).</summary>
        public int EarnedThisRun { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            EventBus<EnemyKilled>.Subscribe(OnKill);
            EventBus<GameStateChanged>.Subscribe(OnStateChanged);
        }

        void OnDestroy()
        {
            EventBus<EnemyKilled>.Unsubscribe(OnKill);
            EventBus<GameStateChanged>.Unsubscribe(OnStateChanged);
            if (Instance == this) Instance = null;
        }

        void OnKill(EnemyKilled k)
        {
            int amount = DropFor(k.Type);
            if (amount <= 0) return;
            var p = SaveSystem.Current.progress;
            p.softCurrency += amount;
            EarnedThisRun  += amount;
            EventBus<CurrencyEarned>.Publish(new CurrencyEarned(amount, p.softCurrency));
        }

        void OnStateChanged(GameStateChanged s)
        {
            // Reset run counter at the start of a new run.
            if (s.Current == GameState.Playing && s.Previous != GameState.Playing) EarnedThisRun = 0;

            // Persist + show run summary on win or game-over.
            if (s.Current == GameState.GameOver || s.Current == GameState.LevelComplete)
            {
                SaveSystem.Save();
                RunSummaryPanel.Instance?.Show(s.Current == GameState.LevelComplete);
            }
        }

        public int DropFor(EnemyType t) => t switch
        {
            EnemyType.Grunt    => gruntDrop,
            EnemyType.Flanker  => flankerDrop,
            EnemyType.Elite    => eliteDrop,
            EnemyType.Charger  => chargerDrop,
            EnemyType.Sniper   => sniperDrop,
            EnemyType.Shielded => shieldedDrop,
            EnemyType.Splitter => splitterDrop,
            EnemyType.Drone    => droneDrop,
            EnemyType.MiniBoss => miniBossDrop,
            _ => 0,
        };
    }

    public readonly struct CurrencyEarned
    {
        public readonly int Amount;
        public readonly int Total;
        public CurrencyEarned(int amount, int total) { Amount = amount; Total = total; }
    }
}
