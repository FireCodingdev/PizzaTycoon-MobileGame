using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using PizzaTycoon.Utils;

namespace PizzaTycoon.Managers
{
    // Carrega cenas com tela de loading — cria a UI em código se não houver prefab
    public class SceneLoader : Singleton<SceneLoader>
    {
        [SerializeField] private GameObject _loadingScreenPrefab;

        private GameObject _loadingScreenInstance;
        private bool _isLoading = false;

        // Referências da loading screen criada em código
        private Slider           _progressBar;
        private TextMeshProUGUI  _tipText;
        private CanvasGroup      _canvasGroup;

        public void LoadScene(string sceneName)
        {
            if (_isLoading) return;
            StartCoroutine(LoadSceneAsync(sceneName));
        }

        public void LoadScene(int sceneIndex)
        {
            if (_isLoading) return;
            StartCoroutine(LoadSceneAsync(sceneIndex));
        }

        public void ReloadCurrentScene()
        {
            LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public IEnumerator LoadSceneAsync(string sceneName)
        {
            _isLoading = true;
            ShowLoadingScreen();

            yield return new WaitForSeconds(0.1f);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] Cena '{sceneName}' nao encontrada no Build Settings.");
                HideLoadingScreen();
                _isLoading = false;
                yield break;
            }
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
                yield return null;

            yield return new WaitForSeconds(0.3f);

            operation.allowSceneActivation = true;
            yield return operation;

            HideLoadingScreen();
            _isLoading = false;
        }

        public IEnumerator LoadSceneAsync(int sceneIndex)
        {
            _isLoading = true;
            ShowLoadingScreen();

            yield return new WaitForSeconds(0.1f);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] Cena de indice {sceneIndex} nao encontrada no Build Settings.");
                HideLoadingScreen();
                _isLoading = false;
                yield break;
            }
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
                yield return null;

            yield return new WaitForSeconds(0.3f);

            operation.allowSceneActivation = true;
            yield return operation;

            HideLoadingScreen();
            _isLoading = false;
        }

        private void ShowLoadingScreen()
        {
            if (_loadingScreenInstance != null) return;

            if (_loadingScreenPrefab != null)
            {
                _loadingScreenInstance = Instantiate(_loadingScreenPrefab);
                return;
            }

            // Cria loading screen em código — sem prefab necessário
            _loadingScreenInstance = new GameObject("_LoadingScreen");
            DontDestroyOnLoad(_loadingScreenInstance);

            var canvas = _loadingScreenInstance.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            _loadingScreenInstance.AddComponent<CanvasScaler>().uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _loadingScreenInstance.GetComponent<CanvasScaler>().referenceResolution =
                new Vector2(390, 844);
            _loadingScreenInstance.AddComponent<GraphicRaycaster>();

            _canvasGroup = _loadingScreenInstance.AddComponent<CanvasGroup>();

            // Fundo laranja escuro
            var bg = new GameObject("BG");
            bg.transform.SetParent(_loadingScreenInstance.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.10f, 0.07f, 0.03f, 1f);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

            // Logo
            var logo = new GameObject("Logo");
            logo.transform.SetParent(_loadingScreenInstance.transform, false);
            var logoTMP = logo.AddComponent<TextMeshProUGUI>();
            logoTMP.text      = "PIZZA\nTYCOON";
            logoTMP.fontSize  = 52;
            logoTMP.fontStyle = FontStyles.Bold;
            logoTMP.color     = Color.white;
            logoTMP.alignment = TextAlignmentOptions.Center;
            logoTMP.outlineColor = new Color(0.8f, 0.35f, 0f);
            logoTMP.outlineWidth = 0.20f;
            var logoRT = logo.GetComponent<RectTransform>();
            logoRT.anchorMin = new Vector2(0f, 0.52f);
            logoRT.anchorMax = new Vector2(1f, 0.78f);
            logoRT.offsetMin = logoRT.offsetMax = Vector2.zero;

            // Barra de progresso — trilho
            var barTrack = new GameObject("BarTrack");
            barTrack.transform.SetParent(_loadingScreenInstance.transform, false);
            var trackImg = barTrack.AddComponent<Image>();
            PizzaTycoon.UI.UIStyleKit.ApplyRounded(trackImg,
                new Color(0.25f, 0.18f, 0.08f, 1f), 14);
            var trackRT = barTrack.GetComponent<RectTransform>();
            trackRT.anchorMin = new Vector2(0.08f, 0.38f);
            trackRT.anchorMax = new Vector2(0.92f, 0.44f);
            trackRT.offsetMin = trackRT.offsetMax = Vector2.zero;

            // Fill da barra
            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(barTrack.transform, false);
            var fillAreaRT = fillArea.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.offsetMin = new Vector2(4f, 4f);
            fillAreaRT.offsetMax = new Vector2(-4f, -4f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.AddComponent<Image>();
            PizzaTycoon.UI.UIStyleKit.ApplyRounded(fillImg, PizzaTycoon.UI.UIStyleKit.Orange, 10);
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

            // Slider invisível para controlar fill
            _progressBar = barTrack.AddComponent<Slider>();
            _progressBar.fillRect   = fillRT;
            _progressBar.minValue   = 0f;
            _progressBar.maxValue   = 1f;
            _progressBar.value      = 0f;
            _progressBar.interactable = false;
            var sliderHandle = new GameObject("Handle");
            sliderHandle.transform.SetParent(barTrack.transform, false);
            _progressBar.handleRect = sliderHandle.AddComponent<RectTransform>();

            // Texto de porcentagem
            var pctGo = new GameObject("Pct");
            pctGo.transform.SetParent(_loadingScreenInstance.transform, false);
            var pctTMP = pctGo.AddComponent<TextMeshProUGUI>();
            pctTMP.text      = "0%";
            pctTMP.fontSize  = 18;
            pctTMP.fontStyle = FontStyles.Bold;
            pctTMP.color     = new Color(1f, 0.85f, 0.5f);
            pctTMP.alignment = TextAlignmentOptions.Center;
            var pctRT = pctGo.GetComponent<RectTransform>();
            pctRT.anchorMin = new Vector2(0f, 0.33f);
            pctRT.anchorMax = new Vector2(1f, 0.38f);
            pctRT.offsetMin = pctRT.offsetMax = Vector2.zero;

            // Dica
            var tipGo = new GameObject("Tip");
            tipGo.transform.SetParent(_loadingScreenInstance.transform, false);
            _tipText = tipGo.AddComponent<TextMeshProUGUI>();
            _tipText.text      = "Carregando...";
            _tipText.fontSize  = 16;
            _tipText.fontStyle = FontStyles.Italic;
            _tipText.color     = new Color(0.9f, 0.80f, 0.60f);
            _tipText.alignment = TextAlignmentOptions.Center;
            var tipRT = tipGo.GetComponent<RectTransform>();
            tipRT.anchorMin = new Vector2(0.05f, 0.24f);
            tipRT.anchorMax = new Vector2(0.95f, 0.32f);
            tipRT.offsetMin = tipRT.offsetMax = Vector2.zero;

            StartCoroutine(AnimateLoadingScreen(pctTMP));
        }

        private static readonly string[] Tips = {
            "Melhore a velocidade para coletar mais rapido!",
            "Nao deixe clientes esperando demais!",
            "Combos aumentam o multiplicador de lucro!",
            "Faca upgrades de pilha para carregar mais itens!",
            "Ganhos offline acumulam enquanto voce nao joga.",
        };

        private IEnumerator AnimateLoadingScreen(TextMeshProUGUI pctTMP)
        {
            if (_tipText != null)
                _tipText.text = Tips[Random.Range(0, Tips.Length)];

            float target = 0f;
            float current = 0f;

            while (_loadingScreenInstance != null)
            {
                target = Mathf.MoveTowards(target, 0.85f, Time.unscaledDeltaTime * 0.4f);
                current = Mathf.Lerp(current, target, Time.unscaledDeltaTime * 6f);

                if (_progressBar != null) _progressBar.value = current;
                if (pctTMP != null) pctTMP.text = $"{Mathf.FloorToInt(current * 100f)}%";
                yield return null;
            }
        }

        private void HideLoadingScreen()
        {
            if (_loadingScreenInstance != null)
            {
                StartCoroutine(FadeAndDestroy(_loadingScreenInstance));
                _loadingScreenInstance = null;
            }
        }

        private IEnumerator FadeAndDestroy(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                // Completa a barra antes de sumir
                var bar = go.GetComponentInChildren<Slider>();
                if (bar != null) bar.value = 1f;
                var pct = go.GetComponentsInChildren<TextMeshProUGUI>();
                foreach (var t in pct)
                    if (t.text.EndsWith("%")) t.text = "100%";

                yield return new WaitForSecondsRealtime(0.3f);

                float elapsed = 0f;
                while (elapsed < 0.35f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    cg.alpha = 1f - elapsed / 0.35f;
                    yield return null;
                }
            }
            Destroy(go);
        }
    }
}
