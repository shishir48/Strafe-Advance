using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using StrafAdvance;

namespace StrafAdvance.Tests.PlayMode
{
    /// <summary>
    /// Sprint 7.2 — drives the live GameScene with synthetic input events for 30 seconds and
    /// asserts that zero errors / exceptions are logged. This is the broad net that catches
    /// NREs in spawning, combat, VFX pooling, and UI that only surface during real play.
    ///
    /// Input is faked through the Input System's virtual Mouse + Keyboard so PlayerController /
    /// AutoShooter / dodge all run their real code paths (strafe drag, hold-to-shoot, dodge).
    /// </summary>
    public class Stability30sTest
    {
        const string ScenePath  = "Assets/_Game/Scenes/GameScene.unity";
        const float  DurationSec = 30f;

        readonly List<string> _errors = new List<string>();
        Mouse    _mouse;
        Keyboard _keyboard;

        [SetUp]
        public void SetUp()
        {
            _errors.Clear();
            // We do our own aggregation; stop the framework from failing on the first logged error
            // so the assertion message lists every offender at once.
            LogAssert.ignoreFailingMessages = true;
            Application.logMessageReceived += Capture;
            _mouse    = InputSystem.AddDevice<Mouse>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= Capture;
            LogAssert.ignoreFailingMessages = false;
            if (_mouse    != null) InputSystem.RemoveDevice(_mouse);
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
        }

        void Capture(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _errors.Add($"[{type}] {condition}\n{stackTrace}");
        }

        [UnityTest]
        public IEnumerator SimulatedGameplay_LogsNoErrors()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene("GameScene");
#endif
            // Let Awake/Start settle, then force the run to start (don't rely on tapping a UI button).
            yield return null;
            yield return null;
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Menu)
                GameManager.Instance.BeginRunFromMenu();

            float elapsed = 0f;
            float lastDodge = 0f;
            while (elapsed < DurationSec)
            {
                // Slow horizontal sweep with the button held → continuous strafe drag.
                float t = elapsed / DurationSec;
                float x = Mathf.Lerp(Screen.width * 0.2f, Screen.width * 0.8f, Mathf.PingPong(t * 6f, 1f));
                float y = Screen.height * 0.5f;
                Press(new Vector2(x, y), held: true);

                // Dodge roughly every 2.5s (one-frame space tap).
                bool dodge = elapsed - lastDodge >= 2.5f;
                if (dodge) lastDodge = elapsed;
                PressKey(Key.Space, dodge);

                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            // Release before asserting.
            Press(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), held: false);
            yield return null;

            CollectionAssert.IsEmpty(_errors,
                $"Errors logged during {DurationSec:F0}s of simulated gameplay:\n" + string.Join("\n---\n", _errors));
        }

        void Press(Vector2 pos, bool held)
        {
            var state = new MouseState { position = pos };
            state = state.WithButton(MouseButton.Left, held);
            InputSystem.QueueStateEvent(_mouse, state);
        }

        void PressKey(Key key, bool down)
        {
            InputSystem.QueueStateEvent(_keyboard, down ? new KeyboardState(key) : new KeyboardState());
        }
    }
}
