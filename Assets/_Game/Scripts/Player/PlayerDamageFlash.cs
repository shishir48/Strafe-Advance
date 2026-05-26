using System.Collections;
using UnityEngine;

namespace StrafAdvance
{
    public class PlayerDamageFlash : MonoBehaviour
    {
        [SerializeField] private float flashDuration = 0.08f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Renderer[]           _renderers;
        private MaterialPropertyBlock _mpb;
        private Coroutine            _flashRoutine;

        void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            _mpb       = new MaterialPropertyBlock();
            EventBus<PlayerDamaged>.Subscribe(OnDamaged);
        }

        void OnDestroy() => EventBus<PlayerDamaged>.Unsubscribe(OnDamaged);

        void OnDamaged(PlayerDamaged d)
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            // Capture current colors now (after any skin tints have been applied).
            var originalColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].GetPropertyBlock(_mpb);
                originalColors[i] = _mpb.HasColor(BaseColorId)
                    ? _mpb.GetColor(BaseColorId)
                    : (_renderers[i].sharedMaterial != null && _renderers[i].sharedMaterial.HasProperty(BaseColorId)
                        ? _renderers[i].sharedMaterial.GetColor(BaseColorId)
                        : Color.white);
                _mpb.Clear(); // Reset so we start fresh
            }

            // Instant red
            SetAllColor(Color.red);

            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / flashDuration);
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _mpb.SetColor(BaseColorId, Color.Lerp(Color.red, originalColors[i], t));
                    _renderers[i].SetPropertyBlock(_mpb);
                }
                yield return null;
            }

            // Restore
            for (int i = 0; i < _renderers.Length; i++)
            {
                _mpb.SetColor(BaseColorId, originalColors[i]);
                _renderers[i].SetPropertyBlock(_mpb);
            }
            _flashRoutine = null;
        }

        void SetAllColor(Color c)
        {
            _mpb.SetColor(BaseColorId, c);
            foreach (var r in _renderers) r.SetPropertyBlock(_mpb);
        }
    }
}
