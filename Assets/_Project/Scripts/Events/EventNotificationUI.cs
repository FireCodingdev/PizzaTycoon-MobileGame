using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PizzaTycoon.Events
{
    // Banner animado que aparece quando um evento inicia ou está ativo
    public class EventNotificationUI : MonoBehaviour
    {
        [Header("Banner")]
        [SerializeField] private RectTransform      _bannerRoot;
        [SerializeField] private CanvasGroup        _canvasGroup;
        [SerializeField] private Image              _iconBackground;
        [SerializeField] private TextMeshProUGUI    _titleText;
        [SerializeField] private TextMeshProUGUI    _descriptionText;
        [SerializeField] private TextMeshProUGUI    _timerText;

        [Header("Desafio Relâmpago")]
        [SerializeField] private GameObject         _challengePanel;
        [SerializeField] private Slider             _challengeBar;
        [SerializeField] private TextMeshProUGUI    _challengeProgressText;

        [Header("Botão fechar")]
        [SerializeField] private Button             _closeButton;

        private Coroutine _slideCoroutine;
        private bool      _visible;

        private void Awake()
        {
            _closeButton?.onClick.AddListener(HideBanner);

            EventManager.OnEventStarted       += ShowEventBanner;
            EventManager.OnEventEnded         += OnEventEnded;
            EventManager.OnChallengeProgress  += UpdateChallenge;
            EventManager.OnChallengeCompleted += OnChallengeCompleted;

            if (_bannerRoot != null)
                _bannerRoot.anchoredPosition = new Vector2(0f, 200f); // fora da tela (acima)
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }

        private void OnDestroy()
        {
            EventManager.OnEventStarted       -= ShowEventBanner;
            EventManager.OnEventEnded         -= OnEventEnded;
            EventManager.OnChallengeProgress  -= UpdateChallenge;
            EventManager.OnChallengeCompleted -= OnChallengeCompleted;
        }

        private void Update()
        {
            if (!_visible || EventManager.Instance == null) return;
            if (_timerText != null)
                _timerText.text = EventManager.Instance.FormattedTimeRemaining();
        }

        // ── Mostrar banner ────────────────────────────────────────────────────

        private void ShowEventBanner(GameEvent ev)
        {
            if (_titleText       != null) _titleText.text       = ev.displayName;
            if (_descriptionText != null)
            {
                string desc = ev.type == GameEventType.LightningChallenge
                    ? string.Format(ev.description, ev.challengeTarget)
                    : ev.description;
                _descriptionText.text = desc;
            }

            if (_iconBackground != null && ColorUtility.TryParseHtmlString(ev.iconColor, out Color c))
                _iconBackground.color = c;

            bool isChallenge = ev.type == GameEventType.LightningChallenge;
            if (_challengePanel != null) _challengePanel.SetActive(isChallenge);
            if (isChallenge)
            {
                if (_challengeBar  != null) { _challengeBar.maxValue = ev.challengeTarget; _challengeBar.value = 0; }
                if (_challengeProgressText != null) _challengeProgressText.text = $"0 / {ev.challengeTarget}";
            }

            if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
            _slideCoroutine = StartCoroutine(SlideIn());
        }

        private void HideBanner()
        {
            if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
            _slideCoroutine = StartCoroutine(SlideOut());
        }

        private void OnEventEnded(GameEvent ev) => HideBanner();

        // ── Desafio Relâmpago ─────────────────────────────────────────────────

        private void UpdateChallenge(GameEvent ev, int current)
        {
            if (_challengeBar          != null) _challengeBar.value        = current;
            if (_challengeProgressText != null)
                _challengeProgressText.text = $"{current} / {ev.challengeTarget}";
        }

        private void OnChallengeCompleted(GameEvent ev)
        {
            if (_titleText != null) _titleText.text = "Desafio Concluído!";
            if (_descriptionText != null)
                _descriptionText.text = $"+{ev.challengeCoins} coins  +{ev.challengeXP} XP";
            StartCoroutine(AutoHideAfter(3f));
        }

        private IEnumerator AutoHideAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            HideBanner();
        }

        // ── Animações ─────────────────────────────────────────────────────────

        private IEnumerator SlideIn()
        {
            _visible = true;
            float duration = 0.35f;
            Vector2 from   = new Vector2(0f, 200f);
            Vector2 to     = Vector2.zero;

            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float p = Mathf.SmoothStep(0f, 1f, t / duration);
                if (_bannerRoot  != null) _bannerRoot.anchoredPosition = Vector2.Lerp(from, to, p);
                if (_canvasGroup != null) _canvasGroup.alpha            = p;
                yield return null;
            }
            if (_bannerRoot  != null) _bannerRoot.anchoredPosition = to;
            if (_canvasGroup != null) _canvasGroup.alpha            = 1f;

            // Auto-esconde após 5s (permanece visível se desafio ainda ativo)
            if (EventManager.Instance?.ActiveEvent?.type != GameEventType.LightningChallenge)
                yield return new WaitForSecondsRealtime(5f);
            else
                yield break;

            yield return StartCoroutine(SlideOut());
        }

        private IEnumerator SlideOut()
        {
            _visible       = false;
            float duration = 0.25f;
            Vector2 from   = _bannerRoot != null ? _bannerRoot.anchoredPosition : Vector2.zero;
            Vector2 to     = new Vector2(0f, 200f);
            float   aFrom  = _canvasGroup != null ? _canvasGroup.alpha : 1f;

            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float p = t / duration;
                if (_bannerRoot  != null) _bannerRoot.anchoredPosition = Vector2.Lerp(from, to, p);
                if (_canvasGroup != null) _canvasGroup.alpha            = Mathf.Lerp(aFrom, 0f, p);
                yield return null;
            }
            if (_bannerRoot  != null) _bannerRoot.anchoredPosition = to;
            if (_canvasGroup != null) _canvasGroup.alpha            = 0f;
        }
    }
}
