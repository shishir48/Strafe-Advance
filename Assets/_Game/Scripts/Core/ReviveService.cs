using System;
using System.Collections;
using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// "One more run" revive. On death the player can spend soft currency (escalating per revive via
    /// <see cref="ReviveCost"/>) to resume the same run: full heal, brief invulnerability, enemies
    /// pushed back. An <see cref="IRewardedAd"/> path is stubbed for a future free (ad-watch) revive.
    /// </summary>
    public class ReviveService : MonoBehaviour
    {
        public static ReviveService Instance { get; private set; }

        const float InvulnSeconds = 2f;
        const float PushBackZ     = 22f;

        int _revivesThisRun;
        IRewardedAd _ad = new NoOpRewardedAd();

        public int  RevivesThisRun => _revivesThisRun;
        public int  CurrentCost    => ReviveCost.For(_revivesThisRun);
        public bool CanAffordRevive => CurrencyService.Instance != null
                                       && CurrencyService.Instance.Balance >= CurrentCost;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        public void SetAdProvider(IRewardedAd ad) { if (ad != null) _ad = ad; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void OnEnable()  => EventBus<GameStateChanged>.Subscribe(OnStateChanged);
        void OnDisable() => EventBus<GameStateChanged>.Unsubscribe(OnStateChanged);

        void OnStateChanged(GameStateChanged s)
        {
            // Reset the per-run counter only on a fresh run (Menu -> Playing), not on a revive resume.
            if (s.Previous == GameState.Menu && s.Current == GameState.Playing)
                _revivesThisRun = 0;
        }

        /// <summary>Spend currency to revive. Returns false if not in GameOver or can't afford.</summary>
        public bool TryReviveWithCurrency()
        {
            if (!IsRevivable()) return false;
            if (CurrencyService.Instance == null) return false;
            if (!CurrencyService.Instance.TrySpend(CurrentCost)) return false;
            _revivesThisRun++;
            DoRevive();
            return true;
        }

        /// <summary>Watch a rewarded ad to revive for free (no-op until an ad SDK is wired).</summary>
        public void TryReviveWithAd()
        {
            if (!IsRevivable()) return;
            _ad.Show(rewarded =>
            {
                if (!rewarded || !IsRevivable()) return;
                _revivesThisRun++;
                DoRevive();
            });
        }

        bool IsRevivable() => GameManager.Instance != null && GameManager.Instance.State == GameState.GameOver;

        void DoRevive()
        {
            Time.timeScale = 1f;
            GameManager.Instance.SetState(GameState.Playing);

            var ph = FindAnyObjectByType<PlayerHealth>();
            if (ph != null)
            {
                ph.Heal(ph.MaxHp);
                StartCoroutine(InvulnWindow(ph));
            }

            PushBackEnemies();

            if (RunSummaryPanel.Instance != null) RunSummaryPanel.Instance.Hide();
        }

        IEnumerator InvulnWindow(PlayerHealth ph)
        {
            ph.SetInvincible(true);
            yield return new WaitForSeconds(InvulnSeconds);
            if (ph != null) ph.SetInvincible(false);
        }

        // Shove live enemies back to the spawn line so the player isn't instantly re-killed.
        // Repositioning (not destroying) keeps WaveSpawner's alive-count accounting intact.
        void PushBackEnemies()
        {
            var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                var e = enemies[i];
                if (e == null) continue;
                Vector3 p = e.transform.position;
                if (p.z < PushBackZ) e.transform.position = new Vector3(p.x, p.y, PushBackZ);
            }
        }
    }
}
