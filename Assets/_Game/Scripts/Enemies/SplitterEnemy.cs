using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// On death, spawns N mini-grunt fragments scattered around its position. Fragments use
    /// the supplied grunt prefab + config but with reduced HP and faster speed.
    /// </summary>
    public class SplitterEnemy : EnemyBase
    {
        [SerializeField] private int   fragmentCount = 3;
        [SerializeField] private float fragmentSpread = 1.2f;
        [SerializeField] private float fragmentSpeedMul = 1.4f;
        [SerializeField] private float fragmentHpMul    = 0.4f;

        private GruntEnemy _fragmentPrefab;
        private EnemyConfig _gruntConfig;
        private Transform _player;
        private ObjectPool<Bullet> _bulletPool;
        private Transform _spawnParent;

        public void InitSplitter(GruntEnemy fragmentPrefab, EnemyConfig gruntConfig,
                                 Transform player, ObjectPool<Bullet> bulletPool, Transform spawnParent)
        {
            _fragmentPrefab = fragmentPrefab;
            _gruntConfig    = gruntConfig;
            _player         = player;
            _bulletPool     = bulletPool;
            _spawnParent    = spawnParent;
        }

        void Update()
        {
            transform.Translate(Vector3.back * Config.moveSpeed * Time.deltaTime);
            if (transform.position.z < -8f) EscapeOffScreen();
        }

        protected override void Die()
        {
            SpawnFragments();
            base.Die();
        }

        void SpawnFragments()
        {
            if (_fragmentPrefab == null || _gruntConfig == null) return;
            var fragConfig = ScriptableObject.CreateInstance<EnemyConfig>();
            fragConfig.maxHp        = Mathf.Max(1, Mathf.RoundToInt(_gruntConfig.maxHp * fragmentHpMul));
            fragConfig.contactDamage = _gruntConfig.contactDamage;
            fragConfig.moveSpeed    = _gruntConfig.moveSpeed * fragmentSpeedMul;
            fragConfig.fireRate     = _gruntConfig.fireRate;
            fragConfig.bulletDamage = _gruntConfig.bulletDamage;

            for (int i = 0; i < fragmentCount; i++)
            {
                Vector3 jitter = new Vector3(
                    Random.Range(-fragmentSpread, fragmentSpread),
                    0f,
                    Random.Range(-fragmentSpread, fragmentSpread) * 0.5f);
                var frag = Instantiate(_fragmentPrefab, transform.position + jitter,
                                       Quaternion.identity, _spawnParent);
                frag.transform.localScale = transform.localScale * 0.65f;
                frag.Initialize(fragConfig);
                frag.InitGrunt(_player, _bulletPool);
                // Spawner doesn't track these — they count as part of splitter's payload.
                // They contribute kills via their own OnDeath wiring inherited from EnemyBase.
            }
        }
    }
}
