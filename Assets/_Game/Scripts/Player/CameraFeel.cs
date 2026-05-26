using System.Collections;
using UnityEngine;

namespace StrafAdvance
{
    [RequireComponent(typeof(Camera))]
    public class CameraFeel : MonoBehaviour
    {
        [Header("Lead Lag (2.1)")]
        [SerializeField] private float leadScale      = 0.12f;
        [SerializeField] private float leadSmoothTime = 0.10f;

        [Header("Roll (2.3)")]
        [SerializeField] private float rollGain        = 0.40f;  // degrees per world-unit/s; strafeSpeed=8 → ~3.2° max
        [SerializeField] private float rollSmoothTime  = 0.08f;
        [SerializeField] private float maxRollDegrees  = 3f;

        [Header("Impulse (2.4) — spring-damper")]
        [SerializeField] private float impulseSpring   = 12f;
        [SerializeField] private float impulseDamping  = 0.7f;   // <1 = underdamped (overshoot)

        [Header("FOV Pulse (2.2)")]
        [SerializeField] private float baseFov   = 55f;
        [SerializeField] private float dodgeFov  = 60f;
        [SerializeField] private float fovInTime = 0.10f;
        [SerializeField] private float fovOutTime = 0.20f;

        private Camera           _cam;
        private PlayerController _player;
        private ScreenShake      _shake;
        private Coroutine        _fovRoutine;

        private Vector3    _restLocalPos;
        private Quaternion _restLocalRot;

        // Lead lag
        private float _leadX;
        private float _leadVelX;

        // Roll
        private float _rollAngle;
        private float _rollVel;

        // Impulse spring
        private Vector3 _impulseOffset;
        private Vector3 _impulseVelocity;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            EventBus<DodgePerformed>.Subscribe(OnDodge);
            EventBus<PlayerDamaged>.Subscribe(OnPlayerHit);
            EventBus<EnemyKilled>.Subscribe(OnEnemyKilled);
        }

        void OnDestroy()
        {
            EventBus<DodgePerformed>.Unsubscribe(OnDodge);
            EventBus<PlayerDamaged>.Unsubscribe(OnPlayerHit);
            EventBus<EnemyKilled>.Unsubscribe(OnEnemyKilled);
        }

        void Start()
        {
            _restLocalPos = transform.localPosition;
            _restLocalRot = transform.localRotation;
            _leadX        = 0f;
            _cam.fieldOfView = baseFov;

            _player = FindFirstObjectByType<PlayerController>();
            _shake  = FindFirstObjectByType<ScreenShake>();
        }

        // ── Event handlers ────────────────────────────────────────────────────

        void OnDodge(DodgePerformed d)
        {
            if (_fovRoutine != null) StopCoroutine(_fovRoutine);
            _fovRoutine = StartCoroutine(FovPulseRoutine());
        }

        void OnPlayerHit(PlayerDamaged d)   => AddImpulse(new Vector3(0f,  0.15f, 0f));
        void OnEnemyKilled(EnemyKilled k)
        {
            if (k.Type == EnemyType.MiniBoss) AddImpulse(new Vector3(0f, -0.25f, 0f));
        }

        void AddImpulse(Vector3 direction)
        {
            _impulseOffset   += direction;
            _impulseVelocity  = Vector3.zero;   // reset so spring starts from new offset
        }

        // ── LateUpdate — sole transform writer ───────────────────────────────

        void LateUpdate()
        {
            // Yield camera control to KillCam during cinematics.
            if (KillCam.Instance != null && KillCam.Instance.IsPlaying) return;

            Vector3    posOffset = Vector3.zero;
            Quaternion rotOffset = Quaternion.identity;

            // 1. Lead lag
            if (_player != null)
            {
                float targetLeadX = _player.transform.position.x * leadScale;
                _leadX  = Mathf.SmoothDamp(_leadX, targetLeadX, ref _leadVelX, leadSmoothTime);
                posOffset.x += _leadX;
            }

            // 2. Roll
            if (_player != null)
            {
                float targetRoll = Mathf.Clamp(-_player.VelocityX * rollGain, -maxRollDegrees, maxRollDegrees);
                _rollAngle = Mathf.SmoothDamp(_rollAngle, targetRoll, ref _rollVel, rollSmoothTime);
                rotOffset  = Quaternion.Euler(0f, 0f, _rollAngle);
            }

            // 3. Impulse spring (Time.deltaTime freezes during hitstop — intentional; camera holds kick)
            float dt = Time.deltaTime;
            if (dt > 0f)
            {
                float critDamp   = 2f * impulseDamping * Mathf.Sqrt(impulseSpring);
                Vector3 spring   = -impulseSpring * _impulseOffset - critDamp * _impulseVelocity;
                _impulseVelocity += spring * dt;
                _impulseOffset   += _impulseVelocity * dt;
            }
            posOffset += _impulseOffset;

            // 4. Shake (ScreenShake computes Perlin offsets in its own LateUpdate)
            if (_shake != null)
            {
                posOffset += _shake.PositionShake;
                rotOffset *= _shake.RotationShake;
            }

            transform.localPosition = _restLocalPos + posOffset;
            transform.localRotation = _restLocalRot * rotOffset;
        }

        // ── FOV coroutine ─────────────────────────────────────────────────────

        IEnumerator FovPulseRoutine()
        {
            float t = 0f;
            while (t < fovInTime)
            {
                t += Time.unscaledDeltaTime;
                _cam.fieldOfView = Mathf.Lerp(baseFov, dodgeFov, t / fovInTime);
                yield return null;
            }
            _cam.fieldOfView = dodgeFov;

            t = 0f;
            while (t < fovOutTime)
            {
                t += Time.unscaledDeltaTime;
                _cam.fieldOfView = Mathf.Lerp(dodgeFov, baseFov, t / fovOutTime);
                yield return null;
            }
            _cam.fieldOfView = baseFov;
            _fovRoutine = null;
        }
    }
}
