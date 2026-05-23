using TMPro;
using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// World-space TMP that floats up + fades. One pooled instance per active number.
    /// Spawned by <see cref="DamageNumberSpawner"/> on bullet hits.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class DamageNumber : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.6f;
        [SerializeField] private float floatSpeed = 1.8f;
        [SerializeField] private Vector3 floatOffset = new Vector3(0f, 1.4f, 0f);
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0.6f, 1, 1.4f);

        private TMP_Text _label;
        private float _t;
        private Camera _cam;
        private Vector3 _spawnPos;

        void Awake()
        {
            _label = GetComponent<TMP_Text>();
            _cam = Camera.main;
        }

        public void Show(int amount, Vector3 worldPos, Color color)
        {
            _label.text  = amount.ToString();
            _label.color = color;
            _t = 0f;
            _spawnPos = worldPos;
            transform.position = worldPos;
            transform.localScale = Vector3.one * scaleCurve.Evaluate(0f);
            gameObject.SetActive(true);
        }

        void LateUpdate()
        {
            _t += Time.unscaledDeltaTime;
            float u = _t / lifetime;
            if (u >= 1f) { gameObject.SetActive(false); return; }

            transform.position    = _spawnPos + floatOffset * u;
            transform.localScale  = Vector3.one * scaleCurve.Evaluate(u);
            var c = _label.color; c.a = 1f - u; _label.color = c;

            if (_cam != null) transform.rotation = _cam.transform.rotation;
        }
    }
}
