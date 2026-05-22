using System;
using UnityEngine;

namespace StrafAdvance
{
    public class EnemyBase : MonoBehaviour, IDamageable
    {
        protected EnemyConfig Config { get; private set; }
        public int CurrentHp { get; private set; }

        public event Action<EnemyBase> OnDeath;

        protected virtual void Awake()
        {
            // Required for bullet OnTriggerEnter to fire
            if (!TryGetComponent<Rigidbody>(out _))
            {
                var rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity  = false;
            }
            // Set layer — used for bullet detection
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) gameObject.layer = enemyLayer;
        }

        public void Initialize(EnemyConfig config)
        {
            Config = config;
            CurrentHp = config.maxHp;
        }

        public void TakeDamage(int amount)
        {
            if (CurrentHp <= 0) return;
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            OnDamageTaken();
            if (CurrentHp == 0)
            {
                OnDeath?.Invoke(this);
                Die();
            }
        }

        protected virtual void OnDamageTaken() { }
        protected virtual void Die() => Destroy(gameObject);
    }
}

