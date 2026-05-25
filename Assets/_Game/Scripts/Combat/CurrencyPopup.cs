using TMPro;
using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// World-space "+N ◆" popup that floats up + fades, billboarded to the main camera.
    /// Pooled and managed by <see cref="CurrencyPopupSpawner"/>.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class CurrencyPopup : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.85f;
        [SerializeField] private Vector3 floatOffset = new Vector3(0f, 1.6f, 0f);

        private TMP_Text _label;
        private float _t;
        private Camera _cam;
        private Vector3 _spawnPos;

        void Awake()
        {
            _label = GetComponent<TMP_Text>();
            _cam = Camera.main;
        }

        public void Show(int amount, Vector3 worldPos)
        {
            // Awake doesn't fire on inactive Instantiate clones until SetActive(true), so
            // initialize lazily here — text assignment otherwise NREs on first pool spawn.
            if (_label == null) _label = GetComponent<TMP_Text>();
            if (_cam == null)   _cam   = Camera.main;
            _label.text = $"+{amount}  <color=#ffd166>◆</color>";
            var col = _label.color; col.a = 1f; _label.color = col;
            _t = 0f;
            _spawnPos = worldPos;
            transform.position = worldPos;
            transform.localScale = Vector3.one;
            gameObject.SetActive(true);
        }

        void LateUpdate()
        {
            _t += Time.unscaledDeltaTime;
            float u = _t / lifetime;
            if (u >= 1f) { gameObject.SetActive(false); return; }
            transform.position = _spawnPos + floatOffset * u;
            var c = _label.color; c.a = 1f - u * u; _label.color = c;
            if (_cam != null) transform.rotation = _cam.transform.rotation;
        }
    }
}
