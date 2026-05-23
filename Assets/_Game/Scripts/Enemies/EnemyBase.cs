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
            if (!TryGetComponent<Rigidbody>(out var rb))
                rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
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
        public event System.Action<EnemyBase> OnEscaped;

        protected void EscapeOffScreen()
        {
            OnEscaped?.Invoke(this);
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        protected virtual void Die()
        {
            SpawnDeathVFX();
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        protected virtual void SpawnDeathVFX()
        {
            var prefab = Resources.Load<GameObject>("VFX/EnemyDeath");
            if (prefab != null) Instantiate(prefab, transform.position, Quaternion.identity);
        }
    }
}

