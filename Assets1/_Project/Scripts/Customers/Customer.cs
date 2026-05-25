using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;
using PizzaTycoon.VFX;
using PizzaTycoon.GameSystems;

namespace PizzaTycoon.Customers
{
    // Representa um cliente individual — controla paciência, pagamento e saída
    public class Customer : MonoBehaviour
    {
        [Header("Configurações")]
        [Tooltip("Tempo em segundos para a paciencia chegar a zero. Cliente NAO vai embora — " +
                 "so afeta o tip/bonus e o emoji exibido.")]
        [SerializeField] private float _patience       = 60f;
        [SerializeField] private float _basePayment    = 10f;
        [SerializeField] private float _bonusPaymentFast = 5f;
        [Tooltip("Quantidade de pizzas que este cliente quer comprar (exibido no balao)")]
        [SerializeField] private int   _orderQuantity = 1;
        [Tooltip("Se true, sorteia uma quantidade entre min/max a cada spawn")]
        [SerializeField] private bool  _randomizeQuantity = true;
        [SerializeField] private int   _minQuantity = 1;
        [SerializeField] private int   _maxQuantity = 5;

        public int OrderQuantity => _orderQuantity;

        [Header("Bubble de Pedido (visual)")]
        [Tooltip("Altura acima da cabeca onde o balao flutua")]
        [SerializeField] private float _bubbleHeight   = 2.1f;
        [Tooltip("Escala geral do balao (1 = padrao)")]
        [SerializeField] private float _bubbleScale    = 1f;
        [SerializeField] private Color _colorFull      = new Color(0.30f, 0.85f, 0.30f);
        [SerializeField] private Color _colorEmpty     = new Color(0.90f, 0.20f, 0.20f);

        [Header("Emoji Sprites (PNGs) — opcional, substitui o ASCII :)")]
        [Tooltip("Sprite mostrado quando paciencia > 70% (ex: happy-face.png ou happiness.png)")]
        [SerializeField] private Sprite _emojiHappySprite;
        [Tooltip("Sprite mostrado quando paciencia 40-70% (ex: smiling-face.png ou confused.png)")]
        [SerializeField] private Sprite _emojiNeutralSprite;
        [Tooltip("Sprite mostrado quando paciencia 15-40% (ex: sad.png ou thinking.png)")]
        [SerializeField] private Sprite _emojiWorriedSprite;
        [Tooltip("Sprite mostrado quando paciencia < 15% (ex: angry.png)")]
        [SerializeField] private Sprite _emojiAngrySprite;
        [Tooltip("Escala do emoji sprite no mundo (default 1)")]
        [SerializeField] private float  _emojiSpriteScale = 1f;

        [Header("Animação")]
        [SerializeField] private Animator _animator; // opcional — sem AnimatorController é ignorado

        [Header("Movimento (caminhar até o slot)")]
        [Tooltip("Velocidade que o cliente anda do SpawnPoint até o slot")]
        [SerializeField] private float _moveSpeed     = 2.5f;
        [Tooltip("Velocidade de rotação enquanto caminha")]
        [SerializeField] private float _rotationSpeed = 8f;
        [Tooltip("Distância em metros para considerar que chegou no slot")]
        [SerializeField] private float _arrivalTolerance = 0.08f;

        private static readonly int IsHappyHash = Animator.StringToHash("IsHappy");
        private static readonly int IsSadHash   = Animator.StringToHash("IsSad");

        private float    _currentPatience;
        private bool     _isWaiting;
        private bool     _isServed;
        private Coroutine _patienceCoroutine;

        // Bubble procedural — sprites arredondados + quads + TMP nomeados com prefix
        // "_PT_" para escapar do HideExistingMesh() do CustomerVisualUpgrader.
        private Transform _bubbleRoot;
        private Transform _patienceFillPivot;     // scale.x = ratio de paciencia
        private Renderer  _patienceFillRenderer;  // cor: verde -> amarelo -> vermelho
        private Material  _patienceFillMaterial;
        private TextMeshPro _emojiLabel;          // fallback ASCII se nao tiver sprite
        private SpriteRenderer _emojiSpriteRenderer;  // emoji como sprite (PNG)
        private TextMeshPro _qtyLabel;            // texto "xN" — atualizado quando qty muda
        private string    _currentEmoji = "";
        private Coroutine _emojiTransitionCoroutine;

        // Movimento e animação walk
        private Vector3    _targetPos;
        private Quaternion _targetRot;
        private bool       _isMoving;
        private bool       _hasArrivedAtFirstSlot;
        private int        _walkSpeedHash, _vertHash, _stateHash;
        private bool       _hasWalkSpeed,  _hasVert,  _hasState;

        public event Action<Customer> OnCustomerLeft;
        public bool IsServed => _isServed;

        private void Awake()
        {
            // Desativa NavMeshAgent — posicionamento gerenciado pela CustomerQueue
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            // Usa Animator apenas se houver AnimatorController conectado.
            // Procura tambem em filhos (CustomerVisualUpgrader injeta o mesh ithappy).
            if (_animator == null)
                _animator = GetComponent<Animator>();
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);
            if (_animator != null && _animator.runtimeAnimatorController == null)
                _animator = null;

            CacheWalkAnimParams();

            // Sorteia quantidade antes de construir o bubble (texto "xN" usa _orderQuantity)
            if (_randomizeQuantity)
                _orderQuantity = UnityEngine.Random.Range(_minQuantity, _maxQuantity + 1);

            // Auto-load dos emojis default de Resources/Sprites/ se nao foram atribuidos manualmente
            LoadDefaultEmojiSprites();

            BuildThoughtBubble();

            // Adiciona visual estilizado (substitui capsula padrão por boneco)
            if (GetComponent<CustomerVisualUpgrader>() == null)
                gameObject.AddComponent<CustomerVisualUpgrader>();
        }

        // Carrega emoji sprites default de Resources/Sprites/ se nao foram atribuidos no
        // Inspector. Permite que TODOS os customers usem o mesmo conjunto de emojis
        // sem precisar arrastar PNG em cada prefab.
        //
        // Caminho esperado: Assets/Resources/Sprites/happy-face.png (etc)
        // Se nao existir nesse caminho, fica null e usa fallback ASCII.
        private void LoadDefaultEmojiSprites()
        {
            if (_emojiHappySprite   == null) _emojiHappySprite   = Resources.Load<Sprite>("Sprites/happy-face");
            if (_emojiNeutralSprite == null) _emojiNeutralSprite = Resources.Load<Sprite>("Sprites/confused");
            if (_emojiWorriedSprite == null) _emojiWorriedSprite = Resources.Load<Sprite>("Sprites/thinking");
            if (_emojiAngrySprite   == null) _emojiAngrySprite   = Resources.Load<Sprite>("Sprites/angry");
        }

        // Detecta quais params de walk o AnimatorController do prefab tem.
        // Suporta os 3 nomes comuns (Synty/ithappy: Vert + State; outros: Speed).
        private void CacheWalkAnimParams()
        {
            if (_animator == null) return;
            int speed = Animator.StringToHash("Speed");
            int vert  = Animator.StringToHash("Vert");
            int state = Animator.StringToHash("State");
            foreach (var p in _animator.parameters)
            {
                if (p.nameHash == speed) { _walkSpeedHash = speed; _hasWalkSpeed = true; }
                if (p.nameHash == vert)  { _vertHash      = vert;  _hasVert      = true; }
                if (p.nameHash == state) { _stateHash     = state; _hasState     = true; }
            }
        }

        // Anima o caminhar/parar (alimenta os params que existem).
        private void SetWalking(bool walking)
        {
            if (_animator == null) return;
            if (_hasWalkSpeed) _animator.SetFloat(_walkSpeedHash, walking ? 1.5f : 0f);
            if (_hasVert)      _animator.SetFloat(_vertHash,      walking ? 1f   : 0f);
            if (_hasState)     _animator.SetFloat(_stateHash,     walking ? 1f   : 0f);
        }

        // Faz o cliente caminhar ate uma posicao alvo (com animacao walk).
        // Ao chegar, encerra o walk e, se ainda nao iniciou, comeca a paciencia.
        public void MoveTo(Vector3 worldPos, Quaternion rotation)
        {
            _targetPos = worldPos;
            _targetRot = rotation;
            _isMoving  = true;
            SetWalking(true);
        }

        // Teletransporta sem animacao (uso interno: setup inicial).
        public void Teleport(Vector3 worldPos, Quaternion rotation)
        {
            transform.position = worldPos;
            transform.rotation = rotation;
            _isMoving = false;
            SetWalking(false);
        }

        // Driver de movimento — caminha em linha reta ate o alvo.
        // Sem pathfinding: confie que SpawnPoint e slots tem caminho livre entre eles.
        private void Update()
        {
            if (!_isMoving) return;

            Vector3 flatTarget = new Vector3(_targetPos.x, transform.position.y, _targetPos.z);
            Vector3 toTarget   = flatTarget - transform.position; toTarget.y = 0f;
            float dist = toTarget.magnitude;

            // Rotaciona para olhar pra direcao do movimento
            if (dist > 0.05f)
            {
                Quaternion look = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, _rotationSpeed * Time.deltaTime);
            }

            // Avanca em linha reta
            transform.position = Vector3.MoveTowards(transform.position, flatTarget, _moveSpeed * Time.deltaTime);

            // Chegou?
            if (dist <= _arrivalTolerance)
            {
                transform.position = flatTarget;
                transform.rotation = _targetRot;
                _isMoving = false;
                SetWalking(false);

                // Primeira chegada = inicia a contagem de paciencia.
                if (!_hasArrivedAtFirstSlot)
                {
                    _hasArrivedAtFirstSlot = true;
                    StartWaiting();
                }
            }
        }

        // Balao de pensamento estilo Pizza Ready: fundo branco + icone de pizza + "xN" +
        // barrinha de paciencia. Todos os meshes nomeados com prefix "_PT_" para nao
        // serem desativados pelo CustomerVisualUpgrader.HideExistingMesh().
        private void BuildThoughtBubble()
        {
            var rootGO = new GameObject("_PT_ThoughtBubble");
            rootGO.transform.SetParent(transform, false);
            rootGO.transform.localPosition = new Vector3(0f, _bubbleHeight, 0f);
            rootGO.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
            rootGO.transform.localScale    = Vector3.one * _bubbleScale;
            _bubbleRoot = rootGO.transform;

            // Sombrinha arredondada atras (semitransparente)
            CreateSprite("_PT_Bubble_Shadow", _bubbleRoot,
                new Vector3(0.04f, -0.04f, 0.02f),
                new Vector2(1.45f, 0.95f),
                GetSharedRoundedSprite(),
                new Color(0f, 0f, 0f, 0.35f),
                sortingOrder: 0);

            // Fundo branco arredondado
            CreateSprite("_PT_Bubble_BG", _bubbleRoot,
                new Vector3(0f, 0f, 0.01f),
                new Vector2(1.40f, 0.90f),
                GetSharedRoundedSprite(),
                Color.white,
                sortingOrder: 1);

            // Icone de pizza (sphere achatada, cor laranja)
            var icon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            icon.name = "_PT_Bubble_PizzaIcon";
            icon.transform.SetParent(_bubbleRoot, false);
            icon.transform.localPosition = new Vector3(-0.32f, 0.08f, -0.08f);
            icon.transform.localScale    = new Vector3(0.55f, 0.55f, 0.05f);
            Destroy(icon.GetComponent<Collider>());
            ApplyUnlit(icon, new Color(0.92f, 0.45f, 0.18f));

            // Toppings (pontinhos vermelhos)
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.094f;
                var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dot.name = "_PT_Bubble_Topping";
                dot.transform.SetParent(icon.transform, false);
                dot.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.25f, Mathf.Sin(a) * 0.25f, -0.5f);
                dot.transform.localScale    = new Vector3(0.18f, 0.18f, 1f);
                Destroy(dot.GetComponent<Collider>());
                ApplyUnlit(dot, new Color(0.85f, 0.15f, 0.10f));
            }

            // Texto "xN" (TMP 3D) — atualizavel em runtime via _qtyLabel
            var qtyGO = new GameObject("_PT_Bubble_Qty");
            qtyGO.transform.SetParent(_bubbleRoot, false);
            _qtyLabel = qtyGO.AddComponent<TextMeshPro>();
            _qtyLabel.text       = "x" + _orderQuantity;
            _qtyLabel.fontSize   = 4f;
            _qtyLabel.fontStyle  = FontStyles.Bold;
            _qtyLabel.color      = new Color(0.15f, 0.15f, 0.15f);
            _qtyLabel.alignment  = TextAlignmentOptions.Center;
            var qtyMr = qtyGO.GetComponent<MeshRenderer>();
            if (qtyMr != null) { qtyMr.sortingOrder = 10; }
            var rt = qtyGO.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta     = new Vector2(0.6f, 0.5f);
                rt.localPosition = new Vector3(0.30f, 0.08f, -0.08f);
                rt.localRotation = Quaternion.identity;
                rt.localScale    = Vector3.one;
            }

            // Patience Bar BG arredondado (cinza claro)
            var barBGSr = CreateSprite("_PT_Bubble_BarBG", _bubbleRoot,
                new Vector3(0f, -0.32f, -0.05f),
                new Vector2(1.20f, 0.14f),
                GetSharedRoundedSprite(),
                new Color(0.82f, 0.82f, 0.82f),
                sortingOrder: 2);

            // Pivot ancorado na borda esquerda da barra
            var pivotGO = new GameObject("_PT_Bubble_BarFillPivot");
            pivotGO.transform.SetParent(barBGSr.transform, false);
            pivotGO.transform.localPosition = new Vector3(-0.5f, 0f, -0.02f);
            pivotGO.transform.localScale    = Vector3.one;
            _patienceFillPivot = pivotGO.transform;

            // Fill verde (Quad simples — escalavel)
            var fill = CreateColoredQuad("_PT_Bubble_BarFill", pivotGO.transform,
                new Vector3(0.5f, 0f, 0f),
                new Vector3(1f, 0.78f, 1f),
                _colorFull);
            _patienceFillRenderer = fill.GetComponent<Renderer>();
            _patienceFillMaterial = _patienceFillRenderer.material;

            // Emoji acima do balao — usa SPRITE se atribuido, senao fallback pra TMP ASCII
            bool hasSprites = _emojiHappySprite != null || _emojiNeutralSprite != null
                              || _emojiWorriedSprite != null || _emojiAngrySprite != null;

            if (hasSprites)
            {
                // SpriteRenderer pra mostrar PNG do emoji
                var spriteGO = new GameObject("_PT_Bubble_EmojiSprite");
                spriteGO.transform.SetParent(_bubbleRoot, false);
                spriteGO.transform.localPosition = new Vector3(0f, 0.70f, -0.10f);
                spriteGO.transform.localRotation = Quaternion.identity;
                // Escala BEM PEQUENA por padrao — PNGs vem em resolucao alta (512px+) com
                // PPU 100, dando ~5 units de tamanho. 0.1 = pra ficar do tamanho do balao.
                spriteGO.transform.localScale    = Vector3.one * (0.1f * _emojiSpriteScale);

                _emojiSpriteRenderer = spriteGO.AddComponent<SpriteRenderer>();
                _emojiSpriteRenderer.sprite       = _emojiHappySprite;
                _emojiSpriteRenderer.sortingOrder = 11;
            }
            else
            {
                // Fallback: TMP ASCII (comportamento antigo)
                var emojiGO = new GameObject("_PT_Bubble_Emoji");
                emojiGO.transform.SetParent(_bubbleRoot, false);
                _emojiLabel = emojiGO.AddComponent<TextMeshPro>();
                _emojiLabel.text      = ":)";
                _emojiLabel.fontSize  = 6f;
                _emojiLabel.fontStyle = FontStyles.Bold;
                _emojiLabel.color     = new Color(0.25f, 0.80f, 0.30f);
                _emojiLabel.alignment = TextAlignmentOptions.Center;
                var emojiMr = emojiGO.GetComponent<MeshRenderer>();
                if (emojiMr != null) { emojiMr.sortingOrder = 11; }
                var emojiRT = emojiGO.GetComponent<RectTransform>();
                if (emojiRT != null)
                {
                    emojiRT.sizeDelta     = new Vector2(0.8f, 0.8f);
                    emojiRT.localPosition = new Vector3(0f, 0.70f, -0.10f);
                    emojiRT.localRotation = Quaternion.identity;
                    emojiRT.localScale    = Vector3.one;
                }
            }
            _currentEmoji = ":)";
        }

        // ── Sprite/Quad helpers ────────────────────────────────────────────────

        private static Sprite _sSharedRoundedSprite;

        // Sprite estatico arredondado, cached — criado 1x e reutilizado por todos
        // os clientes. Cor branca (tintada via SpriteRenderer.color).
        private static Sprite GetSharedRoundedSprite()
        {
            if (_sSharedRoundedSprite != null) return _sSharedRoundedSprite;

            const int W = 160, H = 100, R = 24;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode   = TextureWrapMode.Clamp;

            var pixels = new Color[W * H];
            var clear  = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    pixels[y * W + x] = IsInsideRoundedRect(x, y, W, H, R) ? Color.white : clear;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            _sSharedRoundedSprite = Sprite.Create(tex, new Rect(0, 0, W, H),
                new Vector2(0.5f, 0.5f), 100f);
            return _sSharedRoundedSprite;
        }

        private static bool IsInsideRoundedRect(int x, int y, int w, int h, int r)
        {
            int dx = 0, dy = 0;
            bool corner = false;
            if (x < r && y < r)               { dx = r - x;             dy = r - y;             corner = true; }
            else if (x >= w - r && y < r)     { dx = x - (w - r - 1);    dy = r - y;             corner = true; }
            else if (x < r && y >= h - r)     { dx = r - x;             dy = y - (h - r - 1);    corner = true; }
            else if (x >= w - r && y >= h - r){ dx = x - (w - r - 1);    dy = y - (h - r - 1);    corner = true; }
            if (corner) return dx * dx + dy * dy <= r * r;
            return true;
        }

        // Cria um SpriteRenderer com o sprite e escala adequada para o tamanho desejado.
        private static SpriteRenderer CreateSprite(string name, Transform parent,
            Vector3 localPos, Vector2 worldSize, Sprite sprite, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            // Calcula scale para atingir worldSize a partir do tamanho default do sprite
            Vector2 defaultSize = sprite.rect.size / sprite.pixelsPerUnit;
            go.transform.localScale = new Vector3(
                worldSize.x / defaultSize.x,
                worldSize.y / defaultSize.y,
                1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sprite;
            sr.color        = color;
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        // Cria um Quad colorido sem collider (usado pelo fill da barra de paciencia).
        private static GameObject CreateColoredQuad(string name, Transform parent,
            Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            Destroy(go.GetComponent<Collider>());
            ApplyUnlit(go, color);
            return go;
        }

        private static void ApplyUnlit(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Standard");
            var mat = new Material(sh);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     color);
            if (color.a < 1f)
            {
                mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (mat.HasProperty("_ZWrite"))   mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
            }
            r.material = mat;
        }

        public void StartWaiting()
        {
            _currentPatience = _patience;
            _isWaiting       = true;
            _isServed        = false;

            UpdatePatienceBar(1f);
            _patienceCoroutine = StartCoroutine(PatienceCountdown());
        }

        public float Deliver()
        {
            if (_isServed) return 0f;

            _isServed  = true;
            _isWaiting = false;

            if (_patienceCoroutine != null) StopCoroutine(_patienceCoroutine);

            // Pagamento escala com a quantidade pedida: base * qty + bonus se rapido
            float patienceRatio = _currentPatience / _patience;
            float bonus         = patienceRatio > 0.7f ? _bonusPaymentFast : 0f;
            float payment       = _basePayment * _orderQuantity + bonus;

            StartCoroutine(HappyAndLeave());
            return payment;
        }

        private IEnumerator PatienceCountdown()
        {
            // Cliente NAO vai embora quando paciencia zera — apenas para de descer
            // e fica com emoji bravo ate ser atendido.
            while (_isWaiting && !_isServed)
            {
                _currentPatience = Mathf.Max(0f, _currentPatience - Time.deltaTime);
                UpdatePatienceBar(_currentPatience / _patience);
                yield return null;
            }
        }

        private IEnumerator HappyAndLeave()
        {
            if (_animator != null) _animator.SetTrigger(IsHappyHash);
            if (_bubbleRoot != null) _bubbleRoot.gameObject.SetActive(false);

            // VFX corações
            ParticleManager.Instance?.PlayCustomerHappy(transform.position + Vector3.up);

            // Registra no combo + progressao
            ComboSystem.Instance?.RegisterSale();
            DailyGoalSystem.Instance?.RegisterPizzaDelivered();
            PlayerProgressionSystem.Instance?.RegisterPizzaDelivered();

            yield return new WaitForSeconds(1.5f);
            LeaveQueue();
        }

        private IEnumerator AngryAndLeave()
        {
            if (_animator != null) _animator.SetTrigger(IsSadHash);

            // VFX raiva + quebra combo
            ParticleManager.Instance?.PlayCustomerAngry(transform.position + Vector3.up);
            ComboSystem.Instance?.BreakCombo();

            yield return new WaitForSeconds(1f);
            LeaveQueue();
        }

        private void LeaveQueue()
        {
            _isWaiting = false;
            OnCustomerLeft?.Invoke(this);
            gameObject.SetActive(false);
        }

        private void UpdatePatienceBar(float ratio)
        {
            float r = Mathf.Clamp01(ratio);

            // Escala horizontal da barra (pivot ancorado a esquerda)
            if (_patienceFillPivot != null)
            {
                var s = _patienceFillPivot.localScale;
                _patienceFillPivot.localScale = new Vector3(r, s.y, s.z);
            }

            // Cor da barra (verde -> vermelho)
            if (_patienceFillMaterial != null)
            {
                Color c = Color.Lerp(_colorEmpty, _colorFull, r);
                if (_patienceFillMaterial.HasProperty("_BaseColor")) _patienceFillMaterial.SetColor("_BaseColor", c);
                if (_patienceFillMaterial.HasProperty("_Color"))     _patienceFillMaterial.SetColor("_Color",     c);
            }

            UpdateMoodLabel(r);
        }

        // Determina o emoji pela paciencia e dispara transicao fluida se mudou.
        // Estados:  >70%   feliz   (happy)
        //          40-70%  neutro  (neutral)
        //          15-40%  triste  (worried)
        //          <15%    bravo   (angry)
        private void UpdateMoodLabel(float ratio)
        {
            string newEmoji; Color newColor; Sprite newSprite;
            if      (ratio > 0.70f) { newEmoji = ":)"; newColor = new Color(0.25f, 0.80f, 0.30f); newSprite = _emojiHappySprite; }
            else if (ratio > 0.40f) { newEmoji = ":|"; newColor = new Color(0.95f, 0.75f, 0.10f); newSprite = _emojiNeutralSprite; }
            else if (ratio > 0.15f) { newEmoji = ":/"; newColor = new Color(0.95f, 0.50f, 0.15f); newSprite = _emojiWorriedSprite; }
            else                    { newEmoji = ":("; newColor = new Color(0.95f, 0.20f, 0.20f); newSprite = _emojiAngrySprite; }

            if (newEmoji == _currentEmoji) return;
            _currentEmoji = newEmoji;

            // Modo SPRITE: troca o sprite do SpriteRenderer com pop de scale
            if (_emojiSpriteRenderer != null)
            {
                if (newSprite != null) _emojiSpriteRenderer.sprite = newSprite;
                if (_emojiTransitionCoroutine != null) StopCoroutine(_emojiTransitionCoroutine);
                _emojiTransitionCoroutine = StartCoroutine(SpritePopAnim());
                return;
            }

            // Modo TMP (fallback ASCII)
            if (_emojiLabel == null) return;
            if (_emojiTransitionCoroutine != null) StopCoroutine(_emojiTransitionCoroutine);
            _emojiTransitionCoroutine = StartCoroutine(EmojiTransition(newEmoji, newColor));
        }

        // Animacao de pop pro sprite emoji (mais simples que a do TMP).
        private IEnumerator SpritePopAnim()
        {
            if (_emojiSpriteRenderer == null) yield break;
            var tr = _emojiSpriteRenderer.transform;
            Vector3 baseScale = Vector3.one * (0.1f * _emojiSpriteScale);

            float t = 0f;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                float k = t / 0.15f;
                k = k * k * (3f - 2f * k);   // smoothstep
                tr.localScale = baseScale * Mathf.Lerp(1.0f, 1.35f, k);
                yield return null;
            }
            t = 0f;
            while (t < 0.10f)
            {
                t += Time.deltaTime;
                float k = t / 0.10f;
                tr.localScale = baseScale * Mathf.Lerp(1.35f, 1f, k);
                yield return null;
            }
            tr.localScale = baseScale;
            _emojiTransitionCoroutine = null;
        }

        // Animacao fluida de troca: encolhe ate 0, troca, expande ate 1.25, volta a 1.
        private IEnumerator EmojiTransition(string newEmoji, Color newColor)
        {
            if (_emojiLabel == null) yield break;
            var tr = _emojiLabel.transform;

            // Fase 1: encolhe (0.10s)
            float t = 0f;
            while (t < 0.10f)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(t / 0.10f);
                tr.localScale = Vector3.one * k;
                yield return null;
            }
            tr.localScale = Vector3.zero;

            // Troca texto+cor
            _emojiLabel.text  = newEmoji;
            _emojiLabel.color = newColor;

            // Fase 2: cresce alem (0 -> 1.25, 0.18s) com easing
            t = 0f;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.18f);
                k = k * k * (3f - 2f * k); // smoothstep
                tr.localScale = Vector3.one * (1.25f * k);
                yield return null;
            }

            // Fase 3: volta ao 1 (0.12s)
            t = 0f;
            while (t < 0.12f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.12f);
                tr.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, k);
                yield return null;
            }
            tr.localScale = Vector3.one;
            _emojiTransitionCoroutine = null;
        }

        // Mantem o bubble sempre voltado pra camera independente da rotacao do cliente.
        private void LateUpdate()
        {
            if (_bubbleRoot == null) return;
            _bubbleRoot.rotation = Quaternion.Euler(55f, 0f, 0f);
        }

        private void OnEnable()
        {
            // Mostra o bubble ao reativar (vindo do pool)
            if (_bubbleRoot != null) _bubbleRoot.gameObject.SetActive(true);

            // Re-sorteia quantidade ao reciclar e atualiza texto
            if (_randomizeQuantity)
            {
                _orderQuantity = UnityEngine.Random.Range(_minQuantity, _maxQuantity + 1);
                if (_qtyLabel != null) _qtyLabel.text = "x" + _orderQuantity;
            }

            // Reset de estado quando reciclado do pool
            _hasArrivedAtFirstSlot = false;
            _isMoving              = false;
            SetWalking(false);
        }
    }
}
