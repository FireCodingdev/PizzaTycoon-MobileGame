using System.Collections;
using UnityEngine;
using TMPro;

namespace PizzaTycoon.UI
{
    // Anima o contador de dinheiro no HUD: EaseOutCubic + punch scale + green flash
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class MoneyAnimator : MonoBehaviour
    {
        [SerializeField] private float _countDuration = 0.4f;
        [SerializeField] private float _punchScale    = 1.3f;
        [SerializeField] private Color _flashColor    = new Color(0.18f, 0.80f, 0.44f); // verde
        [SerializeField] private float _flashDuration = 0.3f;
        [SerializeField] private string _prefix       = "$";

        private TextMeshProUGUI _text;
        private Color           _baseColor;
        private float           _displayedValue;
        private Coroutine       _animCoroutine;

        private void Awake()
        {
            _text      = GetComponent<TextMeshProUGUI>();
            _baseColor = _text.color;
        }

        public void AnimateTo(float targetValue)
        {
            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(CountRoutine(targetValue));
        }

        private IEnumerator CountRoutine(float target)
        {
            float start   = _displayedValue;
            float elapsed = 0f;

            // Punch scale: 1.0 → punchScale → 1.0
            StartCoroutine(PunchScaleRoutine());
            StartCoroutine(FlashRoutine());

            while (elapsed < _countDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _countDuration;
                // EaseOutCubic
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                _displayedValue = Mathf.Lerp(start, target, eased);
                _text.text = _prefix + Mathf.FloorToInt(_displayedValue).ToString("N0");
                yield return null;
            }

            _displayedValue = target;
            _text.text = _prefix + Mathf.FloorToInt(target).ToString("N0");
        }

        private IEnumerator PunchScaleRoutine()
        {
            Vector3 original = transform.localScale;
            float   half     = 0.08f;

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(original, original * _punchScale, t / half);
                yield return null;
            }
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(original * _punchScale, original, t / half);
                yield return null;
            }
            transform.localScale = original;
        }

        private IEnumerator FlashRoutine()
        {
            _text.color = _flashColor;
            float elapsed = 0f;
            while (elapsed < _flashDuration)
            {
                elapsed += Time.deltaTime;
                _text.color = Color.Lerp(_flashColor, _baseColor, elapsed / _flashDuration);
                yield return null;
            }
            _text.color = _baseColor;
        }

        // Seta o valor instantaneamente (sem animação) — útil ao carregar save
        public void SetImmediate(float value)
        {
            _displayedValue = value;
            _text.text = _prefix + Mathf.FloorToInt(value).ToString("N0");
        }
    }
}
