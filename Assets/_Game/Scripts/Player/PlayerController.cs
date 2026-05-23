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

        [Header("Sprint")]
        [SerializeField] private float sprintSpeedMul   = 1.6f;
        [SerializeField] private float maxStamina       = 5f;   // seconds of full-tilt sprint
        [SerializeField] private float staminaRegenRate = 0.8f; // per second after delay
        [SerializeField] private float staminaRegenDelay = 1.0f;

        private float _targetX;
        private bool _isDragging;
        private float _dragStartX;
        private float _playerStartX;

        // Dodge state
        private float _dodgeUntil;
        private float _dodgeReadyAt;
        private float _dodgeDirX;

        // Sprint state
        private float _stamina;
        private float _staminaRegenAt;
        public  float Stamina       => _stamina;
        public  float StaminaT      => maxStamina <= 0f ? 0f : Mathf.Clamp01(_stamina / maxStamina);
        public  bool  IsSprinting   { get; private set; }

        public bool DodgeReady    => Time.time >= _dodgeReadyAt;
        public float DodgeCooldownT => Mathf.Clamp01(1f - (_dodgeReadyAt - Time.time) / dodgeCooldown);

        void Start()
        {
            health.Initialize(config);
            _stamina = maxStamina;
        }

        void Update()
        {
            HandleInput();
            HandleDodge();
            HandleStamina();

            float mul = 1f;
            if (Time.time < _dodgeUntil) mul *= dodgeSpeedMul;
            else if (IsSprinting)        mul *= sprintSpeedMul;

            float speed = config.strafeSpeed * mul;
            float newX = Mathf.MoveTowards(transform.position.x, _targetX, speed * Time.deltaTime);
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        }

        void HandleStamina()
        {
            bool wantSprint = GameInput.SprintHeld && _stamina > 0f;
            IsSprinting = wantSprint;

            if (wantSprint)
            {
                _stamina = Mathf.Max(0f, _stamina - Time.deltaTime);
                _staminaRegenAt = Time.time + staminaRegenDelay;
            }
            else if (Time.time >= _staminaRegenAt && _stamina < maxStamina)
            {
                _stamina = Mathf.Min(maxStamina, _stamina + staminaRegenRate * Time.deltaTime);
            }
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
