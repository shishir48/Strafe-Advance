using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    public class LevelCompleteController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private Image[] starImages;
        [SerializeField] private Sprite starFilled;
        [SerializeField] private Sprite starEmpty;

        void Start()
        {
            GameManager.Instance.OnStateChanged += OnStateChanged;
            panel.SetActive(false);
        }

        void OnDestroy() { if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= OnStateChanged; }

        void OnStateChanged(GameState state)
        {
            if (state != GameState.LevelComplete) { panel.SetActive(false); return; }
            panel.SetActive(true);
        }

        public void Show(int score, int stars)
        {
            panel.SetActive(true);
            scoreLabel.text = $"Score: {score}";
            for (int i = 0; i < starImages.Length; i++)
                starImages[i].sprite = i < stars ? starFilled : starEmpty;
        }

        public void OnNextPressed() => GameManager.Instance.SetState(GameState.Menu);
        public void OnShopPressed() => FindAnyObjectByType<ShopController>()?.Show();
    }
}
