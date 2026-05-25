using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PizzaTycoon.Monetization;

namespace PizzaTycoon.UI
{
    public enum ShopTab { Coins, RemoveAds, VIP, Packs }

    // Painel fullscreen de loja com abas e cards de produto
    public class ShopUI : MonoBehaviour
    {
        [Header("Painel")]
        [SerializeField] private CanvasGroup    _canvasGroup;
        [SerializeField] private RectTransform  _panel;

        [Header("Abas")]
        [SerializeField] private Button[]       _tabButtons;   // Coins / RemoveAds / VIP / Packs
        [SerializeField] private GameObject[]   _tabContents;  // painéis de cada aba

        [Header("Scroll de produtos")]
        [SerializeField] private Transform      _coinsContainer;
        [SerializeField] private Transform      _adsContainer;
        [SerializeField] private Transform      _vipContainer;
        [SerializeField] private Transform      _packsContainer;

        [Header("Prefab de card")]
        [SerializeField] private ShopItemCard   _cardPrefab;

        [Header("Botões")]
        [SerializeField] private Button         _closeButton;
        [SerializeField] private Button         _restoreButton;

        [Header("Banner VIP")]
        [SerializeField] private GameObject     _vipBanner;
        [SerializeField] private TextMeshProUGUI _vipBannerText;

        private ShopTab _currentTab = ShopTab.Coins;
        private bool    _initialized;

        private void Awake()
        {
            _closeButton?.onClick.AddListener(Hide);
            _restoreButton?.onClick.AddListener(OnRestorePurchases);

            for (int i = 0; i < _tabButtons.Length; i++)
            {
                int idx = i;
                _tabButtons[i]?.onClick.AddListener(() => SwitchTab((ShopTab)idx));
            }

            IAPManager.OnPurchaseCompleted += OnPurchaseCompleted;
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            IAPManager.OnPurchaseCompleted -= OnPurchaseCompleted;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (!_initialized) BuildAllCards();
            UpdateVIPBanner();
            SwitchTab(ShopTab.Coins);
            StartCoroutine(FadeIn());
        }

        public void Hide()
        {
            StartCoroutine(FadeOut(() => gameObject.SetActive(false)));
        }

        // ── Abas ──────────────────────────────────────────────────────────────

        private void SwitchTab(ShopTab tab)
        {
            _currentTab = tab;
            for (int i = 0; i < _tabContents.Length; i++)
                if (_tabContents[i] != null) _tabContents[i].SetActive(i == (int)tab);
        }

        // ── Construção dos cards ──────────────────────────────────────────────

        private void BuildAllCards()
        {
            _initialized = true;

            BuildCards(_coinsContainer, new[]
            {
                (IAPProducts.COINS_SMALL,  "500 Moedas",    "Pacote pequeno de moedas",       false),
                (IAPProducts.COINS_MEDIUM, "2.500 Moedas",  "Melhor custo-beneficio!",        true),
                (IAPProducts.COINS_LARGE,  "10.000 Moedas", "Pacote de jogador dedicado",     false),
            });

            BuildCards(_adsContainer, new[]
            {
                (IAPProducts.REMOVE_ADS, "Remover Anuncios", "Jogue sem interrupcoes para sempre", false),
            });

            BuildCards(_vipContainer, new[]
            {
                (IAPProducts.VIP_WEEKLY, "VIP Semanal", "2x ganhos + skin exclusiva + sem banner ads", false),
            });

            BuildCards(_packsContainer, new[]
            {
                (IAPProducts.STARTER_PACK, "Pacote Inicial", "2.000 moedas + sem anuncios por 7 dias", false),
            });
        }

        private void BuildCards(Transform container,
            (string id, string name, string desc, bool badge)[] products)
        {
            if (container == null || _cardPrefab == null) return;

            foreach (Transform child in container) Destroy(child.gameObject);

            foreach (var (id, name, desc, badge) in products)
            {
                ShopItemCard card = Instantiate(_cardPrefab, container);
                card.Setup(id, name, desc, badge, OnCardPurchase);
            }
        }

        // ── Compra ────────────────────────────────────────────────────────────

        private void OnCardPurchase(string productId)
        {
            if (IAPManager.Instance == null) return;

            switch (productId)
            {
                case IAPProducts.COINS_SMALL:
                case IAPProducts.COINS_MEDIUM:
                case IAPProducts.COINS_LARGE:
                    IAPManager.Instance.BuyCoins(productId);
                    break;
                case IAPProducts.REMOVE_ADS:
                    IAPManager.Instance.BuyRemoveAds();
                    break;
                case IAPProducts.STARTER_PACK:
                    IAPManager.Instance.BuyStarterPack();
                    break;
                case IAPProducts.VIP_WEEKLY:
                    IAPManager.Instance.BuyVIPWeekly();
                    break;
            }
        }

        private void OnPurchaseCompleted(string productId)
        {
            UpdateVIPBanner();
        }

        private void OnRestorePurchases()
        {
            IAPManager.Instance?.RestorePurchases();
        }

        // ── Banner VIP ────────────────────────────────────────────────────────

        private void UpdateVIPBanner()
        {
            bool isVIP = IAPManager.Instance?.IsVIP == true;
            if (_vipBanner    != null) _vipBanner.SetActive(isVIP);
            if (_vipBannerText!= null && isVIP) _vipBannerText.text = "VIP ATIVO - 2x ganhos ativado!";
        }

        // ── Animações ─────────────────────────────────────────────────────────

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_canvasGroup != null) _canvasGroup.alpha = elapsed / 0.2f;
                yield return null;
            }
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOut(System.Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < 0.15f)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_canvasGroup != null) _canvasGroup.alpha = 1f - elapsed / 0.15f;
                yield return null;
            }
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            onComplete?.Invoke();
        }
    }
}
