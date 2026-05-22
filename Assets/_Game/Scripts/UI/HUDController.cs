using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private Slider playerHpSlider;
        [SerializeField] private TMP_Text waveLabel;
        [SerializeField] private GameObject bossHpGroup;
        [SerializeField] private Slider bossHpSlider;

        void Start() => StartCoroutine(AutoWire());

        IEnumerator AutoWire()
        {
            yield return null;

            var canvas = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>();
            if (canvas == null) yield break;

            if (playerHpSlider == null) playerHpSlider = CreateSlider(canvas.transform, "HPBar",
                new Color(0.31f, 0.76f, 0.97f), new Vector2(10, -10), new Vector2(220, 30));
            if (waveLabel == null) waveLabel = CreateLabel(canvas.transform, "WaveLabel",
                new Vector2(0, -10), new Vector2(200, 35));
            if (bossHpGroup == null)
            {
                var go = new GameObject("BossHPGroup");
                go.transform.SetParent(canvas.transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(0, 20); rt.sizeDelta = new Vector2(-40, 25);
                bossHpGroup = go;
                bossHpSlider = CreateSlider(go.transform, "BossBar", new Color(1f, 0.27f, 0.27f),
                    Vector2.zero, Vector2.zero);
                bossHpGroup.SetActive(false);
            }

            // Hook to PlayerHealth and WaveSpawner
            var health = FindAnyObjectByType<PlayerHealth>();
            if (health != null)
            {
                SetPlayerHp(health.CurrentHp, health.MaxHp);
                health.OnHealthChanged += SetPlayerHp;
            }

            var spawner = FindAnyObjectByType<WaveSpawner>();
            if (spawner != null)
            {
                spawner.OnWaveStarted += idx =>
                {
                    var lc = Resources.Load<LevelConfig>("Level1");
                    if (lc != null) SetWave(idx, lc.waves.Length);
                };
            }
        }

        Slider CreateSlider(Transform parent, string name, Color fillColor, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();

            if (name == "HPBar")
            {
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
            }
            else
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            }
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;

            var slider = go.AddComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 1; slider.value = 1;

            var bg = new GameObject("BG"); bg.transform.SetParent(go.transform, false);
            var bgImg = bg.AddComponent<Image>(); bgImg.color = new Color(0.03f, 0.07f, 0.13f, 0.88f);
            var bgRT = bg.GetComponent<RectTransform>(); bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;

            var fa = new GameObject("FillArea"); fa.transform.SetParent(go.transform, false);
            var faRT = fa.AddComponent<RectTransform>(); faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
            faRT.offsetMin = Vector2.zero; faRT.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill"); fill.transform.SetParent(fa.transform, false);
            var fillImg = fill.AddComponent<Image>(); fillImg.color = fillColor;
            var fillRT = fill.GetComponent<RectTransform>(); fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;

            slider.fillRect = fillRT;
            return slider;
        }

        TMP_Text CreateLabel(Transform parent, string name, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "Wave 1"; tmp.fontSize = 22; tmp.color = new Color(0.31f, 0.76f, 0.97f);
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            return tmp;
        }

        public void SetPlayerHp(int current, int max)
        {
            if (playerHpSlider == null) return;
            playerHpSlider.maxValue = max;
            playerHpSlider.value = current;
        }

        public void SetWave(int waveIndex, int totalWaves)
        {
            if (waveLabel == null) return;
            waveLabel.text = $"Wave {waveIndex + 1}/{totalWaves}";
        }

        public void ShowBossHp(int maxHp)
        {
            if (bossHpGroup == null) return;
            bossHpGroup.SetActive(true);
            bossHpSlider.maxValue = maxHp;
            bossHpSlider.value = maxHp;
        }

        public void UpdateBossHp(int current)
        {
            if (bossHpSlider == null) return;
            bossHpSlider.value = current;
        }

        public void HideBossHp()
        {
            if (bossHpGroup == null) return;
            bossHpGroup.SetActive(false);
        }
    }
}
