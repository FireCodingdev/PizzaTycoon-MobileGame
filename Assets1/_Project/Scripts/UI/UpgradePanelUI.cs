using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PizzaTycoon.Economy;
using PizzaTycoon.Managers;

namespace PizzaTycoon.UI
{
    // Painel de upgrades — lista todos os upgrades disponíveis e permite comprar.
    // NOTA: UpgradeItemUI foi movida para UpgradeItemUI.cs (arquivo próprio),
    //       pois o Unity exige que o nome do arquivo .cs corresponda ao nome da classe
    //       para que AddComponent<UpgradeItemUI>() funcione corretamente.
    public class UpgradePanelUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Transform _upgradeListParent;
        [SerializeField] private UpgradeItemUI _upgradeItemPrefab;
        [SerializeField] private Button _closeButton;

        private readonly List<UpgradeItemUI> _upgradeItems = new List<UpgradeItemUI>();

        private void Awake()
        {
            _closeButton?.onClick.AddListener(Close);
            _panel?.SetActive(false);
        }

        private void OnEnable()
        {
            MoneyManager.OnMoneyChanged    += OnMoneyChanged;
            UpgradeManager.OnUpgradePurchased += OnUpgradePurchased;
        }

        private void OnDisable()
        {
            MoneyManager.OnMoneyChanged    -= OnMoneyChanged;
            UpgradeManager.OnUpgradePurchased -= OnUpgradePurchased;
        }

        public void Open()
        {
            _panel?.SetActive(true);
            RefreshAll();
        }

        public void Close()
        {
            AudioManager.Instance?.PlayButtonClick();
            _panel?.SetActive(false);
        }

        public void Toggle()
        {
            if (_panel != null && _panel.activeSelf) Close();
            else Open();
        }

        private void RefreshAll()
        {
            foreach (var item in _upgradeItems)
                if (item != null) Destroy(item.gameObject);
            _upgradeItems.Clear();

            if (UpgradeManager.Instance == null) return;

            float currentMoney = MoneyManager.Instance?.CurrentMoney ?? 0f;

            foreach (UpgradeData data in UpgradeManager.Instance.GetAllUpgrades())
            {
                if (_upgradeItemPrefab == null) break;
                UpgradeItemUI item = Instantiate(_upgradeItemPrefab, _upgradeListParent);
                item.gameObject.SetActive(true); // template is inactive; activate the clone
                int level   = UpgradeManager.Instance.GetLevel(data.upgradeId);
                float cost  = UpgradeManager.Instance.GetNextCost(data);
                item.Setup(data, level, cost, currentMoney, OnUpgradeButtonClicked);
                _upgradeItems.Add(item);
            }
        }

        private void OnUpgradeButtonClicked(UpgradeData data)
        {
            UpgradeManager.Instance?.TryPurchaseUpgrade(data);
        }

        private void OnMoneyChanged(float newAmount)
        {
            foreach (var item in _upgradeItems)
                item?.UpdateAffordability(newAmount);
        }

        private void OnUpgradePurchased(UpgradeData data, int newLevel)
        {
            RefreshAll();
        }
    }
}
