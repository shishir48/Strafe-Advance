using UnityEngine;

namespace StrafAdvance
{
    public class ShopController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform skinsGrid;
        [SerializeField] private Transform levelsGrid;
        [SerializeField] private ShopItemView itemPrefab;

        private static readonly (string id, string name, string price)[] SkinItems =
        {
            ("skin_bundle_1", "Character Skins Pack", "$2.99"),
            ("skin_bundle_2", "Weapon Skins Pack", "$2.99")
        };

        private static readonly (string id, string name, string price)[] LevelItems =
        {
            ("level_pack_2", "Levels 4-6", "$1.99"),
            ("level_pack_3", "Levels 7-9", "$1.99"),
            ("level_pack_4", "Levels 10-12", "$1.99")
        };

        void Start() => panel.SetActive(false);

        public void Show()
        {
            panel.SetActive(true);
            PopulateGrid(skinsGrid, SkinItems);
            PopulateGrid(levelsGrid, LevelItems);
        }

        public void Hide() => panel.SetActive(false);

        void PopulateGrid(Transform grid, (string id, string name, string price)[] items)
        {
            foreach (Transform child in grid) Destroy(child.gameObject);
            UnlockRegistry reg = IAPManager.Instance.Registry;
            foreach (var (id, name, price) in items)
            {
                bool owned = reg.IsUnlocked(id);
                ShopItemView view = Instantiate(itemPrefab, grid);
                string capturedId = id;
                view.Setup(name, price, owned, () => IAPManager.Instance.BuyProduct(capturedId));
            }
        }
    }
}
