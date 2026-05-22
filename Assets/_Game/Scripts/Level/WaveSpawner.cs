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

            for (int i = 0; i < wave.count; i++)
            {
                SpawnEnemy(wave.enemyType);
                ReportEnemySpawned(1);
                if (wave.spawnInterval > 0f)
                    yield return new WaitForSeconds(wave.spawnInterval);
            }
            _spawning = false;
        }

        void SpawnEnemy(EnemyType type)
        {
            float spawnX = UnityEngine.Random.Range(-2.5f, 2.5f);
            Vector3 spawnPos = new Vector3(spawnX, 0f, 25f);

            switch (type)
            {
                case EnemyType.Grunt:
                    GruntEnemy grunt = Instantiate(gruntPrefab, spawnPos, Quaternion.identity, spawnParent);
                    grunt.Initialize(gruntConfig);
                    grunt.InitGrunt(playerTransform, _enemyBulletPool);
                    grunt.OnDeath += _ => { ReportKill(); GameManager.Instance?.AddKill(); };
                    break;
                case EnemyType.Flanker:
                    if (flankerPrefab == null) { Debug.LogWarning("[WaveSpawner] flankerPrefab null, skipping"); ReportKill(); return; }
                    FlankerEnemy flanker = Instantiate(flankerPrefab, spawnPos, Quaternion.identity, spawnParent);
                    flanker.Initialize(flankerConfig != null ? flankerConfig : gruntConfig);
                    flanker.InitFlanker(playerTransform);
                    flanker.OnDeath += _ => { ReportKill(); GameManager.Instance?.AddKill(); };
                    break;
                case EnemyType.Elite:
                    if (elitePrefab == null) { Debug.LogWarning("[WaveSpawner] elitePrefab null, skipping"); ReportKill(); return; }
                    EliteEnemy elite = Instantiate(elitePrefab, spawnPos, Quaternion.identity, spawnParent);
                    elite.Initialize(eliteConfig != null ? eliteConfig : gruntConfig);
                    elite.InitElite(playerTransform);
                    elite.OnDeath += _ => { ReportKill(); GameManager.Instance?.AddKill(); };
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
            else
                StartCoroutine(SpawnWave(CurrentWaveIndex));
        }
    }
}
