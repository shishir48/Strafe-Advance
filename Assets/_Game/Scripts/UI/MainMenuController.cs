using UnityEngine;

namespace StrafAdvance
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private LevelSelectController levelSelect;

        void Start()
        {
            GameManager.Instance.OnStateChanged += OnStateChanged;
            menuPanel.SetActive(GameManager.Instance.State == GameState.Menu);
        }

        void OnDestroy() { if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= OnStateChanged; }

        void OnStateChanged(GameState state)
            => menuPanel.SetActive(state == GameState.Menu);

        public void OnPlayPressed() => levelSelect.Show();

        public void OnShopPressed()
            => FindAnyObjectByType<ShopController>()?.Show();
    }
}
