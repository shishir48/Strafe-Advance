using System.Collections;
using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Lightweight death "ragdoll". On death:
    ///   • switches Rigidbody to non-kinematic so it tumbles
    ///   • applies an impulse roughly away from the killing bullet (backward + up + spin)
    ///   • disables AI scripts so the corpse doesn't keep marching
    ///   • fades emission/alpha then destroys after <see cref="lifetime"/>
    ///
    /// Hooks <see cref="EnemyBase.OnDeath"/> in <see cref="Awake"/>. Self-contained — add to any enemy prefab.
    /// </summary>
    public class EnemyRagdoll : MonoBehaviour
    {
        [SerializeField] private float lifetime         = 1.6f;
        [SerializeField] private float impulseBack      = 4f;
        [SerializeField] private float impulseUp        = 3f;
        [SerializeField] private float spinTorque       = 8f;
        [SerializeField] private float gravity          = 14f;
        [SerializeField] private float fadeStartFrac    = 0.6f;

        private EnemyBase _enemy;
        private bool _triggered;

        void Awake()
        {
            _enemy = GetComponent<EnemyBase>();
            if (_enemy != null) _enemy.OnDeath += OnEnemyDeath;
        }

        void OnDestroy()
        {
            if (_enemy != null) _enemy.OnDeath -= OnEnemyDeath;
        }

        void OnEnemyDeath(EnemyBase _)
        {
            if (_triggered) return;
            _triggered = true;
            if (_enemy != null) _enemy.SuppressAutoDestroy = true;
            StartCoroutine(TumbleRoutine());
        }

        IEnumerator TumbleRoutine()
        {
            // Disable AI scripts (everything inheriting EnemyBase + brain helpers) so it doesn't keep moving.
            foreach (var mb in GetComponents<MonoBehaviour>())
            {
                if (mb is EnemyRagdoll || mb is EnemyHitReact) continue;
                mb.enabled = false;
            }
            // Disable colliders so it doesn't snag on the player or get re-shot.
            foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;

            // Convert kinematic Rigidbody so physics drives the tumble.
            var rb = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
            rb.isKinematic  = false;
            rb.useGravity   = true;
            rb.linearVelocity   = Vector3.zero;
            rb.angularVelocity  = Vector3.zero;

            // Impulse — back + up + spin. Roughly away from player (assumes player is at -Z, enemy faces -Z).
            rb.AddForce(new Vector3(Random.Range(-1f, 1f), impulseUp, impulseBack), ForceMode.VelocityChange);
            rb.AddTorque(new Vector3(Random.Range(-spinTorque, spinTorque),
                                     Random.Range(-spinTorque, spinTorque),
                                     Random.Range(-spinTorque, spinTorque)),
                                     ForceMode.VelocityChange);

            // Fade the last fraction by lowering emission toward black; then destroy.
            var renderers = GetComponentsInChildren<Renderer>();
            float t = 0f;
            while (t < lifetime)
            {
                t += Time.deltaTime;
                float u = t / lifetime;
                if (u >= fadeStartFrac)
                {
                    float fade = 1f - (u - fadeStartFrac) / (1f - fadeStartFrac);
                    foreach (var r in renderers)
                    {
                        if (r == null || r.material == null) continue;
                        if (r.material.HasProperty("_EmissionColor"))
                        {
                            var c = r.material.GetColor("_EmissionColor");
                            r.material.SetColor("_EmissionColor", c * fade);
                        }
                    }
                }
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
