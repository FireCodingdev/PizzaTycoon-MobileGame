using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PizzaTycoon.Skins
{
    // Painel de customização com abas de categoria e preview
    public class CustomizationUI : MonoBehaviour
    {
        [Header("Painel")]
        [SerializeField] private CanvasGroup        _canvasGroup;
        [SerializeField] private Button             _closeButton;

        [Header("Abas")]
        [SerializeField] private Button[]           _tabButtons;  // Player / Restaurante / Itens
        [SerializeField] private Transform[]        _tabContents;

        [Header("Grid de skins")]
        [SerializeField] private Transform          _skinGridPlayer;
        [SerializeField] private Transform          _skinGridRestaurant;
        [SerializeField] private SkinCardUI         _skinCardPrefab;

        [Header("Preview")]
        [SerializeField] private Image              _previewPrimary;
        [SerializeField] private Image              _previewSecondary;
        [SerializeField] private Image              _previewAccent;
        [SerializeField] private TextMeshProUGUI    _previewSkinName;

        [Header("Botão equipar")]
        [SerializeField] private Button             _equipButton;
        [SerializeField] private TextMeshProUGUI    _equipButtonLabel;

        private string _selectedSkinId;
        private bool   _initialized;

        private void Awake()
        {
            _closeButton?.onClick.AddListener(Hide);
            _equipButton?.onClick.AddListener(OnEquip);

            for (int i = 0; i < _tabButtons.Length; i++)
            {
                int idx = i;
                _tabButtons[i]?.onClick.AddListener(() => SwitchTab(idx));
            }

            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (!_initialized) BuildGrids();
            SwitchTab(0);
            StartCoroutine(FadeIn());
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // ── Abas ──────────────────────────────────────────────────────────────

        private void SwitchTab(int idx)
        {
            for (int i = 0; i < _tabContents.Length; i++)
                if (_tabContents[i] != null) _tabContents[i].gameObject.SetActive(i == idx);
        }

        // ── Grid de skins ─────────────────────────────────────────────────────

        private void BuildGrids()
        {
            _initialized = true;
            var manager = SkinManager.Instance;
            if (manager == null || _skinCardPrefab == null) return;

            BuildGrid(_skinGridPlayer,     SkinCategory.Player,     manager);
            BuildGrid(_skinGridRestaurant, SkinCategory.Restaurant, manager);
        }

        private void BuildGrid(Transform container, SkinCategory category, SkinManager manager)
        {
            if (container == null) return;
            foreach (Transform c in container) Destroy(c.gameObject);

            foreach (var skin in manager.GetByCategory(category))
            {
                if (skin == null) continue;
                var card = Instantiate(_skinCardPrefab, container);
                card.Setup(skin, manager.IsUnlocked(skin.skinId), OnSkinSelected);
            }
        }

        // ── Seleção ───────────────────────────────────────────────────────────

        private void OnSkinSelected(string skinId)
        {
            _selectedSkinId = skinId;
            var manager = SkinManager.Instance;
            if (manager == null) return;

            SkinData data = null;
            foreach (var s in manager.GetAll())
                if (s != null && s.skinId == skinId) { data = s; break; }

            if (data == null) return;

            // Atualiza preview
            if (_previewPrimary   != null) _previewPrimary.color   = data.primaryColor;
            if (_previewSecondary != null) _previewSecondary.color = data.secondaryColor;
            if (_previewAccent    != null) _previewAccent.color    = data.accentColor;
            if (_previewSkinName  != null) _previewSkinName.text   = data.displayName;

            bool unlocked = manager.IsUnlocked(skinId);
            string label  = unlocked ? "EQUIPAR"
                : data.coinCost > 0  ? $"COMPRAR ${data.coinCost}" : "BLOQUEADO";
            if (_equipButtonLabel != null) _equipButtonLabel.text = label;
            if (_equipButton      != null) _equipButton.interactable = unlocked || data.coinCost > 0;
        }

        private void OnEquip()
        {
            if (string.IsNullOrEmpty(_selectedSkinId)) return;
            var manager = SkinManager.Instance;
            if (manager == null) return;

            if (!manager.IsUnlocked(_selectedSkinId))
            {
                manager.TryPurchase(_selectedSkinId);
                BuildGrids(); // atualiza grid após compra
            }
            else
            {
                manager.Equip(_selectedSkinId);
            }
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

    // ── Card individual de skin ───────────────────────────────────────────────

    public class SkinCardUI : MonoBehaviour
    {
        [SerializeField] private Image              _colorPreview;
        [SerializeField] private TextMeshProUGUI    _nameText;
        [SerializeField] private TextMeshProUGUI    _statusText;
        [SerializeField] private Button             _selectButton;
        [SerializeField] private Image              _lockIcon;

        private System.Action<string> _onSelect;
        private string                _skinId;

        public void Setup(SkinData data, bool unlocked, System.Action<string> onSelect)
        {
            _skinId   = data.skinId;
            _onSelect = onSelect;

            if (_colorPreview != null) _colorPreview.color = data.primaryColor;
            if (_nameText     != null) _nameText.text      = data.displayName;
            if (_lockIcon     != null) _lockIcon.gameObject.SetActive(!unlocked);

            string status = unlocked ? "DESBLOQUEADO"
                : data.coinCost > 0  ? $"${data.coinCost}"
                : data.isPremium     ? "[VIP]"
                :                      "[Season]";
            if (_statusText != null) _statusText.text = status;

            _selectButton?.onClick.AddListener(() => _onSelect?.Invoke(_skinId));
        }
    }
}
