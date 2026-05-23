using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Anchor mid-wave threat. Holds at <see cref="holdZ"/> instead of charging.
    /// Phase 1: aimed single shots. Phase 2 (50% HP): faster fire + 3-bullet shotgun spread.
    /// Includes a world-space HP bar that orients to camera.
    /// </summary>
    public class MiniBossEnemy : EnemyBase
    {
        enum Phase { One, Two }

        [SerializeField] private Bullet bulletPrefab;
        [SerializeField] private float holdZ = 14f;
        [SerializeField] private float phase1FireRate = 1.2f;
        [SerializeField] private float phase2FireRate = 0.55f;
        [SerializeField] private float spreadDegrees   = 12f;
        [SerializeField] private int   bulletDamageOverride = 12;
        [SerializeField] private float strafeAmplitude = 2.5f;
        [SerializeField] private float strafeSpeed     = 1.6f;

        private Transform _player;
        private ObjectPool<Bullet> _bulletPool;
        private float _fireTimer;
        private Phase _phase = Phase.One;
        private Transform _hpBar;
        private SpriteRenderer _hpFill;
        private Camera _cam;
        private float _strafePhase;

        public void InitMiniBoss(Transform player, ObjectPool<Bullet> sharedBulletPool)
        {
            _player = player;
            _bulletPool = sharedBulletPool;
            _fireTimer = phase1FireRate;
            _cam = Camera.main;
            _strafePhase = Random.value * 10f;
            BuildHpBar();
            EventBus<BossPhaseChanged>.Publish(new BossPhaseChanged(0, "Mini-Boss appears"));
        }

        void Update()
        {
            // Glide to hold position, then strafe laterally.
            if (transform.position.z > holdZ)
                transform.Translate(Vector3.back * Config.moveSpeed * Time.deltaTime);
            else
            {
                _strafePhase += Time.deltaTime * strafeSpeed;
                float targetX = Mathf.Sin(_strafePhase) * strafeAmplitude;
                var p = transform.position;
                p.x = Mathf.MoveTowards(p.x, targetX, Config.moveSpeed * Time.deltaTime);
                transform.position = p;
            }

            _fireTimer -= Time.deltaTime;
            float rate = _phase == Phase.One ? phase1FireRate : phase2FireRate;
            if (_fireTimer <= 0f)
            {
                _fireTimer = rate;
                Fire();
            }

            UpdateHpBar();
            if (transform.position.z < -8f) EscapeOffScreen();
        }

        protected override void OnDamageTaken()
        {
            if (_phase == Phase.One && Config != null && CurrentHp <= Config.maxHp / 2)
            {
                _phase = Phase.Two;
                EventBus<BossPhaseChanged>.Publish(new BossPhaseChanged(1, "Mini-Boss enrages"));
                EventBus<ShakeRequest>.Publish(new ShakeRequest(0.6f));
            }
        }

        void Fire()
        {
            if (_player == null || _bulletPool == null) return;
            if (_phase == Phase.One)
                ShootOne(0f);
            else
            {
                ShootOne(-spreadDegrees);
                ShootOne(0f);
                ShootOne(spreadDegrees);
            }
        }

        void ShootOne(float angleDeg)
        {
            Bullet b = _bulletPool.Get();
            Quaternion rot = Quaternion.Euler(0, angleDeg, 0) * Quaternion.identity;
            b.transform.SetPositionAndRotation(transform.position, rot);
            b.Setup(_player, bulletDamageOverride, 30f, _bulletPool, isPlayerBullet: false);
        }

        // ─── HP bar ─────────────────────────────────────────────────────────────

        void BuildHpBar()
        {
            _hpBar = new GameObject("MiniBossHpBar").transform;
            _hpBar.SetParent(transform, false);
            _hpBar.localPosition = new Vector3(0f, 1.5f, 0f);

            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(bg.GetComponent<Collider>());
            bg.transform.SetParent(_hpBar, false);
            bg.transform.localScale = new Vector3(2f, 0.18f, 1f);
            var bgMat = bg.GetComponent<Renderer>().material;
            bgMat.color = new Color(0.02f, 0.05f, 0.10f, 1f);

            var fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(fill.GetComponent<Collider>());
            fill.transform.SetParent(_hpBar, false);
            fill.transform.localPosition = new Vector3(-1f, 0f, -0.01f); // left edge anchor
            fill.transform.localScale = new Vector3(2f, 0.14f, 1f);
            var fillMat = fill.GetComponent<Renderer>().material;
            fillMat.color = new Color(1f, 0.27f, 0.27f, 1f);
            _hpFill = null; // not using SpriteRenderer; scale the Quad instead
            _hpBar.gameObject.AddComponent<MiniBossBarAnchor>().Init(this, fill.transform);
        }

        void UpdateHpBar()
        {
            if (_cam != null && _hpBar != null)
                _hpBar.rotation = Quaternion.LookRotation(_hpBar.position - _cam.transform.position);
        }

        public float HpFraction => Config == null ? 1f : Mathf.Clamp01((float)CurrentHp / Config.maxHp);
    }

    /// <summary>Tiny helper: scales the HP fill quad according to the boss HpFraction each frame.</summary>
    public class MiniBossBarAnchor : MonoBehaviour
    {
        MiniBossEnemy _boss;
        Transform _fill;
        Vector3 _baseScale;
        Vector3 _basePos;
        public void Init(MiniBossEnemy boss, Transform fill)
        {
            _boss = boss; _fill = fill;
            _baseScale = fill.localScale; _basePos = fill.localPosition;
        }
        void LateUpdate()
        {
            if (_boss == null || _fill == null) return;
            float f = Mathf.Max(0.001f, _boss.HpFraction);
            var s = _baseScale; s.x = _baseScale.x * f; _fill.localScale = s;
            // Anchor left edge: shift left by (1-f) * half base width
            var p = _basePos; p.x = _basePos.x + _baseScale.x * (1f - f) * -0.5f; _fill.localPosition = p;
        }
    }

    public readonly struct BossPhaseChanged
    {
        public readonly int    Phase;     // 0 = appearing, 1 = phase 2, etc.
        public readonly string Message;
        public BossPhaseChanged(int phase, string message) { Phase = phase; Message = message; }
    }
}
