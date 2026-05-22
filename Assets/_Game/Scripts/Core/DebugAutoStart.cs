using UnityEngine;
using UnityEngine.SceneManagement;

namespace StrafAdvance
{
    public static class DebugAutoStart
    {
        // Disabled — GameManager.InitFlow() handles the full flow now
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "GameScene") return;
            SceneManager.sceneLoaded -= OnSceneLoaded;

            var waveSpawner      = Object.FindAnyObjectByType<WaveSpawner>();
            var corridorScroller = Object.FindAnyObjectByType<CorridorScroller>();

            if (waveSpawner == null || corridorScroller == null)
            {
                Debug.LogWarning("DebugAutoStart: WaveSpawner or CorridorScroller not found.");
                return;
            }

            var l1 = Resources.Load<LevelConfig>("Level1");
            if (l1 == null) { Debug.LogWarning("DebugAutoStart: Resources/Level1.asset not found."); return; }

            corridorScroller.Initialize(l1.worldScrollSpeed);
            waveSpawner.LoadLevel(l1);
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Playing);
            waveSpawner.StartSpawning();
            Debug.Log("DebugAutoStart: Level 1 started.");
        }
    }
}
