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

        public void SetPlayerHp(int current, int max)
        {
            playerHpSlider.maxValue = max;
            playerHpSlider.value = current;
        }

        public void SetWave(int waveIndex, int totalWaves)
            => waveLabel.text = $"Wave {waveIndex + 1}/{totalWaves}";

        public void ShowBossHp(int maxHp)
        {
            bossHpGroup.SetActive(true);
            bossHpSlider.maxValue = maxHp;
            bossHpSlider.value = maxHp;
        }

        public void UpdateBossHp(int current)
            => bossHpSlider.value = current;

        public void HideBossHp()
            => bossHpGroup.SetActive(false);
    }
}
