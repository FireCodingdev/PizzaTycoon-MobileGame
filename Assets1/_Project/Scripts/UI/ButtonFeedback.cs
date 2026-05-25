using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using PizzaTycoon.Managers;

namespace PizzaTycoon.UI
{
    // Feedback visual e sonoro em botões: scale down ao pressionar, punch ao soltar
    public class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float _pressedScale = 0.92f;
        [SerializeField] private float _punchScale   = 1.05f;
        [SerializeField] private float _pressTime    = 0.08f;
        [SerializeField] private float _punchTime    = 0.10f;

        private Vector3   _originalScale;
        private Coroutine _scaleCoroutine;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        public void OnPointerDown(PointerEventData _)
        {
            if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(PressRoutine());
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.GetClip("Click"));
        }

        public void OnPointerUp(PointerEventData _)
        {
            if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(PunchRoutine());
        }

        // Animação de pressão suave até _pressedScale durante _pressTime
        private IEnumerator PressRoutine()
        {
            Vector3 start  = transform.localScale;
            Vector3 target = _originalScale * _pressedScale;
            for (float t = 0f; t < _pressTime; t += Time.unscaledDeltaTime)
            {
                float p = t / _pressTime;
                transform.localScale = Vector3.Lerp(start, target, p);
                yield return null;
            }
            transform.localScale = target;
        }

        private IEnumerator PunchRoutine()
        {
            // Sobe para punchScale
            for (float t = 0f; t < _punchTime * 0.5f; t += Time.unscaledDeltaTime)
            {
                float p = t / (_punchTime * 0.5f);
                transform.localScale = Vector3.Lerp(_originalScale * _pressedScale, _originalScale * _punchScale, p);
                yield return null;
            }
            // Volta ao original
            for (float t = 0f; t < _punchTime * 0.5f; t += Time.unscaledDeltaTime)
            {
                float p = t / (_punchTime * 0.5f);
                transform.localScale = Vector3.Lerp(_originalScale * _punchScale, _originalScale, p);
                yield return null;
            }
            transform.localScale = _originalScale;
        }

        private void OnDisable()
        {
            transform.localScale = _originalScale;
        }
    }
}
