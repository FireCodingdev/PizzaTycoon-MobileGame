using System;
using UnityEngine;
using PizzaTycoon.Utils;
using PizzaTycoon.UI;
using PizzaTycoon.GameSystems;

namespace PizzaTycoon.Managers
{
    // Gerencia a economia do jogo — dinheiro, eventos, texto flutuante e animação do HUD
    [DefaultExecutionOrder(-50)]
    public class MoneyManager : Singleton<MoneyManager>
    {
        [Header("Configurações")]
        [SerializeField] private float _startingMoney = 0f;

        [Header("HUD")]
        [SerializeField] private MoneyAnimator _moneyAnimator;

        public float CurrentMoney { get; private set; }

        public static event Action<float> OnMoneyChanged;
        public static event Action<float> OnMoneyAdded;
        public static event Action<float> OnMoneySpent;

        protected override void Awake()
        {
            base.Awake();
            CurrentMoney = _startingMoney;
        }

        private void Start()
        {
            var saveData = SaveManager.Instance?.CurrentData;
            if (saveData != null && saveData.money > 0)
            {
                CurrentMoney = saveData.money;
                _moneyAnimator?.SetImmediate(CurrentMoney);
                OnMoneyChanged?.Invoke(CurrentMoney);
            }
        }

        public void AddMoney(float amount, Vector3 worldPosition = default)
        {
            if (amount <= 0) return;

            // Aplica multiplicador de combo
            float mult = ComboSystem.Instance?.CurrentMultiplier ?? 1f;
            float final = amount * mult;

            CurrentMoney += final;
            OnMoneyChanged?.Invoke(CurrentMoney);
            OnMoneyAdded?.Invoke(final);

            _moneyAnimator?.AnimateTo(CurrentMoney);

            if (worldPosition != default)
            {
                string label = mult > 1f
                    ? $"+${final:F0} x{mult:F2}"
                    : $"+${final:F0}";
                FloatingText.Show(label, worldPosition, FloatingTextType.Money);
            }

            AudioManager.Instance?.PlayCoinPickup();
            DailyGoalSystem.Instance?.RegisterMoneyEarned(final);
        }

        public bool CanAfford(float amount) => CurrentMoney >= amount;

        public bool SpendMoney(float amount)
        {
            if (!CanAfford(amount)) return false;

            CurrentMoney -= amount;
            OnMoneyChanged?.Invoke(CurrentMoney);
            OnMoneySpent?.Invoke(amount);
            _moneyAnimator?.AnimateTo(CurrentMoney);
            return true;
        }

        public void ShowUnlockText(string message, Vector3 worldPosition)
        {
            FloatingText.Show(message, worldPosition, FloatingTextType.Unlock);
        }
    }
}
