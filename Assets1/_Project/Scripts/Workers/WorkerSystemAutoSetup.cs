using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using PizzaTycoon.Stations;
using PizzaTycoon.Managers;

namespace PizzaTycoon.Workers
{
    // Inicializa o WorkerManager completamente em código:
    //   1. Cria WorkerData runtime para os 4 tipos de worker
    //   2. Auto-detecta as estações da cena
    //   3. Injeta tudo no WorkerManager via reflection
    //   4. Spawna workers NPC que ficam visíveis no mapa
    [DefaultExecutionOrder(30)]
    public class WorkerSystemAutoSetup : MonoBehaviour
    {
        private void Start()
        {
            // Auto-spawn DESABILITADO. Os Workers NAO sao criados automaticamente.
            // Para usar o sistema de workers manualmente, chame Setup() do codigo,
            // ou configure os Workers diretamente no Scene View.
        }

        private void Setup()
        {
            var manager = WorkerManager.Instance;
            if (manager == null) return;

            // Já configurado — não duplica
            if (manager.GetAll() != null && manager.GetAll().Length > 0) return;

            var stations = FindStations();
            if (stations.wheat == null || stations.dough == null ||
                stations.oven  == null || stations.delivery == null)
            {
                Debug.LogWarning("[WorkerSetup] Estações não encontradas — workers não serão criados.");
                return;
            }

            // ── Cria WorkerData em runtime ─────────────────────────────────────
            var catalog = new WorkerData[]
            {
                MakeData("collector", "Coletador",  WorkerType.Collector,
                    hiringCost: 150, baseSpeed: 3.5f, color: new Color(0.20f, 0.65f, 0.30f)),
                MakeData("baker",     "Padeiro",     WorkerType.Baker,
                    hiringCost: 250, baseSpeed: 3.2f, color: new Color(0.85f, 0.55f, 0.15f)),
                MakeData("deliverer", "Entregador",  WorkerType.Deliverer,
                    hiringCost: 400, baseSpeed: 4.0f, color: new Color(0.20f, 0.35f, 0.80f)),
                MakeData("cleaner",   "Faxineiro",   WorkerType.Cleaner,
                    hiringCost: 100, baseSpeed: 3.0f, color: new Color(0.80f, 0.20f, 0.25f)),
            };

            // ── Injeta catalog, prefab e stations via reflection ───────────────
            SetField(manager, "_workerCatalog",   catalog);
            SetField(manager, "_wheatStation",    stations.wheat);
            SetField(manager, "_doughStation",    stations.dough);
            SetField(manager, "_ovenStation",     stations.oven);
            SetField(manager, "_deliveryStation", stations.delivery);

            // Cria prefab-like: um GameObject persistente com WorkerAI
            var prefabGO = new GameObject("[WorkerPrefab]");
            prefabGO.SetActive(false);
            DontDestroyOnLoad(prefabGO);
            BuildWorkerVisual(prefabGO);
            var workerAI = prefabGO.AddComponent<WorkerAI>();
            SetField(manager, "_workerPrefab", workerAI);

            // Cria parent para spawns
            var spawnRoot = new GameObject("[Workers]");
            SetField(manager, "_spawnParent", spawnRoot.transform);

            // ── Contrata automaticamente todos os workers se dinheiro suficiente ─
            // (começa com o mais barato e vai contratando conforme o jogo progride)
            // Por padrão spawna os primeiros 2 sem custo para o player perceber
            SpawnWorkerFree(manager, catalog[0], 1, stations.wheat,    stations.dough,    spawnRoot.transform, catalog[0].uniformColor);
            SpawnWorkerFree(manager, catalog[1], 1, stations.dough,    stations.oven,     spawnRoot.transform, catalog[1].uniformColor);
            SpawnWorkerFree(manager, catalog[2], 1, stations.oven,     stations.delivery, spawnRoot.transform, catalog[2].uniformColor);

            Debug.Log("[WorkerSetup] Workers configurados e spawnados com sucesso.");
        }

        // ── Detecta estações por tipo ──────────────────────────────────────────

        private struct Stations
        {
            public Transform wheat, dough, oven, delivery;
        }

        private Stations FindStations()
        {
            var s = new Stations();
            foreach (var station in FindObjectsOfType<BaseStation>())
            {
                string t = station.GetType().Name;
                if (t.Contains("Wheat"))    s.wheat    = station.transform;
                else if (t.Contains("Dough")) s.dough  = station.transform;
                else if (t.Contains("Oven"))  s.oven   = station.transform;
                else if (t.Contains("Delivery")) s.delivery = station.transform;
            }
            return s;
        }

        // ── Spawna worker diretamente (bypass do sistema de custo) ─────────────

        private static void SpawnWorkerFree(WorkerManager manager, WorkerData data, int level,
            Transform source, Transform target, Transform parent, Color uniformColor)
        {
            var go = new GameObject("[Worker_" + data.displayName + "]");
            go.transform.SetParent(parent, false);
            go.transform.position = source != null
                ? source.position + Vector3.up * 0.1f
                : Vector3.zero;

            BuildWorkerVisual(go, uniformColor);
            var ai = go.AddComponent<WorkerAI>();
            ai.Initialize(data, level, source, target);

            var dict = GetField<Dictionary<string, WorkerAI>>(manager, "_activeWorkers");
            if (dict != null) dict[data.workerId] = ai;
            var levels = GetField<Dictionary<string, int>>(manager, "_workerLevels");
            if (levels != null) levels[data.workerId] = level;
        }

        // ── Monta visual de NPC worker com primitivas ──────────────────────────

        private static void BuildWorkerVisual(GameObject root, Color? uniformColor = null)
        {
            Color shirt = uniformColor ?? new Color(0.85f, 0.55f, 0.15f);
            Color skin  = new Color(0.90f, 0.75f, 0.60f);
            Color pants = new Color(0.20f, 0.20f, 0.35f);

            // Tenta carregar ithappy Base_Mesh
#if UNITY_EDITOR
            var baseMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ithappy/Creative_Characters_FREE/Prefabs/Base_Mesh.prefab");
            if (baseMesh != null)
            {
                var inst = Instantiate(baseMesh);
                inst.name = "_PT_Char";
                inst.transform.SetParent(root.transform, false);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localScale    = Vector3.one * 0.85f;
                if (uniformColor.HasValue) ApplyColorToRenderers(inst, uniformColor.Value);
                return;
            }
#endif
            // Fallback com primitivas
            MakePart(root, "_PT_Head",  PrimitiveType.Sphere,
                new Vector3(0f, 1.60f, 0f), new Vector3(0.34f, 0.34f, 0.34f), skin);
            MakePart(root, "_PT_Body",  PrimitiveType.Capsule,
                new Vector3(0f, 1.05f, 0f), new Vector3(0.42f, 0.38f, 0.28f), shirt);
            MakePart(root, "_PT_LegL",  PrimitiveType.Capsule,
                new Vector3(-0.12f, 0.45f, 0f), new Vector3(0.14f, 0.28f, 0.14f), pants);
            MakePart(root, "_PT_LegR",  PrimitiveType.Capsule,
                new Vector3( 0.12f, 0.45f, 0f), new Vector3(0.14f, 0.28f, 0.14f), pants);
            // Tag para que o HideExistingMesh do CustomerVisualUpgrader não esconda
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
                r.gameObject.name = "_PT_" + r.gameObject.name;
        }

        private static void ApplyColorToRenderers(GameObject root, Color color)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", color);
                mpb.SetColor("_Color", color);
                r.SetPropertyBlock(mpb);
            }
        }

        private static void MakePart(GameObject parent, string name, PrimitiveType type,
            Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale    = scale;
            Destroy(go.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                ?? Shader.Find("Standard"));
            mat.color = color;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        // ── Helpers de WorkerData ─────────────────────────────────────────────

        private static WorkerData MakeData(string id, string name, WorkerType type,
            int hiringCost, float baseSpeed, Color color)
        {
            var d = ScriptableObject.CreateInstance<WorkerData>();
            d.workerId          = id;
            d.displayName       = name;
            d.type              = type;
            d.hiringCost        = hiringCost;
            d.upgradeCostPerLevel = hiringCost / 2;
            d.maxLevel          = 5;
            d.baseSpeed         = baseSpeed;
            d.speedPerLevel     = 0.4f;
            d.uniformColor      = color;
            return d;
        }

        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object obj, string name, object value)
        {
            var f = obj.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            f?.SetValue(obj, value);
        }

        private static T GetField<T>(object obj, string name) where T : class
        {
            var f = obj.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return f?.GetValue(obj) as T;
        }
    }
}
