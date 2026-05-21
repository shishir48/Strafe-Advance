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
#if UNITY_EDITOR
            HandleMouseInput();
#else
            HandleTouchInput();
#endif
        }

        void HandleTouchInput()
        {
            if (Input.touchCount == 0) return;
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                _isDragging = true;
                _dragStartX = touch.position.x;
                _playerStartX = transform.position.x;
            }
            else if (touch.phase == TouchPhase.Moved && _isDragging)
                UpdateTargetX(touch.position.x);
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                _isDragging = false;
        }

        void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _isDragging = true;
                _dragStartX = Input.mousePosition.x;
                _playerStartX = transform.position.x;
            }
            else if (Input.GetMouseButton(0) && _isDragging)
                UpdateTargetX(Input.mousePosition.x);
            else if (Input.GetMouseButtonUp(0))
                _isDragging = false;
        }

        void UpdateTargetX(float screenX)
        {
            float delta = (screenX - _dragStartX) / Screen.width * config.strafeLimit * 2f;
            _targetX = Mathf.Clamp(_playerStartX + delta, -config.strafeLimit, config.strafeLimit);
        }
    }
}
