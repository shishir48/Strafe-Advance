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

            // Lead the player using config-driven prediction + accuracy jitter.
            Vector3 vel = _lastPlayerPos != Vector3.zero
                ? (_player.position - _lastPlayerPos) / Mathf.Max(Time.deltaTime, 0.001f)
                : Vector3.zero;
            _lastPlayerPos = _player.position;
            Vector3 aimAt = AimingMath.PredictPlayerPosition(
                transform.position, _player.position, vel, bulletSpeed: 18f, Config.aimLeadFactor);
            Quaternion rot = Quaternion.LookRotation(aimAt - transform.position, Vector3.up);
            rot = AimingMath.Jitter(rot, Config.accuracyJitterDeg);

            b.transform.SetPositionAndRotation(transform.position, rot);
            b.Setup(_player, Config.bulletDamage, 0f, _bulletPool, isPlayerBullet: false);
        }

        private Vector3 _lastPlayerPos;
    }
}
