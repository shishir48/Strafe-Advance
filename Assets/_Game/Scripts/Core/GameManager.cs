using System;
using System.Collections;
using UnityEngine;

namespace StrafAdvance
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameState State { get; private set; } = GameState.Menu;

        public event Action<GameState> OnStateChanged;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start() => StartCoroutine(DebugAutoStartCoroutine());

        IEnumerator DebugAutoStartCoroutine()
        {
            yield return null; // wait one frame for all Start() calls to complete

            var waveSpawner      = FindAnyObjectByType<WaveSpawner>();
            var corridorScroller = FindAnyObjectByType<CorridorScroller>();

            if (waveSpawner == null || corridorScroller == null)
            {
                Debug.LogWarning("[GameManager] WaveSpawner or CorridorScroller not found for auto-start.");
                yield break;
            }

            var l1 = Resources.Load<LevelConfig>("Level1");
            if (l1 == null) { Debug.LogWarning("[GameManager] Resources/Level1 not found."); yield break; }

            corridorScroller.Initialize(l1.worldScrollSpeed);
            waveSpawner.LoadLevel(l1);
            SetState(GameState.Playing);
            waveSpawner.StartSpawning();

            // Wire game loop events
            var playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.OnDeath += () => SetState(GameState.GameOver);

            waveSpawner.OnAllWavesComplete += () =>
            {
                SetState(GameState.BossFight);
                corridorScroller.Stop();
                var bossPrefab = Resources.Load<GameObject>("Boss");
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
                        bossCtrl.OnDeath += _ => SetState(GameState.LevelComplete);
                    }
                }
                else
                    SetState(GameState.LevelComplete); // no boss — skip to complete
            };

            Debug.Log("[GameManager] Auto-started Level 1.");
        }

        public void SetState(GameState state)
        {
            if (State == state) return;
            State = state;
            OnStateChanged?.Invoke(state);
        }
    }
}
