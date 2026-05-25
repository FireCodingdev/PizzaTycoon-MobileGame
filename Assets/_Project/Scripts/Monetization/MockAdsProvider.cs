using System;
using System.Collections;
using UnityEngine;

namespace PizzaTycoon.Monetization
{
    // Provider mock para testes no Editor sem SDK real
    // Simula delays e comportamento de anúncios reais
    public class MockAdsProvider : IAdsProvider
    {
        public bool IsInitialized { get; private set; }

        private MonoBehaviour _coroutineRunner;
        private bool _bannerVisible;

        public MockAdsProvider(MonoBehaviour coroutineRunner)
        {
            _coroutineRunner = coroutineRunner;
        }

        public void Initialize()
        {
            IsInitialized = true;
            Debug.Log("[MockAds] Inicializado.");
        }

        public bool IsInterstitialReady() => IsInitialized;

        public void ShowInterstitial(Action onComplete)
        {
            Debug.Log("[MockAds] Exibindo Interstitial (mock)...");
            _coroutineRunner.StartCoroutine(SimulateAd(1.5f, onComplete));
        }

        public bool IsRewardedReady() => IsInitialized;

        public void ShowRewarded(Action<bool> onResult)
        {
            Debug.Log("[MockAds] Exibindo Rewarded (mock)...");
            // 90% de chance de assistir completo no mock
            bool completed = UnityEngine.Random.value < 0.9f;
            _coroutineRunner.StartCoroutine(SimulateRewardedAd(2f, completed, onResult));
        }

        public void ShowBanner()
        {
            if (_bannerVisible) return;
            _bannerVisible = true;
            Debug.Log("[MockAds] Banner exibido.");
        }

        public void HideBanner()
        {
            _bannerVisible = false;
            Debug.Log("[MockAds] Banner escondido.");
        }

        private IEnumerator SimulateAd(float delay, Action onComplete)
        {
            yield return new WaitForSecondsRealtime(delay);
            Debug.Log("[MockAds] Interstitial concluído.");
            onComplete?.Invoke();
        }

        private IEnumerator SimulateRewardedAd(float delay, bool completed, Action<bool> onResult)
        {
            yield return new WaitForSecondsRealtime(delay);
            Debug.Log($"[MockAds] Rewarded concluído. Assistiu: {completed}");
            onResult?.Invoke(completed);
        }
    }
}
