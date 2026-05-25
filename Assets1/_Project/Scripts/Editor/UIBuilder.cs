using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using PizzaTycoon.UI;
using PizzaTycoon.Input;

namespace PizzaTycoon.Editor
{
    // Monta a hierarquia completa de UI da cena GameScene
    // Menu: PizzaTycoon > 5. Setup UI
    public static class UIBuilder
    {
        [MenuItem("PizzaTycoon/5. Setup UI")]
        public static void BuildUI()
        {
            // ── PASSO 1: Desseleciona tudo ANTES de qualquer DestroyImmediate ──
            // Isso evita NullReferenceException em GameObjectInspector.OnDisable
            InspectorCleaner.PrepareForDestroy();

            // Verifica se já existe um Canvas HUD na cena
            Canvas existing = FindCanvas("HUDCanvas");
            if (existing != null)
            {
                bool rebuild = EditorUtility.DisplayDialog(
                    "Pizza Tycoon - UI",
                    "Canvas HUDCanvas ja existe na cena. Recriar?",
                    "Sim, Recriar", "Cancelar");
                if (!rebuild) return;

                // Garante deselect ANTES do DestroyImmediate
                InspectorCleaner.PrepareForDestroy();
                Object.DestroyImmediate(existing.gameObject);
            }

            Canvas existingUpgrade = FindCanvas("UpgradePanelCanvas");
            if (existingUpgrade != null)
            {
                InspectorCleaner.PrepareForDestroy();
                Object.DestroyImmediate(existingUpgrade.gameObject);
            }

            // ── PASSO 2: Constrói a UI ──
            GameObject hudCanvas    = BuildHUDCanvas();
            GameObject upgradePanel = BuildUpgradePanel();

            // null check após criação
            if (hudCanvas == null || upgradePanel == null)
            {
                Debug.LogError("[UIBuilder] Falha ao criar Canvas. Verifique o Console.");
                return;
            }

            // ── PASSO 3: Salva e marca a cena como suja ──
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog(
                "Pizza Tycoon - UI",
                "[OK] Canvas HUD e Upgrade Panel criados!\n\n" +
                "- Arraste Main Camera para o campo 'Camera' do Canvas\n" +
                "- Conecte o JoystickController no PlayerController\n" +
                "- Configure o EventSystem (criado automaticamente)\n\n" +
                "Execute 'PizzaTycoon > Z. Clean Inspector Errors' se " +
                "aparecerem erros no Inspector.",
                "OK");
        }

        // ══════════════════════════════════════════════════════════════════════
        // HUD CANVAS
        // ══════════════════════════════════════════════════════════════════════
        private static GameObject BuildHUDCanvas()
        {
            // ── Canvas root ──
            GameObject canvasGO = new GameObject("HUDCanvas");
            if (canvasGO == null) return null;

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // ── EventSystem (único na cena) ──
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // ── TopBar ──
            GameObject topBar = BuildTopBar(canvasGO.transform);
            if (topBar == null) { Object.DestroyImmediate(canvasGO); return null; }

            // ── JoystickPanel ──
            GameObject joystickPanel = BuildJoystickPanel(canvasGO.transform);

            // ── UpgradeButton ──
            GameObject upgradeBtn = BuildUpgradeButton(canvasGO.transform);
            if (upgradeBtn == null) { Object.DestroyImmediate(canvasGO); return null; }

            // ── FloatingTextPool (container vazio) ──
            GameObject ftPool = new GameObject("FloatingTextPool");
            ftPool.transform.SetParent(canvasGO.transform, false);

            // ── PausePanel ──
            GameObject pausePanel = BuildPausePanel(canvasGO.transform);

            // ── HUDController no Canvas root ──
            HUDController hud = canvasGO.AddComponent<HUDController>();
            if (hud == null)
            {
                Debug.LogError("[UIBuilder] Falha ao adicionar HUDController.");
                return canvasGO;
            }

            SerializedObject soHud = new SerializedObject(hud);

            // Money text
            TextMeshProUGUI moneyTxt =
                topBar.transform.Find("MoneyPanel/MoneyText")?.GetComponent<TextMeshProUGUI>();
            if (moneyTxt != null)
                soHud.FindProperty("_moneyText").objectReferenceValue = moneyTxt;
            else
                Debug.LogWarning("[UIBuilder] MoneyText não encontrado na hierarquia.");

            // PauseButton
            Button pauseBtn =
                topBar.transform.Find("PauseButton")?.GetComponent<Button>();
            if (pauseBtn != null)
                soHud.FindProperty("_pauseButton").objectReferenceValue = pauseBtn;

            // UpgradeButton
            Button upgradeBtnComp = upgradeBtn.GetComponent<Button>();
            if (upgradeBtnComp != null)
                soHud.FindProperty("_upgradeButton").objectReferenceValue = upgradeBtnComp;

            // PausePanel
            if (pausePanel != null)
                soHud.FindProperty("_pausePanel").objectReferenceValue = pausePanel;

            // Resume / MainMenu buttons
            if (pausePanel != null)
            {
                Button resumeBtn  = pausePanel.transform.Find("PauseBox/ResumeButton")?.GetComponent<Button>();
                Button menuBtn    = pausePanel.transform.Find("PauseBox/MainMenuButton")?.GetComponent<Button>();
                if (resumeBtn != null) soHud.FindProperty("_resumeButton").objectReferenceValue    = resumeBtn;
                if (menuBtn   != null) soHud.FindProperty("_mainMenuButton").objectReferenceValue  = menuBtn;
            }

            soHud.ApplyModifiedProperties();

            return canvasGO;
        }

        private static GameObject BuildTopBar(Transform parent)
        {
            GameObject topBar = new GameObject("TopBar");
            topBar.transform.SetParent(parent, false);
            topBar.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.4f);

            RectTransform rt = topBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -120f);
            rt.offsetMax = Vector2.zero;

            // ── Logo ──
            GameObject logo = new GameObject("Logo");
            logo.transform.SetParent(topBar.transform, false);
            Image logoImg = logo.AddComponent<Image>();
            logoImg.color = new Color(0.75f, 0.22f, 0.17f);
            RectTransform logoRT = logo.GetComponent<RectTransform>();
            logoRT.anchorMin        = new Vector2(0f, 0.5f);
            logoRT.anchorMax        = new Vector2(0f, 0.5f);
            logoRT.pivot            = new Vector2(0f, 0.5f);
            logoRT.anchoredPosition = new Vector2(20f, 0f);
            logoRT.sizeDelta        = new Vector2(160f, 70f);

            GameObject logoText = new GameObject("LogoText");
            logoText.transform.SetParent(logo.transform, false);
            TextMeshProUGUI lt = logoText.AddComponent<TextMeshProUGUI>();
            lt.text      = "PIZZA";
            lt.fontSize  = 22f;
            lt.fontStyle = FontStyles.Bold;
            lt.color     = Color.white;
            lt.alignment = TextAlignmentOptions.Center;
            StretchRect(lt.rectTransform);

            // ── MoneyPanel ──
            GameObject moneyPanel = new GameObject("MoneyPanel");
            moneyPanel.transform.SetParent(topBar.transform, false);
            HorizontalLayoutGroup hlg = moneyPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing                = 8f;
            hlg.childAlignment         = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;

            RectTransform mpRT  = moneyPanel.GetComponent<RectTransform>();
            mpRT.anchorMin      = new Vector2(0.5f, 0.5f);
            mpRT.anchorMax      = new Vector2(0.5f, 0.5f);
            mpRT.pivot          = new Vector2(0.5f, 0.5f);
            mpRT.anchoredPosition = Vector2.zero;
            mpRT.sizeDelta      = new Vector2(220f, 70f);

            // Ícone de moeda
            GameObject moneyIcon = new GameObject("MoneyIcon");
            moneyIcon.transform.SetParent(moneyPanel.transform, false);
            Image icon = moneyIcon.AddComponent<Image>();
            icon.color = new Color(0.15f, 0.68f, 0.38f);
            moneyIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(40f, 40f);

            // Texto de dinheiro
            GameObject moneyTxtGO = new GameObject("MoneyText");
            moneyTxtGO.transform.SetParent(moneyPanel.transform, false);
            TextMeshProUGUI moneyTxt = moneyTxtGO.AddComponent<TextMeshProUGUI>();
            moneyTxt.text      = "$ 0";
            moneyTxt.fontSize  = 28f;
            moneyTxt.fontStyle = FontStyles.Bold;
            moneyTxt.color     = Color.white;
            moneyTxt.alignment = TextAlignmentOptions.Left;
            moneyTxt.rectTransform.sizeDelta = new Vector2(160f, 50f);

            // ── PauseButton ──
            GameObject pauseBtnGO = CreateButton("PauseButton", topBar.transform, "...", 32f);
            RectTransform pbRT    = pauseBtnGO.GetComponent<RectTransform>();
            pbRT.anchorMin        = new Vector2(1f, 0.5f);
            pbRT.anchorMax        = new Vector2(1f, 0.5f);
            pbRT.pivot            = new Vector2(1f, 0.5f);
            pbRT.anchoredPosition = new Vector2(-20f, 0f);
            pbRT.sizeDelta        = new Vector2(80f, 80f);

            return topBar;
        }

        private static GameObject BuildJoystickPanel(Transform parent)
        {
            GameObject panel = new GameObject("JoystickPanel");
            panel.transform.SetParent(parent, false);
            panel.AddComponent<Image>().color = Color.clear;

            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin      = rt.pivot = Vector2.zero;
            rt.anchorMax      = Vector2.zero;
            rt.anchoredPosition = new Vector2(50f, 50f);
            rt.sizeDelta      = new Vector2(200f, 200f);

            // Background circular do joystick
            GameObject jsBG    = new GameObject("JoystickBackground");
            jsBG.transform.SetParent(panel.transform, false);
            jsBG.AddComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f, 0.4f);
            RectTransform jsBGRT = jsBG.GetComponent<RectTransform>();
            jsBGRT.anchorMin = jsBGRT.anchorMax = new Vector2(0.5f, 0.5f);
            jsBGRT.pivot     = new Vector2(0.5f, 0.5f);
            jsBGRT.sizeDelta = new Vector2(150f, 150f);

            // Handle do joystick
            GameObject jsHandle    = new GameObject("JoystickHandle");
            jsHandle.transform.SetParent(jsBG.transform, false);
            jsHandle.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.85f);
            RectTransform jsHandleRT = jsHandle.GetComponent<RectTransform>();
            jsHandleRT.anchorMin = jsHandleRT.anchorMax = new Vector2(0.5f, 0.5f);
            jsHandleRT.pivot     = new Vector2(0.5f, 0.5f);
            jsHandleRT.sizeDelta = new Vector2(60f, 60f);

            // JoystickController
            JoystickController jsCtrl = panel.AddComponent<JoystickController>();
            if (jsCtrl != null)
            {
                SerializedObject so = new SerializedObject(jsCtrl);
                so.FindProperty("_background").objectReferenceValue       = jsBGRT;
                so.FindProperty("_handle").objectReferenceValue           = jsHandleRT;
                so.FindProperty("_handleRange").floatValue                = 1f;
                so.FindProperty("_deadZone").floatValue                   = 0.15f;
                so.FindProperty("_dynamicPositioning").boolValue          = true;
                so.ApplyModifiedProperties();
            }

            return panel;
        }

        private static GameObject BuildUpgradeButton(Transform parent)
        {
            GameObject btn    = CreateButton("UpgradeButton", parent, "UPGRADE", 22f);
            RectTransform rt  = btn.GetComponent<RectTransform>();
            rt.anchorMin      = new Vector2(1f, 0f);
            rt.anchorMax      = new Vector2(1f, 0f);
            rt.pivot          = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-30f, 30f);
            rt.sizeDelta      = new Vector2(200f, 80f);
            btn.GetComponent<Image>().color = new Color(0.15f, 0.68f, 0.38f);
            return btn;
        }

        private static GameObject BuildPausePanel(Transform parent)
        {
            GameObject panel = new GameObject("PausePanel");
            panel.transform.SetParent(parent, false);
            panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
            StretchRect(panel.GetComponent<RectTransform>());
            panel.SetActive(false);

            // Box central
            GameObject box    = new GameObject("PauseBox");
            box.transform.SetParent(panel.transform, false);
            box.AddComponent<Image>().color = new Color(0.96f, 0.96f, 0.96f);
            RectTransform boxRT = box.GetComponent<RectTransform>();
            boxRT.anchorMin     = new Vector2(0.5f, 0.5f);
            boxRT.anchorMax     = new Vector2(0.5f, 0.5f);
            boxRT.pivot         = new Vector2(0.5f, 0.5f);
            boxRT.anchoredPosition = Vector2.zero;
            boxRT.sizeDelta     = new Vector2(500f, 400f);

            // Titulo
            AddLabel("PauseTitle", box.transform, "PAUSADO", 36f, new Vector2(0f, 100f));

            // Botoes — criados como filhos diretos de PauseBox para que os paths Find() funcionem
            GameObject resumeBtn = CreateButton("ResumeButton", box.transform, "Continuar", 26f);
            SetButtonAnchor(resumeBtn, new Vector2(0.5f, 0.5f), new Vector2(360f, 90f), new Vector2(0f, 0f));
            resumeBtn.GetComponent<Image>().color = new Color(0.15f, 0.68f, 0.38f);

            GameObject menuBtn = CreateButton("MainMenuButton", box.transform, "Menu Principal", 22f);
            SetButtonAnchor(menuBtn, new Vector2(0.5f, 0.5f), new Vector2(360f, 90f), new Vector2(0f, -110f));
            menuBtn.GetComponent<Image>().color = new Color(0.60f, 0.60f, 0.60f);

            return panel;
        }

        // ══════════════════════════════════════════════════════════════════════
        // UPGRADE PANEL
        // ══════════════════════════════════════════════════════════════════════
        private static GameObject BuildUpgradePanel()
        {
            // Canvas separado com maior sorting order
            GameObject canvasGO = new GameObject("UpgradePanelCanvas");
            Canvas canvas       = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler cs         = canvasGO.AddComponent<CanvasScaler>();
            cs.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution  = new Vector2(1080f, 1920f);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Fundo semi-transparente (inicia desativado)
            GameObject bgPanel = new GameObject("UpgradePanelBG");
            bgPanel.transform.SetParent(canvasGO.transform, false);
            bgPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            StretchRect(bgPanel.GetComponent<RectTransform>());
            bgPanel.SetActive(false);

            // Caixa branca central
            GameObject box    = new GameObject("PanelBG");
            box.transform.SetParent(bgPanel.transform, false);
            box.AddComponent<Image>().color = Color.white;
            RectTransform boxRT = box.GetComponent<RectTransform>();
            boxRT.anchorMin = new Vector2(0.1f, 0.1f);
            boxRT.anchorMax = new Vector2(0.9f, 0.9f);
            boxRT.offsetMin = boxRT.offsetMax = Vector2.zero;

            // Titulo
            AddLabel("TitleText", box.transform, "MELHORIAS", 40f, Vector2.zero,
                anchorMin: new Vector2(0f, 0.88f), anchorMax: Vector2.one);

            // Botao fechar
            GameObject closeBtn       = CreateButton("CloseButton", box.transform, "X", 30f);
            RectTransform closeBtnRT  = closeBtn.GetComponent<RectTransform>();
            closeBtnRT.anchorMin      = new Vector2(1f, 1f);
            closeBtnRT.anchorMax      = new Vector2(1f, 1f);
            closeBtnRT.pivot          = new Vector2(1f, 1f);
            closeBtnRT.anchoredPosition = new Vector2(-10f, -10f);
            closeBtnRT.sizeDelta      = new Vector2(70f, 70f);
            closeBtn.GetComponent<Image>().color = new Color(0.9f, 0.3f, 0.25f);

            // ScrollView
            GameObject scrollGO    = new GameObject("ScrollView");
            scrollGO.transform.SetParent(box.transform, false);
            scrollGO.AddComponent<Image>().color = Color.clear;
            ScrollRect scroll      = scrollGO.AddComponent<ScrollRect>();
            RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
            scrollRT.anchorMin     = new Vector2(0f, 0f);
            scrollRT.anchorMax     = new Vector2(1f, 0.87f);
            scrollRT.offsetMin     = new Vector2(20f, 20f);
            scrollRT.offsetMax     = new Vector2(-20f, 0f);

            // Viewport
            GameObject viewport    = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGO.transform, false);
            viewport.AddComponent<Image>().color = Color.clear;
            Mask mask              = viewport.AddComponent<Mask>();
            mask.showMaskGraphic   = false;
            StretchRect(viewport.GetComponent<RectTransform>());

            // Content
            GameObject content     = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing            = 15f;
            vlg.padding            = new RectOffset(10, 10, 10, 10);
            vlg.childControlWidth  = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            ContentSizeFitter csf  = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit        = ContentSizeFitter.FitMode.PreferredSize;
            RectTransform contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin    = new Vector2(0f, 1f);
            contentRT.anchorMax    = new Vector2(1f, 1f);
            contentRT.pivot        = new Vector2(0.5f, 1f);
            contentRT.offsetMin    = contentRT.offsetMax = Vector2.zero;

            scroll.content   = contentRT;
            scroll.viewport  = viewport.GetComponent<RectTransform>();
            scroll.horizontal = false;
            scroll.vertical   = true;

            // 5 items de upgrade de exemplo
            for (int i = 0; i < 5; i++)
                BuildUpgradeItemUI(content.transform, i);

            // UpgradePanelUI script — null check obrigatório
            UpgradePanelUI panelUI = bgPanel.AddComponent<UpgradePanelUI>();
            if (panelUI != null)
            {
                SerializedObject so = new SerializedObject(panelUI);

                SerializedProperty panelProp  = so.FindProperty("_panel");
                SerializedProperty parentProp = so.FindProperty("_upgradeListParent");
                SerializedProperty closeProp  = so.FindProperty("_closeButton");

                if (panelProp  != null) panelProp.objectReferenceValue  = bgPanel;
                if (parentProp != null) parentProp.objectReferenceValue  = content.transform;
                if (closeProp  != null) closeProp.objectReferenceValue   = closeBtn.GetComponent<Button>();

                so.ApplyModifiedProperties();
            }

            return canvasGO;
        }

        private static void BuildUpgradeItemUI(Transform parent, int index)
        {
            string[] names = { "Velocidade", "Capacidade", "Colheita", "Forno", "Funcionario" };
            Color[]  colors = {
                new Color(0.15f, 0.68f, 0.38f),
                new Color(0.20f, 0.60f, 0.86f),
                new Color(0.95f, 0.77f, 0.06f),
                new Color(0.90f, 0.49f, 0.13f),
                new Color(0.56f, 0.27f, 0.68f)
            };

            string itemName  = index < names.Length  ? names[index]  : $"Upgrade {index}";
            Color  itemColor = index < colors.Length ? colors[index] : Color.gray;

            // Item row
            GameObject item    = new GameObject($"UpgradeItem_{itemName}");
            item.transform.SetParent(parent, false);
            item.AddComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f);
            item.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 120f);

            // Ícone colorido
            GameObject iconBG  = new GameObject("IconBG");
            iconBG.transform.SetParent(item.transform, false);
            iconBG.AddComponent<Image>().color = itemColor;
            RectTransform iconRT = iconBG.GetComponent<RectTransform>();
            iconRT.anchorMin     = new Vector2(0f, 0f);
            iconRT.anchorMax     = new Vector2(0f, 1f);
            iconRT.pivot         = new Vector2(0f, 0.5f);
            iconRT.offsetMin     = new Vector2(10f, 10f);
            iconRT.offsetMax     = new Vector2(110f, -10f);

            // Nome
            AddLabel("NameText", item.transform, itemName, 22f, Vector2.zero,
                anchorMin: new Vector2(0.22f, 0.55f), anchorMax: new Vector2(0.75f, 1f),
                color: Color.black, bold: true);

            // Nível
            AddLabel("LevelText", item.transform, "Nivel 0/10", 16f, Vector2.zero,
                anchorMin: new Vector2(0.22f, 0f), anchorMax: new Vector2(0.75f, 0.55f),
                color: new Color(0.4f, 0.4f, 0.4f));

            // Custo
            AddLabel("CostText", item.transform, "$50", 20f, Vector2.zero,
                anchorMin: new Vector2(0.75f, 0.5f), anchorMax: Vector2.one,
                color: new Color(0.15f, 0.68f, 0.38f), bold: true);

            // Botão comprar
            GameObject buyBtn     = CreateButton("BuyButton", item.transform, "COMPRAR", 18f);
            RectTransform buyRT   = buyBtn.GetComponent<RectTransform>();
            buyRT.anchorMin       = new Vector2(0.75f, 0f);
            buyRT.anchorMax       = new Vector2(1f, 0.5f);
            buyRT.offsetMin       = new Vector2(5f, 5f);
            buyRT.offsetMax       = new Vector2(-5f, -5f);
            buyBtn.GetComponent<Image>().color = itemColor;

            // Script UpgradeItemUI — null check
            UpgradeItemUI uiComp = item.AddComponent<UpgradeItemUI>();
            if (uiComp == null)
                Debug.LogWarning($"[UIBuilder] Falha ao adicionar UpgradeItemUI em {itemName}");
        }

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS UI
        // ══════════════════════════════════════════════════════════════════════

        private static GameObject CreateButton(string name, Transform parent,
            string label, float fontSize)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0.15f, 0.68f, 0.38f);

            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.2f, 0.8f, 0.45f);
            cb.pressedColor     = new Color(0.1f, 0.55f, 0.30f);
            btn.colors = cb;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            TextMeshProUGUI txt = textGO.AddComponent<TextMeshProUGUI>();
            txt.text      = label;
            txt.fontSize  = fontSize;
            txt.fontStyle = FontStyles.Bold;
            txt.color     = Color.white;
            txt.alignment = TextAlignmentOptions.Center;
            StretchRect(txt.rectTransform);

            return go;
        }

        private static void AddLabel(string name, Transform parent, string text,
            float fontSize, Vector2 anchoredPos,
            Vector2 anchorMin = default, Vector2 anchorMax = default,
            Color color = default, bool bold = false)
        {
            if (color == default) color = Color.black;
            if (anchorMax == default) anchorMax = Vector2.one;

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;

            RectTransform rt = tmp.rectTransform;
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.offsetMin        = rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = anchoredPos;
        }

        private static void StretchRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void SetButtonAnchor(GameObject btn, Vector2 anchor,
            Vector2 size, Vector2 pos)
        {
            RectTransform rt = btn.GetComponent<RectTransform>();
            rt.anchorMin        = rt.anchorMax = anchor;
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = size;
            rt.anchoredPosition = pos;
        }

        private static Canvas FindCanvas(string name)
        {
            foreach (Canvas c in Object.FindObjectsOfType<Canvas>())
                if (c.name == name) return c;
            return null;
        }
    }
}
