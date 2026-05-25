using System;

namespace PizzaTycoon.Monetization
{
    // Interface para qualquer SDK de anúncios (AdMob, Unity Ads, etc.)
    // Implemente esta interface com o SDK real e injete em AdsManager
    public interface IAdsProvider
    {
        void Initialize();

        bool IsInterstitialReady();
        void ShowInterstitial(Action onComplete);

        bool IsRewardedReady();
        void ShowRewarded(Action<bool> onResult); // bool = assistiu completo

        void ShowBanner();
        void HideBanner();

        bool IsInitialized { get; }
    }
}
