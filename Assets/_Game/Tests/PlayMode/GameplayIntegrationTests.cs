using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using StrafAdvance;

namespace StrafAdvance.Tests.PlayMode
{
    /// <summary>
    /// Gameplay-loop integration tests that drive published events against the live scene and
    /// assert downstream systems react. Complement to <see cref="GameSceneSmokeTests"/> which
    /// only validates boot-time wiring.
    /// </summary>
    public class GameplayIntegrationTests
    {
        const string ScenePath = "Assets/_Game/Scenes/GameScene.unity";

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            SaveSystem.Reset();
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                ScenePath, new UnityEngine.SceneManagement.LoadSceneParameters(LoadSceneMode.Single));
#endif
            // Awake + Start need a couple of frames before everything is queryable.
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
            // Drive the game into Playing so WaveSpawner is loaded and ReportKill paths are safe.
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Menu)
                GameManager.Instance.BeginRunFromMenu();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            // Reset save so cross-test currency / BP state doesn't leak.
            SaveSystem.Reset();
        }

        [UnityTest]
        public IEnumerator Scene_HasCorePlayerAndHud()
        {
            yield return null;
            Assert.IsNotNull(Object.FindAnyObjectByType<PlayerController>(), "PlayerController missing.");
            Assert.IsNotNull(Object.FindAnyObjectByType<PlayerHealth>(),     "PlayerHealth missing.");
            Assert.IsNotNull(Object.FindAnyObjectByType<AutoShooter>(),      "AutoShooter missing.");
            Assert.IsNotNull(ModernHUD.Instance,                             "ModernHUD singleton not initialized.");
            Assert.IsNotNull(MainHubController.Instance,                     "MainHubController singleton not initialized.");
            Assert.IsNotNull(BattlePassService.Instance,                     "BattlePassService singleton not initialized.");
        }

        [UnityTest]
        public IEnumerator BattlePass_GainsXpOnEnemyKilled()
        {
            yield return null;
            Assert.IsNotNull(BattlePassService.Instance);
            int beforeXp = BattlePassService.Instance.Xp;
            // Simulate a kill — bypasses the wave spawner so we test the BP subscription in isolation.
            EventBus<EnemyKilled>.Publish(new EnemyKilled(EnemyType.Grunt, 100, new Vector3(0, 0, 10f)));
            yield return null;
            int afterXp = BattlePassService.Instance.Xp;
            Assert.Greater(afterXp, beforeXp, "Battle Pass XP did not increase after EnemyKilled.");
        }

        [UnityTest]
        public IEnumerator Currency_GrantedOnEnemyKilled()
        {
            yield return null;
            Assert.IsNotNull(CurrencyService.Instance);
            int beforeBal = CurrencyService.Instance.Balance;
            int beforeRun = CurrencyService.Instance.EarnedThisRun;
            EventBus<EnemyKilled>.Publish(new EnemyKilled(EnemyType.Grunt, 100, new Vector3(0, 0, 10f)));
            yield return null;
            int gained = CurrencyService.Instance.Balance - beforeBal;
            Assert.AreEqual(CurrencyService.Instance.DropFor(EnemyType.Grunt), gained,
                "CurrencyService did not grant the per-type drop on EnemyKilled.");
            Assert.Greater(CurrencyService.Instance.EarnedThisRun, beforeRun);
        }

        [UnityTest]
        public IEnumerator CurrencyPopup_SpawnsActivePopupOnKill()
        {
            yield return null;
            Assert.IsNotNull(CurrencyPopupSpawner.Instance);
            EventBus<EnemyKilled>.Publish(new EnemyKilled(EnemyType.Elite, 500, new Vector3(0, 0, 8f)));
            yield return null;
            // Look for any active CurrencyPopup component (pooled or freshly instantiated). includeInactive=false
            // because Show() activates the GO; a previously-deactivated pool member should not count unless re-shown.
            var popups = Object.FindObjectsByType<CurrencyPopup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Assert.Greater(popups.Length, 0, "Expected at least one active CurrencyPopup after EnemyKilled with WorldPos.");
        }
    }
}
