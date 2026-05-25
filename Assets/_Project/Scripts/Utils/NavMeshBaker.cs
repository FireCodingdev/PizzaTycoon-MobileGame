using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace PizzaTycoon.Utils
{
    // Realiza bake do NavMesh em runtime para pathfinding dos clientes
    // PACOTE NECESSÁRIO: "AI Navigation" (com.unity.ai.navigation) via Package Manager
    //
    // Configuração rápida (sem este script):
    // 1. Window > AI > Navigation
    // 2. Aba "Agents": configure raio=0.4, altura=1.5
    // 3. Aba "Bake": marque os pisos como "Navigation Static" e clique "Bake"
    //
    // Este componente oferece bake em runtime como alternativa.
    public class NavMeshBaker : MonoBehaviour
    {
        [Header("Configurações do NavMesh")]
        [SerializeField] private NavMeshBuildSettings _buildSettings = default;
        [SerializeField] private bool _bakeOnStart = true;
        [SerializeField] private float _rebakeInterval = 0f; // 0 = não rebake automático

        [Header("Área de Bake")]
        [SerializeField] private Bounds _bakeBounds = new Bounds(Vector3.zero, new Vector3(50f, 10f, 50f));

        [Header("Referências dos Agentes")]
        [SerializeField] private NavMeshAgent[] _agentsToReconfigure;

        // Configurações padrão de agente para clientes do jogo
        public static readonly NavMeshAgentConfig CustomerAgentConfig = new NavMeshAgentConfig
        {
            Speed           = 2f,
            AngularSpeed    = 120f,
            Acceleration    = 8f,
            StoppingDistance = 0.5f,
            Radius          = 0.4f,
            Height          = 1.5f,
            BaseOffset      = 0f,
        };

        private NavMeshDataInstance _navMeshInstance;

        private void Start()
        {
            _buildSettings = NavMesh.CreateSettings();
            _buildSettings.agentRadius = 0.4f;
            _buildSettings.agentHeight = 1.5f;
            _buildSettings.agentClimb  = 0.4f;
            _buildSettings.agentSlope  = 45f;

            if (_bakeOnStart)
                StartCoroutine(BakeAsync());

            if (_rebakeInterval > 0f)
                StartCoroutine(RebakeLoop());
        }

        private void OnDestroy()
        {
            NavMesh.RemoveNavMeshData(_navMeshInstance);
        }

        public void RequestBake()
        {
            StartCoroutine(BakeAsync());
        }

        private IEnumerator BakeAsync()
        {
            // Aguarda frame para garantir que todos os objetos estão na cena
            yield return new WaitForEndOfFrame();

            var sources = CollectNavMeshSources();

            // Operação assíncrona de build
            AsyncOperation op = NavMeshBuilder.UpdateNavMeshDataAsync(
                GetOrCreateNavMeshData(),
                _buildSettings,
                sources,
                _bakeBounds
            );

            yield return op;

            // Após o bake, reconfigura os agentes
            ReconfigureAgents();

            Debug.Log("[NavMeshBaker] NavMesh baked com sucesso.");
        }

        private System.Collections.Generic.List<NavMeshBuildSource> CollectNavMeshSources()
        {
            var sources = new System.Collections.Generic.List<NavMeshBuildSource>();

            // Coleta todos os MeshFilters marcados como Static
            foreach (MeshFilter mf in FindObjectsOfType<MeshFilter>())
            {
                if (mf == null || mf.sharedMesh == null) continue;

                // Apenas objetos estáticos são incluídos no NavMesh
                if (!mf.gameObject.isStatic && !mf.CompareTag("NavMeshObstacle")) continue;

                var source = new NavMeshBuildSource
                {
                    shape     = NavMeshBuildSourceShape.Mesh,
                    transform = mf.transform.localToWorldMatrix,
                    area      = 0 // Walkable
                };
                source.sourceObject = mf.sharedMesh;
                sources.Add(source);
            }

            // Coleta Terrains (Unity Terrain component)
            foreach (Terrain terrain in FindObjectsOfType<Terrain>())
            {
                var source = new NavMeshBuildSource
                {
                    shape     = NavMeshBuildSourceShape.Terrain,
                    transform  = Matrix4x4.TRS(terrain.transform.position, Quaternion.identity, Vector3.one),
                    area       = 0
                };
                source.sourceObject = terrain.terrainData;
                sources.Add(source);
            }

            return sources;
        }

        private NavMeshData GetOrCreateNavMeshData()
        {
            if (_navMeshInstance.valid)
            {
                NavMesh.RemoveNavMeshData(_navMeshInstance);
            }

            NavMeshData data = new NavMeshData(_buildSettings.agentTypeID);
            _navMeshInstance = NavMesh.AddNavMeshData(data);
            return data;
        }

        private void ReconfigureAgents()
        {
            if (_agentsToReconfigure == null) return;

            foreach (NavMeshAgent agent in _agentsToReconfigure)
            {
                if (agent == null) continue;
                ApplyAgentConfig(agent, CustomerAgentConfig);
            }
        }

        // Configura um NavMeshAgent com os parâmetros de clientes do jogo
        public static void ApplyAgentConfig(NavMeshAgent agent, NavMeshAgentConfig config)
        {
            agent.speed           = config.Speed;
            agent.angularSpeed    = config.AngularSpeed;
            agent.acceleration    = config.Acceleration;
            agent.stoppingDistance = config.StoppingDistance;
            agent.radius          = config.Radius;
            agent.height          = config.Height;
            agent.baseOffset      = config.BaseOffset;
        }

        private IEnumerator RebakeLoop()
        {
            WaitForSeconds wait = new WaitForSeconds(_rebakeInterval);
            while (true)
            {
                yield return wait;
                yield return BakeAsync();
            }
        }

        // Define waypoints fixos em frente ao DeliveryStation para a fila de clientes
        // Retorna as 5 posições de espera baseadas no ponto de referência informado
        public static Vector3[] GetQueueWaypoints(Vector3 deliveryPosition, Vector3 forward, int count = 5)
        {
            Vector3[] waypoints = new Vector3[count];
            float spacing = 1.2f;

            for (int i = 0; i < count; i++)
                waypoints[i] = deliveryPosition + forward * ((i + 1) * spacing);

            return waypoints;
        }
    }

    // Configuração de agente para serialização
    [System.Serializable]
    public class NavMeshAgentConfig
    {
        public float Speed           = 2f;
        public float AngularSpeed    = 120f;
        public float Acceleration    = 8f;
        public float StoppingDistance = 0.5f;
        public float Radius          = 0.4f;
        public float Height          = 1.5f;
        public float BaseOffset      = 0f;
    }
}
