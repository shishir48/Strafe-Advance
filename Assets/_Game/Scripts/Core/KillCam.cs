using System.Collections;
using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Cinematic punctuation for big kills. On <see cref="KillCamRequest"/>:
    ///   • freezes Time.timeScale to <see cref="slowMoScale"/>
    ///   • smoothly moves Main Camera toward the kill position
    ///   • lerps back to its rest position after the cinematic
    /// Self-contained, runs on unscaled time so the slow-mo doesn't choke its own coroutine.
    /// </summary>
    public class KillCam : MonoBehaviour
    {
        public static KillCam Instance { get; private set; }

        [SerializeField] private float slowMoScale  = 0.28f;
        [SerializeField] private float duration     = 1.4f;
        [SerializeField] private float zoomAmount   = 1.6f;       // world units forward toward target

        public bool IsPlaying => _running != null;

        private Camera _cam;
        private Coroutine _running;
        private Vector3 _restPos;
        private Quaternion _restRot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            EventBus<KillCamRequest>.Subscribe(OnRequest);
        }

        void OnDestroy()
        {
            EventBus<KillCamRequest>.Unsubscribe(OnRequest);
            if (Instance == this) Instance = null;
            // Defensive: always restore timeScale if we're torn down mid-effect.
            Time.timeScale = 1f;
        }

        void OnRequest(KillCamRequest req) => Play(req.WorldPos);

        public void Play(Vector3 worldPos)
        {
            _cam ??= Camera.main;
            if (_cam == null) return;
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Routine(worldPos));
        }

        IEnumerator Routine(Vector3 worldPos)
        {
            _restPos = _cam.transform.position;
            _restRot = _cam.transform.rotation;
            float restoreScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = slowMoScale;

            Vector3 toward = Vector3.Lerp(_restPos, worldPos, 0.35f);
            // Push camera further forward along its current forward, capped distance.
            Vector3 zoomed = toward + _cam.transform.forward * zoomAmount;
            Quaternion lookAt = Quaternion.LookRotation((worldPos - zoomed).normalized, Vector3.up);

            float t = 0f;
            float half = duration * 0.4f; // ease in for first 40%
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u;
                if (t < half) u = Mathf.SmoothStep(0f, 1f, t / half);
                else          u = Mathf.SmoothStep(1f, 0f, (t - half) / (duration - half));
                _cam.transform.position = Vector3.Lerp(_restPos, zoomed, u);
                _cam.transform.rotation = Quaternion.Slerp(_restRot, lookAt, u);
                yield return null;
            }

            _cam.transform.position = _restPos;
            _cam.transform.rotation = _restRot;
            Time.timeScale = restoreScale;
            _running = null;
        }
    }

    public readonly struct KillCamRequest
    {
        public readonly Vector3 WorldPos;
        public KillCamRequest(Vector3 worldPos) { WorldPos = worldPos; }
    }
}
