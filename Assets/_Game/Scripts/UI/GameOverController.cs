using UnityEngine;

namespace StrafAdvance
{
    public class GameOverController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;

        void Start()
        {
            GameManager.Instance.OnStateChanged += OnStateChanged;
            panel.SetActive(false);
        }

        void OnDestroy() => GameManager.Instance.OnStateChanged -= OnStateChanged;

        void OnStateChanged(GameState state)
            => panel.SetActive(state == GameState.GameOver);

        public void OnRetryPressed() => GameManager.Instance.SetState(GameState.Playing);
        public void OnMenuPressed() => GameManager.Instance.SetState(GameState.Menu);
        public void OnShopPressed() => FindAnyObjectByType<ShopController>().Show();
    }
}
