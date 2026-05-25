using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PizzaTycoon.Managers;
using PizzaTycoon.GameSystems;

namespace PizzaTycoon.UI
{
    // Controla o HUD in-game: dinheiro, Daily Goal bar, Combo display e botões.
    //
    // Campos:
    //   _dailyGoalSlider  — Slider (0..1) que representa o progresso da meta ativa
    //   _dailyGoalText    — Label exibindo o tipo e valor da meta
    //   _comboDisplay     — GameObject raiz do painel de combo (ativado/desativado)
    //   _comboMultiplierText — "x1.5" exibido no centro do painel
    //   _comboCountText   — contador "12 COMBO" exibido na parte inferior
    //
    // CORREÇÃO: substituído caractere Unicode U+2713 (checkmark ✓) por "[OK]"
    //   para evitar warnings do LiberationSans SDF que não contém esse glyph.
    public class HUDController : MonoBehaviour
    {
        [Header("Dinheiro")]
        [SerializeField] private TextMeshProUGUI _moneyText;
        [SerializeField] private float _moneyAnimDuration = 0.5f;
        [Tooltip("Tamanho minimo da fonte do dinheiro quando auto-shrink (default 14)")]
        [SerializeField] private float _moneyFontMin = 14f;
        [Tooltip("Tamanho maximo da fonte do dinheiro (default 48). TMP encolhe automaticamente pra caber.")]
        [SerializeField] private float _moneyFontMax = 48f;

        [Header("Daily Goal")]
        [SerializeField] private Slider            _dailyGoalSlider;
        [SerializeField] private TextMeshProUGUI   _dailyGoalText;
        [Tooltip("Container onde as 3 metas diarias sao exibidas como lista. " +
                 "Se atribuido (ex: 'listadetarefa'), substitui o _dailyGoalText rotacionando uma por vez. " +
                 "Cria 1 TMP por meta como filho. Adiciona VerticalLayoutGroup automatico se nao existir.")]
        [SerializeField] private Transform         _taskListContainer;
        [Tooltip("Tamanho da fonte das linhas de tarefa (em pontos). Default 22 — bom " +
                 "balanco entre legibilidade e cabe em modal estreito. Aumente pra 28-36 se " +
                 "o modal for largo. NAO use Scale do RectTransform.")]
        [SerializeField] private float             _taskRowFontSize  = 22f;
        [Tooltip("Cor do texto de tarefa NAO completada. Use cor escura se o fundo for branco.")]
        [SerializeField] private Color             _taskColorActive  = new Color(0.12f, 0.10f, 0.08f);  // preto suave
        [Tooltip("Cor do texto de tarefa COMPLETADA.")]
        [SerializeField] private Color             _taskColorDone    = new Color(0.15f, 0.65f, 0.20f);  // verde escuro
        [Tooltip("Cor do header 'Nivel X - XP'. Use cor escura/laranja escuro se o fundo for branco.")]
        [SerializeField] private Color             _taskColorHeader  = new Color(0.85f, 0.40f, 0.05f);  // laranja escuro

        [Header("Task List Toggle (botao de mostrar/esconder)")]
        [Tooltip("Botao que mostra/esconde o painel de tarefas. Persiste estado em PlayerPrefs 'TaskListVisible'.")]
        [SerializeField] private Button            _taskToggleButton;
        [Tooltip("GameObject que sera ativado/desativado pelo botao (geralmente o DailyGoalBG inteiro).")]
        [SerializeField] private GameObject        _taskTogglePanel;
        [Tooltip("Estado inicial se nao houver valor salvo em PlayerPrefs (default: visivel)")]
        [SerializeField] private bool              _taskListVisibleInitially = true;
        [Tooltip("Opcional: texto do botao quando tarefas estao VISIVEIS (ex: 'V' ou 'X')")]
        [SerializeField] private string            _taskToggleLabelVisible = "X";
        [Tooltip("Opcional: texto do botao quando tarefas estao OCULTAS (ex: '?' ou '!')")]
        [SerializeField] private string            _taskToggleLabelHidden  = "!";

        [Header("Auto-Resize (caixa branca cresce com conteudo)")]
        [Tooltip("Painel de fundo (ex: DailyGoalBG) que deve CRESCER automaticamente pra " +
                 "envolver as tarefas. Adiciona VerticalLayoutGroup + ContentSizeFitter. " +
                 "AVISO: o VLG vai empilhar verticalmente TODOS os filhos do painel — " +
                 "garanta que so existem os elementos que devem ser stackados (Title, Lista).")]
        [SerializeField] private GameObject        _autoResizeBackground;
        [Tooltip("Padding interno do painel auto-resize (em pixels: left, right, top, bottom)")]
        [SerializeField] private RectOffset        _autoResizePadding;  // inicializado em Awake
        [Tooltip("Espacamento entre filhos do painel auto-resize")]
        [SerializeField] private float             _autoResizeSpacing = 4f;
        [Tooltip("Se MARCADO, o codigo RESETA o RectTransform do Listadetarefa toda hora " +
                 "(anchors stretch, scale 1, position 0). Se DESMARCADO (default), preserva " +
                 "seus ajustes manuais de scale/position/pivot no Inspector.")]
        [SerializeField] private bool              _forceResetTaskListTransform = false;

        [Tooltip("Se MARCADO (default), forca scale (1,1,1) no Listadetarefa SEMPRE. Scale " +
                 "diferente distorce textos filhos (garbled). Desmarque APENAS se quiser " +
                 "usar scale do RectTransform pra controlar tamanho do texto (nao recomendado).")]
        [SerializeField] private bool              _forceScaleOne = true;

        [Tooltip("Se MARCADO (default), auto-converte ancoras stretched do Auto Resize " +
                 "Background pra single-point (top-center). NECESSARIO pro ContentSizeFitter " +
                 "funcionar e o modal crescer. Desmarque se quiser configurar ancoras manual.")]
        [SerializeField] private bool              _autoFixBackgroundAnchors = true;

        [Tooltip("Largura minima do modal (Width do Tarefa). Default 340px — cabe '[ ] Venda 10 pizzas (0/10)' " +
                 "em uma linha com font 22. Aumente pra 400+ se quiser mais espaco. 0 = nao forca largura.")]
        [SerializeField] private float             _taskModalMinWidth = 340f;

        [Tooltip("Se MARCADO (default), todos os filhos do Auto Resize Background EXCETO " +
                 "o Listadetarefa sao marcados com 'Ignore Layout'. Isso preserva o header " +
                 "(checkmark, titulo 'Tarefa:', icone) na posicao original, e a VLG so empilha o Listadetarefa.")]
        [SerializeField] private bool              _ignoreLayoutOnHeaderItems = true;

        [Tooltip("Padding-top do modal de tarefas. Default 8px (titulo agora vem como " +
                 "primeira linha da lista). Aumente se voce tiver um header absoluto " +
                 "separado e quiser reservar espaco pra ele.")]
        [SerializeField] private float             _taskListTopPadding = 8f;

        [Tooltip("[DEPRECATED — nao usado mais] Modal sempre cresce com conteudo agora.")]
        [SerializeField] private bool              _lockModalSize = false;

        [Header("Sistema de Progressao (XP/Nivel)")]
        [Tooltip("Se TRUE, a lista de tarefas exibe as MISSOES do PlayerProgressionSystem. " +
                 "Se FALSE, exibe as 3 metas diarias do DailyGoalSystem (modo antigo).")]
        [SerializeField] private bool              _useMissionsAsTasks = true;
        [Tooltip("Mostra header de nivel/XP no topo da lista (ex: 'Nivel 2  -  120/200 XP')")]
        [SerializeField] private bool              _showLevelHeader    = true;
        [Tooltip("Mostra TITULO ('TAREFA:') como primeira linha da lista. " +
                 "Use se voce deletou o modal de header separado.")]
        [SerializeField] private bool              _showTitleInList    = true;
        [Tooltip("Texto do titulo exibido (ex: 'TAREFA:', 'MISSOES', etc)")]
        [SerializeField] private string            _titleText          = "TAREFA:";
        [Tooltip("Tamanho da fonte do titulo (default 30 — maior que as missoes)")]
        [SerializeField] private float             _titleFontSize      = 30f;
        [Tooltip("Cor do titulo. Default verde estilo Pizza Ready.")]
        [SerializeField] private Color             _titleColor         = new Color(0.18f, 0.55f, 0.18f);

        [Header("Combo")]
        [SerializeField] private GameObject        _comboDisplay;       // raiz — ativado quando combo >= 2
        [SerializeField] private TextMeshProUGUI   _comboMultiplierText; // "x1.5"
        [SerializeField] private TextMeshProUGUI   _comboCountText;     // "12 COMBO"

        [Header("Botões")]
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _upgradeButton;
        [Tooltip("Sprite usado como visual do botao Upgrade (ex: seta-para-cima.png). " +
                 "Se atribuido, substitui o visual atual.")]
        [SerializeField] private Sprite _upgradeButtonSprite;

        [Header("Botoes HUD Extra")]
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _mapButton;
        [SerializeField] private Button _soundButton;      // botão mute/unmute
        [SerializeField] private TextMeshProUGUI _levelText; // nível/XP do jogador (opcional)

        [Header("Auto-Styling (default OFF — preserva styling manual)")]
        [Tooltip("Se MARCADO, ApplyStyle() sobrescreve cores/sprites/fontes dos elementos do HUD em runtime (estilo Pizza Ready). Se DESMARCADO, mantem exatamente o que voce configurou no Inspector.")]
        [SerializeField] private bool _applyAutoStyle = false;
        [Tooltip("Painel de pausa pre-configurado na cena. Se NULL, sera criado um runtime.")]
        [SerializeField] private GameObject _pausePanel;

        [Header("Settings Panel (in-game)")]
        [Tooltip("Painel de configuracoes in-game. Se NULL, sera criado um runtime quando " +
                 "o botao Settings for clicado.")]
        [SerializeField] private GameObject _inGameSettingsPanel;

        private bool       _isPaused;

        // ── estado interno ─────────────────────────────────────────────────
        private float _displayedMoney = 0f;
        private float _targetMoney    = 0f;
        private Coroutine _moneyAnimCoroutine;
        private Coroutine _comboBounceCoroutine;

        // Índice da meta a exibir na barra (rotaciona entre as 3 a cada 5s)
        private int       _goalDisplayIndex = 0;
        private float     _goalRotateTimer  = 0f;
        private const float GoalRotateInterval = 5f;

        // ── Unity lifecycle ────────────────────────────────────────────────
        private void Awake()
        {
            // RectOffset NAO pode ser inicializado como field initializer
            // (Unity reclama de set_left fora de Awake/Start) — fazemos aqui.
            if (_autoResizePadding == null)
                _autoResizePadding = new RectOffset(10, 10, 8, 8);

            SetupButtons();
        }

        private void Start()
        {
            AutoWire();

            // Só cria PausePanel runtime se voce nao tiver um na cena
            if (_pausePanel == null) BuildPausePanel();

            SetupButtons();

            // Auto-styling DESLIGADO por padrao — preserva o styling manual do Inspector.
            // Marque _applyAutoStyle no Inspector se quiser que ApplyStyle() rode.
            if (_applyAutoStyle) ApplyStyle();

            // Auto-size do TMP do dinheiro — encolhe automaticamente pra caber na pilula
            ConfigureMoneyAutoSize();

            // Aplica sprite custom no botao Upgrade se atribuido
            ApplyUpgradeButtonSprite();

            // Configura auto-resize do painel de fundo (se atribuido)
            ConfigureAutoResizeBackground();

            RefreshGoalDisplay();
            if (_comboDisplay != null) _comboDisplay.SetActive(false);

            // Aplica estado inicial da lista de tarefas (PlayerPrefs ou default)
            ApplyTaskTogglePref();
        }

        // Adiciona VerticalLayoutGroup + ContentSizeFitter na CADEIA INTEIRA de pais
        // entre _taskListContainer e _autoResizeBackground. Assim cada nivel cresce
        // automaticamente com o conteudo (texto -> Listadetarefa -> DailyGoalBG -> Tarefa).
        //
        // BONUS: reparenta o _taskListContainer pra DENTRO do _autoResizeBackground se
        // estiver solto na hierarquia.
        private void ConfigureAutoResizeBackground()
        {
            if (_autoResizeBackground == null) return;

            // 0a) Garante que o background tem uma Image VISIVEL branca pra ser o modal.
            var bgImage = _autoResizeBackground.GetComponent<Image>();
            if (bgImage == null)
            {
                bgImage = _autoResizeBackground.AddComponent<Image>();
                bgImage.color = new Color(0.97f, 0.97f, 0.97f);
                bgImage.sprite = null;
                bgImage.raycastTarget = false;
            }

            // 0b) FORCA scale (1,1,1) no background — scale nao-uniforme distorce tudo.
            //     Mesmo se voce ajustou manual no Inspector, codigo reseta pra evitar bug.
            var bgRT0 = _autoResizeBackground.GetComponent<RectTransform>();
            if (bgRT0 != null && bgRT0.localScale != Vector3.one)
            {
                Debug.LogWarning($"[HUD] Tarefa.scale era {bgRT0.localScale} — resetado pra 1,1,1.");
                bgRT0.localScale = Vector3.one;
            }

            // 0c) FORCA pivot Y=1 (topo) pra modal crescer pra BAIXO, nao pra cima.
            if (bgRT0 != null && Mathf.Abs(bgRT0.pivot.y - 1f) > 0.01f)
            {
                bgRT0.pivot = new Vector2(0.5f, 1f);
            }

            // 0d) Auto-converte ancoras stretched pra single-point (top-center).
            if (_autoFixBackgroundAnchors)
                ConvertToSinglePointAnchor(_autoResizeBackground);

            // 1) Forca Width minima no Tarefa pra texto nao quebrar palavra por palavra
            if (_taskModalMinWidth > 0f)
            {
                var bgRT = _autoResizeBackground.GetComponent<RectTransform>();
                if (bgRT != null && bgRT.sizeDelta.x < _taskModalMinWidth)
                {
                    bgRT.sizeDelta = new Vector2(_taskModalMinWidth, bgRT.sizeDelta.y);
                }
            }

            // 2) Aplica VLG + CSF no _autoResizeBackground (topo da cadeia)
            ApplyAutoLayoutTo(_autoResizeBackground);

            // 2.1) Aumenta padding-top do VLG do background pra deixar espaco pro header
            //      (titulo "Tarefa:" + checkmark + icone) que esta posicionado absolutamente.
            var bgVLG = _autoResizeBackground.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (bgVLG != null && _taskListTopPadding > 0f)
            {
                bgVLG.padding = new RectOffset(
                    bgVLG.padding.left,
                    bgVLG.padding.right,
                    Mathf.RoundToInt(_taskListTopPadding),   // top com espaco pro header
                    bgVLG.padding.bottom);
            }

            // 2.5) Marca filhos NAO-task com LayoutElement.ignoreLayout = true
            //      Assim VLG so stackeia o Listadetarefa, e header (checkmark, titulo, icone)
            //      ficam nas posicoes absolutas que voce configurou no Inspector.
            if (_ignoreLayoutOnHeaderItems && _taskListContainer != null)
            {
                IgnoreLayoutOnSiblings(_autoResizeBackground.transform, _taskListContainer);
            }

            // 2) Aplica em TODA a cadeia de pais entre Listadetarefa e o background.
            //    Isso garante que DailyGoalBG (parent intermediario) tambem cresce.
            if (_taskListContainer != null)
            {
                Transform t = _taskListContainer.parent;
                while (t != null && t.gameObject != _autoResizeBackground &&
                       t.IsChildOf(_autoResizeBackground.transform))
                {
                    ApplyAutoLayoutTo(t.gameObject);
                    t = t.parent;
                }
            }

            // Auto-reparent: se a lista de tarefas nao for filha do background, move pra dentro.
            if (_taskListContainer != null)
            {
                if (_taskListContainer.parent != _autoResizeBackground.transform)
                {
                    _taskListContainer.SetParent(_autoResizeBackground.transform, false);
                    Debug.Log($"[HUD] Listadetarefa reparenteada pra dentro de {_autoResizeBackground.name}");
                }

                // Force scale (1,1,1) em TODA a cadeia (background -> ... -> listadetarefa)
                // Scale diferente de 1 distorce filhos e bagunca ContentSizeFitter.
                if (_forceScaleOne)
                {
                    ForceScaleOneInChain(_taskListContainer, _autoResizeBackground.transform);
                }

                // Reset completo (anchors, position) — APENAS se flag marcada
                ApplyLegacyResetBlock();
            }
        }

        // Marca todos os filhos diretos de 'parent' (EXCETO 'preserve') com
        // LayoutElement.ignoreLayout = true. Isso faz o VerticalLayoutGroup ignorar
        // esses filhos no calculo da altura, deixando-os em posicoes absolutas.
        private static void IgnoreLayoutOnSiblings(Transform parent, Transform preserve)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == preserve) continue;
                // Tambem nao marca filhos que sao ancestrais de preserve (caso a hierarquia
                // tenha intermediarios — ex: DailyGoalBG -> Listadetarefa)
                if (preserve.IsChildOf(child)) continue;

                var le = child.GetComponent<UnityEngine.UI.LayoutElement>();
                if (le == null) le = child.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                le.ignoreLayout = true;
            }
        }

        // Forca localScale=(1,1,1) em todos os transforms da cadeia entre start e endParent.
        private static void ForceScaleOneInChain(Transform start, Transform endParent)
        {
            Transform t = start;
            while (t != null)
            {
                if (t.localScale != Vector3.one) t.localScale = Vector3.one;
                if (t == endParent) break;
                t = t.parent;
            }
        }

        // Converte ancoras stretched (anchorMin != anchorMax) pra single-point top-center,
        // preservando a posicao visual atual. Necessario pro ContentSizeFitter funcionar.
        // Tambem FORCA pivot Y = 1 (topo) pra modal crescer pra BAIXO, nao pra cima.
        private static void ConvertToSinglePointAnchor(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;

            // SEMPRE forca pivot Y=1 (topo) pra modal crescer DOWN, nao UP.
            // Pivot Y=0 (base) faria modal expandir pra cima escondendo title.
            if (Mathf.Abs(rt.pivot.y - 1f) > 0.01f)
            {
                Vector2 oldPivot = rt.pivot;
                Vector2 newPivot = new Vector2(rt.pivot.x, 1f);
                // Ajusta anchoredPosition pra preservar visual quando muda pivot
                Vector2 sizeDeltaWorld = rt.rect.size;
                rt.anchoredPosition += new Vector2(0f, sizeDeltaWorld.y * (1f - oldPivot.y));
                rt.pivot = newPivot;
            }

            // Ja eh single-point? nao precisa converter ancoras
            if (rt.anchorMin == rt.anchorMax) return;

            // Captura a posicao mundial atual do canto top-center
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            // corners: 0=bottom-left, 1=top-left, 2=top-right, 3=bottom-right
            Vector3 topCenterWorld = (corners[1] + corners[2]) * 0.5f;
            float currentWidth  = Vector3.Distance(corners[1], corners[2]);
            float currentHeight = Vector3.Distance(corners[0], corners[1]);

            // Converte ancoras pra top-center
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(currentWidth, currentHeight);

            // Reposiciona pra ficar no mesmo lugar visualmente
            if (rt.parent is RectTransform parentRT)
            {
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRT,
                    RectTransformUtility.WorldToScreenPoint(null, topCenterWorld),
                    null,
                    out localPoint);
                rt.anchoredPosition = localPoint;
            }

            Debug.Log($"[HUD] {go.name}: ancoras convertidas pra top-center single-point. " +
                      "ContentSizeFitter agora vai funcionar.");
        }

        // Aplica VerticalLayoutGroup + ContentSizeFitter num GameObject pra ele
        // crescer automaticamente conforme os filhos.
        // Sem opcao de lock — sempre auto-resize. Rows tem altura FIXA entao nao tem jitter.
        private void ApplyAutoLayoutTo(GameObject go)
        {
            if (go == null) return;

            var vlg = go.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (vlg == null) vlg = go.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            vlg.spacing                = _autoResizeSpacing;
            vlg.padding                = _autoResizePadding;
            vlg.childAlignment         = TextAnchor.UpperCenter;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = false;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;

            var csf = go.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (csf == null) csf = go.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            csf.verticalFit   = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        }

        // Reset de RectTransform do _taskListContainer — APENAS se flag marcada.
        // Por padrao preserva scale/position/anchors que voce ajustou no Inspector.
        private void ApplyLegacyResetBlock()
        {
            if (_taskListContainer == null) return;
            if (!_forceResetTaskListTransform) return;

            var rt = _taskListContainer as RectTransform;
            if (rt == null) return;

            rt.anchorMin     = Vector2.zero;
            rt.anchorMax     = Vector2.one;
            rt.offsetMin     = Vector2.zero;
            rt.offsetMax     = Vector2.zero;
            rt.pivot         = new Vector2(0.5f, 0.5f);
            rt.localScale    = Vector3.one;
            rt.localPosition = Vector3.zero;
            rt.localRotation = Quaternion.identity;
        }

        private void OnEnable()
        {
            MoneyManager.OnMoneyChanged             += OnMoneyChanged;
            ComboSystem.OnComboUpdated              += OnComboUpdated;
            ComboSystem.OnComboReset                += OnComboReset;
            DailyGoalSystem.OnGoalProgress          += OnGoalProgress;
            PlayerProgressionSystem.OnMissionProgress     += OnMissionProgressed;
            PlayerProgressionSystem.OnMissionCompleted    += OnMissionDone;
            PlayerProgressionSystem.OnActiveMissionsChanged += OnMissionsChanged;
            PlayerProgressionSystem.OnXPChanged           += OnXPChanged;
            PlayerProgressionSystem.OnLevelUp             += OnLevelChanged;
        }

        private void OnDisable()
        {
            MoneyManager.OnMoneyChanged             -= OnMoneyChanged;
            ComboSystem.OnComboUpdated              -= OnComboUpdated;
            ComboSystem.OnComboReset                -= OnComboReset;
            DailyGoalSystem.OnGoalProgress          -= OnGoalProgress;
            PlayerProgressionSystem.OnMissionProgress     -= OnMissionProgressed;
            PlayerProgressionSystem.OnMissionCompleted    -= OnMissionDone;
            PlayerProgressionSystem.OnActiveMissionsChanged -= OnMissionsChanged;
            PlayerProgressionSystem.OnXPChanged           -= OnXPChanged;
            PlayerProgressionSystem.OnLevelUp             -= OnLevelChanged;
        }

        // Handlers do PlayerProgressionSystem
        // RECRIA a lista APENAS em mudancas estruturais (completar/mudar de nivel).
        // Em mudancas frequentes (XP, progress de missao) atualiza SO o texto, sem rebuild.
        private void OnMissionProgressed(Mission m) { UpdateRowTextsOnly(); }
        private void OnMissionDone(Mission m)       { if (_useMissionsAsTasks) RefreshGoalDisplay(); }
        private void OnMissionsChanged()            { if (_useMissionsAsTasks) RefreshGoalDisplay(); }
        private void OnXPChanged(int xp, int need)  { UpdateRowTextsOnly(); }
        private void OnLevelChanged(int newLevel)   { if (_useMissionsAsTasks) RefreshGoalDisplay(); }

        // Atualiza apenas o texto das rows existentes — evita rebuild + jitter.
        // Cada missao usa 2 linhas: titulo (linha A) + reward (linha B).
        private void UpdateRowTextsOnly()
        {
            if (!_useMissionsAsTasks) return;
            if (_taskListContainer == null) return;
            var prog = PlayerProgressionSystem.Instance;
            if (prog == null) return;

            int childIdx = 0;
            if (_showTitleInList) childIdx++;

            if (_showLevelHeader && childIdx < _taskListContainer.childCount)
            {
                var headerTMP = _taskListContainer.GetChild(childIdx).GetComponent<TextMeshProUGUI>();
                if (headerTMP != null)
                    headerTMP.text = $"Nivel {prog.CurrentLevel}  -  {prog.CurrentXP}/{prog.XPForNextLevel} XP";
                childIdx++;
            }

            // Atualiza missoes (2 linhas cada: title + reward)
            var missions = prog.ActiveMissions;
            for (int i = 0; i < missions.Count; i++)
            {
                var m = missions[i];
                Color c = m.IsCompleted ? _taskColorDone : _taskColorActive;

                // Linha A: titulo
                if (childIdx < _taskListContainer.childCount)
                {
                    var titleTMP = _taskListContainer.GetChild(childIdx).GetComponent<TextMeshProUGUI>();
                    if (titleTMP != null)
                    {
                        string status = m.IsCompleted ? "[OK] " : "[ ] ";
                        string progress = m.Type switch
                        {
                            MissionType.EarnMoney      => $" (${m.Current:F0}/${m.Target:F0})",
                            MissionType.ReachCombo     => $" ({(int)m.Current}x/{(int)m.Target}x)",
                            MissionType.Tutorial       => "",
                            _                          => $" ({(int)m.Current}/{(int)m.Target})"
                        };
                        titleTMP.text  = $"{status}{m.Title}{progress}";
                        titleTMP.color = c;
                    }
                    childIdx++;
                }

                // Linha B: reward (se existe e missao tem reward)
                bool hasReward = m.XPReward > 0 || m.MoneyReward > 0;
                if (hasReward && childIdx < _taskListContainer.childCount)
                {
                    // Reward text geralmente nao muda — pode pular se quiser otimizar
                    childIdx++;
                }
            }
        }

        private void AutoWire()
        {
            // Só completa os campos que já não foram ligados no Inspector
            foreach (var btn in FindObjectsOfType<Button>(includeInactive: true))
            {
                string n = btn.gameObject.name;
                if (_pauseButton   == null && Has(n, "Pause"))            _pauseButton   = btn;
                if (_upgradeButton == null && Has(n, "Upgrade"))          _upgradeButton = btn;
                if (_settingsButton== null && Has(n, "Settings","Config"))_settingsButton= btn;
                if (_mapButton     == null && Has(n, "Map"))              _mapButton     = btn;
                if (_soundButton   == null && Has(n, "Sound","Mute","Audio")) _soundButton = btn;
                // Busca abrangente por nomes possiveis do botao de toggle
                if (_taskToggleButton == null && Has(n,
                    "TaskToggle","ToggleTask","ToggleTarefa","TaskShow","TaskHide",
                    "TarefaToggle","TasksToggle","ToggleTasks","BtnTarefa","ListaToggle")) _taskToggleButton = btn;
            }

            // Fallback: se _taskTogglePanel nao foi atribuido, usa _autoResizeBackground
            // ou _taskListContainer.parent (DailyGoalBG). Garante que o botao funcione
            // mesmo se voce esquecer de plugar o painel.
            if (_taskTogglePanel == null)
            {
                if (_autoResizeBackground != null)
                    _taskTogglePanel = _autoResizeBackground;
                else if (_taskListContainer != null && _taskListContainer.parent != null)
                    _taskTogglePanel = _taskListContainer.parent.gameObject;
            }
        }

        // ── Painel de pausa (Pizza Ready style) ───────────────────────────
        private void BuildPausePanel()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            _pausePanel = new GameObject("_PausePanelRuntime");
            _pausePanel.transform.SetParent(canvas.transform, false);
            _pausePanel.transform.SetAsLastSibling();

            // Overlay semi-transparente cobre tela inteira
            var overlay = _pausePanel.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.60f);
            overlay.raycastTarget = true;
            var oRT = _pausePanel.GetComponent<RectTransform>();
            oRT.anchorMin = Vector2.zero;
            oRT.anchorMax = Vector2.one;
            oRT.offsetMin = oRT.offsetMax = Vector2.zero;

            // Cartão central — cor creme arredondado
            var card = new GameObject("Card");
            card.transform.SetParent(_pausePanel.transform, false);
            var cardImg = card.AddComponent<Image>();
            UIStyleKit.StyleCard(cardImg, 36);
            var cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(340f, 300f);
            cardRT.anchoredPosition = Vector2.zero;

            // Sombra do cartão
            var cardSh = new GameObject("CardSh");
            cardSh.transform.SetParent(_pausePanel.transform, false);
            cardSh.transform.SetSiblingIndex(card.transform.GetSiblingIndex());
            var cardShImg = cardSh.AddComponent<Image>();
            UIStyleKit.ApplyRounded(cardShImg, new Color(0f, 0f, 0f, 0.35f), 36);
            var cardShRT = cardSh.GetComponent<RectTransform>();
            cardShRT.anchorMin = cardShRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardShRT.sizeDelta = new Vector2(344f, 300f);
            cardShRT.anchoredPosition = new Vector2(4f, -8f);

            // Faixa laranja no topo do cartão (estilo Pizza Ready)
            var header = new GameObject("Header");
            header.transform.SetParent(card.transform, false);
            var hImg = header.AddComponent<Image>();
            UIStyleKit.ApplyRounded(hImg, UIStyleKit.Orange, 36);
            var hRT = header.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0f, 1f);
            hRT.anchorMax = Vector2.one;
            hRT.offsetMin = new Vector2(0f, -90f);
            hRT.offsetMax = Vector2.zero;

            // Título
            var title = UIStyleKit.MakeText(header.transform, "PAUSADO", 32,
                UIStyleKit.White, new Vector2(0f, -10f), new Vector2(300f, 60f));
            UIStyleKit.StyleLabel(title, UIStyleKit.White, 32, true, true);

            // Botão Continuar (verde)
            var btnContinue = UIStyleKit.MakeButton(card.transform, "Continuar",
                UIStyleKit.Green, UIStyleKit.GreenDark,
                new Vector2(0f, -30f), new Vector2(260f, 65f));
            btnContinue.onClick.AddListener(OnResumeClicked);

            // Botão Menu Principal (vermelho)
            var btnMenu = UIStyleKit.MakeButton(card.transform, "Menu Principal",
                UIStyleKit.Red, UIStyleKit.RedDark,
                new Vector2(0f, -110f), new Vector2(260f, 65f));
            btnMenu.onClick.AddListener(OnMainMenuClicked);

            _pausePanel.SetActive(false);
        }

        // ── Aplica estilo Pizza Ready em todos os elementos do HUD ─────────
        private void ApplyStyle()
        {
            // Dinheiro
            if (_moneyText != null) UIStyleKit.StyleMoney(_moneyText);

            // Pill escuro atrás do texto de dinheiro
            var moneyBg = _moneyText != null
                ? _moneyText.transform.parent?.GetComponent<Image>()
                : null;
            if (moneyBg != null) UIStyleKit.ApplyRounded(moneyBg, UIStyleKit.BgDark, 28);

            // Upgrade button — grande, verde, pill
            if (_upgradeButton != null)
                UIStyleKit.StyleButton(_upgradeButton, UIStyleKit.Green, UIStyleKit.GreenDark, 32, 7f);

            // Settings/Pause button — cinza escuro arredondado
            if (_settingsButton != null)
                UIStyleKit.StyleButton(_settingsButton,
                    new Color(0.22f, 0.20f, 0.18f, 0.90f),
                    new Color(0.08f, 0.07f, 0.05f, 0.90f), 28, 4f);

            // Map button
            if (_mapButton != null)
                UIStyleKit.StyleButton(_mapButton,
                    new Color(0.20f, 0.50f, 0.90f, 0.90f),
                    new Color(0.08f, 0.25f, 0.60f, 0.90f), 28, 4f);

            // Combo display — fundo laranja arredondado
            if (_comboDisplay != null)
            {
                var cImg = _comboDisplay.GetComponent<Image>();
                if (cImg != null) UIStyleKit.ApplyRounded(cImg, UIStyleKit.Orange, 20);
            }
            if (_comboMultiplierText != null)
                UIStyleKit.StyleLabel(_comboMultiplierText, UIStyleKit.White, 26, true, true);
            if (_comboCountText != null)
                UIStyleKit.StyleLabel(_comboCountText, UIStyleKit.White, 18, true, false);

            // Daily goal text
            if (_dailyGoalText != null)
                UIStyleKit.StyleLabel(_dailyGoalText, UIStyleKit.White, 16, false, false);

            // NOTA: posicionamento dos botoes (Upgrade/Map/Settings) deve ser feito
            // MANUALMENTE no Inspector via RectTransform. ApplyStyle() agora so cuida
            // de cor/sprite/sombra — nao sobrescreve mais anchors nem positions.
        }

        private static bool Has(string source, params string[] keywords)
        {
            foreach (var kw in keywords)
                if (source.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        private void Update()
        {
            // Rotaciona a meta exibida a cada N segundos
            if (DailyGoalSystem.Instance == null) return;
            _goalRotateTimer += Time.deltaTime;
            if (_goalRotateTimer >= GoalRotateInterval)
            {
                _goalRotateTimer = 0f;
                _goalDisplayIndex = (_goalDisplayIndex + 1) % DailyGoalSystem.Instance.Goals.Length;
                RefreshGoalDisplay();
            }
        }

        // ── Botões ─────────────────────────────────────────────────────────
        private void SetupButtons()
        {
            // RemoveAllListeners antes de adicionar — evita duplicatas se chamado mais de uma vez
            Bind(_pauseButton,     OnPauseClicked);
            Bind(_upgradeButton,   OnUpgradeClicked);
            Bind(_settingsButton,  OnSettingsClicked);
            Bind(_mapButton,       OnMapClicked);
            Bind(_soundButton,     OnSoundClicked);
            Bind(_taskToggleButton, OnTaskToggleClicked);
        }

        private static void Bind(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null) return;
            btn.onClick.RemoveListener(action);
            btn.onClick.AddListener(action);
        }

        // ── Dinheiro ───────────────────────────────────────────────────────
        private void OnMoneyChanged(float newAmount)
        {
            _targetMoney = newAmount;
            if (_moneyAnimCoroutine != null) StopCoroutine(_moneyAnimCoroutine);
            _moneyAnimCoroutine = StartCoroutine(AnimateMoneyCounter());
        }

        private IEnumerator AnimateMoneyCounter()
        {
            float startMoney = _displayedMoney;
            float elapsed = 0f;
            while (elapsed < _moneyAnimDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _displayedMoney = Mathf.Lerp(startMoney, _targetMoney, elapsed / _moneyAnimDuration);
                UpdateMoneyDisplay();
                yield return null;
            }
            _displayedMoney = _targetMoney;
            UpdateMoneyDisplay();
        }

        private void UpdateMoneyDisplay()
        {
            if (_moneyText != null)
                _moneyText.text = $"${_displayedMoney:F0}";
        }

        // ── Daily Goal ─────────────────────────────────────────────────────
        private void OnGoalProgress(DailyGoal goal)
        {
            if (DailyGoalSystem.Instance == null) return;
            var goals = DailyGoalSystem.Instance.Goals;

            // Modo lista: sempre atualiza (qualquer meta muda)
            if (_taskListContainer != null)
            {
                RefreshGoalDisplay();
                return;
            }

            // Modo texto unico: so atualiza se a meta progredida for a exibida
            if (_goalDisplayIndex < goals.Length && goals[_goalDisplayIndex] == goal)
                RefreshGoalDisplay();
        }

        private void RefreshGoalDisplay()
        {
            // ── Modo Missoes (sistema novo de progressao com XP/Nivel) ─────────
            if (_useMissionsAsTasks)
            {
                var prog = PlayerProgressionSystem.Instance;
                if (prog == null) return;
                if (_taskListContainer != null)
                {
                    RenderMissionList(prog);
                    return;
                }
                // Sem container — mostra so a missao atual no _dailyGoalText (fallback)
                if (_dailyGoalText != null && prog.ActiveMissions.Count > 0)
                {
                    var m = prog.ActiveMissions[0];
                    _dailyGoalText.text  = FormatMissionText(m, includeStatus: true);
                    _dailyGoalText.color = m.IsCompleted ? _taskColorDone : _taskColorActive;
                    if (_dailyGoalSlider != null) _dailyGoalSlider.value = m.Progress;
                }
                return;
            }

            // ── Modo Antigo: DailyGoals ────────────────────────────────────────
            if (DailyGoalSystem.Instance == null) return;
            var goals = DailyGoalSystem.Instance.Goals;
            if (goals == null || goals.Length == 0) return;

            if (_taskListContainer != null)
            {
                RenderTaskList(goals);
                return;
            }

            // ── Modo Texto Unico: rotaciona uma meta por vez (comportamento antigo)
            _goalDisplayIndex = Mathf.Clamp(_goalDisplayIndex, 0, goals.Length - 1);
            var goal = goals[_goalDisplayIndex];

            // Barra de progresso
            if (_dailyGoalSlider != null)
                _dailyGoalSlider.value = goal.Progress;

            if (_dailyGoalText != null)
            {
                _dailyGoalText.text  = FormatGoalText(goal, includeStatus: false) +
                                       (goal.Completed ? " [OK]" : $"  ({_goalDisplayIndex + 1}/{goals.Length})");
                _dailyGoalText.color = goal.Completed ? _taskColorDone : _taskColorActive;
            }
        }

        // Renderiza as 3 metas como lista de TMPs filhos do _taskListContainer.
        // Recria os filhos a cada chamada (simples e estavel). Garante um
        // VerticalLayoutGroup + ContentSizeFitter pra a lista crescer com conteudo.
        private void RenderTaskList(DailyGoal[] goals)
        {
            // Garante VerticalLayoutGroup pra alinhamento
            var vlg = _taskListContainer.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = _taskListContainer.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                vlg.spacing                  = 4f;
                vlg.padding                  = new RectOffset(8, 8, 4, 4);
                vlg.childAlignment           = TextAnchor.UpperLeft;
                vlg.childControlWidth        = true;
                vlg.childControlHeight       = false;
                vlg.childForceExpandWidth    = true;
                vlg.childForceExpandHeight   = false;
            }

            // ContentSizeFitter pro container CRESCER com conteudo
            var csf = _taskListContainer.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (csf == null)
            {
                csf = _taskListContainer.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                csf.verticalFit   = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                csf.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            }

            // Limpa filhos existentes (recria toda vez — simples e correto)
            for (int i = _taskListContainer.childCount - 1; i >= 0; i--)
            {
                var child = _taskListContainer.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else                       DestroyImmediate(child);
            }

            // Cria 1 linha TMP por meta
            for (int i = 0; i < goals.Length; i++)
            {
                var goal = goals[i];
                var rowGO = new GameObject($"Task_{i + 1}");
                rowGO.transform.SetParent(_taskListContainer, false);
                var tmp = rowGO.AddComponent<TextMeshProUGUI>();
                tmp.text       = FormatGoalText(goal, includeStatus: true);
                tmp.fontSize   = _taskRowFontSize;
                tmp.fontStyle  = FontStyles.Bold;
                tmp.color      = goal.Completed ? _taskColorDone : _taskColorActive;
                tmp.alignment  = TextAlignmentOptions.MidlineLeft;
                tmp.enableWordWrapping = false;

                // Layout element pra controlar altura preferida (importante pro VLG)
                var le = rowGO.AddComponent<UnityEngine.UI.LayoutElement>();
                le.preferredHeight = _taskRowFontSize + 6f;
                le.minHeight       = _taskRowFontSize;
            }
        }

        // Formata texto da meta. includeStatus=true prefixa "[OK]"/"[ ]" no inicio.
        private static string FormatGoalText(DailyGoal goal, bool includeStatus)
        {
            string body = goal.Type switch
            {
                GoalType.EarnMoney     => $"Ganhar ${goal.Target:F0}  (${goal.Current:F0})",
                GoalType.DeliverPizzas => $"Entregar {(int)goal.Target} pizzas  ({(int)goal.Current}/{(int)goal.Target})",
                GoalType.ReachCombo    => $"Combo {(int)goal.Target}x  (atual {(int)goal.Current}x)",
                _                       => goal.Type.ToString()
            };
            if (!includeStatus) return body;
            string status = goal.Completed ? "[OK] " : "[  ] ";
            return status + body;
        }

        // ── Sistema de Missoes (PlayerProgressionSystem) ─────────────────────

        // Renderiza header de XP/Nivel + lista de missoes ativas no container.
        private void RenderMissionList(PlayerProgressionSystem prog)
        {
            EnsureContainerLayout();

            // Limpa filhos antigos
            for (int i = _taskListContainer.childCount - 1; i >= 0; i--)
            {
                var child = _taskListContainer.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else                       DestroyImmediate(child);
            }

            // Titulo "TAREFA:" como primeira linha — substitui o modal de titulo separado.
            if (_showTitleInList && !string.IsNullOrEmpty(_titleText))
            {
                CreateTitleRow("__Title", _titleText, _titleColor, _titleFontSize);
            }

            // Header: "Nivel 2 - 120/200 XP"
            if (_showLevelHeader)
            {
                CreateTaskRow("__Header",
                    $"Nivel {prog.CurrentLevel}  -  {prog.CurrentXP}/{prog.XPForNextLevel} XP",
                    _taskColorHeader, bold: true);
            }

            // Missoes ativas — cada uma vira 2 linhas FIXAS (titulo + reward).
            var missions = prog.ActiveMissions;
            if (missions.Count == 0)
            {
                CreateTaskRow("__NoMission",
                    "Aguarde proxima missao...",
                    _taskColorActive, bold: false);
                return;
            }

            for (int i = 0; i < missions.Count; i++)
            {
                var m = missions[i];
                Color c = m.IsCompleted ? _taskColorDone : _taskColorActive;

                // Linha 1: titulo + progresso (fonte normal)
                string status = m.IsCompleted ? "[OK] " : "[ ] ";
                string progress = m.Type switch
                {
                    MissionType.EarnMoney      => $" (${m.Current:F0}/${m.Target:F0})",
                    MissionType.ReachCombo     => $" ({(int)m.Current}x/{(int)m.Target}x)",
                    MissionType.Tutorial       => "",
                    _                          => $" ({(int)m.Current}/{(int)m.Target})"
                };
                CreateTaskRow($"Mission_{i}_Title", $"{status}{m.Title}{progress}", c, bold: true);

                // Linha 2: reward (fonte menor, cinza)
                string reward = "";
                if (m.XPReward > 0)    reward += $"  +{m.XPReward}XP";
                if (m.MoneyReward > 0) reward += $"  +${m.MoneyReward}";
                if (!string.IsNullOrEmpty(reward))
                {
                    CreateTaskRowSmall($"Mission_{i}_Reward", reward,
                        new Color(0.50f, 0.50f, 0.50f), bold: false);
                }
            }
        }

        // Cria a primeira linha "TAREFA:" — titulo grande e centralizado no topo da lista.
        // Altura FIXA (sem ContentSizeFitter) pra evitar jitter de re-layout.
        private void CreateTitleRow(string name, string text, Color color, float fontSize)
        {
            var rowGO = new GameObject(name, typeof(RectTransform));
            rowGO.transform.SetParent(_taskListContainer, false);
            rowGO.AddComponent<CanvasRenderer>();
            var tmp = rowGO.AddComponent<TextMeshProUGUI>();

            if (_moneyText != null && _moneyText.font != null)
                tmp.font = _moneyText.font;

            tmp.text              = text;
            tmp.fontSize          = fontSize;
            tmp.color             = color;
            tmp.fontStyle         = FontStyles.Bold;
            tmp.alignment         = TextAlignmentOptions.Center;
            tmp.enableAutoSizing  = false;
            tmp.enableWordWrapping = false;
            tmp.richText          = false;
            tmp.raycastTarget     = false;

            // Altura FIXA (sem CSF) — evita conflito com VLG/CSF do parent
            float h = fontSize * 1.3f;
            var le = rowGO.AddComponent<UnityEngine.UI.LayoutElement>();
            le.minHeight       = h;
            le.preferredHeight = h;
            le.flexibleHeight  = 0f;
            le.flexibleWidth   = 1f;
            le.layoutPriority  = 1;   // overrides TMP preferred size
        }

        // Versao menor de CreateTaskRow — usada pra linha de reward (subtitulo cinza).
        private void CreateTaskRowSmall(string name, string text, Color color, bool bold)
        {
            var rowGO = new GameObject(name, typeof(RectTransform));
            rowGO.transform.SetParent(_taskListContainer, false);
            rowGO.AddComponent<CanvasRenderer>();
            var tmp = rowGO.AddComponent<TextMeshProUGUI>();

            if (_moneyText != null && _moneyText.font != null)
                tmp.font = _moneyText.font;

            float smallSize = Mathf.Max(_taskRowFontSize * 0.7f, 14f);

            tmp.text              = text;
            tmp.fontSize          = smallSize;
            tmp.color             = color;
            tmp.fontStyle         = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.alignment         = TextAlignmentOptions.MidlineLeft;
            tmp.enableAutoSizing  = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode      = TextOverflowModes.Overflow;
            tmp.richText          = false;
            tmp.raycastTarget     = false;

            float h = smallSize * 1.3f;
            var le = rowGO.AddComponent<UnityEngine.UI.LayoutElement>();
            le.minHeight       = h;
            le.preferredHeight = h;
            le.flexibleHeight  = 0f;
            le.flexibleWidth   = 1f;
            le.layoutPriority  = 1;
        }

        // Cria 1 linha de texto no container — TMP limpo, sem rich text, sem ContentSizeFitter.
        // Setup minimo pra evitar bugs de rendering em TMPs criados via AddComponent.
        private void CreateTaskRow(string name, string text, Color color, bool bold)
        {
            // Cria GameObject com RectTransform (necessario antes do TMP)
            var rowGO = new GameObject(name, typeof(RectTransform));
            rowGO.transform.SetParent(_taskListContainer, false);

            // CanvasRenderer + TMP (ordem importa)
            rowGO.AddComponent<CanvasRenderer>();
            var tmp = rowGO.AddComponent<TextMeshProUGUI>();

            // Copia SOMENTE font asset do _moneyText (que ja funciona).
            // NAO copia fontMaterial (cria instancia desnecessaria e pode bugar).
            if (_moneyText != null && _moneyText.font != null)
                tmp.font = _moneyText.font;

            // Minimo 16 pra ficar legivel
            float effectiveSize = Mathf.Max(_taskRowFontSize, 16f);

            tmp.text              = text;
            tmp.fontSize          = effectiveSize;
            tmp.color             = color;
            tmp.fontStyle         = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.alignment         = TextAlignmentOptions.MidlineLeft;
            tmp.enableAutoSizing  = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode      = TextOverflowModes.Overflow;  // NAO trunca — deixa texto sair se preciso
            tmp.richText          = false;
            tmp.raycastTarget     = false;

            // Altura FIXA — LayoutElement ignora preferred size do TMP. Mudanca de texto
            // NAO triggera resize do row, evitando cascata de jitter no modal.
            float h = effectiveSize * 1.4f;
            var le = rowGO.AddComponent<UnityEngine.UI.LayoutElement>();
            le.minHeight       = h;
            le.preferredHeight = h;
            le.flexibleHeight  = 0f;
            le.flexibleWidth   = 1f;
            le.layoutPriority  = 1;   // prioridade > 0 (TMP padrao) — overrides TMP preferred size
        }

        // Garante VLG + CSF no container (compartilhado entre RenderMissionList e RenderTaskList).
        private void EnsureContainerLayout()
        {
            var vlg = _taskListContainer.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = _taskListContainer.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                vlg.spacing                  = 4f;
                vlg.padding                  = new RectOffset(8, 8, 4, 4);
                vlg.childAlignment           = TextAnchor.UpperLeft;
                vlg.childControlWidth        = true;
                vlg.childControlHeight       = false;
                vlg.childForceExpandWidth    = true;
                vlg.childForceExpandHeight   = false;
            }
            var csf = _taskListContainer.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (csf == null)
            {
                csf = _taskListContainer.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                csf.verticalFit   = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                csf.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            }
        }

        // Formata missao em UMA UNICA linha simples (sem rich text, sem newline).
        // Mantem tudo previsivel e evita bugs de rendering com tags HTML.
        private static string FormatMissionText(Mission m, bool includeStatus)
        {
            string progress = m.Type switch
            {
                MissionType.EarnMoney      => $" (${m.Current:F0}/${m.Target:F0})",
                MissionType.ReachCombo     => $" ({(int)m.Current}x/{(int)m.Target}x)",
                MissionType.Tutorial       => "",
                _                          => $" ({(int)m.Current}/{(int)m.Target})"
            };
            string status = includeStatus ? (m.IsCompleted ? "[OK] " : "[ ] ") : "";

            string reward = "";
            if (m.XPReward > 0)    reward += $"  +{m.XPReward}XP";
            if (m.MoneyReward > 0) reward += $"  +${m.MoneyReward}";

            return $"{status}{m.Title}{progress}{reward}";
        }

        // ── Combo ──────────────────────────────────────────────────────────
        private void OnComboUpdated(int combo, float multiplier)
        {
            if (_comboDisplay == null) return;

            // Só mostra a partir do combo 2
            bool show = combo >= 2;
            _comboDisplay.SetActive(show);
            if (!show) return;

            if (_comboMultiplierText != null)
                _comboMultiplierText.text = $"x{multiplier:F2}";

            if (_comboCountText != null)
                _comboCountText.text = $"{combo} COMBO";

            // Cor do fundo muda conforme nível do combo
            // 2-4 → laranja, 5-9 → dourado, 10-19 → roxo, 20+ → vermelho
            var bg = _comboDisplay.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = combo >= 20 ? new Color(0.85f, 0.10f, 0.10f, 0.90f)
                         : combo >= 10 ? new Color(0.55f, 0.10f, 0.85f, 0.90f)
                         : combo >=  5 ? new Color(0.85f, 0.72f, 0.05f, 0.90f)
                                       : new Color(0.95f, 0.55f, 0.05f, 0.88f);
            }

            // Animação de bounce ao atualizar o combo
            if (_comboBounceCoroutine != null) StopCoroutine(_comboBounceCoroutine);
            _comboBounceCoroutine = StartCoroutine(BounceComboDisplay());
        }

        private void OnComboReset()
        {
            if (_comboDisplay != null)
                _comboDisplay.SetActive(false);
        }

        // Pequeno bounce de escala no painel de combo (feel do Pizza Ready)
        private IEnumerator BounceComboDisplay()
        {
            if (_comboDisplay == null) yield break;

            var rt = _comboDisplay.GetComponent<RectTransform>();
            if (rt == null) yield break;

            Vector3 original = Vector3.one;
            Vector3 punched  = Vector3.one * 1.15f;

            float t = 0f;
            float dur = 0.12f;
            while (t < dur)
            {
                t += Time.deltaTime;
                rt.localScale = Vector3.Lerp(original, punched, t / dur);
                yield return null;
            }

            t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                rt.localScale = Vector3.Lerp(punched, original, t / dur);
                yield return null;
            }

            rt.localScale = original;
        }

        // ── Ações dos botões ───────────────────────────────────────────────
        private void OnPauseClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            ShowPause();
        }

        private void OnResumeClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            HidePause();
        }

        private void OnMainMenuClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            HidePause();
            GameManager.Instance?.GoToMainMenu();
        }

        private void ShowPause()
        {
            if (_isPaused) return;
            _isPaused = true;
            Time.timeScale = 0f;
            if (_pausePanel != null) _pausePanel.SetActive(true);
        }

        private void HidePause()
        {
            if (!_isPaused) return;
            _isPaused = false;
            Time.timeScale = 1f;
            if (_pausePanel != null) _pausePanel.SetActive(false);
        }

        private void OnUpgradeClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            UpgradePanelUI upgradePanel = FindObjectOfType<UpgradePanelUI>(includeInactive: true);
            upgradePanel?.Toggle();
        }

        public void OnSettingsClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            if (_inGameSettingsPanel == null) BuildInGameSettingsPanel();
            if (_inGameSettingsPanel != null) _inGameSettingsPanel.SetActive(true);
        }

        private void HideInGameSettings()
        {
            AudioManager.Instance?.PlayButtonClick();
            if (_inGameSettingsPanel != null) _inGameSettingsPanel.SetActive(false);
        }

        // Cria o painel de Settings in-game — QUADRADO PURO, sem arredondamento.
        private void BuildInGameSettingsPanel()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            _inGameSettingsPanel = new GameObject("_InGameSettingsRuntime");
            _inGameSettingsPanel.transform.SetParent(canvas.transform, false);
            _inGameSettingsPanel.transform.SetAsLastSibling();

            // Overlay escuro semi-transparente
            var overlay = _inGameSettingsPanel.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.65f);
            overlay.raycastTarget = true;
            var oRT = _inGameSettingsPanel.GetComponent<RectTransform>();
            oRT.anchorMin = Vector2.zero;
            oRT.anchorMax = Vector2.one;
            oRT.offsetMin = oRT.offsetMax = Vector2.zero;

            // Card BRANCO QUADRADO — ocupa ~85% da tela (anchor stretch com margens).
            var card = new GameObject("Card");
            card.transform.SetParent(_inGameSettingsPanel.transform, false);
            var cardImg = card.AddComponent<Image>();
            cardImg.sprite = null;
            cardImg.color  = new Color(0.96f, 0.96f, 0.96f);
            var cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.06f, 0.10f);   // 6% margem horizontal, 10% bottom
            cardRT.anchorMax = new Vector2(0.94f, 0.85f);   // 6% margem horizontal, 15% top
            cardRT.offsetMin = cardRT.offsetMax = Vector2.zero;

            // Header AZUL QUADRADO — maior pra combinar com card grande
            var header = new GameObject("Header");
            header.transform.SetParent(card.transform, false);
            var hImg = header.AddComponent<Image>();
            hImg.sprite = null;
            hImg.color  = new Color(0.27f, 0.57f, 0.95f);
            var hRT = header.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0f, 1f);
            hRT.anchorMax = new Vector2(1f, 1f);
            hRT.pivot     = new Vector2(0.5f, 1f);
            hRT.sizeDelta = new Vector2(0f, 80f);   // 80px altura (era 56)
            hRT.anchoredPosition = Vector2.zero;

            // Texto "CONFIGURACOES" centrado no header
            var headerTextGO = new GameObject("HeaderText");
            headerTextGO.transform.SetParent(header.transform, false);
            var headerTmp = headerTextGO.AddComponent<TextMeshProUGUI>();
            headerTmp.text      = "CONFIGURACOES";
            headerTmp.fontSize  = 32;     // maior (era 22)
            headerTmp.fontStyle = FontStyles.Bold;
            headerTmp.color     = Color.white;
            headerTmp.alignment = TextAlignmentOptions.Center;
            var headerTextRT = headerTextGO.GetComponent<RectTransform>();
            headerTextRT.anchorMin = Vector2.zero;
            headerTextRT.anchorMax = Vector2.one;
            headerTextRT.offsetMin = new Vector2(20f, 0f);
            headerTextRT.offsetMax = new Vector2(-70f, 0f);

            // Botao X vermelho QUADRADO no canto superior direito do header
            var closeBtn = BuildRectButton(header.transform, "X",
                new Color(0.92f, 0.22f, 0.18f), new Color(0.65f, 0.10f, 0.08f),
                new Vector2(56f, 56f), radius: 6);   // maior (era 40)
            var closeRT = closeBtn.GetComponent<RectTransform>();
            closeRT.anchorMin = closeRT.anchorMax = new Vector2(1f, 0.5f);
            closeRT.pivot     = new Vector2(1f, 0.5f);
            closeRT.anchoredPosition = new Vector2(-12f, 0f);
            var closeTmp = closeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (closeTmp != null) closeTmp.fontSize = 28;
            closeBtn.onClick.AddListener(HideInGameSettings);

            // Toggles QUADRADOS (versao local sem arredondamento)
            // Posicionados no terco superior do card (logo abaixo do header)
            BuildSquareToggleRow(card.transform, "SOM",
                0.72f, 0.80f,
                "SoundEnabled", true,
                v => AudioManager.Instance?.SetMasterVolume(v ? 1f : 0f));

            BuildSquareToggleRow(card.transform, "MUSICA",
                0.62f, 0.70f,
                "MusicEnabled", true,
                v => AudioManager.Instance?.SetMusicVolume(v ? 0.6f : 0f));

            BuildSquareToggleRow(card.transform, "HAPTICO",
                0.52f, 0.60f,
                "HapticEnabled", true,
                v =>
                {
#if UNITY_ANDROID || UNITY_IOS
                    if (v) Handheld.Vibrate();
#endif
                });

            // Botao PAUSAR — ancorado ao BOTTOM do card (nao center)
            var pauseBtn = BuildRectButton(card.transform, "PAUSAR",
                new Color(0.95f, 0.55f, 0.05f), new Color(0.65f, 0.30f, 0.02f),
                new Vector2(420f, 76f), radius: 0);
            var pauseRT = pauseBtn.GetComponent<RectTransform>();
            pauseRT.anchorMin = new Vector2(0.5f, 0f);
            pauseRT.anchorMax = new Vector2(0.5f, 0f);
            pauseRT.pivot     = new Vector2(0.5f, 0f);
            pauseRT.anchoredPosition = new Vector2(0f, 130f);   // 130px do fundo
            var pauseTmp = pauseBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (pauseTmp != null) pauseTmp.fontSize = 26;
            pauseBtn.onClick.AddListener(() =>
            {
                HideInGameSettings();
                ShowPause();
            });

            // Botao MENU PRINCIPAL — ancorado ao BOTTOM, abaixo do PAUSAR
            var menuBtn = BuildRectButton(card.transform, "MENU PRINCIPAL",
                new Color(0.85f, 0.20f, 0.15f), new Color(0.55f, 0.08f, 0.06f),
                new Vector2(420f, 76f), radius: 0);
            var menuRT = menuBtn.GetComponent<RectTransform>();
            menuRT.anchorMin = new Vector2(0.5f, 0f);
            menuRT.anchorMax = new Vector2(0.5f, 0f);
            menuRT.pivot     = new Vector2(0.5f, 0f);
            menuRT.anchoredPosition = new Vector2(0f, 40f);     // 40px do fundo
            var menuTmp = menuBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (menuTmp != null) menuTmp.fontSize = 26;
            menuBtn.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButtonClick();
                GameManager.Instance?.GoToMainMenu();
            });

            _inGameSettingsPanel.SetActive(false);
        }

        // Cria toggle row QUADRADA — label esquerda + botao on/off quadrado direita.
        private static void BuildSquareToggleRow(Transform parent, string label,
            float anchorYMin, float anchorYMax,
            string prefKey, bool defaultOn,
            System.Action<bool> onChange)
        {
            var row = new GameObject("Toggle_" + label, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rowRT = row.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0.05f, anchorYMin);
            rowRT.anchorMax = new Vector2(0.95f, anchorYMax);
            rowRT.offsetMin = rowRT.offsetMax = Vector2.zero;

            // Label esquerda
            var lblGO = new GameObject("Lbl", typeof(RectTransform));
            lblGO.transform.SetParent(row.transform, false);
            lblGO.AddComponent<CanvasRenderer>();
            var lblTmp = lblGO.AddComponent<TextMeshProUGUI>();
            lblTmp.text      = label;
            lblTmp.fontSize  = 22;
            lblTmp.fontStyle = FontStyles.Bold;
            lblTmp.color     = UIStyleKit.TextDark;
            lblTmp.alignment = TextAlignmentOptions.Left;
            var lblRT = lblGO.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0f, 0f);
            lblRT.anchorMax = new Vector2(0.55f, 1f);
            lblRT.offsetMin = new Vector2(10f, 0f);
            lblRT.offsetMax = Vector2.zero;

            // Estado inicial
            bool isOn = PlayerPrefs.GetInt(prefKey, defaultOn ? 1 : 0) == 1;

            // Botao toggle QUADRADO
            var btnGO = new GameObject("Toggle", typeof(RectTransform));
            btnGO.transform.SetParent(row.transform, false);
            btnGO.AddComponent<CanvasRenderer>();
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.sprite = null;            // SEM SPRITE = quadrado
            var btn = btnGO.AddComponent<Button>();
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.65f, 0.15f);
            btnRT.anchorMax = new Vector2(1.00f, 0.85f);
            btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;

            // Texto do toggle
            var btnTxtGO = new GameObject("Lbl", typeof(RectTransform));
            btnTxtGO.transform.SetParent(btnGO.transform, false);
            btnTxtGO.AddComponent<CanvasRenderer>();
            var btnTmp = btnTxtGO.AddComponent<TextMeshProUGUI>();
            btnTmp.fontSize  = 18;
            btnTmp.fontStyle = FontStyles.Bold;
            btnTmp.color     = Color.white;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.raycastTarget = false;
            var btnTxtRT = btnTxtGO.GetComponent<RectTransform>();
            btnTxtRT.anchorMin = Vector2.zero;
            btnTxtRT.anchorMax = Vector2.one;
            btnTxtRT.offsetMin = btnTxtRT.offsetMax = Vector2.zero;

            Color onColor  = new Color(0.18f, 0.65f, 0.20f);
            Color offColor = new Color(0.55f, 0.52f, 0.48f);
            void ApplyVisual(bool on)
            {
                btnImg.color = on ? onColor : offColor;
                btnTmp.text  = on ? "ON" : "OFF";
            }
            ApplyVisual(isOn);

            btn.onClick.AddListener(() =>
            {
                isOn = !isOn;
                PlayerPrefs.SetInt(prefKey, isOn ? 1 : 0);
                PlayerPrefs.Save();
                onChange?.Invoke(isOn);
                ApplyVisual(isOn);
            });
        }

        // Cria um botao QUADRADO PURO — sem sprite arredondado, so cor solida.
        private static Button BuildRectButton(Transform parent, string label,
            Color fill, Color shadow, Vector2 size, int radius = 0)
        {
            var go = new GameObject("Btn_" + label.Replace(" ", ""), typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<CanvasRenderer>();
            var img = go.AddComponent<Image>();
            img.sprite = null;        // SEM SPRITE = quadrado puro
            img.color  = fill;
            var btn = go.AddComponent<Button>();
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            // Sombra atras — tambem quadrada
            var sh = new GameObject("_Sh", typeof(RectTransform));
            sh.transform.SetParent(go.transform, false);
            sh.transform.SetAsFirstSibling();
            sh.AddComponent<CanvasRenderer>();
            var sImg = sh.AddComponent<Image>();
            sImg.sprite = null;
            sImg.color  = shadow;
            sImg.raycastTarget = false;
            var sRT = sh.GetComponent<RectTransform>();
            sRT.anchorMin = Vector2.zero;
            sRT.anchorMax = Vector2.one;
            sRT.offsetMin = new Vector2(0f, -4f);
            sRT.offsetMax = new Vector2(0f, -2f);

            // Texto
            var txt = new GameObject("Lbl", typeof(RectTransform));
            txt.transform.SetParent(go.transform, false);
            txt.AddComponent<CanvasRenderer>();
            var tmp = txt.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = Mathf.Clamp(size.y * 0.40f, 14f, 24f);
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            var trt = txt.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            return btn;
        }

        public void OnMapClicked()
        {
            Debug.Log("[HUDController] Map em breve.");
        }

        // Mostra/esconde o painel de tarefas — persiste estado em PlayerPrefs.
        private void OnTaskToggleClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            if (_taskTogglePanel == null) return;

            bool nowVisible = !_taskTogglePanel.activeSelf;
            _taskTogglePanel.SetActive(nowVisible);
            PlayerPrefs.SetInt("TaskListVisible", nowVisible ? 1 : 0);
            PlayerPrefs.Save();

            UpdateTaskToggleLabel(nowVisible);
        }

        // Aplica o estado salvo do toggle (chamado no Start).
        private void ApplyTaskTogglePref()
        {
            if (_taskTogglePanel == null) return;
            bool visible = PlayerPrefs.GetInt("TaskListVisible",
                _taskListVisibleInitially ? 1 : 0) == 1;
            _taskTogglePanel.SetActive(visible);
            UpdateTaskToggleLabel(visible);
        }

        // Atualiza o texto do botao de toggle conforme estado atual.
        private void UpdateTaskToggleLabel(bool visible)
        {
            if (_taskToggleButton == null) return;
            var tmp = _taskToggleButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = visible ? _taskToggleLabelVisible : _taskToggleLabelHidden;
        }

        // Substitui o visual do botao Upgrade pelo sprite atribuido em _upgradeButtonSprite.
        // Util pra usar PNGs custom (ex: seta-para-cima.png) em vez do sprite padrao.
        private void ApplyUpgradeButtonSprite()
        {
            if (_upgradeButton == null || _upgradeButtonSprite == null) return;
            var img = _upgradeButton.GetComponent<Image>();
            if (img == null) return;

            img.sprite = _upgradeButtonSprite;
            img.type   = Image.Type.Simple;
            img.preserveAspect = true;
            img.color  = Color.white;     // cor branca pra mostrar a sprite original

            // Esconde texto interno (se existir) — o sprite ja eh o visual
            var tmp = _upgradeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.gameObject.SetActive(false);
        }

        // Configura o TextMeshPro do dinheiro pra encolher automaticamente quando o
        // numero crescer (ex: $999999 cabe na mesma pilula que $100).
        private void ConfigureMoneyAutoSize()
        {
            if (_moneyText == null) return;
            _moneyText.enableAutoSizing  = true;
            _moneyText.fontSizeMin       = _moneyFontMin;
            _moneyText.fontSizeMax       = _moneyFontMax;
            _moneyText.enableWordWrapping = false;   // dinheiro fica em 1 linha
            _moneyText.overflowMode      = TextOverflowModes.Overflow;
        }

        // Toggle de mute/unmute do som — usa PlayerPrefs "SoundEnabled" (0/1).
        // Atualiza visual do botao (cor + texto SOM/MUDO) e o volume do AudioManager.
        private void OnSoundClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            bool currentlyMuted = PlayerPrefs.GetInt("SoundEnabled", 1) == 0;
            bool newState = currentlyMuted; // liga se estava mudo, desliga se estava ligado
            PlayerPrefs.SetInt("SoundEnabled", newState ? 1 : 0);
            AudioManager.Instance?.SetMasterVolume(newState ? 1f : 0f);

            // Atualiza visual do botão
            if (_soundButton != null)
            {
                var img = _soundButton.GetComponent<Image>();
                var tmp = _soundButton.GetComponentInChildren<TextMeshProUGUI>();
                if (img != null)
                    img.color = newState
                        ? new Color(0.22f, 0.20f, 0.18f, 0.90f)
                        : new Color(0.70f, 0.12f, 0.08f, 0.90f);
                if (tmp != null)
                    tmp.text = newState ? "SOM" : "MUDO";
            }
        }
    }
}