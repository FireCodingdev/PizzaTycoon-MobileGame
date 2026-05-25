using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PizzaTycoon.Seasons
{
    // Painel fullscreen do Season Pass
    public class SeasonPassUI : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TextMeshProUGUI    _titleText;
        [SerializeField] private TextMeshProUGUI    _timerText;
        [SerializeField] private Button             _premiumButton;
        [SerializeField] private TextMeshProUGUI    _premiumButtonLabel;

        [Header("XP")]
        [SerializeField] private Slider             _xpBar;
        [SerializeField] private TextMeshProUGUI    _xpText;
        [SerializeField] private TextMeshProUGUI    _levelText;

        [Header("Track")]
        [SerializeField] private ScrollRect         _trackScroll;
        [SerializeField] private Transform          _nodesParent;
        [SerializeField] private SeasonNodeUI       _nodePrefab;

        [Header("Botões")]
        [SerializeField] private Button             _claimAllButton;
        [SerializeField] private Button             _closeButton;

        [Header("Level Up VFX")]
        [SerializeField] private GameObject         _levelUpEffect;

        private bool _initialized;

        private void Awake()
        {
            _closeButton?.onClick.AddListener(Hide);
            _premiumButton?.onClick.AddListener(OnActivatePremium);
            _claimAllButton?.onClick.AddListener(OnClaimAll);

            SeasonManager.OnLevelUp    += OnLevelUp;
            SeasonManager.OnXPGained   += RefreshXPBar;
            SeasonManager.OnRewardClaimed += _ => RefreshNodes();

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            SeasonManager.OnLevelUp       -= OnLevelUp;
            SeasonManager.OnXPGained      -= RefreshXPBar;
            SeasonManager.OnRewardClaimed -= _ => RefreshNodes();
        }

        private void Update()
        {
            UpdateTimer();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (!_initialized) BuildNodes();
            RefreshAll();
        }

        public void Hide() => gameObject.SetActive(false);

        // ── Header ────────────────────────────────────────────────────────────

        private void RefreshAll()
        {
            var mgr = SeasonManager.Instance;
            if (mgr?.CurrentSeason == null) return;

            if (_titleText       != null) _titleText.text = $"Temporada {mgr.CurrentSeason.seasonNumber}: {mgr.CurrentSeason.theme}";
            if (_premiumButton   != null) _premiumButton.gameObject.SetActive(!mgr.IsPremium);
            if (_premiumButtonLabel != null)
                _premiumButtonLabel.text = mgr.IsPremium ? "PREMIUM ATIVO" : "ATIVAR PREMIUM";

            RefreshXPBar(0, mgr.CurrentXP);
            RefreshNodes();
        }

        private void UpdateTimer()
        {
            if (_timerText == null || SeasonManager.Instance == null) return;
            TimeSpan rem = SeasonManager.Instance.TimeRemaining();
            _timerText.text = $"Encerra em: {rem.Days}d {rem.Hours}h {rem.Minutes}m";
        }

        private void RefreshXPBar(int gained, int total)
        {
            var mgr = SeasonManager.Instance;
            if (mgr == null) return;

            if (_xpBar   != null) _xpBar.value = mgr.LevelProgress;
            if (_xpText  != null) _xpText.text = $"{total % 1000} / 1000 XP";
            if (_levelText != null) _levelText.text = $"Nível {mgr.CurrentLevel}";
        }

        // ── Nodes ─────────────────────────────────────────────────────────────

        private void BuildNodes()
        {
            _initialized = true;
            if (_nodesParent == null || _nodePrefab == null) return;

            foreach (Transform c in _nodesParent) Destroy(c.gameObject);

            var mgr = SeasonManager.Instance;
            if (mgr?.CurrentSeason == null) return;

            for (int i = 0; i < 30; i++)
            {
                var node = Instantiate(_nodePrefab, _nodesParent);
                SeasonReward free    = i < mgr.CurrentSeason.freeTrack.Length    ? mgr.CurrentSeason.freeTrack[i]    : null;
                SeasonReward premium = i < mgr.CurrentSeason.premiumTrack.Length  ? mgr.CurrentSeason.premiumTrack[i] : null;
                node.Setup(i + 1, free, premium, mgr.CurrentLevel, mgr.IsPremium);
            }
        }

        private void RefreshNodes()
        {
            if (_nodesParent == null) return;
            var mgr = SeasonManager.Instance;
            int childIdx = 0;
            foreach (Transform child in _nodesParent)
            {
                var node = child.GetComponent<SeasonNodeUI>();
                if (node != null && mgr?.CurrentSeason != null)
                {
                    int lvl      = childIdx + 1;
                    SeasonReward free    = childIdx < mgr.CurrentSeason.freeTrack.Length    ? mgr.CurrentSeason.freeTrack[childIdx]    : null;
                    SeasonReward premium = childIdx < mgr.CurrentSeason.premiumTrack.Length  ? mgr.CurrentSeason.premiumTrack[childIdx] : null;
                    node.Setup(lvl, free, premium, mgr.CurrentLevel, mgr.IsPremium);
                }
                childIdx++;
            }
        }

        // ── Ações ─────────────────────────────────────────────────────────────

        private void OnActivatePremium()
        {
            SeasonManager.Instance?.ActivatePremium();
            RefreshAll();
        }

        private void OnClaimAll()
        {
            // Rewards são concedidos automaticamente pelo SeasonManager — este botão é cosmético
            Debug.Log("[SeasonPassUI] Todas as recompensas pendentes coletadas.");
        }

        private void OnLevelUp(int level)
        {
            RefreshAll();
            if (_levelUpEffect != null)
                StartCoroutine(ShowLevelUpEffect());

            // Scroll até o node atual
            ScrollToCurrentLevel(level);
        }

        private IEnumerator ShowLevelUpEffect()
        {
            _levelUpEffect.SetActive(true);
            yield return new WaitForSeconds(2f);
            _levelUpEffect.SetActive(false);
        }

        private void ScrollToCurrentLevel(int level)
        {
            if (_trackScroll == null) return;
            float pos = Mathf.Clamp01((float)(level - 1) / 30f);
            _trackScroll.horizontalNormalizedPosition = pos;
        }
    }

    // ── Node individual da trilha ─────────────────────────────────────────────

    public class SeasonNodeUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _levelLabel;
        [SerializeField] private Image           _freeIcon;
        [SerializeField] private Image           _premiumIcon;
        [SerializeField] private Image           _lockOverlay;
        [SerializeField] private Image           _currentIndicator; // pulsa no level atual
        [SerializeField] private Color           _unlockedColor = new Color(0.18f, 0.80f, 0.44f);
        [SerializeField] private Color           _lockedColor   = new Color(0.5f,  0.5f,  0.5f);

        public void Setup(int level, SeasonReward free, SeasonReward premium,
                          int currentLevel, bool isPremium)
        {
            if (_levelLabel != null) _levelLabel.text = level.ToString();

            bool unlocked = level <= currentLevel;

            if (_freeIcon    != null && free    != null)
            {
                if (ColorUtility.TryParseHtmlString(free.iconColor, out Color c))
                    _freeIcon.color = unlocked ? c : _lockedColor;
            }
            if (_premiumIcon != null && premium != null)
            {
                if (ColorUtility.TryParseHtmlString(premium.iconColor, out Color c))
                    _premiumIcon.color = (unlocked && isPremium) ? c : _lockedColor;
                if (_lockOverlay != null)
                    _lockOverlay.gameObject.SetActive(!isPremium);
            }

            if (_currentIndicator != null)
                _currentIndicator.gameObject.SetActive(level == currentLevel + 1);
        }
    }
}
