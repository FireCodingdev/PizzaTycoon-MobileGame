using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PizzaTycoon.Managers;

namespace PizzaTycoon.Achievements
{
    // Painel de conquistas com filtro por categoria + toast de desbloqueio
    public class AchievementsUI : MonoBehaviour
    {
        [Header("Painel")]
        [SerializeField] private GameObject         _panel;
        [SerializeField] private CanvasGroup        _canvasGroup;

        [Header("Filtro de categorias")]
        [SerializeField] private Button[]           _categoryButtons; // Todas / Produção / Vendas / Combo / Progressão / Especial
        [SerializeField] private int                _allCategoryIndex = 0;

        [Header("Lista")]
        [SerializeField] private Transform          _contentParent;
        [SerializeField] private AchievementRowUI   _rowPrefab;

        [Header("Toast de desbloqueio")]
        [SerializeField] private GameObject         _toastRoot;
        [SerializeField] private TextMeshProUGUI    _toastTitle;
        [SerializeField] private TextMeshProUGUI    _toastReward;
        [SerializeField] private Image              _toastIcon;

        [Header("Botão fechar")]
        [SerializeField] private Button _closeButton;

        private AchievementCategory? _currentFilter = null;

        private void Awake()
        {
            _closeButton?.onClick.AddListener(Hide);
            AchievementManager.OnAchievementUnlocked += ShowToast;

            for (int i = 0; i < _categoryButtons.Length; i++)
            {
                int idx = i;
                _categoryButtons[i]?.onClick.AddListener(() => SetFilter(idx));
            }

            if (_toastRoot != null) _toastRoot.SetActive(false);
            Hide();
        }

        private void OnDestroy()
        {
            AchievementManager.OnAchievementUnlocked -= ShowToast;
        }

        public void Show()
        {
            _panel?.SetActive(true);
            RefreshList(null);
            StartCoroutine(FadeIn());
        }

        public void Hide()
        {
            _panel?.SetActive(false);
        }

        // ── Filtro ────────────────────────────────────────────────────────────

        private void SetFilter(int buttonIndex)
        {
            if (buttonIndex == _allCategoryIndex)
            {
                _currentFilter = null;
                RefreshList(null);
            }
            else
            {
                var cat = (AchievementCategory)(buttonIndex - 1);
                _currentFilter = cat;
                RefreshList(cat);
            }
        }

        private void RefreshList(AchievementCategory? filter)
        {
            if (_contentParent == null || _rowPrefab == null) return;

            foreach (Transform child in _contentParent)
                Destroy(child.gameObject);

            var manager = AchievementManager.Instance;
            if (manager == null) return;

            foreach (var achievement in manager.GetAll())
            {
                if (achievement == null) continue;
                if (filter.HasValue && achievement.category != filter.Value) continue;

                int mIdx = manager.GetMilestoneIndex(achievement.id);
                if (achievement.isHidden && mIdx == 0) continue; // oculta não desbloqueada

                var row = Instantiate(_rowPrefab, _contentParent);
                row.Setup(achievement, manager.GetProgress(achievement.id), mIdx);
            }
        }

        // ── Toast ─────────────────────────────────────────────────────────────

        private void ShowToast(AchievementData data, int milestoneIndex)
        {
            if (_toastRoot == null) return;
            StopCoroutine(nameof(ToastRoutine));
            StartCoroutine(ToastRoutine(data, milestoneIndex));
        }

        private IEnumerator ToastRoutine(AchievementData data, int milestoneIndex)
        {
            if (_toastTitle  != null) _toastTitle.text = $"Conquista: {data.title}";
            if (_toastIcon   != null) _toastIcon.color = data.GetIconColor();

            int reward = (data.coinRewards != null && milestoneIndex < data.coinRewards.Length)
                ? data.coinRewards[milestoneIndex] : 0;
            if (_toastReward != null)
                _toastReward.text = reward > 0 ? $"+${reward}" : "";

            _toastRoot.SetActive(true);

            // Slide in
            var rect = _toastRoot.GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector2 hidden = rect.anchoredPosition + Vector2.right * 400f;
                Vector2 shown  = rect.anchoredPosition;

                rect.anchoredPosition = hidden;
                for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
                {
                    rect.anchoredPosition = Vector2.Lerp(hidden, shown, t / 0.3f);
                    yield return null;
                }
                rect.anchoredPosition = shown;
            }

            yield return new WaitForSecondsRealtime(3f);

            // Slide out
            if (rect != null)
            {
                Vector2 shown  = rect.anchoredPosition;
                Vector2 hidden = shown + Vector2.right * 400f;
                for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
                {
                    rect.anchoredPosition = Vector2.Lerp(shown, hidden, t / 0.2f);
                    yield return null;
                }
            }
            _toastRoot.SetActive(false);
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

    // ── Row individual de conquista ───────────────────────────────────────────

    public class AchievementRowUI : MonoBehaviour
    {
        [SerializeField] private Image              _iconImage;
        [SerializeField] private TextMeshProUGUI    _titleText;
        [SerializeField] private TextMeshProUGUI    _descriptionText;
        [SerializeField] private TextMeshProUGUI    _progressText;
        [SerializeField] private Slider             _progressBar;
        [SerializeField] private Image              _background;
        [SerializeField] private Color              _completedColor = new Color(0.18f, 0.80f, 0.44f, 0.2f);
        [SerializeField] private Color              _normalColor    = Color.white;

        public void Setup(AchievementData data, float currentValue, int milestoneIndex)
        {
            bool completed = milestoneIndex >= data.MilestoneCount;
            bool hasNext   = !completed && data.milestones != null && milestoneIndex < data.milestones.Length;

            if (_iconImage     != null) _iconImage.color = data.GetIconColor();
            if (_titleText     != null) _titleText.text  = data.title;
            if (_background    != null) _background.color = completed ? _completedColor : _normalColor;

            if (hasNext)
            {
                float target = data.milestones[milestoneIndex];
                if (_progressBar  != null) { _progressBar.value = currentValue / target; }
                if (_progressText != null)
                    _progressText.text = $"{Mathf.FloorToInt(currentValue)}/{Mathf.FloorToInt(target)}";
                if (_descriptionText != null)
                    _descriptionText.text = data.GetDescription(milestoneIndex);
            }
            else
            {
                if (_progressBar     != null) _progressBar.value  = 1f;
                if (_progressText    != null) _progressText.text  = "MAXIMO";
                if (_descriptionText != null) _descriptionText.text = data.description;
            }
        }
    }
}
