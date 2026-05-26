using System.Collections;
using UnityEngine;

namespace StrafAdvance
{
    public class PlayerDamageFlash : MonoBehaviour
    {
        [SerializeField] private float flashDuration = 0.08f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Renderer[]          _renderers;
        private MaterialPropertyBlock _mpb;
        private Color[]             _originalColors;
        private Coroutine           _flashRoutine;

        void Awake()
        {
            _renderers      = GetComponentsInChildren<Renderer>(includeInactive: true);
            _mpb            = new MaterialPropertyBlock();
            _originalColors = new Color[_renderers.Length];

            for (int i = 0; i < _renderers.Length; i++)
            {
                var mat = _renderers[i].sharedMaterial;
                _originalColors[i] = mat != null && mat.HasProperty(BaseColorId)
                    ? mat.GetColor(BaseColorId)
                    : Color.white;
            }

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
            // Instant red
            SetAllColor(Color.red);

            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / flashDuration);
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _mpb.SetColor(BaseColorId, Color.Lerp(Color.red, _originalColors[i], t));
                    _renderers[i].SetPropertyBlock(_mpb);
                }
                yield return null;
            }

            RestoreAll();
            _flashRoutine = null;
        }

        void SetAllColor(Color c)
        {
            _mpb.SetColor(BaseColorId, c);
            foreach (var r in _renderers) r.SetPropertyBlock(_mpb);
        }

        void RestoreAll()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                _mpb.SetColor(BaseColorId, _originalColors[i]);
                _renderers[i].SetPropertyBlock(_mpb);
            }
        }
    }
}
