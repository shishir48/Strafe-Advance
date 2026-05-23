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
                    SpawnEnemy(wave.enemyType);
                    ReportEnemySpawned(1);
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
                SpawnEnemy(entry.enemyType);
                ReportEnemySpawned(1);
                if (entry.spawnInterval > 0f)
                    yield return new WaitForSeconds(entry.spawnInterval);
            }
            onDone?.Invoke();
        }

        void SpawnEnemy(EnemyType type)
        {
            float spawnX = UnityEngine.Random.Range(-2.5f, 2.5f);
            Vector3 spawnPos = new Vector3(spawnX, 0f, 25f);

            switch (type)
            {
                case EnemyType.Grunt:
                    if (gruntPrefab == null) return;
                    GruntEnemy grunt = Instantiate(gruntPrefab, spawnPos, Quaternion.identity, spawnParent);
                    grunt.Initialize(gruntConfig);
                    grunt.InitGrunt(playerTransform, _enemyBulletPool);
                    grunt.OnDeath += _ => { ReportKill(); GameManager.Instance?.AddKill(); EventBus<EnemyKilled>.Publish(new EnemyKilled(EnemyType.Grunt, 100)); };
                    grunt.OnEscaped += _ => ReportKill();
                    break;
                case EnemyType.Flanker:
                    if (flankerPrefab == null) { Debug.LogWarning("[WaveSpawner] flankerPrefab null, skipping"); ReportKill(); return; }
                    FlankerEnemy flanker = Instantiate(flankerPrefab, spawnPos, Quaternion.identity, spawnParent);
                    flanker.Initialize(flankerConfig != null ? flankerConfig : gruntConfig);
                    flanker.InitFlanker(playerTransform);
                    flanker.OnDeath += _ => { ReportKill(); GameManager.Instance?.AddKill(); EventBus<EnemyKilled>.Publish(new EnemyKilled(EnemyType.Flanker, 200)); };
                    flanker.OnEscaped += _ => ReportKill();
                    break;
                case EnemyType.Elite:
                    if (elitePrefab == null) { Debug.LogWarning("[WaveSpawner] elitePrefab null, skipping"); ReportKill(); return; }
                    EliteEnemy elite = Instantiate(elitePrefab, spawnPos, Quaternion.identity, spawnParent);
                    elite.Initialize(eliteConfig != null ? eliteConfig : gruntConfig);
                    elite.InitElite(playerTransform);
                    elite.OnDeath += _ => { ReportKill(); GameManager.Instance?.AddKill(); EventBus<EnemyKilled>.Publish(new EnemyKilled(EnemyType.Elite, 500)); };
                    elite.OnEscaped += _ => ReportKill();
                    break;
                case EnemyType.Charger:
                    if (chargerPrefab == null) return;
                    ChargerEnemy charger = Instantiate(chargerPrefab, spawnPos, Quaternion.identity, spawnParent);
                    charger.Initialize(chargerConfig != null ? chargerConfig : gruntConfig);
                    charger.InitCharger(playerTransform);
                    charger.OnDeath += _ => { ReportKill(); GameManager.Instance?.AddKill(); EventBus<EnemyKilled>.Publish(new EnemyKilled(EnemyType.Charger, 250)); };
                    charger.OnEscaped += _ => ReportKill();
                    break;
                case EnemyType.Sniper:
                    if (sniperPrefab == null) return;
                    SniperEnemy sniper = Instantiate(sniperPrefab, spawnPos, Quaternion.identity, spawnParent);
                    sniper.Initialize(sniperConfig != null ? sniperConfig : gruntConfig);
                    sniper.InitSniper(playerTransform, _enemyBulletPool);
                    sniper.OnDeath += _ => { ReportKill(); GameManager.Instance?.AddKill(); EventBus<EnemyKilled>.Publish(new EnemyKilled(EnemyType.Sniper, 400)); };
                    sniper.OnEscaped += _ => ReportKill();
                    break;
            }
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
