using System;
using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Utils;
using PizzaTycoon.Managers;

namespace PizzaTycoon.Decorations
{
    // Gerencia decorações compradas e seus bônus passivos
    public class DecorationSystem : Singleton<DecorationSystem>
    {
        [Header("Catálogo")]
        [SerializeField] private DecorationData[] _allDecorations;

        [Header("Posições de placement (cena)")]
        [SerializeField] private Transform[] _placementSlots;

        private HashSet<string> _purchased = new();

        public static event Action<DecorationData> OnDecorationPurchased;

        protected override void Awake()
        {
            base.Awake();
            LoadFromSave();
        }

        private void Start() => RebuildScene();

        // ── Compra ────────────────────────────────────────────────────────────

        public bool TryPurchase(string id)
        {
            DecorationData data = Find(id);
            if (data == null || _purchased.Contains(id)) return false;
            if (!MoneyManager.Instance.SpendMoney(data.coinCost)) return false;

            _purchased.Add(id);
            SaveManager.Instance?.CurrentData?.purchasedDecorationIds.Add(id);
            RebuildScene();
            OnDecorationPurchased?.Invoke(data);
            return true;
        }

        public bool IsPurchased(string id) => _purchased.Contains(id);

        // ── Bônus agregados ───────────────────────────────────────────────────

        public float GetTotalBonus(BonusType type)
        {
            float total = 0f;
            foreach (var id in _purchased)
            {
                DecorationData d = Find(id);
                if (d != null && d.bonusType == type)
                    total += d.bonusAmount;
            }
            return total;
        }

        public float CoinMultiplier    => 1f + GetTotalBonus(BonusType.CoinMultiplier);
        public float CustomerSpeedMult => 1f + GetTotalBonus(BonusType.CustomerSpeed);
        public float XPMultiplier      => 1f + GetTotalBonus(BonusType.XPMultiplier);
        public float PizzaSpeedMult    => 1f + GetTotalBonus(BonusType.PizzaSpeed);

        // ── Placement na cena ─────────────────────────────────────────────────

        private void RebuildScene()
        {
            // Limpa slots
            if (_placementSlots == null) return;
            foreach (var slot in _placementSlots)
            {
                if (slot == null) continue;
                foreach (Transform child in slot) Destroy(child.gameObject);
            }

            // Cria marcadores visuais (cube primitivo colorido) para cada compra
            int slotIdx = 0;
            foreach (var id in _purchased)
            {
                if (slotIdx >= _placementSlots.Length) break;
                DecorationData d = Find(id);
                if (d == null || _placementSlots[slotIdx] == null) { slotIdx++; continue; }

                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Deco_{d.displayName}";
                go.transform.SetParent(_placementSlots[slotIdx], false);
                go.transform.localScale = Vector3.one * 0.4f;

                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mpb = new MaterialPropertyBlock();
                    mpb.SetColor("_BaseColor", d.previewColor);
                    rend.SetPropertyBlock(mpb);
                }

                // Remove collider para não interferir no gameplay
                Destroy(go.GetComponent<Collider>());
                slotIdx++;
            }
        }

        // ── Consultas ─────────────────────────────────────────────────────────

        public DecorationData[] GetAll()                         => _allDecorations;
        public DecorationData[] GetByCategory(DecorationCategory cat)
        {
            var list = new List<DecorationData>();
            foreach (var d in _allDecorations)
                if (d != null && d.category == cat) list.Add(d);
            return list.ToArray();
        }

        private DecorationData Find(string id)
        {
            foreach (var d in _allDecorations)
                if (d != null && d.decorationId == id) return d;
            return null;
        }

        // ── Save / Load ───────────────────────────────────────────────────────

        private void LoadFromSave()
        {
            var data = SaveManager.Instance?.CurrentData;
            if (data?.purchasedDecorationIds == null) return;
            foreach (var id in data.purchasedDecorationIds)
                _purchased.Add(id);
        }
    }
}
