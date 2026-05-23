using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Grunt variant with front shield. Bullets coming from the front are absorbed and
    /// chip the shield (<see cref="shieldHits"/> hits to break). After break, takes normal damage.
    /// Forces flanking play.
    /// </summary>
    public class ShieldedEnemy : EnemyBase
    {
        [SerializeField] private int   shieldHits = 5;
        [SerializeField] private float shieldFrontAngleDeg = 70f; // half-angle of front cone
        [SerializeField] private GameObject shieldVisual; // optional child mesh (cube/plane) — alpha drops with hits

        private int _shieldRemaining;
        private Transform _player;

        public void InitShielded(Transform player)
        {
            _player = player;
            _shieldRemaining = shieldHits;
            UpdateShieldVisual();
        }

        void Update()
        {
            transform.Translate(Vector3.back * Config.moveSpeed * Time.deltaTime);
            if (transform.position.z < -8f) EscapeOffScreen();
        }

        /// <summary>
        /// Overridden damage path: bullets hitting the shielded front are absorbed.
        /// EnemyBase.TakeDamage is bypassed when the shield blocks.
        /// </summary>
        public override void TakeDamage(int amount)
        {
            if (CurrentHp <= 0) return;
            if (_shieldRemaining > 0 && IsHitFromFront())
            {
                _shieldRemaining--;
                UpdateShieldVisual();
                EventBus<ShieldHit>.Publish(new ShieldHit(transform.position, _shieldRemaining));
                return;
            }
            base.TakeDamage(amount);
        }

        bool IsHitFromFront()
        {
            // Bullet origin not directly available — approximate via player position direction.
            if (_player == null) return true;
            Vector3 toPlayer = (_player.position - transform.position).normalized;
            // Enemy faces -Z (toward player). "Front" is the -Z hemisphere.
            float angle = Vector3.Angle(Vector3.back, toPlayer);
            return angle <= shieldFrontAngleDeg;
        }

        void UpdateShieldVisual()
        {
            if (shieldVisual == null) return;
            if (_shieldRemaining <= 0) { shieldVisual.SetActive(false); return; }
            shieldVisual.SetActive(true);
            // Optional: tweak material alpha proportional to remaining hits.
            var r = shieldVisual.GetComponentInChildren<Renderer>();
            if (r != null && r.material != null)
            {
                Color c = r.material.color;
                c.a = Mathf.Lerp(0.25f, 0.85f, (float)_shieldRemaining / shieldHits);
                r.material.color = c;
            }
        }
    }

    public readonly struct ShieldHit
    {
        public readonly Vector3 Position;
        public readonly int Remaining;
        public ShieldHit(Vector3 p, int r) { Position = p; Remaining = r; }
    }
}
