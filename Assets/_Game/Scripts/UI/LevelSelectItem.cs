using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    public class LevelSelectItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private Button playButton;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private TMP_Text priceLabel;

        private Action _onPlay;

        public void Setup(LevelConfig config, bool unlocked, string price, Action onPlay)
        {
            _onPlay = onPlay;
            nameLabel.text = config.levelName;
            lockOverlay.SetActive(!unlocked);
            priceLabel.text = unlocked ? "Play" : price;
            playButton.interactable = true;
        }

        public void OnPlayButtonPressed() => _onPlay?.Invoke();
    }
}
