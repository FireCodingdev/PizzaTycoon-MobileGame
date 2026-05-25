using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PizzaTycoon.Managers;
using PizzaTycoon.Performance;

namespace PizzaTycoon.UI
{
    // Tela de carregamento com barra de progresso, dicas e logo animado
    // Coloque na LoadingScene — transita automaticamente para GameScene
    public class LoadingScreenUI : MonoBehaviour
    {
        [Header("Visuais")]
        [SerializeField] private CanvasGroup        _canvasGroup;
        [SerializeField] private Transform          _logoTransform;
        [SerializeField] private Slider             _progressBar;
        [SerializeField] private TextMeshProUGUI    _tipText;
        [SerializeField] private TextMeshProUGUI    _percentText;

        [Header("Configuração")]
        [SerializeField] private string             _nextScene = "GameScene";
        [SerializeField] private float              _minLoadTime = 2f; // tempo mínimo na tela

        private static readonly string[] Tips =
        {
            "Melhore a velocidade primeiro para coletar mais trigo!",
            "Nao deixe clientes esperando - a paciencia deles e curta!",
            "Assista anuncios para ganhar 2x por 2 minutos.",
            "Quanto mais combos, maior o multiplicador de lucro!",
            "Salve dinheiro para desbloquear novas fases!",
            "Clientes felizes rendem mais gorjeta - seja rapido!",
            "O forno pode assar varias pizzas ao mesmo tempo.",
            "Metas diarias dao recompensas bonus - complete todas!",
            "Faca upgrades de pilha para carregar mais itens de vez.",
            "Ganhos offline acumulam enquanto voce nao joga.",
        };

        private float _progress;

        private void Start()
        {
            ShowRandomTip();
            StartCoroutine(LoadSequence());
        }

        private IEnumerator LoadSequence()
        {
            // Anima logo: escala 0 → 1 com bounce
            yield return StartCoroutine(AnimateLogo());

            float startTime = Time.realtimeSinceStartup;

            // Etapas de loading com progresso fake + real
            yield return StartCoroutine(LoadStep(0.00f, 0.20f, "Iniciando audio...",
                () => AudioManager.Instance != null));

            yield return StartCoroutine(LoadStep(0.20f, 0.40f, "Pre-carregando pools...",
                WarmupPools()));

            yield return StartCoroutine(LoadStep(0.40f, 0.60f, "Carregando progresso salvo...",
                () => SaveManager.Instance?.CurrentData != null));

            yield return StartCoroutine(LoadStep(0.60f, 0.75f, "Aplicando configuracoes...",
                () => PerformanceManager.Instance != null));

            yield return StartCoroutine(LoadStep(0.75f, 0.90f, "Calculando ganhos offline...",
                () => true));

            yield return StartCoroutine(LoadStep(0.90f, 1.00f, "Preparando interface...",
                () => true));

            // Garante tempo mínimo na tela
            float elapsed = Time.realtimeSinceStartup - startTime;
            if (elapsed < _minLoadTime)
                yield return new WaitForSeconds(_minLoadTime - elapsed);

            // Fade out e troca de cena
            yield return StartCoroutine(FadeOut());
            Managers.SceneLoader.Instance?.LoadSceneAsync(_nextScene);
        }

        // ── Etapa de loading ─────────────────────────────────────────────────

        private IEnumerator LoadStep(float from, float to, string message, System.Func<bool> condition)
        {
            ShowRandomTip();
            float duration = (to - from) * _minLoadTime;
            float elapsed  = 0f;

            while (elapsed < duration || !condition())
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetProgress(Mathf.Lerp(from, to, t));
                yield return null;
            }
            SetProgress(to);
        }

        private IEnumerator LoadStep(float from, float to, string message, IEnumerator task)
        {
            ShowRandomTip();
            SetProgress(from);
            yield return StartCoroutine(task);

            // Anima progresso até o alvo
            float start = _progress;
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                SetProgress(Mathf.Lerp(start, to, t / 0.3f));
                yield return null;
            }
            SetProgress(to);
        }

        private IEnumerator WarmupPools()
        {
            var expansion = FindObjectOfType<ObjectPoolExpansion>();
            if (expansion != null)
                yield return StartCoroutine(expansion.WarmupAll());
        }

        // ── UI ────────────────────────────────────────────────────────────────

        private void SetProgress(float value)
        {
            _progress = value;
            if (_progressBar  != null) _progressBar.value = value;
            if (_percentText  != null) _percentText.text  = $"{Mathf.FloorToInt(value * 100f)}%";
        }

        private void ShowRandomTip()
        {
            if (_tipText != null)
                _tipText.text = Tips[Random.Range(0, Tips.Length)];
        }

        // ── Animação logo ─────────────────────────────────────────────────────

        private IEnumerator AnimateLogo()
        {
            if (_logoTransform == null) yield break;

            _logoTransform.localScale = Vector3.zero;
            float duration = 0.6f;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float p = t / duration;
                // Easing com overshooot (bounce)
                float scale = p < 0.7f
                    ? Mathf.Lerp(0f, 1.15f, p / 0.7f)
                    : Mathf.Lerp(1.15f, 1f, (p - 0.7f) / 0.3f);
                _logoTransform.localScale = Vector3.one * scale;
                yield return null;
            }
            _logoTransform.localScale = Vector3.one;
        }

        private IEnumerator FadeOut()
        {
            float elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                if (_canvasGroup != null) _canvasGroup.alpha = 1f - elapsed / 0.4f;
                yield return null;
            }
        }
    }
}
