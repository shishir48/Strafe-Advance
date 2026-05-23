using System.Collections;
using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Briefly drives <c>Time.timeScale</c> toward 0 on impact, then snaps back. Senior-grade
    /// game-feel staple — adds weight to hits without ragdoll overhead.
    ///
    /// Subscribes to <c>EnemyKilled</c> (small) and <c>PlayerDamaged</c> (medium). Custom calls
    /// can publish <see cref="HitstopRequest"/> for explosions / boss phases.
    /// </summary>
    public class Hitstop : MonoBehaviour
    {
        public static Hitstop Instance { get; private set; }

        [SerializeField] private float gruntDuration = 0.04f;
        [SerializeField] private float eliteDuration = 0.10f;
        [SerializeField] private float playerHitDuration = 0.06f;

        private Coroutine _running;
        private float _restoreScale = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            EventBus<EnemyKilled>.Subscribe(OnKill);
            EventBus<PlayerDamaged>.Subscribe(OnHit);
            EventBus<HitstopRequest>.Subscribe(OnRequest);
        }

        void OnDestroy()
        {
            EventBus<EnemyKilled>.Unsubscribe(OnKill);
            EventBus<PlayerDamaged>.Unsubscribe(OnHit);
            EventBus<HitstopRequest>.Unsubscribe(OnRequest);
            if (Instance == this) Instance = null;
            // Always restore timeScale on teardown so the editor isn't left frozen.
            Time.timeScale = _restoreScale;
        }

        void OnKill(EnemyKilled k)        => Freeze(k.Type == EnemyType.Elite ? eliteDuration : gruntDuration);
        void OnHit (PlayerDamaged d)      => Freeze(playerHitDuration);
        void OnRequest(HitstopRequest r)  => Freeze(r.Seconds);

        public void Freeze(float seconds)
        {
            if (seconds <= 0f) return;
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(FreezeRoutine(seconds));
        }

        IEnumerator FreezeRoutine(float seconds)
        {
            _restoreScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(seconds);
            Time.timeScale = _restoreScale;
            _running = null;
        }
    }

    public readonly struct HitstopRequest
    {
        public readonly float Seconds;
        public HitstopRequest(float seconds) { Seconds = seconds; }
    }
}
