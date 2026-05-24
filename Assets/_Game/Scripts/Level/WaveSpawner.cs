using System;
using System.Collections;
using UnityEngine;

namespace StrafAdvance
{
    public class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private Transform spawnParent;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private EnemyConfig gruntConfig;
        [SerializeField] private EnemyConfig flankerConfig;
        [SerializeField] private EnemyConfig eliteConfig;
        [SerializeField] private GruntEnemy gruntPrefab;
        [SerializeField] private FlankerEnemy flankerPrefab;
        [SerializeField] private EliteEnemy elitePrefab;
        [SerializeField] private ChargerEnemy chargerPrefab;
        [SerializeField] private EnemyConfig chargerConfig;
        [SerializeField] private SniperEnemy sniperPrefab;
        [SerializeField] private EnemyConfig sniperConfig;
        [SerializeField] private ShieldedEnemy shieldedPrefab;
        [SerializeField] private EnemyConfig shieldedConfig;
        [SerializeField] private SplitterEnemy splitterPrefab;
        [SerializeField] private EnemyConfig splitterConfig;
        [SerializeField] private DroneEnemy dronePrefab;
        [SerializeField] private EnemyConfig droneConfig;
        [SerializeField] private MiniBossEnemy miniBossPrefab;
        [SerializeField] private EnemyConfig miniBossConfig;
        [SerializeField] private Bullet enemyBulletPrefab;

        private ObjectPool<Bullet> _enemyBulletPool;
        private LevelConfig _level;
        private int _enemiesAlive;
        private bool _spawning;

        public int CurrentWaveIndex { get; private set; }
        public event Action OnAllWavesComplete;
        public event Action<int> OnWaveStarted;

        void Awake()
        {
            if (enemyBulletPrefab != null)
                _enemyBulletPool = new ObjectPool<Bullet>(enemyBulletPrefab, 30, spawnParent);
        }

        public void LoadLevel(LevelConfig level)
        {
            _level = level;
            CurrentWaveIndex = 0;
            _enemiesAlive = 0;
        }

        public void StartSpawning() => StartCoroutine(SpawnWave(CurrentWaveIndex));

        IEnumerator SpawnWave(int index)
        {
            _spawning = true;
            WaveConfig wave = _level.waves[index];
            OnWaveStarted?.Invoke(index);
            EventBus<WaveStarted>.Publish(new WaveStarted(index, _level.waves.Length));

            if (wave.UsesEntries)
            {
                int active = wave.entries.Length;
                foreach (var entry in wave.entries)
                    StartCoroutine(SpawnEntry(entry, () => active--));
                while (active > 0) yield return null;
            }
            else
            {
                for (int i = 0; i < wave.count; i++)
                {
                    if (TrySpawnEnemy(wave.enemyType)) ReportEnemySpawned(1);
                    if (wave.spawnInterval > 0f)
                        yield return new WaitForSeconds(wave.spawnInterval);
                }
            }

            _spawning = false;
            // All enemies may have died/escaped during spawning — advance if none alive
            if (_enemiesAlive <= 0)
            {
                CurrentWaveIndex++;
                if (CurrentWaveIndex >= _level.waves.Length)
                    OnAllWavesComplete?.Invoke();
                else
                    StartCoroutine(SpawnWave(CurrentWaveIndex));
            }
        }

        IEnumerator SpawnEntry(WaveEntry entry, Action onDone)
        {
            if (entry.startDelay > 0f) yield return new WaitForSeconds(entry.startDelay);
            for (int i = 0; i < entry.count; i++)
            {
                if (TrySpawnEnemy(entry.enemyType)) ReportEnemySpawned(1);
                if (entry.spawnInterval > 0f)
                    yield return new WaitForSeconds(entry.spawnInterval);
            }
            onDone?.Invoke();
        }

        /// <summary>
        /// Atomic spawn: Instantiates + wires the requested enemy type. Returns false (and logs an
        /// error once per type per scene) if the required prefab is missing in the inspector — the
        /// caller must skip <see cref="ReportEnemySpawned"/> so the wave doesn't dead-lock waiting
        /// for kills that will never be reported.
        /// </summary>
        bool TrySpawnEnemy(EnemyType type)
        {
            float spawnX = UnityEngine.Random.Range(-2.5f, 2.5f);
            Vector3 spawnPos = new Vector3(spawnX, 0f, 25f);
            float diff = DifficultyService.Current;

            EnemyConfig ScaledOr(EnemyConfig c) =>
                c != null ? c.WithDifficulty(diff) : null;

            switch (type)
            {
                case EnemyType.Grunt:
                {
                    if (gruntPrefab == null) { WarnMissingPrefab(type); return false; }
                    GruntEnemy e = Instantiate(gruntPrefab, spawnPos, Quaternion.identity, spawnParent);
                    e.Initialize(ScaledOr(gruntConfig));
                    e.InitGrunt(playerTransform, _enemyBulletPool);
                    WireDeathAndEscape(e, EnemyType.Grunt, 100);
                    return true;
                }
                case EnemyType.Flanker:
                {
                    if (flankerPrefab == null) { WarnMissingPrefab(type); return false; }
                    FlankerEnemy e = Instantiate(flankerPrefab, spawnPos, Quaternion.identity, spawnParent);
                    e.Initialize(ScaledOr(flankerConfig != null ? flankerConfig : gruntConfig));
                    e.InitFlanker(playerTransform);
                    WireDeathAndEscape(e, EnemyType.Flanker, 200);
                    return true;
                }
                case EnemyType.Elite:
                {
                    if (elitePrefab == null) { WarnMissingPrefab(type); return false; }
                    EliteEnemy e = Instantiate(elitePrefab, spawnPos, Quaternion.identity, spawnParent);
                    e.Initialize(ScaledOr(eliteConfig != null ? eliteConfig : gruntConfig));
                    e.InitElite(playerTransform);
                    WireDeathAndEscape(e, EnemyType.Elite, 500);
                    return true;
                }
                case EnemyType.Charger:
                {
                    if (chargerPrefab == null) { WarnMissingPrefab(type); return false; }
                    ChargerEnemy e = Instantiate(chargerPrefab, spawnPos, Quaternion.identity, spawnParent);
                    e.Initialize(ScaledOr(chargerConfig != null ? chargerConfig : gruntConfig));
                    e.InitCharger(playerTransform);
                    WireDeathAndEscape(e, EnemyType.Charger, 250);
                    return true;
                }
                case EnemyType.Sniper:
                {
                    if (sniperPrefab == null) { WarnMissingPrefab(type); return false; }
                    SniperEnemy e = Instantiate(sniperPrefab, spawnPos, Quaternion.identity, spawnParent);
                    e.Initialize(ScaledOr(sniperConfig != null ? sniperConfig : gruntConfig));
                    e.InitSniper(playerTransform, _enemyBulletPool);
                    WireDeathAndEscape(e, EnemyType.Sniper, 400);
                    return true;
                }
                case EnemyType.Shielded:
                {
                    if (shieldedPrefab == null) { WarnMissingPrefab(type); return false; }
                    ShieldedEnemy e = Instantiate(shieldedPrefab, spawnPos, Quaternion.identity, spawnParent);
                    e.Initialize(ScaledOr(shieldedConfig != null ? shieldedConfig : gruntConfig));
                    e.InitShielded(playerTransform);
                    WireDeathAndEscape(e, EnemyType.Shielded, 350);
                    return true;
                }
                case EnemyType.Splitter:
                {
                    if (splitterPrefab == null) { WarnMissingPrefab(type); return false; }
                    SplitterEnemy e = Instantiate(splitterPrefab, spawnPos, Quaternion.identity, spawnParent);
                    e.Initialize(ScaledOr(splitterConfig != null ? splitterConfig : gruntConfig));
                    e.InitSplitter(gruntPrefab, gruntConfig, playerTransform, _enemyBulletPool, spawnParent);
                    WireDeathAndEscape(e, EnemyType.Splitter, 300);
                    return true;
                }
                case EnemyType.Drone:
                {
                    if (dronePrefab == null) { WarnMissingPrefab(type); return false; }
                    DroneEnemy e = Instantiate(dronePrefab, spawnPos, Quaternion.identity, spawnParent);
                    e.Initialize(ScaledOr(droneConfig != null ? droneConfig : gruntConfig));
                    e.InitDrone(playerTransform);
                    WireDeathAndEscape(e, EnemyType.Drone, 120);
                    return true;
                }
                case EnemyType.MiniBoss:
                {
                    if (miniBossPrefab == null) { WarnMissingPrefab(type); return false; }
                    MiniBossEnemy mb = Instantiate(miniBossPrefab, spawnPos, Quaternion.identity, spawnParent);
                    mb.Initialize(ScaledOr(miniBossConfig != null ? miniBossConfig : eliteConfig));
                    mb.InitMiniBoss(playerTransform, _enemyBulletPool);
                    mb.OnDeath += e =>
                    {
                        ReportKill();
                        GameManager.Instance?.AddKill();
                        EventBus<EnemyKilled>.Publish(new EnemyKilled(EnemyType.MiniBoss, 1500));
                        EventBus<ShakeRequest>.Publish(new ShakeRequest(0.9f));
                        EventBus<HitstopRequest>.Publish(new HitstopRequest(0.2f));
                        EventBus<KillCamRequest>.Publish(new KillCamRequest(e.transform.position));
                    };
                    mb.OnEscaped += _ => ReportKill();
                    return true;
                }
            }
            return false;
        }

        void WireDeathAndEscape(EnemyBase enemy, EnemyType type, int scoreReward)
        {
            enemy.OnDeath += _ =>
            {
                ReportKill();
                GameManager.Instance?.AddKill();
                EventBus<EnemyKilled>.Publish(new EnemyKilled(type, scoreReward));
            };
            enemy.OnEscaped += _ => ReportKill();
        }

        // Deduplicate warnings so a missing-drone-prefab wave doesn't spam the console.
        readonly System.Collections.Generic.HashSet<EnemyType> _warnedTypes = new System.Collections.Generic.HashSet<EnemyType>();
        void WarnMissingPrefab(EnemyType type)
        {
            if (!_warnedTypes.Add(type)) return;
            Debug.LogError($"[WaveSpawner] {type}Prefab is not assigned. Wave entries of this type will be skipped. Run StrafAdvance/4. Setup GameScene (or StrafAdvance/15. Rewire WaveSpawner Prefabs) to fix.");
        }

        public void ReportEnemySpawned(int count) => _enemiesAlive += count;

        public void ReportKill()
        {
            _enemiesAlive = Mathf.Max(0, _enemiesAlive - 1);
            if (_enemiesAlive > 0 || _spawning) return;

            CurrentWaveIndex++;
            if (CurrentWaveIndex >= _level.waves.Length)
                OnAllWavesComplete?.Invoke();
            else if (Application.isPlaying)
                StartCoroutine(SpawnWave(CurrentWaveIndex));
        }
    }
}
