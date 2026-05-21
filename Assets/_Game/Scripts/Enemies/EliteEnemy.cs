using UnityEngine;

namespace StrafAdvance
{
    public class EliteEnemy : EnemyBase
    {
        [SerializeField] private PowerUp powerUpPrefab;

        private Transform _player;
        private float _chargeTimer = 3f;
        private bool _charging;
        private Vector3 _chargeDir;

        public void InitElite(Transform player) => _player = player;

        void Update()
        {
            if (_charging)
            {
                transform.Translate(_chargeDir * 9f * Time.deltaTime, Space.World);
                if (transform.position.z < -8f) Destroy(gameObject);
            }
            else
            {
                transform.Translate(Vector3.back * Config.moveSpeed * 0.4f * Time.deltaTime);
                _chargeTimer -= Time.deltaTime;
                if (_chargeTimer <= 0f)
                {
                    _charging = true;
                    _chargeDir = _player != null
                        ? (_player.position - transform.position).normalized
                        : Vector3.back;
                }
            }
        }

        protected override void Die()
        {
            if (powerUpPrefab != null)
                Instantiate(powerUpPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            base.Die();
        }
    }
}
