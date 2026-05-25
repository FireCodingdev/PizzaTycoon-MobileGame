using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PizzaTycoon.Economy;
using PizzaTycoon.Managers;

namespace PizzaTycoon.UI
{
    // IMPORTANTE: este arquivo DEVE ter o mesmo nome da classe (UpgradeItemUI.cs)
    // para o Unity encontrar o componente via AddComponent<UpgradeItemUI>().
    // Anteriormente estava definido dentro de UpgradePanelUI.cs — isso causava
    // os warnings "referenced script (Unknown) on this Behaviour is missing".
    public class UpgradeItemUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Button _buyButton;
        [SerializeField] private GameObject _maxLevelLabel;

        private UpgradeData _data;
        private float _cost;
        private System.Action<UpgradeData> _onBuy;

        public void Setup(UpgradeData data, int currentLevel, float cost,
            float playerMoney, System.Action<UpgradeData> onBuy)
        {
            _data = data;
            _cost = cost;
            _onBuy = onBuy;

            if (_icon != null && data.icon != null) _icon.sprite = data.icon;
            if (_nameText != null)        _nameText.text        = data.displayName;
            if (_descriptionText != null) _descriptionText.text = data.description;
            if (_levelText != null)       _levelText.text       = $"Nivel {currentLevel}/{data.maxLevel}";

            bool isMax = data.IsMaxLevel(currentLevel);
            if (_maxLevelLabel != null) _maxLevelLabel.SetActive(isMax);

            if (_buyButton != null)
            {
                _buyButton.gameObject.SetActive(!isMax);
                _buyButton.onClick.RemoveAllListeners();
                _buyButton.onClick.AddListener(() => _onBuy?.Invoke(_data));
            }

            if (_costText != null)
                _costText.text = isMax ? "MAX" : $"${cost:F0}";

            UpdateAffordability(playerMoney);
        }

        public void UpdateAffordability(float playerMoney)
        {
            if (_buyButton == null || _data == null) return;
            int level = UpgradeManager.Instance?.GetLevel(_data.upgradeId) ?? 0;
            _buyButton.interactable = playerMoney >= _cost && !_data.IsMaxLevel(level);
        }
    }
}
