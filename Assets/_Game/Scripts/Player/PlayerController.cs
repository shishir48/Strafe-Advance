using UnityEngine;

namespace StrafAdvance
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private PlayerHealth health;

        [Header("Dodge")]
        [SerializeField] private float dodgeDuration  = 0.25f;
        [SerializeField] private float dodgeCooldown  = 1.5f;
        [SerializeField] private float dodgeSpeedMul  = 4f;

        private float _targetX;
        private bool _isDragging;
        private float _dragStartX;
        private float _playerStartX;

        // Dodge state
        private float _dodgeUntil;
        private float _dodgeReadyAt;
        private float _dodgeDirX;

        public bool DodgeReady    => Time.time >= _dodgeReadyAt;
        public float DodgeCooldownT => Mathf.Clamp01(1f - (_dodgeReadyAt - Time.time) / dodgeCooldown);

        void Start() => health.Initialize(config);

        void Update()
        {
            HandleInput();
            HandleDodge();
            float speed = config.strafeSpeed * (Time.time < _dodgeUntil ? dodgeSpeedMul : 1f);
            float newX = Mathf.MoveTowards(transform.position.x, _targetX, speed * Time.deltaTime);
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        }

        void HandleDodge()
        {
            // I-frames while dashing
            bool dashing = Time.time < _dodgeUntil;
            health.SetInvincible(dashing);

            if (!GameInput.DodgePressedThisFrame || !DodgeReady) return;

            // Direction: toward current target if available, else last drag direction, else +1
            float dir = Mathf.Sign(_targetX - transform.position.x);
            if (Mathf.Approximately(dir, 0f)) dir = _dodgeDirX != 0f ? _dodgeDirX : 1f;
            _dodgeDirX = dir;
            _targetX   = Mathf.Clamp(transform.position.x + dir * config.strafeLimit, -config.strafeLimit, config.strafeLimit);
            _dodgeUntil   = Time.time + dodgeDuration;
            _dodgeReadyAt = Time.time + dodgeCooldown;
            EventBus<DodgePerformed>.Publish(new DodgePerformed(dir));
        }

        void HandleInput()
        {
            if (GameInput.PrimaryPressedThisFrame)
            {
                _isDragging   = true;
                _dragStartX   = GameInput.PointerPosition.x;
                _playerStartX = transform.position.x;
            }
            else if (GameInput.PrimaryHeld && _isDragging)
            {
                UpdateTargetX(GameInput.PointerPosition.x);
            }
            else if (GameInput.PrimaryReleasedThisFrame)
            {
                _isDragging = false;
            }
        }

        void UpdateTargetX(float screenX)
        {
            float delta = (screenX - _dragStartX) / Screen.width * config.strafeLimit * 2f;
            _targetX = Mathf.Clamp(_playerStartX + delta, -config.strafeLimit, config.strafeLimit);
        }
    }
}
