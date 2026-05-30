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
    /// Sprint 7.5 — Restart (= scene reload) does NOT trigger RuntimeInitializeOnLoadMethod, so any
    /// component that subscribes to the static EventBus in Awake but forgets to unsubscribe in
    /// OnDestroy leaks a handler on every restart. Pause→Restart five times rapidly and assert the
    /// per-event handler counts never grow past the first-load baseline.
    /// </summary>
    public class RestartStabilityTests
    {
        const string ScenePath = "Assets/_Game/Scenes/GameScene.unity";
        const int    Restarts  = 5;

        [UnityTest]
        public IEnumerator RepeatedRestart_DoesNotLeakEventBusListeners()
        {
            yield return LoadAndSettle();
            var baseline = Snapshot();

            for (int i = 0; i < Restarts - 1; i++)
            {
                yield return LoadAndSettle();
                var now = Snapshot();

                var leaks = new List<string>();
                foreach (var kv in now)
                    if (kv.Value > baseline[kv.Key])
                        leaks.Add($"{kv.Key}: {baseline[kv.Key]} → {kv.Value}");

                CollectionAssert.IsEmpty(leaks,
                    $"EventBus listener leak after restart #{i + 1}:\n" + string.Join("\n", leaks));
            }
        }

        IEnumerator LoadAndSettle()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene("GameScene");
#endif
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.2f);
        }

        static Dictionary<string, int> Snapshot() => new Dictionary<string, int>
        {
            { "GameStateChanged", EventBus<GameStateChanged>.HandlerCount },
            { "EnemyKilled",      EventBus<EnemyKilled>.HandlerCount },
            { "WaveStarted",      EventBus<WaveStarted>.HandlerCount },
            { "PlayerDamaged",    EventBus<PlayerDamaged>.HandlerCount },
            { "DodgePerformed",   EventBus<DodgePerformed>.HandlerCount },
        };
    }
}
