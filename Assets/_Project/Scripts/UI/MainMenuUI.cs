using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PizzaTycoon.Managers;

namespace PizzaTycoon.UI
{
    // Menu principal construído 100% em código — sem arrastar nada no Inspector.
    // Estilo Pizza Ready: fundo gradiente quente, logo grande, botão JOGAR verde.
    public class MainMenuUI : MonoBehaviour
    {
        // Referências criadas em código
        private Canvas           _canvas;
        private Button           _playButton;
        private Button           _settingsButton;
        private Button           _resetSaveButton;
        private GameObject       _settingsPanel;
        private TextMeshProUGUI  _offlineEarningsText;

        private void Awake()
        {
            BuildCanvas();
            BuildBackground();
            BuildLogo();
            BuildOfflineEarnings();
            BuildPlayButton();
            BuildSettingsButton();
            BuildSettingsPanel();
        }

        private void Start()
        {
            // Mostra dinheiro total do save
            if (_offlineEarningsText != null)
            {
                float total = SaveManager.Instance != null
                    ? SaveManager.Instance.CurrentData?.money ?? 0f
                    : 0f;
                if (total > 0f)
                {
                    _offlineEarningsText.text = $"Dinheiro salvo: ${total:F0}";
                    _offlineEarningsText.gameObject.SetActive(true);
                }
                else
                {
                    _offlineEarningsText.gameObject.SetActive(false);
                }
            }

            // Inscreve no evento de ganhos offline para mostrar popup quando aplicado
            SaveManager.OnOfflineEarningsApplied += ShowOfflineGain;
        }

        private void OnDestroy()
        {
            SaveManager.OnOfflineEarningsApplied -= ShowOfflineGain;
        }

        private void ShowOfflineGain(float amount)
        {
            if (_offlineEarningsText == null) return;
            _offlineEarningsText.text = $"+${amount:F0} ganho enquanto estava fora!";
            _offlineEarningsText.gameObject.SetActive(true);
        }

        // ── Construção da UI ─────────────────────────────────────────────────

        private void BuildCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 0;
                gameObject.AddComponent<CanvasScaler>().uiScaleMode =
                    CanvasScaler.ScaleMode.ScaleWithScreenSize;
                GetComponent<CanvasScaler>().referenceResolution = new Vector2(390, 844);
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        // Fundo: gradiente quente (laranja escuro → creme) via dois retângulos sobrepostos
        private void BuildBackground()
        {
            // Retângulo de baixo — creme
            var bg = MakeFullRect("BG_Bottom");
            bg.AddComponent<Image>().color = new Color(0.99f, 0.94f, 0.80f);

            // Retângulo de cima — laranja com transparência descendente (simula gradiente)
            var top = MakeFullRect("BG_Top");
            var topImg = top.AddComponent<Image>();
            topImg.color = new Color(0.95f, 0.48f, 0.05f, 0.85f);
            var topRT = top.GetComponent<RectTransform>();
            topRT.anchorMin = new Vector2(0f, 0.45f);
            topRT.anchorMax = Vector2.one;
            topRT.offsetMin = topRT.offsetMax = Vector2.zero;
        }

        // Logo: emoji pizza + nome do jogo em texto grande
        private void BuildLogo()
        {
            var logoArea = new GameObject("LogoArea");
            logoArea.transform.SetParent(_canvas.transform, false);
            var logoRT = logoArea.AddComponent<RectTransform>();
            logoRT.anchorMin = new Vector2(0f, 0.52f);
            logoRT.anchorMax = new Vector2(1f, 0.92f);
            logoRT.offsetMin = logoRT.offsetMax = Vector2.zero;

            // Emoji pizza (símbolo ascii já que TMP pode não ter emoji)
            var emoji = new GameObject("Emoji");
            emoji.transform.SetParent(logoArea.transform, false);
            var emojiTMP = emoji.AddComponent<TextMeshProUGUI>();
            emojiTMP.text      = "PIZZA";
            emojiTMP.fontSize  = 22;
            emojiTMP.fontStyle = FontStyles.Bold;
            emojiTMP.color     = new Color(1f, 1f, 1f, 0.6f);
            emojiTMP.alignment = TextAlignmentOptions.Center;
            var emojiRT = emoji.GetComponent<RectTransform>();
            emojiRT.anchorMin = new Vector2(0f, 0.62f);
            emojiRT.anchorMax = new Vector2(1f, 0.85f);
            emojiRT.offsetMin = emojiRT.offsetMax = Vector2.zero;

            // Nome principal
            var title = new GameObject("Title");
            title.transform.SetParent(logoArea.transform, false);
            var titleTMP = title.AddComponent<TextMeshProUGUI>();
            titleTMP.text      = "TYCOON";
            titleTMP.fontSize  = 72;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color     = Color.white;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.outlineColor = new Color(0.55f, 0.18f, 0f);
            titleTMP.outlineWidth = 0.22f;
            var titleRT = title.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 0.18f);
            titleRT.anchorMax = new Vector2(1f, 0.65f);
            titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

            // Subtítulo
            var sub = new GameObject("Sub");
            sub.transform.SetParent(logoArea.transform, false);
            var subTMP = sub.AddComponent<TextMeshProUGUI>();
            subTMP.text      = "Seja o melhor pizzaiolo!";
            subTMP.fontSize  = 20;
            subTMP.fontStyle = FontStyles.Italic;
            subTMP.color     = new Color(1f, 0.92f, 0.75f);
            subTMP.alignment = TextAlignmentOptions.Center;
            var subRT = sub.GetComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0f, 0f);
            subRT.anchorMax = new Vector2(1f, 0.22f);
            subRT.offsetMin = subRT.offsetMax = Vector2.zero;
        }

        // Texto de ganhos offline — aparece só quando tem dinheiro
        private void BuildOfflineEarnings()
        {
            var go = new GameObject("OfflineEarnings");
            go.transform.SetParent(_canvas.transform, false);

            // Pílula de fundo
            var bg = go.AddComponent<Image>();
            UIStyleKit.ApplyRounded(bg, new Color(0.10f, 0.07f, 0.02f, 0.75f), 20);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.42f);
            rt.anchorMax = new Vector2(0.95f, 0.50f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var txt = new GameObject("Txt");
            txt.transform.SetParent(go.transform, false);
            _offlineEarningsText = txt.AddComponent<TextMeshProUGUI>();
            _offlineEarningsText.text      = "";
            _offlineEarningsText.fontSize  = 17;
            _offlineEarningsText.fontStyle = FontStyles.Bold;
            _offlineEarningsText.color     = UIStyleKit.Yellow;
            _offlineEarningsText.alignment = TextAlignmentOptions.Center;
            var trt = txt.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(12f, 0f);
            trt.offsetMax = new Vector2(-12f, 0f);
        }

        // Botão JOGAR — grande, verde, pill, centro da tela
        private void BuildPlayButton()
        {
            _playButton = UIStyleKit.MakeButton(
                _canvas.transform, "JOGAR",
                UIStyleKit.Green, UIStyleKit.GreenDark,
                new Vector2(0f, 0f), new Vector2(280f, 80f), 40);

            // Posiciona no centro-baixo da tela
            var rt = _playButton.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.36f);
            rt.anchoredPosition = Vector2.zero;

            // Tamanho do texto maior
            var tmp = _playButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.fontSize = 34;

            _playButton.onClick.AddListener(OnPlayClicked);

            // Animação de pulso no botão
            StartCoroutine(PulseButton(_playButton.GetComponent<RectTransform>()));
        }

        // Botão de configurações — pequeno, canto inferior
        private void BuildSettingsButton()
        {
            _settingsButton = UIStyleKit.MakeButton(
                _canvas.transform, "Configuracoes",
                new Color(0.22f, 0.20f, 0.18f, 0.85f),
                new Color(0.08f, 0.07f, 0.05f, 0.85f),
                new Vector2(0f, 0f), new Vector2(200f, 52f), 26);

            var rt = _settingsButton.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.18f);
            rt.anchoredPosition = Vector2.zero;

            _settingsButton.onClick.AddListener(OnSettingsClicked);

            // Botão reset save (debug) — canto inferior direito, discreto
            _resetSaveButton = UIStyleKit.MakeButton(
                _canvas.transform, "Reset Save",
                new Color(0.5f, 0.1f, 0.1f, 0.6f),
                new Color(0.3f, 0.05f, 0.05f, 0.6f),
                Vector2.zero, new Vector2(120f, 36f), 18);
            var rstRT = _resetSaveButton.GetComponent<RectTransform>();
            rstRT.anchorMin = rstRT.anchorMax = new Vector2(0.5f, 0.07f);
            rstRT.anchoredPosition = Vector2.zero;
            var rstTmp = _resetSaveButton.GetComponentInChildren<TextMeshProUGUI>();
            if (rstTmp != null) rstTmp.fontSize = 13;
            _resetSaveButton.onClick.AddListener(OnResetSave);
        }

        // Painel "CONTEXTO" estilo Pizza Ready — card creme com header laranja,
        // toggles SOM/HAPTICO, linha de idioma, redes sociais, links e versao.
        private void BuildSettingsPanel()
        {
            _settingsPanel = new GameObject("SettingsPanel");
            _settingsPanel.transform.SetParent(_canvas.transform, false);
            _settingsPanel.transform.SetAsLastSibling();

            // Overlay escuro tela inteira
            var overlay = _settingsPanel.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.65f);
            overlay.raycastTarget = true;
            var oRT = _settingsPanel.GetComponent<RectTransform>();
            oRT.anchorMin = Vector2.zero;
            oRT.anchorMax = Vector2.one;
            oRT.offsetMin = oRT.offsetMax = Vector2.zero;

            // Sombra do card (atras, deslocada 4px/8px)
            var cardShadow = new GameObject("CardShadow");
            cardShadow.transform.SetParent(_settingsPanel.transform, false);
            var shImg = cardShadow.AddComponent<Image>();
            UIStyleKit.ApplyRounded(shImg, new Color(0f, 0f, 0f, 0.35f), 32);
            var shRT = cardShadow.GetComponent<RectTransform>();
            shRT.anchorMin = new Vector2(0.05f, 0.12f);
            shRT.anchorMax = new Vector2(0.95f, 0.88f);
            shRT.offsetMin = new Vector2(4f, -8f);
            shRT.offsetMax = new Vector2(4f, -8f);

            // Card creme principal
            var card = new GameObject("Card");
            card.transform.SetParent(_settingsPanel.transform, false);
            var cardImg = card.AddComponent<Image>();
            UIStyleKit.StyleCard(cardImg, 32);
            var cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.05f, 0.12f);
            cardRT.anchorMax = new Vector2(0.95f, 0.88f);
            cardRT.offsetMin = cardRT.offsetMax = Vector2.zero;

            // Header laranja no topo (altura ~14%)
            var header = new GameObject("Header");
            header.transform.SetParent(card.transform, false);
            var hImg = header.AddComponent<Image>();
            UIStyleKit.ApplyRounded(hImg, UIStyleKit.Orange, 32);
            var hRT = header.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0f, 0.86f);
            hRT.anchorMax = Vector2.one;
            hRT.offsetMin = hRT.offsetMax = Vector2.zero;

            // Cobertor pra esconder os cantos arredondados de baixo do header
            // (efeito: header arredondado SO no topo, plano embaixo).
            var hCover = new GameObject("HeaderCover");
            hCover.transform.SetParent(header.transform, false);
            var hCoverImg = hCover.AddComponent<Image>();
            hCoverImg.color = UIStyleKit.Orange;
            hCoverImg.raycastTarget = false;
            var hCoverRT = hCover.GetComponent<RectTransform>();
            hCoverRT.anchorMin = new Vector2(0f, 0f);
            hCoverRT.anchorMax = new Vector2(1f, 0.3f);
            hCoverRT.offsetMin = hCoverRT.offsetMax = Vector2.zero;

            // Titulo "CONTEXTO" centralizado no header
            UIStyleKit.MakeText(header.transform, "CONTEXTO", 24, Color.white,
                Vector2.zero, new Vector2(280f, 50f));

            // Botão X vermelho — canto superior direito do overlay
            var closeBtn = UIStyleKit.MakeButton(_settingsPanel.transform, "X",
                UIStyleKit.Red, UIStyleKit.RedDark,
                Vector2.zero, new Vector2(36f, 36f), 18);
            var closeBtnRT = closeBtn.GetComponent<RectTransform>();
            closeBtnRT.anchorMin = closeBtnRT.anchorMax = new Vector2(1f, 1f);
            closeBtnRT.pivot     = new Vector2(1f, 1f);
            closeBtnRT.anchoredPosition = new Vector2(-12f, -12f);
            var closeTmp = closeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (closeTmp != null) closeTmp.fontSize = 18;
            closeBtn.onClick.AddListener(OnCloseSettings);

            // Toggle SOM
            UIStyleKit.MakeToggleRow(card.transform, "SOM",
                0.70f, 0.82f,
                "SoundEnabled", true,
                v => AudioManager.Instance?.SetMasterVolume(v ? 1f : 0f));

            // Toggle HAPTICO (vibração — só funciona em Android/iOS)
            UIStyleKit.MakeToggleRow(card.transform, "HAPTICO",
                0.57f, 0.69f,
                "HapticEnabled", true,
                v =>
                {
#if UNITY_ANDROID || UNITY_IOS
                    if (v) Handheld.Vibrate();
#endif
                });

            // Linha de idioma — fundo branco arredondado, "Portugues" + check verde
            var langRow = new GameObject("LangRow");
            langRow.transform.SetParent(card.transform, false);
            var langImg = langRow.AddComponent<Image>();
            UIStyleKit.ApplyRounded(langImg, UIStyleKit.White, 8);
            var langRT = langRow.GetComponent<RectTransform>();
            langRT.anchorMin = new Vector2(0.05f, 0.46f);
            langRT.anchorMax = new Vector2(0.95f, 0.55f);
            langRT.offsetMin = langRT.offsetMax = Vector2.zero;

            // Label "Portugues"
            var langLblGO = new GameObject("Lbl");
            langLblGO.transform.SetParent(langRow.transform, false);
            var langLblTmp = langLblGO.AddComponent<TextMeshProUGUI>();
            langLblTmp.text      = "Portugues";
            langLblTmp.fontSize  = 13;
            langLblTmp.fontStyle = FontStyles.Bold;
            langLblTmp.color     = UIStyleKit.TextDark;
            langLblTmp.alignment = TextAlignmentOptions.Left;
            var langLblRT = langLblGO.GetComponent<RectTransform>();
            langLblRT.anchorMin = new Vector2(0f, 0f);
            langLblRT.anchorMax = new Vector2(0.7f, 1f);
            langLblRT.offsetMin = new Vector2(14f, 0f);
            langLblRT.offsetMax = Vector2.zero;

            // Check verde "[OK]" (evita unicode fora do font SDF)
            var langCheckGO = new GameObject("Check");
            langCheckGO.transform.SetParent(langRow.transform, false);
            var langCheckTmp = langCheckGO.AddComponent<TextMeshProUGUI>();
            langCheckTmp.text      = "[OK]";
            langCheckTmp.fontSize  = 14;
            langCheckTmp.fontStyle = FontStyles.Bold;
            langCheckTmp.color     = UIStyleKit.Green;
            langCheckTmp.alignment = TextAlignmentOptions.Center;
            var langCheckRT = langCheckGO.GetComponent<RectTransform>();
            langCheckRT.anchorMin = new Vector2(0.75f, 0f);
            langCheckRT.anchorMax = new Vector2(0.97f, 1f);
            langCheckRT.offsetMin = langCheckRT.offsetMax = Vector2.zero;

            // Redes sociais
            UIStyleKit.MakeSocialRow(card.transform, "YOUTUBE",   "+200 SEGUIR", "https://youtube.com",   -50f);
            UIStyleKit.MakeSocialRow(card.transform, "TIKTOK",    "+200 SEGUIR", "https://tiktok.com",    -100f);
            UIStyleKit.MakeSocialRow(card.transform, "INSTAGRAM", "+200 SEGUIR", "https://instagram.com", -150f);

            // Separador horizontal — linha 0.5px (1px na pratica)
            var sep = new GameObject("Separator");
            sep.transform.SetParent(card.transform, false);
            var sepImg = sep.AddComponent<Image>();
            sepImg.color = new Color(0.70f, 0.65f, 0.55f, 0.5f);
            var sepRT = sep.GetComponent<RectTransform>();
            sepRT.anchorMin = new Vector2(0.5f, 0.5f);
            sepRT.anchorMax = new Vector2(0.5f, 0.5f);
            sepRT.sizeDelta = new Vector2(0f, 1f);
            sepRT.anchoredPosition = new Vector2(0f, -195f);
            // Stretch horizontalmente 90% do pai
            sepRT.anchorMin = new Vector2(0.05f, 0.5f);
            sepRT.anchorMax = new Vector2(0.95f, 0.5f);
            sepRT.sizeDelta = new Vector2(0f, 1f);
            sepRT.anchoredPosition = new Vector2(0f, -195f);

            // Links Privacy / Terms (lado a lado com gap 20px)
            UIStyleKit.MakeLinkText(card.transform, "Privacy Policy",
                "https://example.com/privacy",
                new Vector2(-60f, -215f), new Vector2(110f, 22f));
            UIStyleKit.MakeLinkText(card.transform, "Terms of Service",
                "https://example.com/terms",
                new Vector2( 60f, -215f), new Vector2(110f, 22f));

            // Versao "v1.0.0" — verde escuro pequena
            var verGO = new GameObject("Version");
            verGO.transform.SetParent(card.transform, false);
            var verTmp = verGO.AddComponent<TextMeshProUGUI>();
            verTmp.text      = "v1.0.0";
            verTmp.fontSize  = 11;
            verTmp.fontStyle = FontStyles.Normal;
            verTmp.color     = UIStyleKit.GreenDark;
            verTmp.alignment = TextAlignmentOptions.Center;
            var verRT = verGO.GetComponent<RectTransform>();
            verRT.anchorMin = verRT.anchorMax = new Vector2(0.5f, 0.5f);
            verRT.sizeDelta = new Vector2(120f, 20f);
            verRT.anchoredPosition = new Vector2(0f, -235f);

            _settingsPanel.SetActive(false);
        }

        private void BuildVolumeSlider(Transform parent, string label, float anchorY,
            string prefKey, float defaultVal, System.Action<float> onChange)
        {
            var row = new GameObject("Row_" + label);
            row.transform.SetParent(parent, false);
            var rowRT = row.AddComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0.05f, anchorY - 0.08f);
            rowRT.anchorMax = new Vector2(0.95f, anchorY + 0.04f);
            rowRT.offsetMin = rowRT.offsetMax = Vector2.zero;

            // Label
            var lbl = new GameObject("Lbl");
            lbl.transform.SetParent(row.transform, false);
            var lblTMP = lbl.AddComponent<TextMeshProUGUI>();
            lblTMP.text      = label;
            lblTMP.fontSize  = 15;
            lblTMP.fontStyle = FontStyles.Bold;
            lblTMP.color     = UIStyleKit.TextDark;
            lblTMP.alignment = TextAlignmentOptions.Left;
            var lblRT = lbl.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0f, 0.5f);
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;

            // Slider
            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(row.transform, false);
            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value    = PlayerPrefs.GetFloat(prefKey, defaultVal);

            var bgImg = sliderGo.AddComponent<Image>();
            UIStyleKit.ApplyRounded(bgImg, new Color(0.8f, 0.75f, 0.65f), 10);
            var sRT = sliderGo.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0f, 0f);
            sRT.anchorMax = new Vector2(1f, 0.5f);
            sRT.offsetMin = sRT.offsetMax = Vector2.zero;

            // Fill area
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRT = fillArea.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.offsetMin = new Vector2(5f, 2f);
            fillAreaRT.offsetMax = new Vector2(-5f, -2f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.AddComponent<Image>();
            UIStyleKit.ApplyRounded(fillImg, UIStyleKit.Orange, 8);
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(slider.value, 1f);
            fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

            slider.fillRect = fillRT;

            slider.onValueChanged.AddListener(v => {
                PlayerPrefs.SetFloat(prefKey, v);
                onChange?.Invoke(v);
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private GameObject MakeFullRect(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        // Animação de pulso suave no botão JOGAR
        private IEnumerator PulseButton(RectTransform rt)
        {
            Vector3 baseScale = Vector3.one;
            while (true)
            {
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.9f)
                {
                    rt.localScale = Vector3.Lerp(baseScale, baseScale * 1.06f,
                        Mathf.Sin(t * Mathf.PI));
                    yield return null;
                }
            }
        }

        // ── Handlers ─────────────────────────────────────────────────────────

        private void OnPlayClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene("SampleScene");
            else
                GameManager.Instance?.StartGame();
        }

        public void OnSettingsClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            _settingsPanel?.SetActive(true);
        }

        private void OnCloseSettings()
        {
            AudioManager.Instance?.PlayButtonClick();
            _settingsPanel?.SetActive(false);
        }

        private void OnResetSave()
        {
            SaveManager.Instance?.DeleteSave();
            Debug.Log("[MainMenuUI] Save resetado.");
        }

        // Propriedades públicas para compatibilidade com código externo
        public void OnMasterVolumeChanged(float v) => AudioManager.Instance?.SetMasterVolume(v);
        public void OnSFXVolumeChanged(float v)    => AudioManager.Instance?.SetSFXVolume(v);
        public void OnMusicVolumeChanged(float v)  => AudioManager.Instance?.SetMusicVolume(v);
    }
}
