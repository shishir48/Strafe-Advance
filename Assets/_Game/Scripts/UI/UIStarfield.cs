using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    public class UIStarfield : MonoBehaviour
    {
        private struct Star { public RectTransform RT; public float Speed; }

        private Star[] _stars;
        private RectTransform _container;
        private float _w = 1080f;
        private float _h = 1920f;
        private bool _sized;

        public void Init(RectTransform container, int count = 60)
        {
            _container = container;
            _stars = new Star[count];

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Star");
                go.transform.SetParent(container, false);
                var rt = go.AddComponent<RectTransform>();
                float size = Random.Range(2f, 5f);
                rt.sizeDelta = new Vector2(size, size);
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.anchoredPosition = new Vector2(Random.Range(0f, _w), Random.Range(0f, _h));
                var img = go.AddComponent<Image>();
                float brightness = Random.Range(0.4f, 1.0f);
                img.color = new Color(brightness, brightness, Mathf.Min(brightness * 1.2f, 1f), Random.Range(0.3f, 0.7f));
                img.raycastTarget = false;
                _stars[i] = new Star { RT = rt, Speed = Random.Range(20f, 80f) };
            }
        }

        void Update()
        {
            if (_stars == null || _container == null) return;
            if (!_sized)
            {
                float rw = _container.rect.width;
                float rh = _container.rect.height;
                if (rw > 0f && rh > 0f) { _w = rw; _h = rh; _sized = true; }
            }
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < _stars.Length; i++)
            {
                if (_stars[i].RT == null) continue;
                var p = _stars[i].RT.anchoredPosition;
                p.y -= _stars[i].Speed * dt;
                if (p.y < -10f) { p.y = _h + 5f; p.x = Random.Range(0f, _w); }
                _stars[i].RT.anchoredPosition = p;
            }
        }
    }
}
