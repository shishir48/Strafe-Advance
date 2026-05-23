using UnityEngine;

namespace StrafAdvance
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private PlayerHealth health;

        private float _targetX;
        private bool _isDragging;
        private float _dragStartX;
        private float _playerStartX;

        void Start() => health.Initialize(config);

        void Update()
        {
            HandleInput();
            float newX = Mathf.MoveTowards(
                transform.position.x, _targetX,
                config.strafeSpeed * Time.deltaTime);
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
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
