using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PizzaTycoon.UI
{
    // Sequência de splash ao abrir o app: Studio → Unity → Loading
    // Toque em qualquer lugar para pular
    public class SplashScreenManager : MonoBehaviour
    {
        [Header("Splashes")]
        [SerializeField] private CanvasGroup _studioSplash; // Logo do seu estúdio
        [SerializeField] private CanvasGroup _unitySplash;  // "Made with Unity"

        [Header("Configuração")]
        [SerializeField] private string _nextScene     = "LoadingScene";
        [SerializeField] private float  _studioTime    = 2.0f;
        [SerializeField] private float  _unityTime     = 1.0f;
        [SerializeField] private float  _fadeDuration  = 0.5f;

        private bool _skipped;

        private void Start()
        {
            StartCoroutine(PlaySplashSequence());
        }

        private void Update()
        {
            // Qualquer toque pula o splash
            if (!_skipped && (UnityEngine.Input.touchCount > 0 || UnityEngine.Input.GetMouseButtonDown(0)))
                Skip();
        }

        private void Skip()
        {
            _skipped = true;
            StopAllCoroutines();
            GoToNextScene();
        }

        private IEnumerator PlaySplashSequence()
        {
            // 1. Splash do estúdio
            if (_studioSplash != null)
            {
                yield return StartCoroutine(FadeGroup(_studioSplash, 0f, 1f));
                yield return new WaitForSeconds(_studioTime);
                yield return StartCoroutine(FadeGroup(_studioSplash, 1f, 0f));
            }

            if (_skipped) yield break;

            // 2. Made with Unity
            if (_unitySplash != null)
            {
                yield return StartCoroutine(FadeGroup(_unitySplash, 0f, 1f));
                yield return new WaitForSeconds(_unityTime);
                yield return StartCoroutine(FadeGroup(_unitySplash, 1f, 0f));
            }

            if (_skipped) yield break;

            GoToNextScene();
        }

        private IEnumerator FadeGroup(CanvasGroup group, float from, float to)
        {
            group.alpha = from;
            group.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, elapsed / _fadeDuration);
                yield return null;
            }
            group.alpha = to;

            if (Mathf.Approximately(to, 0f))
                group.gameObject.SetActive(false);
        }

        private void GoToNextScene()
        {
            Managers.SceneLoader.Instance?.LoadSceneAsync(_nextScene);
        }
    }
}
