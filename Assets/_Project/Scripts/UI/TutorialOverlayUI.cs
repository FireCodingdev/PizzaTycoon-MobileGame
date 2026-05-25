using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PizzaTycoon.GameSystems;

namespace PizzaTycoon.UI
{
    // Cria a UI do tutorial em runtime e conecta no TutorialManager via reflection.
    // Mostra: painel inferior com passo atual e seta animada no mundo.
    // O botão "Pular" foi REMOVIDO — o tutorial é obrigatório.
    public class TutorialOverlayUI : MonoBehaviour
    {
        private static TutorialOverlayUI _instance;
        private Canvas _canvas;
        private GameObject _panel;
        private TextMeshProUGUI _stepText;
        private TextMeshProUGUI _toastText;
        private GameObject _toastPanel;
        private GameObject _worldArrow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            if (_instance != null) return;

            // Só instala em cenas de jogo
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName == "MainMenu") return;

            // PlayerPrefs "TutorialEnabled" (1 = on, 0 = off). Default ON.
            if (PlayerPrefs.GetInt("TutorialEnabled", 1) == 0) return;

            var go = new GameObject("[TutorialOverlayUI]");
            _instance = go.AddComponent<TutorialOverlayUI>();
        }

        private void Awake()
        {
            BuildCanvas();
            BuildStepPanel();
            BuildToastPanel();
            BuildWorldArrow();
            HookTutorialManager();
        }

        private void BuildCanvas()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 844);
            gameObject.AddComponent<GraphicRaycaster>();
        }

        private void BuildStepPanel()
        {
            _panel = new GameObject("StepPanel");
            _panel.transform.SetParent(_canvas.transform, false);

            // Painel compacto no TOPO — não cobre gameplay.
            var bg = _panel.AddComponent<Image>();
            UIStyleKit.ApplyRounded(bg, new Color(0.10f, 0.07f, 0.02f, 0.88f), 14);

            var rt = _panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, 1f);
            rt.anchorMax = new Vector2(0.92f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 64f);              // altura fixa 64px
            rt.anchoredPosition = new Vector2(0f, -140f);     // 140px do topo

            // Texto do passo — ocupa todo o painel (sem espaço para botão)
            var txtGo = new GameObject("StepTxt");
            txtGo.transform.SetParent(_panel.transform, false);
            _stepText = txtGo.AddComponent<TextMeshProUGUI>();
            _stepText.text      = "";
            _stepText.fontSize  = 13;
            _stepText.fontStyle = FontStyles.Bold;
            _stepText.color     = Color.white;
            _stepText.alignment = TextAlignmentOptions.Center;
            _stepText.enableWordWrapping = true;
            var txtRT = txtGo.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = new Vector2(12f, 4f);
            txtRT.offsetMax = new Vector2(-12f, -4f); // margem simétrica — sem botão

            // BOTÃO PULAR REMOVIDO — tutorial é obrigatório

            _panel.SetActive(false);
        }

        private void BuildToastPanel()
        {
            _toastPanel = new GameObject("ToastPanel");
            _toastPanel.transform.SetParent(_canvas.transform, false);

            var bg = _toastPanel.AddComponent<Image>();
            UIStyleKit.ApplyRounded(bg, new Color(0.10f, 0.07f, 0.02f, 0.88f), 22);

            var rt = _toastPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.10f, 0.78f);
            rt.anchorMax = new Vector2(0.90f, 0.85f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var txtGo = new GameObject("ToastTxt");
            txtGo.transform.SetParent(_toastPanel.transform, false);
            _toastText = txtGo.AddComponent<TextMeshProUGUI>();
            _toastText.text      = "";
            _toastText.fontSize  = 16;
            _toastText.fontStyle = FontStyles.Bold;
            _toastText.color     = UIStyleKit.Yellow;
            _toastText.alignment = TextAlignmentOptions.Center;
            var txtRT = txtGo.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = new Vector2(15f, 0f);
            txtRT.offsetMax = new Vector2(-15f, 0f);

            _toastPanel.SetActive(false);
        }

        private void BuildWorldArrow()
        {
            _worldArrow = new GameObject("TutorialArrow");
            _worldArrow.transform.position = Vector3.zero;

            // Seta formada por uma cápsula vertical + base
            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "Stem";
            stem.transform.SetParent(_worldArrow.transform, false);
            stem.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            stem.transform.localScale = new Vector3(0.15f, 0.5f, 0.15f);
            var stemCol = stem.GetComponent<Collider>();
            if (stemCol != null) Destroy(stemCol);
            var stemMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            stemMat.color = UIStyleKit.Yellow;
            stem.GetComponent<Renderer>().sharedMaterial = stemMat;

            var tip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tip.name = "Tip";
            tip.transform.SetParent(_worldArrow.transform, false);
            tip.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            tip.transform.localScale = new Vector3(0.4f, 0.2f, 0.4f);
            var tipCol = tip.GetComponent<Collider>();
            if (tipCol != null) Destroy(tipCol);
            tip.GetComponent<Renderer>().sharedMaterial = stemMat;

            _worldArrow.SetActive(false);
        }

        // Injeta as referências no TutorialManager via reflection
        private void HookTutorialManager()
        {
            var tm = TutorialManager.Instance;
            if (tm == null)
            {
                tm = FindObjectOfType<TutorialManager>();
                if (tm == null)
                {
                    var go = new GameObject("[TutorialManager]");
                    tm = go.AddComponent<TutorialManager>();
                }
            }

            SetField(tm, "_tutorialPanel", _panel);
            SetField(tm, "_stepText",      _stepText);
            SetField(tm, "_tutorialText",  _toastText);
            SetField(tm, "_worldArrow",    _worldArrow.transform);

            // Inicia o tutorial automaticamente
            tm.StartTutorial();
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }
}