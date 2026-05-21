using System;
using UnityEngine;

namespace StrafAdvance
{
    public class EnemyBase : MonoBehaviour, IDamageable
    {
        protected EnemyConfig Config { get; private set; }
        public int CurrentHp { get; private set; }

        public event Action<EnemyBase> OnDeath;

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

