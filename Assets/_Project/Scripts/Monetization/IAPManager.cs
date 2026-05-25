using System;
using UnityEngine;
using PizzaTycoon.Utils;
using PizzaTycoon.Managers;

namespace PizzaTycoon.Monetization
{
    // Gerencia compras in-app via IIAPProvider injetável
    public class IAPManager : Singleton<IAPManager>
    {
        private IIAPProvider _provider;

        public bool IsInitialized => _provider?.IsInitialized == true;

        public static event Action<string> OnPurchaseCompleted;
        public static event Action<string> OnPurchaseFailed;

        protected override void Awake()
        {
            base.Awake();
            _provider = new MockIAPProvider(this);
            _provider.Initialize(
                IAPProducts.All,
                onSuccess: () => Debug.Log("[IAPManager] IAP pronto."),
                onFail:    err => Debug.LogWarning($"[IAPManager] Falha ao inicializar: {err}")
            );
        }

        // ── Compras ───────────────────────────────────────────────────────────

        public void BuyCoins(string productId)
        {
            if (!IsInitialized) return;
            _provider.Purchase(productId, success =>
            {
                if (!success) { OnPurchaseFailed?.Invoke(productId); return; }

                int coins = IAPProducts.GetCoinsForProduct(productId);
                MoneyManager.Instance?.AddMoney(coins);
                OnPurchaseCompleted?.Invoke(productId);
            });
        }

        public void BuyRemoveAds()
        {
            if (!IsInitialized) return;
            _provider.Purchase(IAPProducts.REMOVE_ADS, success =>
            {
                if (!success) { OnPurchaseFailed?.Invoke(IAPProducts.REMOVE_ADS); return; }

                AdsManager.Instance?.HideBanner();
                if (AdsManager.Instance != null) AdsManager.Instance.AdsRemoved = true;
                OnPurchaseCompleted?.Invoke(IAPProducts.REMOVE_ADS);
                Debug.Log("[IAPManager] Anúncios removidos.");
            });
        }

        public void BuyStarterPack()
        {
            if (!IsInitialized) return;
            _provider.Purchase(IAPProducts.STARTER_PACK, success =>
            {
                if (!success) { OnPurchaseFailed?.Invoke(IAPProducts.STARTER_PACK); return; }

                int coins = IAPProducts.GetCoinsForProduct(IAPProducts.STARTER_PACK);
                MoneyManager.Instance?.AddMoney(coins);
                // Remove ads por 7 dias — salvo como flag com timestamp
                SaveManager.Instance.CurrentData.starterPackExpiry =
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 7 * 86400;
                OnPurchaseCompleted?.Invoke(IAPProducts.STARTER_PACK);
            });
        }

        public void BuyVIPWeekly()
        {
            if (!IsInitialized) return;
            _provider.Purchase(IAPProducts.VIP_WEEKLY, success =>
            {
                if (!success) { OnPurchaseFailed?.Invoke(IAPProducts.VIP_WEEKLY); return; }

                SaveManager.Instance.CurrentData.isVIP = true;
                OnPurchaseCompleted?.Invoke(IAPProducts.VIP_WEEKLY);
                Debug.Log("[IAPManager] VIP ativado.");
            });
        }

        public void RestorePurchases()
        {
            // Com SDK real: chame o método de restore do provider
            // Com mock: verifica produtos owned e reaplicar benefícios
            Debug.Log("[IAPManager] Restaurando compras...");
            if (_provider.IsProductOwned(IAPProducts.REMOVE_ADS))
            {
                if (AdsManager.Instance != null) AdsManager.Instance.AdsRemoved = true;
                AdsManager.Instance?.HideBanner();
            }
        }

        public bool IsVIP => SaveManager.Instance?.CurrentData?.isVIP == true;

        public bool AdsRemovedByPack()
        {
            var data = SaveManager.Instance?.CurrentData;
            if (data == null) return false;
            return data.starterPackExpiry > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        // ── Provider injection ────────────────────────────────────────────────

        public void SetProvider(IIAPProvider provider)
        {
            _provider = provider;
            _provider.Initialize(
                IAPProducts.All,
                onSuccess: () => Debug.Log("[IAPManager] Provider real pronto."),
                onFail:    err => Debug.LogWarning($"[IAPManager] {err}")
            );
        }
    }
}
