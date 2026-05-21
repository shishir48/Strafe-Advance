using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace StrafAdvance
{
    public class IAPManager : MonoBehaviour, IDetailedStoreListener
    {
        public static IAPManager Instance { get; private set; }

        public UnlockRegistry Registry { get; } = new UnlockRegistry();

        private IStoreController _store;
        private IExtensionProvider _extensions;

        private static readonly string[] ProductIds =
        {
            "level_pack_2", "level_pack_3", "level_pack_4",
            "skin_bundle_1", "skin_bundle_2"
        };
        private static readonly string[] ConsumableIds = { "powerup_pack" };

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePurchasing();
        }

        void InitializePurchasing()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (string id in ProductIds)
                builder.AddProduct(id, ProductType.NonConsumable);
            foreach (string id in ConsumableIds)
                builder.AddProduct(id, ProductType.Consumable);
            UnityPurchasing.Initialize(this, builder);
        }

        public void BuyProduct(string productId)
        {
            if (_store == null) { Debug.LogWarning("Store not initialized"); return; }
            _store.InitiatePurchase(productId);
        }

        public void RestorePurchases()
        {
            if (_extensions == null) return;
            _extensions.GetExtension<IGooglePlayStoreExtensions>().RestoreTransactions(
                (success, msg) => Debug.Log(success ? "Restore OK" : $"Restore failed: {msg}"));
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _store = controller;
            _extensions = extensions;
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            Registry.Unlock(args.purchasedProduct.definition.id);
            return PurchaseProcessingResult.Complete;
        }

        public void OnInitializeFailed(InitializationFailureReason error) =>
            Debug.LogError($"IAP init failed: {error}");

        public void OnInitializeFailed(InitializationFailureReason error, string message) =>
            Debug.LogError($"IAP init failed: {error} — {message}");

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription desc) =>
            Debug.LogWarning($"Purchase failed: {product.definition.id} — {desc.reason}");

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) =>
            Debug.LogWarning($"Purchase failed: {product.definition.id} — {reason}");
    }
}
