using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Slow support enemy. Doesn't shoot — periodically heals nearby living enemies, making it a
    /// priority kill. Pulses on a fixed cadence; uses a cheap scene scan (fine for small counts).
    /// </summary>
    public class HealerEnemy : EnemyBase
    {
        [SerializeField] private float healRadius   = 4.0f;
        [SerializeField] private int   healPerPulse = 6;
        [SerializeField] private float pulseInterval = 1.5f;

        private float _timer;

        public void InitHealer(Transform player)
        {
            // player unused (no targeting) — kept for spawner-call symmetry with other enemies.
            _timer = pulseInterval;
        }

        void Update()
        {
            transform.Translate(Vector3.back * Config.moveSpeed * Time.deltaTime);

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = pulseInterval;
                Pulse();
            }

            if (transform.position.z < -8f) EscapeOffScreen();
        }

        void Pulse()
        {
            var all = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            bool healedAny = false;
            for (int i = 0; i < all.Length; i++)
            {
                var e = all[i];
                if (e == this || e == null) continue;
                if (Vector3.Distance(transform.position, e.transform.position) <= healRadius)
                {
                    e.Heal(healPerPulse);
                    healedAny = true;
                }
            }
            if (healedAny)
                VFXPool.Instance.Get("VFX/HitSpark", transform.position, Quaternion.identity);
        }
    }
}
