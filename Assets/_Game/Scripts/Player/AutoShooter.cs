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
        private bool _multishotBuff;
        private int _enemyLayer;
        private WeaponConfig _weapon;
        private Perk _perkStats;

        void Start()
        {
            if (bulletPrefab == null) { Debug.LogError("AutoShooter: bulletPrefab not assigned on Player prefab!"); return; }
            _pool = new ObjectPool<Bullet>(bulletPrefab, 20, transform);
            _enemyLayer = LayerMask.GetMask("Enemy");
            RefreshLoadout();
        }

        /// <summary>Call after Settings or Loadout screen changes equipped weapon/perks.</summary>
        public void RefreshLoadout()
        {
            string id = SaveSystem.Current.progress.equippedWeaponId;
            _weapon = WeaponCatalog.Find(id);
            _perkStats = PlayerProgression.Instance != null
                ? PlayerProgression.Instance.GetEquippedStats()
                : new Perk();
        }

        float EffectiveFireRate => _weapon.fireRate * _fireRateMultiplier * _perkStats.fireRateMultiplier;
        int   EffectiveDamage   => Mathf.Max(1, Mathf.RoundToInt(_weapon.damage * _perkStats.damageMultiplier));
        int   EffectiveMultishot => _multishotBuff ? Mathf.Max(_weapon.multishotCount, 3) : _weapon.multishotCount;

        void Update()
        {
            _fireTimer -= Time.deltaTime;
            if (_fireTimer <= 0f)
            {
                _fireTimer = EffectiveFireRate;
                Fire();
            }
        }

        void Fire()
        {
            Transform fp = firePoint != null ? firePoint : transform;
            Transform target = FindNearestEnemy();
            int total = EffectiveMultishot;

            if (total <= 1)
            {
                SpawnBullet(fp.position, fp.rotation, target);
            }
            else
            {
                // Center spread fan; total bullets symmetric around firePoint.forward.
                float spread = _weapon.multishotSpread;
                float half   = (total - 1) * 0.5f;
                for (int i = 0; i < total; i++)
                {
                    float angle = (i - half) * spread;
                    Vector3 offset = fp.right * ((i - half) * 0.15f);
                    SpawnBullet(fp.position + offset, Quaternion.Euler(0, angle, 0) * fp.rotation, target);
                }
            }

            SpawnMuzzleFlash(fp.position, fp.rotation);
        }

        private static GameObject _muzzleFlashPrefab;
        void SpawnMuzzleFlash(Vector3 pos, Quaternion rot)
        {
            if (_muzzleFlashPrefab == null)
                _muzzleFlashPrefab = AssetLoader.Load<GameObject>("VFX/MuzzleFlash");
            if (_muzzleFlashPrefab != null) Instantiate(_muzzleFlashPrefab, pos, rot);
        }

        void SpawnBullet(Vector3 pos, Quaternion rot, Transform target)
        {
            if (_pool == null) return;
            Bullet b = _pool.Get();
            b.transform.SetPositionAndRotation(pos, rot);
            b.Setup(target, EffectiveDamage, _weapon.homingStrength, _pool, isPlayerBullet: true);
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
        public void SetMultishot(bool enabled) => _multishotBuff = enabled;
    }
}
