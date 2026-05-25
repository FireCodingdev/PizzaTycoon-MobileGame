using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Utils;
using PizzaTycoon.Player;
using PizzaTycoon.Stations;
using PizzaTycoon.Customers;

namespace PizzaTycoon.Economy
{
    // Aplica upgrades ao jogo — lê dados do SaveManager e modifica componentes em runtime
    public class UpgradeManager : Singleton<UpgradeManager>
    {
        [SerializeField] private List<UpgradeData> _availableUpgrades = new List<UpgradeData>();

        // Referências para aplicar upgrades diretamente
        [Header("Referências de Runtime")]
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PlayerStacker _playerStacker;
        [SerializeField] private List<BaseStation> _allStations;
        [SerializeField] private CustomerSpawner _customerSpawner;
        [SerializeField] private OvenStation _ovenStation;

        private readonly Dictionary<string, int> _upgradeLevels = new Dictionary<string, int>();

        public static System.Action<UpgradeData, int> OnUpgradePurchased;

        protected override void Awake()
        {
            base.Awake();
            LoadUpgradeLevels();
        }

        private void Start()
        {
            ApplyAllUpgrades();
        }

        public bool TryPurchaseUpgrade(UpgradeData data)
        {
            if (data == null) return false;

            int currentLevel = GetLevel(data.upgradeId);
            if (data.IsMaxLevel(currentLevel)) return false;

            float cost = data.GetCostForLevel(currentLevel + 1);
            if (!PizzaTycoon.Managers.MoneyManager.Instance.CanAfford(cost)) return false;

            if (!PizzaTycoon.Managers.MoneyManager.Instance.SpendMoney(cost)) return false;

            int newLevel = currentLevel + 1;
            _upgradeLevels[data.upgradeId] = newLevel;

            // Persiste no save
            PizzaTycoon.Managers.SaveManager.Instance?.UpdateUpgradeLevel(data.upgradeId, newLevel);

            ApplyUpgrade(data, newLevel);
            OnUpgradePurchased?.Invoke(data, newLevel);

            PizzaTycoon.Managers.AudioManager.Instance?.PlayUpgradePurchase();
            return true;
        }

        public int GetLevel(string upgradeId)
        {
            return _upgradeLevels.TryGetValue(upgradeId, out int level) ? level : 0;
        }

        public float GetNextCost(UpgradeData data)
        {
            if (data == null) return 0f;
            int currentLevel = GetLevel(data.upgradeId);
            return data.IsMaxLevel(currentLevel) ? 0f : data.GetCostForLevel(currentLevel + 1);
        }

        private void LoadUpgradeLevels()
        {
            var saveData = PizzaTycoon.Managers.SaveManager.Instance?.CurrentData;
            if (saveData == null) return;

            _upgradeLevels["playerSpeed"] = saveData.playerSpeedLevel;
            _upgradeLevels["stackCapacity"] = saveData.stackCapacityLevel;
            _upgradeLevels["productionSpeed"] = saveData.productionSpeedLevel;
            // ovenSlots persistido como um nivel adicional opcional
            _upgradeLevels["ovenSlots"] = 0;
        }

        private void ApplyAllUpgrades()
        {
            foreach (var upgrade in _availableUpgrades)
            {
                int level = GetLevel(upgrade.upgradeId);
                if (level > 0)
                    ApplyUpgrade(upgrade, level);
            }
        }

        private void ApplyUpgrade(UpgradeData data, int level)
        {
            float value = data.GetValueForLevel(level);

            switch (data.effectType)
            {
                case UpgradeEffectType.PlayerSpeed:
                    if (_playerController != null)
                        _playerController.MoveSpeed = value;
                    break;

                case UpgradeEffectType.StackCapacity:
                    if (_playerStacker != null)
                        _playerStacker.MaxStackSize = Mathf.RoundToInt(value);
                    break;

                case UpgradeEffectType.ProductionSpeed:
                    foreach (var station in _allStations)
                        if (station != null)
                            station.ProductionInterval = 1f / value;
                    break;

                case UpgradeEffectType.SpawnInterval:
                    if (_customerSpawner != null)
                        _customerSpawner.SpawnInterval = value;
                    break;

                case UpgradeEffectType.OvenSlots:
                    if (_ovenStation != null)
                        _ovenStation.OvenSlots = Mathf.RoundToInt(value);
                    break;
            }
        }

        public List<UpgradeData> GetAllUpgrades() => _availableUpgrades;
    }
}
