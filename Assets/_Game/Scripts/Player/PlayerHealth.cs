using System;
using UnityEngine;

namespace StrafAdvance
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }

        private bool _invincible;

        public event Action OnDeath;
        public event Action<int, int> OnHealthChanged;

        public void Initialize(PlayerConfig config)
        {
            MaxHp = config.maxHp;
            CurrentHp = MaxHp;
        }

        public void TakeDamage(int amount)
        {
            if (CurrentHp <= 0 || _invincible) return;
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            OnHealthChanged?.Invoke(CurrentHp, MaxHp);
            if (CurrentHp == 0)
                OnDeath?.Invoke();
        }

        public void Heal(int amount)
        {
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            OnHealthChanged?.Invoke(CurrentHp, MaxHp);
        }

        public void SetInvincible(bool on) => _invincible = on;
    }
}
