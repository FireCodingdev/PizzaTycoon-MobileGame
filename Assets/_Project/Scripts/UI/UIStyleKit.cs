using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PizzaTycoon.UI
{
    // Gera sprites e aplica o visual "Pizza Ready" em elementos UI em runtime.
    // Nenhum asset externo necessário — tudo gerado por código.
    public static class UIStyleKit
    {
        // ── Paleta Pizza Ready ────────────────────────────────────────────────
        public static readonly Color BgDark       = new Color(0.08f, 0.05f, 0.02f, 0.80f);
        public static readonly Color PanelCream   = new Color(0.99f, 0.96f, 0.88f, 1.00f);
        public static readonly Color Green        = new Color(0.20f, 0.75f, 0.18f, 1.00f);
        public static readonly Color GreenDark    = new Color(0.10f, 0.48f, 0.10f, 1.00f);
        public static readonly Color Red          = new Color(0.90f, 0.20f, 0.14f, 1.00f);
        public static readonly Color RedDark      = new Color(0.60f, 0.08f, 0.06f, 1.00f);
        public static readonly Color Orange       = new Color(1.00f, 0.55f, 0.05f, 1.00f);
        public static readonly Color OrangeDark   = new Color(0.75f, 0.32f, 0.02f, 1.00f);
        public static readonly Color Yellow       = new Color(1.00f, 0.85f, 0.08f, 1.00f);
        public static readonly Color TextDark     = new Color(0.10f, 0.06f, 0.01f, 1.00f);
        public static readonly Color White        = Color.white;

        // ── Geração de sprite arredondado (9-slice) ───────────────────────────
        public static Sprite RoundedRect(int radius, Color color)
        {
            int size = Mathf.Max(radius * 2 + 2, 8);
            var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode   = TextureWrapMode.Clamp;

            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = Inside(x, y, size, size, radius) ? color : Color.clear;

            tex.SetPixels(px);
            tex.Apply();

            float b = radius;
            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 1f, 0,
                SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        static bool Inside(int px, int py, int w, int h, int r)
        {
            if (px >= r && px < w - r) return true;
            if (py >= r && py < h - r) return true;
            int cx = px < r ? r : w - r - 1;
            int cy = py < r ? r : h - r - 1;
            float dx = px - cx, dy = py - cy;
            return dx * dx + dy * dy <= (r + 0.5f) * (r + 0.5f);
        }

        // ── Texto ─────────────────────────────────────────────────────────────
        public static void StyleMoney(TextMeshProUGUI t)
        {
            t.fontSize   = 38;
            t.fontStyle  = FontStyles.Bold;
            t.color      = White;
            t.alignment  = TextAlignmentOptions.Center;
            Outline(t, TextDark, 0.28f);
        }

        public static void StyleLabel(TextMeshProUGUI t, Color color, float size = 20,
            bool bold = true, bool outline = false)
        {
            t.fontSize  = size;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.color     = color;
            t.alignment = TextAlignmentOptions.Center;
            if (outline) Outline(t, TextDark, 0.18f);
        }

        static void Outline(TextMeshProUGUI t, Color color, float width)
        {
            // Setters já criam material instance e ativam keyword OUTLINE_ON
            t.outlineColor = color;
            t.outlineWidth = width;
        }

        // ── Imagem arredondada ────────────────────────────────────────────────
        public static void ApplyRounded(Image img, Color fill, int radius = 24)
        {
            img.sprite = RoundedRect(radius, Color.white);
            img.type   = Image.Type.Sliced;
            img.color  = fill;
        }

        // ── Botão estilizado com sombra 3D (estilo Pizza Ready) ──────────────
        public static void StyleButton(Button btn, Color fill, Color shadow,
            int radius = 26, float shadowOffset = 6f)
        {
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = RoundedRect(radius, Color.white);
                img.type   = Image.Type.Sliced;
                img.color  = fill;
            }

            // Sombra — imagem atrás deslocada para baixo
            if (btn.transform.Find("_Sh") == null)
            {
                var sh = new GameObject("_Sh");
                sh.transform.SetParent(btn.transform, false);
                sh.transform.SetAsFirstSibling();
                var si = sh.AddComponent<Image>();
                si.sprite = RoundedRect(radius, Color.white);
                si.type   = Image.Type.Sliced;
                si.color  = shadow;
                var rt = sh.GetComponent<RectTransform>();
                rt.anchorMin    = Vector2.zero;
                rt.anchorMax    = Vector2.one;
                rt.offsetMin    = new Vector2(0f, -shadowOffset);
                rt.offsetMax    = new Vector2(0f, -shadowOffset * 0.35f);
            }

            // Texto
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.fontStyle = FontStyles.Bold;
                tmp.color     = White;
                Outline(tmp, shadow, 0.15f);
            }
        }

        // ── Painel (cartão) ───────────────────────────────────────────────────
        public static void StyleCard(Image img, int radius = 32)
        {
            img.sprite = RoundedRect(radius, Color.white);
            img.type   = Image.Type.Sliced;
            img.color  = PanelCream;
        }

        // ── Cria um GameObject de texto TMP filho ─────────────────────────────
        public static TextMeshProUGUI MakeText(Transform parent, string text,
            float fontSize, Color color, Vector2 pos, Vector2 size)
        {
            var go  = new GameObject("Txt_" + text.Replace(" ", ""));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = color;
            tmp.alignment = TextAlignmentOptions.Center;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin       = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta       = size;
            rt.anchoredPosition= pos;
            return tmp;
        }

        // ── Cria um botão completo com sombra e texto ─────────────────────────
        public static Button MakeButton(Transform parent, string label,
            Color fill, Color shadow, Vector2 pos, Vector2 size, int radius = 26)
        {
            var go  = new GameObject("Btn_" + label.Replace(" ", ""));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRect(radius, Color.white);
            img.type   = Image.Type.Sliced;
            img.color  = fill;
            var btn = go.AddComponent<Button>();
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin       = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta       = size;
            rt.anchoredPosition= pos;

            // sombra
            var sh = new GameObject("_Sh");
            sh.transform.SetParent(go.transform, false);
            sh.transform.SetAsFirstSibling();
            var si = sh.AddComponent<Image>();
            si.sprite = RoundedRect(radius, Color.white);
            si.type   = Image.Type.Sliced;
            si.color  = shadow;
            var srt = sh.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(0f, -7f);
            srt.offsetMax = new Vector2(0f, -3f);

            // texto
            var tgo = new GameObject("Lbl");
            tgo.transform.SetParent(go.transform, false);
            var tmp = tgo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = Mathf.Clamp(size.y * 0.38f, 16f, 32f);
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = White;
            tmp.alignment = TextAlignmentOptions.Center;
            Outline(tmp, shadow, 0.14f);
            var trt = tgo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            return btn;
        }

        // ── Toggle Row (label + on/off pill) ──────────────────────────────────
        //
        // Cria uma linha com label a esquerda e toggle on/off a direita.
        // Estado lido/salvo via PlayerPrefs (int 0/1).
        public static void MakeToggleRow(
            Transform parent, string label,
            float anchorYMin, float anchorYMax,
            string prefKey, bool defaultOn,
            System.Action<bool> onChange)
        {
            // Container da linha — ocupa a faixa vertical [anchorYMin, anchorYMax]
            // com margem horizontal de 5% em cada lado.
            var row = new GameObject("ToggleRow_" + label);
            row.transform.SetParent(parent, false);
            var rowRT = row.AddComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0.05f, anchorYMin);
            rowRT.anchorMax = new Vector2(0.95f, anchorYMax);
            rowRT.offsetMin = rowRT.offsetMax = Vector2.zero;

            // Label a esquerda — TMP 15px bold cor TextDark
            var lblGO = new GameObject("Lbl");
            lblGO.transform.SetParent(row.transform, false);
            var lblTmp = lblGO.AddComponent<TextMeshProUGUI>();
            lblTmp.text      = label;
            lblTmp.fontSize  = 15;
            lblTmp.fontStyle = FontStyles.Bold;
            lblTmp.color     = TextDark;
            lblTmp.alignment = TextAlignmentOptions.Left;
            var lblRT = lblGO.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0f, 0f);
            lblRT.anchorMax = new Vector2(0.55f, 1f);
            lblRT.offsetMin = new Vector2(8f, 0f);
            lblRT.offsetMax = Vector2.zero;

            // Estado inicial
            bool isOn = PlayerPrefs.GetInt(prefKey, defaultOn ? 1 : 0) == 1;

            // Toggle a direita — Button arredondado (radius 14)
            var btnGO = new GameObject("Toggle");
            btnGO.transform.SetParent(row.transform, false);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.sprite = RoundedRect(14, Color.white);
            btnImg.type   = Image.Type.Sliced;
            var btn = btnGO.AddComponent<Button>();
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.65f, 0.15f);
            btnRT.anchorMax = new Vector2(1.00f, 0.85f);
            btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;

            // Texto do toggle
            var btnTxtGO = new GameObject("Lbl");
            btnTxtGO.transform.SetParent(btnGO.transform, false);
            var btnTmp = btnTxtGO.AddComponent<TextMeshProUGUI>();
            btnTmp.fontSize  = 12;
            btnTmp.fontStyle = FontStyles.Bold;
            btnTmp.color     = White;
            btnTmp.alignment = TextAlignmentOptions.Center;
            var btnTxtRT = btnTxtGO.GetComponent<RectTransform>();
            btnTxtRT.anchorMin = Vector2.zero;
            btnTxtRT.anchorMax = Vector2.one;
            btnTxtRT.offsetMin = btnTxtRT.offsetMax = Vector2.zero;

            // Aplica visual inicial baseado no estado
            Color offColor = new Color(0.55f, 0.52f, 0.48f);
            void ApplyVisual(bool on)
            {
                btnImg.color = on ? Green : offColor;
                btnTmp.text  = on ? "SOBRE" : "OFF";
            }
            ApplyVisual(isOn);

            // Click — inverte estado, salva, callback, atualiza visual
            btn.onClick.AddListener(() =>
            {
                isOn = !isOn;
                PlayerPrefs.SetInt(prefKey, isOn ? 1 : 0);
                PlayerPrefs.Save();
                onChange?.Invoke(isOn);
                ApplyVisual(isOn);
            });
        }

        // ── Social Row (rede social + botão SEGUIR) ───────────────────────────
        //
        // Linha com fundo branco, nome da rede social a esquerda e botao roxo
        // "+200 SEGUIR" a direita. Click abre URL.
        public static void MakeSocialRow(
            Transform parent, string networkName,
            string rewardText, string url, float yPos)
        {
            // Container — fundo branco arredondado (radius 10), 90% largura, 44px altura
            var row = new GameObject("SocialRow_" + networkName);
            row.transform.SetParent(parent, false);
            var bg = row.AddComponent<Image>();
            ApplyRounded(bg, White, 10);
            var rowRT = row.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0.5f, 0.5f);
            rowRT.anchorMax = new Vector2(0.5f, 0.5f);
            rowRT.sizeDelta = new Vector2(0f, 44f);
            rowRT.anchoredPosition = new Vector2(0f, yPos);

            // Definir width relativo ao pai via stretch horizontal
            // (Usamos anchor central + sizeDelta — calculamos via SetWidthAnchor)
            // Simples: stretch horizontalmente respeitando 90% do pai usando offset.
            rowRT.anchorMin = new Vector2(0.05f, 0.5f);
            rowRT.anchorMax = new Vector2(0.95f, 0.5f);
            rowRT.pivot     = new Vector2(0.5f, 0.5f);
            rowRT.sizeDelta = new Vector2(0f, 44f);
            rowRT.anchoredPosition = new Vector2(0f, yPos);

            // Nome da rede a esquerda — TMP 13px bold TextDark, padding 12px
            var lblGO = new GameObject("Lbl");
            lblGO.transform.SetParent(row.transform, false);
            var lblTmp = lblGO.AddComponent<TextMeshProUGUI>();
            lblTmp.text      = networkName;
            lblTmp.fontSize  = 13;
            lblTmp.fontStyle = FontStyles.Bold;
            lblTmp.color     = TextDark;
            lblTmp.alignment = TextAlignmentOptions.Left;
            var lblRT = lblGO.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0f, 0f);
            lblRT.anchorMax = new Vector2(0.55f, 1f);
            lblRT.offsetMin = new Vector2(12f, 0f);
            lblRT.offsetMax = Vector2.zero;

            // Botao SEGUIR a direita — roxo com sombra, radius 8
            Color purple     = new Color(0.78f, 0.22f, 0.72f);
            Color purpleDark = new Color(0.45f, 0.08f, 0.42f);

            var btnGO = new GameObject("Btn_Follow");
            btnGO.transform.SetParent(row.transform, false);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.sprite = RoundedRect(8, Color.white);
            btnImg.type   = Image.Type.Sliced;
            btnImg.color  = purple;
            var btn = btnGO.AddComponent<Button>();
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.62f, 0.15f);
            btnRT.anchorMax = new Vector2(0.98f, 0.85f);
            btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;

            // Sombra do botao
            var sh = new GameObject("_Sh");
            sh.transform.SetParent(btnGO.transform, false);
            sh.transform.SetAsFirstSibling();
            var sImg = sh.AddComponent<Image>();
            sImg.sprite = RoundedRect(8, Color.white);
            sImg.type   = Image.Type.Sliced;
            sImg.color  = purpleDark;
            var sRT = sh.GetComponent<RectTransform>();
            sRT.anchorMin = Vector2.zero;
            sRT.anchorMax = Vector2.one;
            sRT.offsetMin = new Vector2(0f, -4f);
            sRT.offsetMax = new Vector2(0f, -2f);

            // Texto do botao
            var btnTxtGO = new GameObject("Lbl");
            btnTxtGO.transform.SetParent(btnGO.transform, false);
            var btnTmp = btnTxtGO.AddComponent<TextMeshProUGUI>();
            btnTmp.text      = rewardText;
            btnTmp.fontSize  = 11;
            btnTmp.fontStyle = FontStyles.Bold;
            btnTmp.color     = White;
            btnTmp.alignment = TextAlignmentOptions.Center;
            var btnTxtRT = btnTxtGO.GetComponent<RectTransform>();
            btnTxtRT.anchorMin = Vector2.zero;
            btnTxtRT.anchorMax = Vector2.one;
            btnTxtRT.offsetMin = btnTxtRT.offsetMax = Vector2.zero;

            // OnClick — abre URL + som
            btn.onClick.AddListener(() =>
            {
                Application.OpenURL(url);
                PizzaTycoon.Managers.AudioManager.Instance?.PlayButtonClick();
            });
        }

        // ── Link clicavel (Privacy, Terms) ────────────────────────────────────
        //
        // Texto sublinhado em marrom escuro, com Button transparente por cima.
        public static Button MakeLinkText(
            Transform parent, string label, string url,
            Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Link_" + label.Replace(" ", ""));
            go.transform.SetParent(parent, false);

            // Image transparente — necessaria para Button receber clicks (raycastTarget)
            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);   // 100% transparente
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            // TMP sublinhado por cima da Image
            var txtGO = new GameObject("Txt");
            txtGO.transform.SetParent(go.transform, false);
            var tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text       = label;
            tmp.fontSize   = 12;
            tmp.fontStyle  = FontStyles.Underline;
            tmp.color      = new Color(0.55f, 0.30f, 0.08f);
            tmp.alignment  = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;   // deixa o click passar pra Image
            var txtRT = txtGO.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;

            btn.onClick.AddListener(() =>
            {
                Application.OpenURL(url);
                PizzaTycoon.Managers.AudioManager.Instance?.PlayButtonClick();
            });

            return btn;
        }
    }
}
