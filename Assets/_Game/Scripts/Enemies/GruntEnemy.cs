using UnityEngine;

namespace StrafAdvance
{
    public class GruntEnemy : EnemyBase
    {
        [SerializeField] private Bullet bulletPrefab;

        private Transform _player;
        private ObjectPool<Bullet> _bulletPool;
        private float _fireTimer;

        public void InitGrunt(Transform player, ObjectPool<Bullet> sharedBulletPool)
        {
            _player = player;
            _bulletPool = sharedBulletPool;
            _fireTimer = Config.fireRate;
        }

        void Update()
        {
            transform.Translate(Vector3.back * Config.moveSpeed * Time.deltaTime);

            _fireTimer -= Time.deltaTime;
            if (_fireTimer <= 0f)
            {
                _fireTimer = Config.fireRate;
                FireAtPlayer();
            }

            if (transform.position.z < -8f)
                EscapeOffScreen();
        }

        void FireAtPlayer()
        {
            if (_player == null || _bulletPool == null) return;
            Bullet b = _bulletPool.Get();
            b.transform.SetPositionAndRotation(transform.position, Quaternion.identity);
            b.Setup(_player, Config.bulletDamage, 0f, _bulletPool, isPlayerBullet: false);
        }
    }
}
