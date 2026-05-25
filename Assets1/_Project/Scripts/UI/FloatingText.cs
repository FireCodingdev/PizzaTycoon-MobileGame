using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PizzaTycoon.UI
{
    public enum FloatingTextType { Money, Combo, Miss, Unlock }

    // Pool de 20 textos flutuantes 3D com tipos visuais distintos.
    //
    // CORREÇÕES (v2):
    //   1. _mainCam é atualizado a cada frame em Update() — evita null quando
    //      o FloatingText é criado antes da câmera existir na cena.
    //   2. Guard em Awake() impede que a instância seja sobrescrita se o GameObject
    //      for criado pelo getter Instance (Awake roda durante AddComponent).
    //   3. InitPool() usa Camera.main com retry para garantir câmera válida.
    public class FloatingText : MonoBehaviour
    {
        // ── Singleton interno (lazy) ─────────────────────────────────────────
        private static FloatingText _instance;
        private static FloatingText Instance
        {
            get
            {
                if (_instance != null) return _instance;

                // Procura na cena antes de criar um novo
                _instance = FindObjectOfType<FloatingText>();
                if (_instance != null) return _instance;

                var go = new GameObject("[FloatingText_Pool]");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<FloatingText>();
                // Nota: _instance é atribuído ANTES de InitPool para que Awake()
                // não entre em loop ao chamar Instance novamente.
                _instance.InitPool();
                return _instance;
            }
        }

        // ── Entrada pública (static) ─────────────────────────────────────────

        public static void Show(string text, Vector3 worldPos, FloatingTextType type = FloatingTextType.Money)
            => Instance.SpawnFromPool(text, worldPos, type);

        // ── Configuração por tipo ────────────────────────────────────────────

        private static readonly Dictionary<FloatingTextType, Color> TypeColors = new()
        {
            { FloatingTextType.Money,  new Color(0.18f, 0.80f, 0.44f) }, // verde
            { FloatingTextType.Combo,  new Color(1.00f, 0.76f, 0.03f) }, // amarelo
            { FloatingTextType.Miss,   new Color(0.91f, 0.30f, 0.24f) }, // vermelho
            { FloatingTextType.Unlock, new Color(0.20f, 0.60f, 0.86f) }, // azul
        };

        private static readonly Dictionary<FloatingTextType, float> TypeScales = new()
        {
            { FloatingTextType.Money,  1.0f },
            { FloatingTextType.Combo,  1.4f },
            { FloatingTextType.Miss,   0.9f },
            { FloatingTextType.Unlock, 1.6f },
        };

        // ── Pool interno ─────────────────────────────────────────────────────

        private const int PoolSize = 20;
        private readonly Queue<TextEntry> _pool = new();

        private class TextEntry
        {
            public GameObject  Go;
            public TextMeshPro TMP;
            public bool        InUse;
        }

        private UnityEngine.Camera _mainCam;
        private bool _poolInitialized;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            // Guard: se já existe uma instância (criada pelo getter), não reinicializa.
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            _mainCam  = UnityEngine.Camera.main;
        }

        private void OnEnable()
        {
            _mainCam = UnityEngine.Camera.main;
        }

        // CORREÇÃO PRINCIPAL: atualiza _mainCam a cada frame para lidar com
        // câmeras que são criadas depois do FloatingText (comum no SceneRebuilder).
        private void Update()
        {
            if (_mainCam == null)
                _mainCam = UnityEngine.Camera.main;
        }

        private void InitPool()
        {
            if (_poolInitialized) return;
            _poolInitialized = true;

            _mainCam = UnityEngine.Camera.main;

            for (int i = 0; i < PoolSize; i++)
            {
                var go  = new GameObject("FloatingText");
                go.transform.SetParent(transform);
                var tmp = go.AddComponent<TextMeshPro>();
                tmp.alignment    = TextAlignmentOptions.Center;
                tmp.fontSize     = 5f;
                tmp.sortingOrder = 10;
                go.SetActive(false);
                _pool.Enqueue(new TextEntry { Go = go, TMP = tmp });
            }
        }

        private void SpawnFromPool(string text, Vector3 worldPos, FloatingTextType type)
        {
            // Garante que o pool está inicializado (caso o GameObject tenha sido
            // criado externamente sem chamar InitPool).
            if (!_poolInitialized) InitPool();

            if (_mainCam == null) _mainCam = UnityEngine.Camera.main;

            // Encontra entry disponível
            TextEntry entry = null;
            int count = _pool.Count;
            for (int i = 0; i < count; i++)
            {
                var e = _pool.Dequeue();
                if (!e.InUse) { entry = e; break; }
                _pool.Enqueue(e);
            }
            if (entry == null) return; // todas em uso

            entry.InUse = true;
            _pool.Enqueue(entry);
            StartCoroutine(Animate(entry, text, worldPos, type));
        }

        private IEnumerator Animate(TextEntry entry, string text, Vector3 worldPos, FloatingTextType type)
        {
            var go  = entry.Go;
            var tmp = entry.TMP;

            Color  col   = TypeColors.TryGetValue(type, out var c) ? c : Color.white;
            float  scale = TypeScales.TryGetValue(type, out var s) ? s : 1f;

            go.transform.position   = worldPos + Vector3.up * 0.5f;
            go.transform.localScale = Vector3.one * scale;
            tmp.text  = text;
            tmp.color = col;
            go.SetActive(true);

            float duration = type == FloatingTextType.Unlock ? 1.4f : 0.9f;
            float floatSpd = type == FloatingTextType.Combo  ? 2.0f : 1.5f;
            Vector3 startPos = go.transform.position;
            float elapsed    = 0f;

            // Punch de entrada (0 → scale nos primeiros 0.1s)
            float punchTime = 0.1f;
            for (float t = 0f; t < punchTime; t += Time.deltaTime)
            {
                go.transform.localScale = Vector3.one * scale * (t / punchTime);
                // Câmera pode aparecer durante a animação — tenta a cada frame
                if (_mainCam == null) _mainCam = UnityEngine.Camera.main;
                if (_mainCam != null) go.transform.forward = _mainCam.transform.forward;
                yield return null;
            }
            go.transform.localScale = Vector3.one * scale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float norm = elapsed / duration;

                go.transform.position = startPos + Vector3.up * (floatSpd * norm);

                float alpha = norm < 0.5f ? 1f : 1f - (norm - 0.5f) / 0.5f;
                tmp.color = new Color(col.r, col.g, col.b, alpha);

                // CORREÇÃO: sempre tenta resolver câmera antes de usar
                if (_mainCam == null) _mainCam = UnityEngine.Camera.main;
                if (_mainCam != null) go.transform.forward = _mainCam.transform.forward;

                yield return null;
            }

            go.SetActive(false);
            entry.InUse = false;
        }
    }
}