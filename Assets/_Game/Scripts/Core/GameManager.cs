using System;
using System.Collections;
using UnityEngine;

namespace StrafAdvance
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameState State => _fsm.Current;

        /// <summary>Legacy event — prefer <c>EventBus&lt;GameStateChanged&gt;.Subscribe</c>.</summary>
        public event Action<GameState> OnStateChanged;

        private readonly StateMachine<GameState> _fsm = BuildFsm();

        static StateMachine<GameState> BuildFsm()
        {
            var fsm = new StateMachine<GameState>(GameState.Menu, strict: true);
            // Allowed transitions — anything else is rejected by TryTransition.
            fsm.Allow(GameState.Menu,          GameState.Playing);
            fsm.Allow(GameState.Playing,       GameState.BossFight);
            fsm.Allow(GameState.Playing,       GameState.GameOver);
            fsm.Allow(GameState.Playing,       GameState.LevelComplete);
            fsm.Allow(GameState.BossFight,     GameState.GameOver);
            fsm.Allow(GameState.BossFight,     GameState.LevelComplete);
            fsm.Allow(GameState.GameOver,      GameState.Menu);
            fsm.Allow(GameState.LevelComplete, GameState.Menu);
            return fsm;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            EventBus<GameStateChanged>.Clear();
            EventBus<EnemyKilled>.Clear();
            EventBus<EnemyDamaged>.Clear();
            EventBus<WaveStarted>.Clear();
            EventBus<PlayerDamaged>.Clear();
            EventBus<DodgePerformed>.Clear();
            EventBus<ComboChanged>.Clear();
            EventBus<HitstopRequest>.Clear();
            EventBus<ShakeRequest>.Clear();
            EventBus<XpGained>.Clear();
            EventBus<PlayerLeveledUp>.Clear();
            EventBus<PerkUnlocked>.Clear();
            EventBus<ShieldHit>.Clear();
            EventBus<KillCamRequest>.Clear();
            EventBus<BossPhaseChanged>.Clear();
            EventBus<CurrencyEarned>.Clear();
            EventBus<DailyLoginCheckedIn>.Clear();
            EventBus<AchievementUnlocked>.Clear();
            EventBus<BattlePassTierReached>.Clear();
            EventBus<LanguageChanged>.Clear();
            EventBus<SkinEquipped>.Clear();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Initialize localization once per process. Auto-detects + persists on first run.
            Loc.Init();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public int Score { get; private set; }
        public int KillCount { get; private set; }

        private bool _runLoopWired;

        void Start()
        {
            // Stay in Menu — MainHubController/MainHub UX handle the Play button.
            // If MainHub never spawns (e.g. legacy GameScene) we still auto-start after a short delay so the scene is playable.
            StartCoroutine(LegacyFallbackAutoStart());
        }

        public void AddKill()
        {
            KillCount++;
            // Base award; ComboTracker adds multiplier-scaled bonus via AddScore.
            Score += 100;
        }

        public void AddScore(int amount) => Score = Mathf.Max(0, Score + amount);

        IEnumerator LegacyFallbackAutoStart()
        {
            // Give MainHub a frame to register. If it's missing, fall back to auto-start so the project still boots into a run.
            yield return null;
            if (MainHubController.Instance != null) yield break;
            yield return new WaitForSeconds(0.5f);
            if (MainHubController.Instance == null && State == GameState.Menu)
            {
                Debug.LogWarning("[GameManager] No MainHubController found — auto-starting Level 1.");
                BeginRunFromMenu();
            }
        }

        /// <summary>Transition from Menu → Playing. Loads Level 1, wires the game loop, starts spawning.</summary>
        public void BeginRunFromMenu()
        {
            if (State != GameState.Menu) return;

            var waveSpawner      = FindAnyObjectByType<WaveSpawner>();
            var corridorScroller = FindAnyObjectByType<CorridorScroller>();
            var l1 = AssetLoader.Load<LevelConfig>("Level1");

            if (waveSpawner == null || corridorScroller == null || l1 == null)
            {
                Debug.LogWarning("[GameManager] Missing required components for BeginRunFromMenu.");
                return;
            }

            Score = 0;
            KillCount = 0;

            corridorScroller.Initialize(l1.worldScrollSpeed);
            waveSpawner.LoadLevel(l1);
            SetState(GameState.Playing);
            waveSpawner.StartSpawning();

            if (_runLoopWired) return;
            _runLoopWired = true;

            // Wire game loop events (idempotent guard above prevents double-subscription on retry).
            var playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.OnDeath += () => SetState(GameState.GameOver);

            waveSpawner.OnAllWavesComplete += () =>
            {
                SetState(GameState.BossFight);
                corridorScroller.Stop();
                var bossPrefab = AssetLoader.Load<GameObject>("Boss");
                var bossSource = l1.bossPrefab != null ? l1.bossPrefab : bossPrefab;
                if (bossSource != null)
                {
                    var boss = Instantiate(bossSource, new Vector3(0, 0, 12f), Quaternion.identity);
                    var bossCtrl = boss.GetComponent<BossController>();
                    if (bossCtrl != null)
                    {
                        var bossConf = ScriptableObject.CreateInstance<EnemyConfig>();
                        bossConf.maxHp = 200; bossConf.moveSpeed = 1.5f;
                        bossConf.contactDamage = 20; bossConf.fireRate = 3f;
                        bossCtrl.Initialize(bossConf);
                        bossCtrl.OnDeath += e =>
                        {
                            EventBus<KillCamRequest>.Publish(new KillCamRequest(e.transform.position));
                            EventBus<ShakeRequest>.Publish(new ShakeRequest(1f));
                            EventBus<HitstopRequest>.Publish(new HitstopRequest(0.35f));
                            SetState(GameState.LevelComplete);
                        };
                    }
                }
                else
                    SetState(GameState.LevelComplete); // no boss — skip to complete
            };
        }

        public void SetState(GameState state)
        {
            var prev = _fsm.Current;
            if (!_fsm.TryTransition(state))
            {
                if (prev != state) Debug.LogWarning($"[GameManager] Illegal transition {prev} → {state}, ignored.");
                return;
            }
            OnStateChanged?.Invoke(state);
            EventBus<GameStateChanged>.Publish(new GameStateChanged(prev, state));
            // RunSummaryPanel listens for GameOver/LevelComplete and presents Restart/Menu — no extra overlay needed.
        }
    }
}
