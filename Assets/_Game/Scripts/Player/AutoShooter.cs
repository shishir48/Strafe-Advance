using UnityEngine;

namespace StrafAdvance
{
    public class AutoShooter : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Bullet bulletPrefab;

        private ObjectPool<Bullet> _pool;
        private float _fireTimer;
        private float _fireRateMultiplier = 1f;
        private bool _multishot;
        private int _enemyLayer;

        void Start()
        {
            if (bulletPrefab == null) { Debug.LogError("AutoShooter: bulletPrefab not assigned on Player prefab!"); return; }
            _pool = new ObjectPool<Bullet>(bulletPrefab, 20, transform);
            _enemyLayer = LayerMask.GetMask("Enemy");
        }

        void Update()
        {
            _fireTimer -= Time.deltaTime;
            if (_fireTimer <= 0f)
            {
                _fireTimer = config.fireRate * _fireRateMultiplier;
                Fire();
            }
        }

        void Fire()
        {
            Transform fp = firePoint != null ? firePoint : transform;
            Transform target = FindNearestEnemy();
            SpawnBullet(fp.position, fp.rotation, target);
            SpawnMuzzleFlash(fp.position, fp.rotation);

            if (_multishot)
            {
                SpawnBullet(
                    fp.position + fp.right * 0.3f,
                    Quaternion.Euler(0, 10f, 0) * fp.rotation, target);
                SpawnBullet(
                    fp.position - fp.right * 0.3f,
                    Quaternion.Euler(0, -10f, 0) * fp.rotation, target);
            }
        }

        private static GameObject _muzzleFlashPrefab;
        void SpawnMuzzleFlash(Vector3 pos, Quaternion rot)
        {
            if (_muzzleFlashPrefab == null)
                _muzzleFlashPrefab = Resources.Load<GameObject>("VFX/MuzzleFlash");
            if (_muzzleFlashPrefab != null) Instantiate(_muzzleFlashPrefab, pos, rot);
        }

        void SpawnBullet(Vector3 pos, Quaternion rot, Transform target)
        {
            if (_pool == null) return;
            Bullet b = _pool.Get();
            b.transform.SetPositionAndRotation(pos, rot);
            b.Setup(target, config.bulletDamage, config.homingStrength, _pool, isPlayerBullet: true);
        }

        Transform FindNearestEnemy()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 50f, _enemyLayer);
            Transform nearest = null;
            float minSqDist = float.MaxValue;

            foreach (Collider hit in hits)
            {
                Vector3 dir = hit.transform.position - transform.position;
                if (dir.z <= 0f) continue;
                if (Vector3.Angle(transform.forward, dir) > 90f) continue;
                float sqDist = dir.sqrMagnitude;
                if (sqDist < minSqDist) { minSqDist = sqDist; nearest = hit.transform; }
            }
            return nearest;
        }

        public void SetFireRateMultiplier(float multiplier) => _fireRateMultiplier = multiplier;
        public void SetMultishot(bool enabled) => _multishot = enabled;
    }
}
