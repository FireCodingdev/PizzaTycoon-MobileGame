using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PizzaTycoon.Monetization;

namespace PizzaTycoon.UI
{
    public enum RewardedState { Available, Active, Cooldown }

    // Botão "▶ 2x" no HUD — exibe estado e conta regressiva
    public class RewardedButtonUI : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Button          _button;
        [SerializeField] private TextMeshProUGUI _labelText;
        [SerializeField] private Image           _buttonImage;
        [SerializeField] private Color           _availableColor = new Color(0.18f, 0.80f, 0.44f);
        [SerializeField] private Color           _activeColor    = new Color(0.95f, 0.77f, 0.06f);
        [SerializeField] private Color           _cooldownColor  = new Color(0.5f,  0.5f,  0.5f);

        [Header("Cooldown")]
        [SerializeField] private float _cooldownSeconds = 300f; // 5 minutos

        private RewardedState _state = RewardedState.Available;
        private float         _remainingTime;
        private Coroutine     _pulseCoroutine;
        private Coroutine     _timerCoroutine;

        private void Awake()
        {
            _button?.onClick.AddListener(OnButtonClicked);
            AdsManager.OnSpeedBoostStarted += OnBoostStarted;
        }

        private void OnDestroy()
        {
            AdsManager.OnSpeedBoostStarted -= OnBoostStarted;
        }

        private void Start()
        {
            SetState(RewardedState.Available);
        }

        private void OnButtonClicked()
        {
            if (_state != RewardedState.Available) return;
            // Se não tem ads (VIP ou IAP), bônus automático
            if (AdsManager.Instance?.AdsRemoved == true)
            {
                AdsManager.Instance?.ShowRewardedForCoins();
                return;
            }
            AdsManager.Instance?.ShowRewardedForSpeedBoost();
        }

        private void OnBoostStarted(float duration)
        {
            _remainingTime = duration;
            SetState(RewardedState.Active);
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _timerCoroutine = StartCoroutine(CountdownTimer(duration, onComplete: StartCooldown));
        }

        private void StartCooldown()
        {
            if (AdsManager.Instance?.AdsRemoved == true)
            {
                // Sem cooldown para quem removeu ads
                SetState(RewardedState.Available);
                return;
            }
            _remainingTime = _cooldownSeconds;
            SetState(RewardedState.Cooldown);
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _timerCoroutine = StartCoroutine(CountdownTimer(_cooldownSeconds,
                onComplete: () => SetState(RewardedState.Available)));
        }

        private void SetState(RewardedState state)
        {
            _state = state;

            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);

            switch (state)
            {
                case RewardedState.Available:
                    SetColor(_availableColor);
                    SetLabel("> 2x");
                    _button.interactable = true;
                    _pulseCoroutine = StartCoroutine(PulseRoutine());
                    break;

                case RewardedState.Active:
                    SetColor(_activeColor);
                    _button.interactable = false;
                    break;

                case RewardedState.Cooldown:
                    SetColor(_cooldownColor);
                    _button.interactable = false;
                    break;
            }
        }

        private IEnumerator CountdownTimer(float total, System.Action onComplete)
        {
            float remaining = total;
            while (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                string label = _state == RewardedState.Active
                    ? $"2x {FormatTime(remaining)}"
                    : $"Disponivel em {FormatTime(remaining)}";
                SetLabel(label);
                yield return null;
            }
            onComplete?.Invoke();
        }

        private IEnumerator PulseRoutine()
        {
            while (_state == RewardedState.Available)
            {
                float t = (Mathf.Sin(Time.unscaledTime * 3f) + 1f) * 0.5f;
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.08f, t);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        private void SetColor(Color c)
        {
            if (_buttonImage != null) _buttonImage.color = c;
        }

        private void SetLabel(string text)
        {
            if (_labelText != null) _labelText.text = text;
        }

        private string FormatTime(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m}:{s:D2}";
        }
    }
}
