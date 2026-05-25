using System.Collections.Generic;
using UnityEngine;

namespace PizzaTycoon.Stations
{
    // Armazena pizzas visualmente no balcao. O DeliveryStation usa este componente
    // para acumular pizzas (player dropa) antes de vender ao cliente.
    //
    // Slots aparecem em fileira na direcao X local. O numero de slots = MaxCapacity.
    // Gizmo laranja no Scene View mostra onde cada slot fica.
    public class CounterStorage : MonoBehaviour
    {
        [Header("Capacidade")]
        [Tooltip("Maximo de pizzas que cabe simultaneamente no balcao")]
        [SerializeField] private int _maxCapacity = 5;

        [Header("Layout Visual")]
        [Tooltip("Modo de empilhamento: Row = em fileira (X), Stack = empilhadas (Y), Grid = combinacao")]
        [SerializeField] private StackMode _stackMode = StackMode.Stack;
        [Tooltip("Comprimento (em X local) da fileira (so usado em Row e Grid)")]
        [SerializeField] private float _stackLength  = 1.8f;
        [Tooltip("Altura Y local da PRIMEIRA pizza (sobre o balcao)")]
        [SerializeField] private float _stackHeight  = 0f;
        [Tooltip("Espacamento vertical (Y) entre pizzas empilhadas")]
        [SerializeField] private float _stackSpacing = 0.08f;
        [Tooltip("Escala da pizza visual")]
        [SerializeField] private float _pizzaScale   = 1f;
        [Tooltip("So usado em modo Grid: quantas pizzas por coluna antes de comecar outra")]
        [SerializeField] private int   _gridColumnHeight = 3;

        public enum StackMode
        {
            Row,    // Em fileira no X (comportamento antigo)
            Stack,  // Empilhadas no Y (uma em cima da outra)
            Grid    // Empilhadas em colunas: enche uma coluna no Y, depois proxima no X
        }

        [Header("Cores da pizza visual")]
        [SerializeField] private Color _doughColor   = new Color(0.92f, 0.55f, 0.20f);
        [SerializeField] private Color _toppingColor = new Color(0.85f, 0.15f, 0.10f);

        private readonly List<GameObject> _visualPizzas = new List<GameObject>();

        public int  CurrentCount => _visualPizzas.Count;
        public int  MaxCapacity  => _maxCapacity;
        public bool HasRoom      => _visualPizzas.Count < _maxCapacity;
        public bool IsEmpty      => _visualPizzas.Count == 0;
        public bool HasAtLeast(int n) => _visualPizzas.Count >= n;

        // Adiciona 1 pizza no balcao se houver espaco. Retorna false se cheio.
        public bool TryAddPizza()
        {
            if (!HasRoom) return false;
            int newIndex = _visualPizzas.Count;
            var pizza   = BuildPizzaVisual(newIndex);
            _visualPizzas.Add(pizza);
            return true;
        }

        // Remove N pizzas do TOPO da pilha (visualmente abre espaco a direita).
        // Retorna false se nao houver pizzas suficientes.
        public bool TryTakePizzas(int n)
        {
            if (n <= 0) return false;
            if (_visualPizzas.Count < n) return false;

            for (int i = 0; i < n; i++)
            {
                int last = _visualPizzas.Count - 1;
                if (_visualPizzas[last] != null) Destroy(_visualPizzas[last]);
                _visualPizzas.RemoveAt(last);
            }
            return true;
        }

        // Limpa todas as pizzas (uso interno / reset).
        public void Clear()
        {
            foreach (var p in _visualPizzas)
                if (p != null) Destroy(p);
            _visualPizzas.Clear();
        }

        // ── Construcao do visual ────────────────────────────────────────────

        private GameObject BuildPizzaVisual(int slotIndex)
        {
            var root = new GameObject($"_PT_StackedPizza_{slotIndex}");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = GetLocalSlotPosition(slotIndex);
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale    = Vector3.one * _pizzaScale;

            // Massa (disco achatado)
            var basePart = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePart.name = "_PT_PizzaDough";
            basePart.transform.SetParent(root.transform, false);
            basePart.transform.localPosition = Vector3.zero;
            basePart.transform.localScale    = new Vector3(0.30f, 0.025f, 0.30f);
            Destroy(basePart.GetComponent<Collider>());
            ApplyColor(basePart, _doughColor);

            // Toppings (4 esferas vermelhas em cima)
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                var top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                top.name = "_PT_PizzaTopping";
                top.transform.SetParent(basePart.transform, false);
                top.transform.localPosition = new Vector3(
                    Mathf.Cos(a) * 0.28f, 0.7f, Mathf.Sin(a) * 0.28f);
                top.transform.localScale    = new Vector3(0.22f, 3.0f, 0.22f);
                Destroy(top.GetComponent<Collider>());
                ApplyColor(top, _toppingColor);
            }

            return root;
        }

        private Vector3 GetLocalSlotPosition(int slotIndex)
        {
            switch (_stackMode)
            {
                case StackMode.Stack:
                    // Tudo no mesmo X, subindo no Y a cada pizza
                    return new Vector3(0f, _stackHeight + slotIndex * _stackSpacing, 0f);

                case StackMode.Grid:
                {
                    // Coluna por coluna: enche _gridColumnHeight no Y, depois passa pra proxima X
                    int colH = Mathf.Max(1, _gridColumnHeight);
                    int col  = slotIndex / colH;
                    int row  = slotIndex % colH;
                    int totalCols = Mathf.CeilToInt((float)_maxCapacity / colH);
                    float spacingX = totalCols > 1 ? _stackLength / (totalCols - 1) : 0f;
                    float xOffset  = totalCols > 1
                        ? -_stackLength * 0.5f + spacingX * col
                        : 0f;
                    return new Vector3(xOffset, _stackHeight + row * _stackSpacing, 0f);
                }

                case StackMode.Row:
                default:
                {
                    float spacing = _stackLength / Mathf.Max(1, _maxCapacity);
                    float xOffset = -_stackLength * 0.5f + spacing * 0.5f + spacing * slotIndex;
                    return new Vector3(xOffset, _stackHeight, 0f);
                }
            }
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            var mat = new Material(sh);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     color);
            r.sharedMaterial = mat;
        }

#if UNITY_EDITOR
        // Mostra no Scene View os slots da fileira (esferas laranjas + linha).
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.65f, 0.10f, 0.8f);
            for (int i = 0; i < _maxCapacity; i++)
            {
                Vector3 worldPos = transform.TransformPoint(GetLocalSlotPosition(i));
                Gizmos.DrawWireSphere(worldPos, 0.18f * _pizzaScale);
            }
            Gizmos.color = new Color(1f, 0.65f, 0.10f, 0.4f);
            Vector3 left  = transform.TransformPoint(new Vector3(-_stackLength * 0.5f, _stackHeight, 0f));
            Vector3 right = transform.TransformPoint(new Vector3( _stackLength * 0.5f, _stackHeight, 0f));
            Gizmos.DrawLine(left, right);

            UnityEditor.Handles.color = new Color(1f, 0.65f, 0.10f, 1f);
            UnityEditor.Handles.Label(
                transform.TransformPoint(new Vector3(0f, _stackHeight + 0.3f, 0f)),
                $"COUNTER ({_maxCapacity} slots)");
        }
#endif
    }
}
