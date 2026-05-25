using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PizzaTycoon.Monetization;

namespace PizzaTycoon.UI
{
    // Card individual de produto na loja
    public class ShopItemCard : MonoBehaviour
    {
        [Header("Elementos visuais")]
        [SerializeField] private Image              _iconImage;
        [SerializeField] private TextMeshProUGUI    _nameText;
        [SerializeField] private TextMeshProUGUI    _descriptionText;
        [SerializeField] private TextMeshProUGUI    _priceText;
        [SerializeField] private Button             _buyButton;
        [SerializeField] private GameObject         _bestValueBadge;
        [SerializeField] private Image              _cardBackground;
        [SerializeField] private Color              _vipColor = new Color(1f, 0.84f, 0.0f);

        private string _productId;
        private System.Action<string> _onBuyCallback;

        public void Setup(string productId, string displayName, string description,
                          bool showBestValueBadge, System.Action<string> onBuy)
        {
            _productId      = productId;
            _onBuyCallback  = onBuy;

            if (_nameText        != null) _nameText.text        = displayName;
            if (_descriptionText != null) _descriptionText.text = description;
            if (_priceText       != null) _priceText.text       = IAPProducts.GetDisplayPrice(productId);
            if (_bestValueBadge  != null) _bestValueBadge.SetActive(showBestValueBadge);

            // Cor VIP para assinatura
            if (_cardBackground != null && productId == IAPProducts.VIP_WEEKLY)
                _cardBackground.color = _vipColor;

            // Ícone placeholder por cor
            if (_iconImage != null)
                _iconImage.color = GetProductColor(productId);

            _buyButton?.onClick.AddListener(OnBuyClicked);

            // Mostra estado VIP ativo se assinante
            UpdateBuyButtonState();
        }

        private void OnBuyClicked()
        {
            GetComponent<ButtonFeedback>()?.OnPointerDown(null);
            _onBuyCallback?.Invoke(_productId);
        }

        private void UpdateBuyButtonState()
        {
            if (_buyButton == null) return;
            bool owned = _productId == IAPProducts.REMOVE_ADS &&
                         IAPManager.Instance?.IsVIP == true;
            _buyButton.interactable = !owned;

            var label = _buyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = owned ? "ATIVO" : "COMPRAR";
        }

        private Color GetProductColor(string productId) => productId switch
        {
            IAPProducts.COINS_SMALL  => new Color(0.95f, 0.77f, 0.06f),
            IAPProducts.COINS_MEDIUM => new Color(0.90f, 0.50f, 0.13f),
            IAPProducts.COINS_LARGE  => new Color(0.91f, 0.30f, 0.24f),
            IAPProducts.REMOVE_ADS  => new Color(0.20f, 0.60f, 0.86f),
            IAPProducts.STARTER_PACK => new Color(0.15f, 0.68f, 0.38f),
            IAPProducts.VIP_WEEKLY   => new Color(1.00f, 0.84f, 0.00f),
            _                        => Color.white
        };
    }
}
