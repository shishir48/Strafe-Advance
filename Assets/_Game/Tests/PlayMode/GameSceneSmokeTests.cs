using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using StrafAdvance;

namespace StrafAdvance.Tests.PlayMode
{
    /// <summary>
    /// Smoke tests that load the live GameScene and verify it boots cleanly + the wave-spawner
    /// has every prefab/config slot populated. This is the test class that would have caught the
    /// "wave 3 has no enemies" regression — pure EditMode tests can't because they never spawn a
    /// real scene.
    /// </summary>
    public class GameSceneSmokeTests
    {
        const string ScenePath = "Assets/_Game/Scenes/GameScene.unity";

        readonly List<string> _errors = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _errors.Clear();
            Application.logMessageReceived += CaptureErrors;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= CaptureErrors;
        }

        void CaptureErrors(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _errors.Add($"[{type}] {condition}");
        }

        [UnityTest]
        public IEnumerator GameScene_BootsWithoutErrors()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                ScenePath, new UnityEngine.SceneManagement.LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene("GameScene");
#endif
            // Give scripts a few frames to Awake + Start.
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.5f);

            CollectionAssert.IsEmpty(_errors, "Errors detected during scene boot:\n" + string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator WaveSpawner_HasAllPrefabsAndConfigsAssigned()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                ScenePath, new UnityEngine.SceneManagement.LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return null;
            yield return null;

            var ws = Object.FindAnyObjectByType<WaveSpawner>();
            Assert.IsNotNull(ws, "WaveSpawner missing from GameScene.");

            // All serialized prefab/config slots that drive wave spawning. If any is null, that enemy
            // type silently skips and (pre-fix) the wave dead-locked. Post-fix it logs an error — but
            // the design intent is that menu '4. Setup GameScene' or '15. Rewire WaveSpawner Prefabs'
            // has populated them, so a healthy build asserts non-null here.
            string[] slots =
            {
                "gruntPrefab", "flankerPrefab", "elitePrefab", "chargerPrefab",
                "sniperPrefab", "shieldedPrefab", "splitterPrefab", "dronePrefab", "miniBossPrefab",
                "gruntConfig", "flankerConfig", "eliteConfig", "chargerConfig",
                "sniperConfig", "shieldedConfig", "splitterConfig", "droneConfig", "miniBossConfig",
                "enemyBulletPrefab",
            };

            var missing = new List<string>();
            var fields = typeof(WaveSpawner).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            foreach (var slot in slots)
            {
                var field = System.Array.Find(fields, f => f.Name == slot);
                Assert.IsNotNull(field, $"WaveSpawner.{slot} field not found via reflection (renamed?).");
                var val = field.GetValue(ws) as Object;
                if (val == null) missing.Add(slot);
            }
            CollectionAssert.IsEmpty(missing,
                "WaveSpawner has unassigned serialized fields. Run StrafAdvance/4. Setup GameScene or 15. Rewire WaveSpawner Prefabs.\n" +
                "Missing: " + string.Join(", ", missing));
        }
    }
}
