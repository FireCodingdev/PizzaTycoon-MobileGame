using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PizzaTycoon.UI
{
    // Card de resumo da sessão — aparece ao pausar ou a cada 10 min
    // Cada stat aparece com delay de 0.2s entre eles
    public class SessionSummaryUI : MonoBehaviour
    {
        [Header("Painel raiz")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Cards de stats")]
        [SerializeField] private TextMeshProUGUI _moneyText;
        [SerializeField] private TextMeshProUGUI _pizzasText;
        [SerializeField] private TextMeshProUGUI _bestComboText;
        [SerializeField] private TextMeshProUGUI _timeText;

        [Header("Botão fechar")]
        [SerializeField] private Button _closeButton;

        private float _cardDelay = 0.2f;

        private void Awake()
        {
            _closeButton?.onClick.AddListener(Hide);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        public void Show(float moneyEarned, int pizzasSold, int bestCombo, float sessionSeconds)
        {
            gameObject.SetActive(true);
            SetTexts(moneyEarned, pizzasSold, bestCombo, sessionSeconds);
            StopAllCoroutines();
            StartCoroutine(FadeInCards());
        }

        public void Hide()
        {
            StopAllCoroutines();
            StartCoroutine(FadeOut());
        }

        private void SetTexts(float money, int pizzas, int combo, float seconds)
        {
            if (_moneyText    != null) _moneyText.text    = $"${money:N0}";
            if (_pizzasText   != null) _pizzasText.text   = pizzas.ToString();
            if (_bestComboText!= null) _bestComboText.text= $"x{combo}";
            if (_timeText     != null) _timeText.text     = FormatTime(seconds);

            // Oculta todos inicialmente para a animação reveal
            SetAllCardsAlpha(0f);
        }

        private IEnumerator FadeInCards()
        {
            // Fade in do painel
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_canvasGroup != null) _canvasGroup.alpha = elapsed / 0.2f;
                yield return null;
            }
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;

            // Revela cada card com delay
            TextMeshProUGUI[] cards = { _moneyText, _pizzasText, _bestComboText, _timeText };
            foreach (var card in cards)
            {
                if (card == null) continue;
                yield return StartCoroutine(RevealCard(card));
                yield return new WaitForSecondsRealtime(_cardDelay);
            }
        }

        private IEnumerator RevealCard(TextMeshProUGUI card)
        {
            float elapsed = 0f;
            float dur     = 0.18f;
            Color col     = card.color;

            // Slide up + fade in
            Vector3 startPos = card.transform.localPosition + Vector3.down * 20f;
            Vector3 endPos   = card.transform.localPosition;

            card.transform.localPosition = startPos;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / dur);
                card.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                card.color = new Color(col.r, col.g, col.b, t);
                yield return null;
            }
            card.transform.localPosition = endPos;
            card.color = col;
        }

        private IEnumerator FadeOut()
        {
            float elapsed = 0f;
            float dur     = 0.2f;
            float startA  = _canvasGroup != null ? _canvasGroup.alpha : 1f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_canvasGroup != null) _canvasGroup.alpha = Mathf.Lerp(startA, 0f, elapsed / dur);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        private void SetAllCardsAlpha(float a)
        {
            foreach (var t in new[] { _moneyText, _pizzasText, _bestComboText, _timeText })
                if (t != null) t.color = new Color(t.color.r, t.color.g, t.color.b, a);
        }

        private string FormatTime(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m:D2}:{s:D2}";
        }
    }
}
