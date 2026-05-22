using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        public int Score { get; private set; }
        public int KillCount { get; private set; }

        void Start() => StartCoroutine(InitFlow());

        public void AddKill()
        {
            KillCount++;
            Score += 100;
        }

        IEnumerator InitFlow()
        {
            yield return null;

            var waveSpawner      = FindAnyObjectByType<WaveSpawner>();
            var corridorScroller = FindAnyObjectByType<CorridorScroller>();
            var l1 = Resources.Load<LevelConfig>("Level1");

            if (waveSpawner == null || corridorScroller == null || l1 == null)
            {
                Debug.LogWarning("[GameManager] Missing required components for auto-start.");
                yield break;
            }

            // Show tap-to-start screen
            var tapCanvas = CreateTapToStartScreen();
            yield return WaitForTap();
            Destroy(tapCanvas);

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
            if (state == GameState.GameOver)   ShowOverlay("GAME OVER\nTap to retry", OnRetryTap);
            if (state == GameState.LevelComplete) ShowOverlay("YOU WIN!\nTap to continue", OnWinTap);
        }

        // ── Tap-to-start ────────────────────────────────────────────────────────
        GameObject CreateTapToStartScreen()
        {
            var go = new GameObject("TapToStart");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99;
            go.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
                UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var bg = new GameObject("BG");
            bg.transform.SetParent(go.transform, false);
            var bgImg = bg.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0, 0, 0, 0.75f);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

            var label = new GameObject("Label");
            label.transform.SetParent(go.transform, false);
            var tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = "STRAFE ADVANCE\n\nTap to Start";
            tmp.fontSize = 52; tmp.color = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            var rt = label.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            return go;
        }

        IEnumerator WaitForTap()
        {
            yield return null;
            while (!Input.GetMouseButtonDown(0) && Input.touchCount == 0 && !Input.anyKeyDown)
                yield return null;
        }

        void ShowOverlay(string message, Action onTap)
        {
            StartCoroutine(OverlayRoutine(message, onTap));
        }

        IEnumerator OverlayRoutine(string message, Action onTap)
        {
            var go = new GameObject("Overlay");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            go.AddComponent<UnityEngine.UI.CanvasScaler>();
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var bg = new GameObject("BG"); bg.transform.SetParent(go.transform, false);
            var bgImg = bg.AddComponent<UnityEngine.UI.Image>(); bgImg.color = new Color(0, 0, 0, 0.8f);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

            var label = new GameObject("Label"); label.transform.SetParent(go.transform, false);
            var tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = message; tmp.fontSize = 52; tmp.color = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            var rt = label.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            yield return new WaitForSeconds(0.5f);
            while (!Input.GetMouseButtonDown(0) && Input.touchCount == 0 && !Input.anyKeyDown)
                yield return null;

            Destroy(go);
            onTap?.Invoke();
        }

        void OnRetryTap()
        {
            SetState(GameState.Menu);
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        void OnWinTap()
        {
            SetState(GameState.Menu);
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
