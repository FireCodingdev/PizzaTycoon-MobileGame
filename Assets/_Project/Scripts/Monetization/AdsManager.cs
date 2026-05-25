using System;
using System.Collections;
using UnityEngine;
using PizzaTycoon.Utils;
using PizzaTycoon.Managers;

namespace PizzaTycoon.Monetization
{
    // Gerencia todos os anúncios do jogo via IAdsProvider injetável
    // Por padrão usa MockAdsProvider — troque pelo SDK real sem mudar este arquivo
    public class AdsManager : Singleton<AdsManager>
    {
        [Header("Configurações")]
        [SerializeField] private int   _interstitialFrequency = 3;  // a cada N upgrades
        [SerializeField] private float _rewardedSpeedDuration = 120f; // segundos de 2x velocidade
        [SerializeField] private float _rewardedCoinsBonus    = 100f;
        [SerializeField] private bool  _adsRemoved            = false;

        // Provider injetável — substitua por AdMobProvider, UnityAdsProvider, etc.
        private IAdsProvider _provider;

        private int  _upgradeCount;
        private bool _speedBoostActive;

        public static event Action       OnAdStarted;
        public static event Action       OnAdFinished;
        public static event Action<float> OnSpeedBoostStarted; // segundos de duração

        // Propriedade verificada por RewardedButtonUI
        public bool AdsRemoved
        {
            get => _adsRemoved || SaveManager.Instance?.CurrentData?.adsRemoved == true;
            set
            {
                _adsRemoved = value;
                if (SaveManager.Instance != null)
                    SaveManager.Instance.CurrentData.adsRemoved = value;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _provider = new MockAdsProvider(this);
            _provider.Initialize();

            // Verifica estado salvo
            if (SaveManager.Instance?.CurrentData != null)
                _adsRemoved = SaveManager.Instance.CurrentData.adsRemoved;
        }

        // ── Interstitial ─────────────────────────────────────────────────────

        // Chame após cada compra de upgrade
        public void OnUpgradePurchased()
        {
            if (AdsRemoved) return;
            _upgradeCount++;
            if (_upgradeCount >= _interstitialFrequency)
            {
                _upgradeCount = 0;
                ShowInterstitial();
            }
        }

        private void ShowInterstitial()
        {
            if (!_provider.IsInterstitialReady()) return;
            OnAdStarted?.Invoke();
            Time.timeScale = 0f;
            _provider.ShowInterstitial(() =>
            {
                Time.timeScale = 1f;
                OnAdFinished?.Invoke();
            });
        }

        // ── Rewarded ──────────────────────────────────────────────────────────

        // Retorna true se o botão de rewarded deve estar disponível
        public bool IsRewardedAvailable() =>
            _provider.IsRewardedReady() && !_speedBoostActive;

        // Recompensa: 2x velocidade por 2 minutos
        public void ShowRewardedForSpeedBoost()
        {
            if (!_provider.IsRewardedReady()) return;
            OnAdStarted?.Invoke();
            Time.timeScale = 0f;
            _provider.ShowRewarded(completed =>
            {
                Time.timeScale = 1f;
                OnAdFinished?.Invoke();
                if (completed)
                    StartCoroutine(ApplySpeedBoost());
            });
        }

        // Recompensa: dobrar ganhos offline
        public void ShowRewardedForOfflineBonus(float offlineAmount)
        {
            if (!_provider.IsRewardedReady()) return;
            OnAdStarted?.Invoke();
            Time.timeScale = 0f;
            _provider.ShowRewarded(completed =>
            {
                Time.timeScale = 1f;
                OnAdFinished?.Invoke();
                if (completed)
                    MoneyManager.Instance?.AddMoney(offlineAmount); // dobra o valor
            });
        }

        // Recompensa: +$100 de bônus imediato
        public void ShowRewardedForCoins()
        {
            if (!_provider.IsRewardedReady()) return;
            OnAdStarted?.Invoke();
            Time.timeScale = 0f;
            _provider.ShowRewarded(completed =>
            {
                Time.timeScale = 1f;
                OnAdFinished?.Invoke();
                if (completed)
                    MoneyManager.Instance?.AddMoney(_rewardedCoinsBonus);
            });
        }

        // ── Banner ────────────────────────────────────────────────────────────

        public void ShowBanner()
        {
            if (AdsRemoved) return;
            _provider.ShowBanner();
        }

        public void HideBanner() => _provider.HideBanner();

        // ── Velocidade 2x ────────────────────────────────────────────────────

        private IEnumerator ApplySpeedBoost()
        {
            _speedBoostActive = true;
            float remaining   = _rewardedSpeedDuration;

            OnSpeedBoostStarted?.Invoke(remaining);

            // Aplica 2x em todos os PlayerControllers
            var player = FindObjectOfType<Player.PlayerController>();
            float originalSpeed = player != null ? player.MoveSpeed : 5f;
            if (player != null) player.MoveSpeed = originalSpeed * 2f;

            while (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                yield return null;
            }

            if (player != null) player.MoveSpeed = originalSpeed;
            _speedBoostActive = false;
        }

        // ── Provider injection ────────────────────────────────────────────────

        // Chame para trocar o mock pelo SDK real em runtime
        public void SetProvider(IAdsProvider provider)
        {
            _provider = provider;
            if (!provider.IsInitialized) provider.Initialize();
        }
    }
}
