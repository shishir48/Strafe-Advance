using UnityEngine;

namespace StrafAdvance
{
    public class LevelSelectController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform gridParent;
        [SerializeField] private LevelSelectItem itemPrefab;
        [SerializeField] private LevelConfig[] allLevels;

        void Start() => panel.SetActive(false);

        public void Show()
        {
            panel.SetActive(true);
            PopulateGrid();
        }

        public void Hide() => panel.SetActive(false);

        void PopulateGrid()
        {
            foreach (Transform child in gridParent)
                Destroy(child.gameObject);

            UnlockRegistry registry = IAPManager.Instance.Registry;
            foreach (LevelConfig level in allLevels)
            {
                bool unlocked = level.iapProductId == "free" || registry.IsUnlocked(level.iapProductId);
                LevelSelectItem item = Instantiate(itemPrefab, gridParent);
                LevelConfig captured = level;
                item.Setup(level, unlocked, "$1.99", () => OnLevelSelected(captured, unlocked));
            }
        }

        void OnLevelSelected(LevelConfig level, bool unlocked)
        {
            if (!unlocked)
            {
                IAPManager.Instance.BuyProduct(level.iapProductId);
                return;
            }
            Hide();
            FindAnyObjectByType<WaveSpawner>().LoadLevel(level);
            FindAnyObjectByType<CorridorScroller>().Initialize(level.worldScrollSpeed);
            GameManager.Instance.SetState(GameState.Playing);
            FindAnyObjectByType<WaveSpawner>().StartSpawning();
        }
    }
}
