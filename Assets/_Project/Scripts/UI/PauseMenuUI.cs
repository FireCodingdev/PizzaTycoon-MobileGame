using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PizzaTycoon.Managers;

namespace PizzaTycoon.UI
{
    // Menu de pausa: slide down 0.3s, controles de som/música, continuar/reiniciar
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("Painel")]
        [SerializeField] private RectTransform _panel;
        [SerializeField] private float         _slideDistance = 600f;
        [SerializeField] private float         _slideDuration = 0.3f;

        [Header("Botões")]
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _restartButton;

        [Header("Sliders de áudio")]
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _musicSlider;

        private Vector2 _hiddenPos;
        private Vector2 _shownPos;
        private bool    _visible;
        private Coroutine _slideCoroutine;

        public bool IsVisible => _visible;

        private void Awake()
        {
            if (_panel != null)
            {
                _shownPos  = _panel.anchoredPosition;
                _hiddenPos = _shownPos + Vector2.up * _slideDistance;
                _panel.anchoredPosition = _hiddenPos;
            }

            _continueButton?.onClick.AddListener(Hide);
            _restartButton?.onClick.AddListener(OnRestart);

            _sfxSlider?.onValueChanged.AddListener(v => AudioManager.Instance?.SetSFXVolume(v));
            _musicSlider?.onValueChanged.AddListener(v => AudioManager.Instance?.SetMusicVolume(v));

            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            _visible = true;
            Time.timeScale = 0f;
            SyncSliders();
            SlidePanel(_shownPos);
        }

        public void Hide()
        {
            _visible = false;
            SlidePanel(_hiddenPos, onComplete: () =>
            {
                Time.timeScale = 1f;
                gameObject.SetActive(false);
            });
        }

        private void OnRestart()
        {
            Time.timeScale = 1f;
            GameManager.Instance?.StartGame();
            gameObject.SetActive(false);
        }

        private void SlidePanel(Vector2 target, System.Action onComplete = null)
        {
            if (_panel == null) return;
            if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
            _slideCoroutine = StartCoroutine(SlideRoutine(target, onComplete));
        }

        private IEnumerator SlideRoutine(Vector2 target, System.Action onComplete)
        {
            Vector2 start   = _panel.anchoredPosition;
            float   elapsed = 0f;

            while (elapsed < _slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _slideDuration);
                _panel.anchoredPosition = Vector2.Lerp(start, target, t);
                yield return null;
            }
            _panel.anchoredPosition = target;
            onComplete?.Invoke();
        }

        private void SyncSliders()
        {
            if (AudioManager.Instance == null) return;
            if (_sfxSlider   != null) _sfxSlider.value   = AudioManager.Instance.SFXVolume;
            if (_musicSlider != null) _musicSlider.value  = AudioManager.Instance.MusicVolume;
        }
    }
}
