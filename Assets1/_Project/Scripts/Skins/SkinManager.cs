using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Utils;
using PizzaTycoon.Managers;

namespace PizzaTycoon.Skins
{
    // Aplica skins via MaterialPropertyBlock — sem criar novos materials
    public class SkinManager : Singleton<SkinManager>
    {
        [Header("Dados")]
        [SerializeField] private SkinData[] _allSkins;

        [Header("Referências de cena")]
        [SerializeField] private Renderer[] _playerRenderers;
        [SerializeField] private Renderer[] _restaurantRenderers;

        private HashSet<string> _unlockedSkins = new();
        private string _equippedPlayer      = "classic";
        private string _equippedRestaurant  = "classic_italian";

        private static readonly int BaseColorProp    = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionProp     = Shader.PropertyToID("_EmissionColor");

        protected override void Awake()
        {
            base.Awake();
            LoadFromSave();
        }

        private void Start()
        {
            ApplyEquipped();
        }

        // ── Desbloqueio ───────────────────────────────────────────────────────

        public bool IsUnlocked(string skinId) => _unlockedSkins.Contains(skinId);

        public bool TryPurchase(string skinId)
        {
            SkinData data = FindSkin(skinId);
            if (data == null || IsUnlocked(skinId)) return false;
            if (data.coinCost > 0 && !MoneyManager.Instance.SpendMoney(data.coinCost))
                return false;

            _unlockedSkins.Add(skinId);
            SaveManager.Instance?.CurrentData?.unlockedSkinIds.Add(skinId);
            return true;
        }

        // Chamado pelo SeasonManager ao desbloquear recompensa de skin
        public void UnlockSkin(string skinId)
        {
            if (!_unlockedSkins.Contains(skinId))
            {
                _unlockedSkins.Add(skinId);
                SaveManager.Instance?.CurrentData?.unlockedSkinIds.Add(skinId);
            }
        }

        // ── Equipar ───────────────────────────────────────────────────────────

        public void Equip(string skinId)
        {
            SkinData data = FindSkin(skinId);
            if (data == null || !IsUnlocked(skinId)) return;

            switch (data.category)
            {
                case SkinCategory.Player:
                    _equippedPlayer = skinId;
                    SaveManager.Instance.CurrentData.equippedPlayerSkin = skinId;
                    ApplyToRenderers(_playerRenderers, data);
                    break;

                case SkinCategory.Restaurant:
                    _equippedRestaurant = skinId;
                    SaveManager.Instance.CurrentData.equippedRestaurantSkin = skinId;
                    ApplyToRenderers(_restaurantRenderers, data);
                    break;
            }
        }

        // ── Aplicação via MaterialPropertyBlock ───────────────────────────────

        private void ApplyEquipped()
        {
            Equip(_equippedPlayer);
            Equip(_equippedRestaurant);
        }

        private void ApplyToRenderers(Renderer[] renderers, SkinData skin)
        {
            if (renderers == null) return;
            var mpb = new MaterialPropertyBlock();

            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorProp, skin.primaryColor);
                r.SetPropertyBlock(mpb);
            }
        }

        // Aplica preview de skin (sem salvar) — para CustomizationUI
        public void PreviewSkin(string skinId, Renderer[] targetRenderers)
        {
            SkinData data = FindSkin(skinId);
            if (data == null) return;
            ApplyToRenderers(targetRenderers, data);
        }

        // ── Consultas ─────────────────────────────────────────────────────────

        public SkinData[] GetAll()          => _allSkins ?? System.Array.Empty<SkinData>();
        public SkinData[] GetByCategory(SkinCategory cat)
        {
            var result = new System.Collections.Generic.List<SkinData>();
            if (_allSkins == null) return result.ToArray();
            foreach (var s in _allSkins)
                if (s != null && s.category == cat) result.Add(s);
            return result.ToArray();
        }

        public string EquippedPlayer     => _equippedPlayer;
        public string EquippedRestaurant => _equippedRestaurant;

        private SkinData FindSkin(string id)
        {
            if (_allSkins == null) return null;
            foreach (var s in _allSkins)
                if (s != null && s.skinId == id) return s;
            return null;
        }

        // ── Save / Load ───────────────────────────────────────────────────────

        private void LoadFromSave()
        {
            var data = SaveManager.Instance?.CurrentData;
            if (data == null) return;

            _unlockedSkins.Clear();
            _unlockedSkins.Add("classic");           // padrão sempre desbloqueado
            _unlockedSkins.Add("classic_italian");

            if (data.unlockedSkinIds != null)
                foreach (var id in data.unlockedSkinIds)
                    _unlockedSkins.Add(id);

            _equippedPlayer     = data.equippedPlayerSkin    ?? "classic";
            _equippedRestaurant = data.equippedRestaurantSkin?? "classic_italian";
        }
    }
}
