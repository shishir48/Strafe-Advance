using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Rushes the player and detonates on death, dealing AoE damage with linear falloff
    /// (<see cref="BomberBlast"/>). Telegraphs by pulsing emissive as it nears.
    /// </summary>
    public class BomberEnemy : EnemyBase
    {
        [SerializeField] private float blastRadius    = 3.5f;
        [SerializeField] private int   blastMaxDamage = 30;
        [SerializeField] private float lateralHoming  = 0.6f;
        [SerializeField] private Color telegraphColor = new Color(3f, 0.5f, 0.1f, 1f);

        private Transform _player;
        private Renderer[] _renderers;

        public void InitBomber(Transform player)
        {
            _player = player;
            _renderers = GetComponentsInChildren<Renderer>();
        }

        void Update()
        {
            Vector3 dir = Vector3.back;
            if (_player != null)
            {
                float dx = _player.position.x - transform.position.x;
                dir += new Vector3(Mathf.Clamp(dx, -1f, 1f), 0f, 0f) * lateralHoming;
            }
            transform.position += dir.normalized * Config.moveSpeed * Time.deltaTime;

            // Telegraph: pulse emission stronger as it closes on the player.
            if (_player != null && _renderers != null)
            {
                float prox = Mathf.Clamp01(1f - Mathf.Abs(transform.position.z - _player.position.z) / 14f);
                float pulse = prox * (0.6f + 0.4f * Mathf.Sin(Time.time * 12f));
                SetEmission(Color.Lerp(Color.black, telegraphColor, pulse));
            }

            if (transform.position.z < -8f) EscapeOffScreen();
        }

        protected override void Die()
        {
            Detonate();
            base.Die();
        }

        void Detonate()
        {
            VFXPool.Instance.Get("VFX/EnemyDeath", transform.position, Quaternion.identity);
            EventBus<ShakeRequest>.Publish(new ShakeRequest(0.55f));
            EventBus<HitstopRequest>.Publish(new HitstopRequest(0.05f));

            if (_player != null && _player.TryGetComponent<PlayerHealth>(out var ph))
            {
                float d = Vector3.Distance(transform.position, _player.position);
                int dmg = BomberBlast.DamageAt(d, blastRadius, blastMaxDamage);
                if (dmg > 0) ph.TakeDamage(dmg);
            }
        }

        void SetEmission(Color c)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r is ParticleSystemRenderer || r is LineRenderer) continue;
                RendererEmission.Set(r, c);
            }
        }
    }
}
