using System;
using UnityEngine;
using PizzaTycoon.Utils;
using PizzaTycoon.Managers;

namespace PizzaTycoon.Map
{
    // Gerencia o desbloqueio do setor de entrega (Delivery Sector) por $5000
    public class MapExpansion : Singleton<MapExpansion>
    {
        [Header("Custo de desbloqueio")]
        [SerializeField] private int _deliverySectorCost = 5000;

        [Header("Objetos de cena")]
        [SerializeField] private GameObject _deliverySectorRoot;   // ativado ao desbloquear
        [SerializeField] private GameObject _lockedSectorOverlay;  // desativado ao desbloquear

        public bool DeliverySectorUnlocked { get; private set; }

        public static event Action OnDeliverySectorUnlocked;

        protected override void Awake()
        {
            base.Awake();
            MoneyManager.OnMoneyChanged += OnMoneyChanged;
        }

        private void OnDestroy()
        {
            MoneyManager.OnMoneyChanged -= OnMoneyChanged;
        }

        private void Start()
        {
            var data = SaveManager.Instance?.CurrentData;
            if (data != null && data.deliverySectorUnlocked)
                ApplyUnlock(notify: false);
        }

        private void OnMoneyChanged(float total)
        {
            // Sugere desbloqueio ao atingir a meta
            if (!DeliverySectorUnlocked && total >= _deliverySectorCost)
                Debug.Log($"[MapExpansion] Setor de Entrega disponível por ${_deliverySectorCost}!");
        }

        public bool TryUnlockDeliverySector()
        {
            if (DeliverySectorUnlocked) return false;
            if (!MoneyManager.Instance.SpendMoney(_deliverySectorCost)) return false;
            ApplyUnlock(notify: true);
            return true;
        }

        private void ApplyUnlock(bool notify)
        {
            DeliverySectorUnlocked = true;

            if (_deliverySectorRoot  != null) _deliverySectorRoot.SetActive(true);
            if (_lockedSectorOverlay != null) _lockedSectorOverlay.SetActive(false);

            var data = SaveManager.Instance?.CurrentData;
            if (data != null) data.deliverySectorUnlocked = true;

            if (notify)
            {
                OnDeliverySectorUnlocked?.Invoke();
                Debug.Log("[MapExpansion] Setor de Entrega desbloqueado!");
            }
        }

        public int DeliverySectorCost => _deliverySectorCost;
    }
}
