using UnityEngine;

namespace StrafAdvance
{
    public class Bullet : MonoBehaviour, IPoolable
    {
        private int _damage;
        private float _speed = 18f;
        private float _homingStrength;
        private Transform _target;
        private ObjectPool<Bullet> _pool;
        private bool _isPlayerBullet;
        private Rigidbody _rb;

        void Awake()
        {
            if (!TryGetComponent<Rigidbody>(out _rb))
                _rb = gameObject.AddComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.useGravity  = false;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        public void Setup(Transform target, int damage, float homingStrength,
                          ObjectPool<Bullet> pool, bool isPlayerBullet)
        {
            _target = target;
            _damage = damage;
            _homingStrength = homingStrength;
            _pool = pool;
            _isPlayerBullet = isPlayerBullet;
        }

        void FixedUpdate()
        {
            if (_target != null && _homingStrength > 0f)
            {
                Vector3 dir = (_target.position - transform.position).normalized;
                transform.forward = Vector3.RotateTowards(
                    transform.forward, dir,
                    _homingStrength * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f);
            }
            Vector3 next = transform.position + transform.forward * _speed * Time.fixedDeltaTime;
            _rb.MovePosition(next);

            if (next.z > 60f || next.z < -10f)
                _pool?.Return(this);
        }

        void OnTriggerEnter(Collider other)
        {
            // Use layer check instead of tag to avoid TagManager dependency
            int hitLayer = other.gameObject.layer;
            int enemyLayer  = LayerMask.NameToLayer("Enemy");
            int playerLayer = LayerMask.NameToLayer("Player");

            bool hitEnemy  = _isPlayerBullet  && hitLayer == enemyLayer;
            bool hitPlayer = !_isPlayerBullet  && hitLayer == playerLayer;

            // Fallback: if layers not set, check for IDamageable on correct component type
            if (!hitEnemy && !hitPlayer)
            {
                if (_isPlayerBullet && other.TryGetComponent<EnemyBase>(out _)) hitEnemy = true;
                else if (!_isPlayerBullet && other.TryGetComponent<PlayerHealth>(out _)) hitPlayer = true;
            }

            if (!hitEnemy && !hitPlayer) return;

            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_damage);
                _pool?.Return(this);
            }
        }

        public void OnGetFromPool() { }
        public void OnReturnToPool() { _target = null; }
    }
}
