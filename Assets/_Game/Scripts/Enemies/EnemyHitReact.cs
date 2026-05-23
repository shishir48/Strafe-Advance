using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Universal damage-response component. Add once to every enemy prefab root.
    /// On <c>EnemyDamaged</c> firing for this enemy's position, it:
    ///   - flashes every Renderer's material emission to white for <see cref="flashSeconds"/>
    ///   - scales the model up to <see cref="popScale"/> over <see cref="popSeconds"/> then back
    ///   - applies a brief positional knockback away from the hit
    ///
    /// Material instancing is unavoidable for the flash (URP shared mats can't be tinted per-instance
    /// without instancing). Restored on death.
    /// </summary>
    public class EnemyHitReact : MonoBehaviour
    {
        [SerializeField] private float flashSeconds = 0.08f;
        [SerializeField] private float popScale     = 1.18f;
        [SerializeField] private float popSeconds   = 0.12f;
        [SerializeField] private float knockback    = 0.18f;
        [SerializeField] private Color flashColor   = new Color(2.5f, 2.5f, 2.5f, 1f); // HDR white

        // Per-renderer state captured at start so we can restore.
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly List<Color>    _baseEmission = new List<Color>();
        private Coroutine _flashRoutine, _popRoutine;
        private Vector3 _baseScale;
        private float   _approxRadius = 0.5f;

        void Awake()
        {
            // Capture renderers + base emission once. Skip particles + line renderers.
            GetComponentsInChildren(true, _renderers);
            _renderers.RemoveAll(r => r is ParticleSystemRenderer || r is LineRenderer);
            _baseEmission.Clear();
            foreach (var r in _renderers)
            {
                var mat = r.material; // forces instance — required for per-instance flash
                _baseEmission.Add(mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black);
            }
            _baseScale = transform.localScale;
        }

        void OnEnable()  => EventBus<EnemyDamaged>.Subscribe(OnDamaged);
        void OnDisable() => EventBus<EnemyDamaged>.Unsubscribe(OnDamaged);

        void OnDamaged(EnemyDamaged msg)
        {
            // Only react if the damage event is at our position (within bounds).
            if ((msg.Position - transform.position).sqrMagnitude > _approxRadius * _approxRadius * 4f) return;
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            if (_popRoutine   != null) StopCoroutine(_popRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());
            _popRoutine   = StartCoroutine(PopRoutine());
            transform.position += Vector3.back * knockback; // small back-knock; player faces +Z
        }

        IEnumerator FlashRoutine()
        {
            for (int i = 0; i < _renderers.Count; i++)
            {
                var r = _renderers[i];
                if (r != null && r.material != null && r.material.HasProperty("_EmissionColor"))
                    r.material.SetColor("_EmissionColor", flashColor);
            }
            yield return new WaitForSeconds(flashSeconds);
            for (int i = 0; i < _renderers.Count; i++)
            {
                var r = _renderers[i];
                if (r != null && r.material != null && r.material.HasProperty("_EmissionColor"))
                    r.material.SetColor("_EmissionColor", _baseEmission[i]);
            }
            _flashRoutine = null;
        }

        IEnumerator PopRoutine()
        {
            float t = 0f;
            while (t < popSeconds)
            {
                t += Time.deltaTime;
                float u = t / popSeconds;
                // Smooth pulse: 0→1→0 over the duration.
                float k = Mathf.Sin(u * Mathf.PI);
                transform.localScale = _baseScale * Mathf.Lerp(1f, popScale, k);
                yield return null;
            }
            transform.localScale = _baseScale;
            _popRoutine = null;
        }
    }
}
