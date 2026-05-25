using System;
using System.Collections;
using UnityEngine;
using PizzaTycoon.Utils;

namespace PizzaTycoon.GameSystems
{
    // Gerencia o sistema de combo de vendas consecutivas
    // Thresholds: 5x→×1.1 / 10x→×1.25 / 20x→×1.5. Reset após 5s sem venda.
    public class ComboSystem : Singleton<ComboSystem>
    {
        [SerializeField] private float _comboTimeout  = 5f;

        private static readonly int[]   Thresholds   = { 5, 10, 20 };
        private static readonly float[] Multipliers  = { 1.1f, 1.25f, 1.5f };

        public int   CurrentCombo      { get; private set; }
        public float CurrentMultiplier { get; private set; } = 1f;

        public static event Action<int, float> OnComboUpdated;  // (combo, multiplier)
        public static event Action             OnComboReset;

        private Coroutine _timeoutCoroutine;

        // Chama após cada entrega bem-sucedida
        public float RegisterSale()
        {
            CurrentCombo++;
            CurrentMultiplier = GetMultiplier(CurrentCombo);

            if (_timeoutCoroutine != null) StopCoroutine(_timeoutCoroutine);
            _timeoutCoroutine = StartCoroutine(ComboTimeout());

            OnComboUpdated?.Invoke(CurrentCombo, CurrentMultiplier);
            return CurrentMultiplier;
        }

        // Quebra o combo (cliente saiu insatisfeito, etc.)
        public void BreakCombo()
        {
            if (CurrentCombo == 0) return;
            CurrentCombo      = 0;
            CurrentMultiplier = 1f;
            if (_timeoutCoroutine != null) StopCoroutine(_timeoutCoroutine);
            OnComboReset?.Invoke();
        }

        private IEnumerator ComboTimeout()
        {
            yield return new WaitForSeconds(_comboTimeout);
            BreakCombo();
        }

        private static float GetMultiplier(int combo)
        {
            float mult = 1f;
            for (int i = Thresholds.Length - 1; i >= 0; i--)
            {
                if (combo >= Thresholds[i])
                {
                    mult = Multipliers[i];
                    break;
                }
            }
            return mult;
        }
    }
}
