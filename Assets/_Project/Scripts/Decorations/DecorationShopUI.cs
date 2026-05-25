using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PizzaTycoon.Decorations
{
    public class DecorationShopUI : MonoBehaviour
    {
        [Header("Painel")]
        [SerializeField] private CanvasGroup        _canvasGroup;
        [SerializeField] private Button             _closeButton;

        [Header("Filtro de categorias")]
        [SerializeField] private Button[]           _categoryButtons;   // Interior/Exterior/Equipment/Seasonal/Todos
        [SerializeField] private TextMeshProUGUI[]  _categoryLabels;

        [Header("Grid")]
        [SerializeField] private Transform          _grid;
        [SerializeField] private DecorationCardUI   _cardPrefab;

        [Header("Painel de bônus totais")]
        [SerializeField] private TextMeshProUGUI    _totalCoinsText;
        [SerializeField] private TextMeshProUGUI    _totalXPText;
        [SerializeField] private TextMeshProUGUI    _totalSpeedText;

        private int _activeFilter = -1; // -1 = todos

        private void Awake()
        {
            _closeButton?.onClick.AddListener(Hide);

            for (int i = 0; i < _categoryButtons.Length; i++)
            {
                int idx = i;
                _categoryButtons[i]?.onClick.AddListener(() => FilterBy(idx));
            }

            DecorationSystem.OnDecorationPurchased += _ => Refresh();
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            DecorationSystem.OnDecorationPurchased -= _ => Refresh();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            _activeFilter = -1;
            Refresh();
            StartCoroutine(FadeIn());
        }

        public void Hide() => gameObject.SetActive(false);

        private void FilterBy(int categoryIndex)
        {
            _activeFilter = categoryIndex == _activeFilter ? -1 : categoryIndex;
            Refresh();
        }

        private void Refresh()
        {
            var sys = DecorationSystem.Instance;
            if (sys == null || _grid == null || _cardPrefab == null) return;

            foreach (Transform child in _grid) Destroy(child.gameObject);

            DecorationData[] list = _activeFilter < 0
                ? sys.GetAll()
                : sys.GetByCategory((DecorationCategory)_activeFilter);

            foreach (var d in list)
            {
                if (d == null) continue;
                var card = Instantiate(_cardPrefab, _grid);
                card.Setup(d, sys.IsPurchased(d.decorationId), OnBuy);
            }

            RefreshBonusSummary(sys);
        }

        private void RefreshBonusSummary(DecorationSystem sys)
        {
            float coins = (sys.CoinMultiplier - 1f) * 100f;
            float xp    = (sys.XPMultiplier   - 1f) * 100f;
            float speed = (sys.PizzaSpeedMult  - 1f) * 100f;

            if (_totalCoinsText != null) _totalCoinsText.text = $"Coins: +{coins:F0}%";
            if (_totalXPText    != null) _totalXPText.text    = $"XP: +{xp:F0}%";
            if (_totalSpeedText != null) _totalSpeedText.text  = $"Velocidade: +{speed:F0}%";
        }

        private void OnBuy(string id)
        {
            DecorationSystem.Instance?.TryPurchase(id);
        }

        private IEnumerator FadeIn()
        {
            if (_canvasGroup == null) yield break;
            _canvasGroup.alpha = 0f;
            for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                _canvasGroup.alpha = t / 0.2f;
                yield return null;
            }
            _canvasGroup.alpha = 1f;
        }
    }

    // ── Card individual de decoração ──────────────────────────────────────────

    public class DecorationCardUI : MonoBehaviour
    {
        [SerializeField] private Image              _colorSwatch;
        [SerializeField] private TextMeshProUGUI    _nameText;
        [SerializeField] private TextMeshProUGUI    _bonusText;
        [SerializeField] private TextMeshProUGUI    _costText;
        [SerializeField] private Button             _buyButton;
        [SerializeField] private TextMeshProUGUI    _buyLabel;
        [SerializeField] private Image              _checkmark;

        private System.Action<string> _onBuy;
        private string                _id;

        public void Setup(DecorationData data, bool purchased, System.Action<string> onBuy)
        {
            _id    = data.decorationId;
            _onBuy = onBuy;

            if (_colorSwatch != null) _colorSwatch.color = data.previewColor;
            if (_nameText    != null) _nameText.text     = data.displayName;
            if (_bonusText   != null) _bonusText.text    = data.bonusDescription;
            if (_costText    != null) _costText.text     = purchased ? "" : $"${data.coinCost}";
            if (_checkmark   != null) _checkmark.gameObject.SetActive(purchased);

            if (_buyButton != null)
            {
                _buyButton.interactable = !purchased;
                _buyButton.onClick.AddListener(() => _onBuy?.Invoke(_id));
            }
            if (_buyLabel != null) _buyLabel.text = purchased ? "COMPRADO" : "COMPRAR";
        }
    }
}
