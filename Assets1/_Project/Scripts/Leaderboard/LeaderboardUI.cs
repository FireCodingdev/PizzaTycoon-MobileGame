using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PizzaTycoon.Leaderboard
{
    public class LeaderboardUI : MonoBehaviour
    {
        [Header("Painel")]
        [SerializeField] private CanvasGroup        _canvasGroup;
        [SerializeField] private Button             _closeButton;

        [Header("Abas de categoria")]
        [SerializeField] private Button             _tabCoins;
        [SerializeField] private Button             _tabSold;
        [SerializeField] private Button             _tabCombo;

        [Header("Lista")]
        [SerializeField] private Transform          _entriesParent;
        [SerializeField] private LeaderboardRowUI   _rowPrefab;

        [Header("Rank do jogador")]
        [SerializeField] private TextMeshProUGUI    _playerRankText;

        private LeaderboardCategory _activeCategory = LeaderboardCategory.TotalCoins;

        private void Awake()
        {
            _closeButton?.onClick.AddListener(Hide);
            _tabCoins?.onClick.AddListener(() => SwitchTab(LeaderboardCategory.TotalCoins));
            _tabSold ?.onClick.AddListener(() => SwitchTab(LeaderboardCategory.PizzasSold));
            _tabCombo?.onClick.AddListener(() => SwitchTab(LeaderboardCategory.BestCombo));
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            LeaderboardManager.Instance?.SubmitCurrentStats();
            SwitchTab(_activeCategory);
            StartCoroutine(FadeIn());
        }

        public void Hide() => gameObject.SetActive(false);

        private void SwitchTab(LeaderboardCategory cat)
        {
            _activeCategory = cat;
            BuildList(cat);
        }

        private void BuildList(LeaderboardCategory cat)
        {
            if (_entriesParent == null || _rowPrefab == null) return;
            foreach (Transform c in _entriesParent) Destroy(c.gameObject);

            var mgr = LeaderboardManager.Instance;
            if (mgr == null) return;

            List<LeaderboardEntry> entries = mgr.GetTop(cat);
            StartCoroutine(AnimateRows(entries));

            int rank = mgr.GetPlayerRank(cat);
            if (_playerRankText != null)
                _playerRankText.text = rank > 0 ? $"Seu rank: #{rank}" : "Fora do top 10";
        }

        private IEnumerator AnimateRows(List<LeaderboardEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var row = Instantiate(_rowPrefab, _entriesParent);
                row.Setup(i + 1, entries[i]);
                row.AnimateIn(i * 0.06f);
                yield return new WaitForSecondsRealtime(0.05f);
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

    // ── Linha individual do ranking ───────────────────────────────────────────

    public class LeaderboardRowUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI    _rankText;
        [SerializeField] private TextMeshProUGUI    _nameText;
        [SerializeField] private TextMeshProUGUI    _scoreText;
        [SerializeField] private Image              _background;
        [SerializeField] private Color              _goldColor   = new Color(1f,  0.84f, 0f);
        [SerializeField] private Color              _silverColor = new Color(0.75f,0.75f,0.75f);
        [SerializeField] private Color              _bronzeColor = new Color(0.8f, 0.5f, 0.2f);
        [SerializeField] private Color              _normalColor = new Color(0.15f,0.15f,0.15f);

        private RectTransform _rect;

        private void Awake() => _rect = GetComponent<RectTransform>();

        public void Setup(int rank, LeaderboardEntry entry)
        {
            if (_rankText  != null) _rankText.text  = $"#{rank}";
            if (_nameText  != null) _nameText.text  = entry.playerName;
            if (_scoreText != null) _scoreText.text = FormatScore(entry);

            if (_background != null)
                _background.color = rank switch
                {
                    1 => _goldColor,
                    2 => _silverColor,
                    3 => _bronzeColor,
                    _ => _normalColor
                };
        }

        private string FormatScore(LeaderboardEntry e)
        {
            if (!System.Enum.TryParse<LeaderboardCategory>(e.category, out var cat))
                return e.score.ToString();
            return cat switch
            {
                LeaderboardCategory.TotalCoins => $"${e.score:N0}",
                LeaderboardCategory.BestCombo  => $"x{e.score}",
                _                              => e.score.ToString("N0")
            };
        }

        public void AnimateIn(float delay)
        {
            StartCoroutine(SlideIn(delay));
        }

        private IEnumerator SlideIn(float delay)
        {
            if (_rect == null) yield break;
            yield return new WaitForSecondsRealtime(delay);

            var cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

            Vector2 start = _rect.anchoredPosition + new Vector2(-60f, 0f);
            Vector2 end   = _rect.anchoredPosition;
            cg.alpha = 0f;

            for (float t = 0f; t < 0.18f; t += Time.unscaledDeltaTime)
            {
                float p = t / 0.18f;
                _rect.anchoredPosition = Vector2.Lerp(start, end, p);
                cg.alpha = p;
                yield return null;
            }
            _rect.anchoredPosition = end;
            cg.alpha = 1f;
        }
    }
}
