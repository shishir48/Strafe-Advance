using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    public class ShopItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text buttonLabel;

        public void Setup(string itemName, string price, bool owned, Action onBuy)
        {
            nameLabel.text = itemName;
            priceLabel.text = owned ? "Owned" : price;
            buttonLabel.text = owned ? "Equip" : "Buy";
            actionButton.onClick.RemoveAllListeners();
            if (!owned) actionButton.onClick.AddListener(() => onBuy?.Invoke());
        }
    }
}
