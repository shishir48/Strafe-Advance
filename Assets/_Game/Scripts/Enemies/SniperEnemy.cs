using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Stationary long-range enemy. Holds position near spawn, telegraphs each shot with a laser
    /// sight (LineRenderer) for <see cref="telegraphSeconds"/>, then fires a high-damage homing bullet.
    /// Provides skill expression — players who dodge after the laser appears avoid damage.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class SniperEnemy : EnemyBase
    {
        [SerializeField] private Bullet bulletPrefab;
        [SerializeField] private float  holdZ = 18f;
        [SerializeField] private float  fireRateOverride = 2.5f;
        [SerializeField] private float  telegraphSeconds = 0.7f;
        [SerializeField] private int    bulletDamageOverride = 25;
        [SerializeField] private float  homingStrength = 90f;
        [SerializeField] private Color  laserIdleColor = new Color(1f, 0.2f, 0.2f, 0.3f);
        [SerializeField] private Color  laserChargeColor = new Color(1f, 0.05f, 0.05f, 1f);

        private Transform _player;
        private ObjectPool<Bullet> _bulletPool;
        private LineRenderer _laser;
        private float _fireTimer;
        private float _chargeTimer;

        // Shared across all snipers — laser color is driven per-instance via
        // startColor/endColor, so one cached material avoids a per-spawn
        // Shader.Find + a leaked Material instance per enemy.
        private static Material s_LaserMat;

        public void InitSniper(Transform player, ObjectPool<Bullet> sharedBulletPool)
        {
            _player = player;
            _bulletPool = sharedBulletPool;
            _fireTimer = fireRateOverride;
            _laser = GetComponent<LineRenderer>();
            _laser.positionCount = 2;
            _laser.startWidth = 0.04f;
            _laser.endWidth   = 0.01f;
            if (s_LaserMat == null) s_LaserMat = new Material(Shader.Find("Sprites/Default"));
            _laser.sharedMaterial = s_LaserMat;
            _laser.startColor = _laser.endColor = laserIdleColor;
        }

        void Update()
        {
            // Glide toward holdZ, then stop.
            if (transform.position.z > holdZ)
            {
                transform.Translate(Vector3.back * Config.moveSpeed * Time.deltaTime);
            }

            if (_player != null && _laser != null)
            {
                _laser.SetPosition(0, transform.position + Vector3.down * 0.1f);
                _laser.SetPosition(1, _player.position);
            }

            _fireTimer   -= Time.deltaTime;
            _chargeTimer -= Time.deltaTime;

            // Telegraph last `telegraphSeconds` of cooldown by ramping color.
            if (_fireTimer <= telegraphSeconds && _fireTimer > 0f)
            {
                float t = 1f - (_fireTimer / telegraphSeconds);
                var c = Color.Lerp(laserIdleColor, laserChargeColor, t);
                _laser.startColor = _laser.endColor = c;
                _laser.startWidth = Mathf.Lerp(0.04f, 0.12f, t);
            }

            if (_fireTimer <= 0f)
            {
                _fireTimer = fireRateOverride;
                _laser.startColor = _laser.endColor = laserIdleColor;
                _laser.startWidth = 0.04f;
                Fire();
            }

            if (transform.position.z < -8f) EscapeOffScreen();
        }

        void Fire()
        {
            if (_player == null || _bulletPool == null) return;
            Bullet b = _bulletPool.Get();
            b.transform.SetPositionAndRotation(transform.position, Quaternion.identity);
            b.Setup(_player, bulletDamageOverride, homingStrength, _bulletPool, isPlayerBullet: false);
        }
    }
}
