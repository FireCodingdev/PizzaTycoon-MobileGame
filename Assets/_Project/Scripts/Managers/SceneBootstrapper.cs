using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace PizzaTycoon.Managers
{
    [ExecuteInEditMode]
    public class SceneBootstrapper : MonoBehaviour
    {
        [Header("Configurações de Montagem")]
        [SerializeField] private bool _buildOnAwake = false;

        private void Awake()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && _buildOnAwake)
                BuildScene();
#endif
        }

#if UNITY_EDITOR
        [MenuItem("PizzaTycoon/3. Build Scene")]
        public static void BuildScene()
        {
            if (!ConfirmBuild()) return;
            EditorUtility.DisplayProgressBar("Pizza Tycoon", "Iniciando...", 0f);
            try
            {
                ClearScene();
                EditorUtility.DisplayProgressBar("Pizza Tycoon", "Criando terreno...", 0.15f);
                CreateTerrain();
                EditorUtility.DisplayProgressBar("Pizza Tycoon", "Posicionando estações...", 0.30f);
                PlaceStations();
                EditorUtility.DisplayProgressBar("Pizza Tycoon", "Instanciando jogador...", 0.50f);
                PlacePlayer();
                EditorUtility.DisplayProgressBar("Pizza Tycoon", "Configurando câmera...", 0.60f);
                SetupCamera();
                EditorUtility.DisplayProgressBar("Pizza Tycoon", "Configurando iluminação...", 0.70f);
                SetupLighting();
                EditorUtility.DisplayProgressBar("Pizza Tycoon", "Criando Managers...", 0.80f);
                SetupManagers();
                EditorUtility.DisplayProgressBar("Pizza Tycoon", "Spawner de Clientes...", 0.90f);
                SetupCustomerSpawner();
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            finally { EditorUtility.ClearProgressBar(); }

            EditorUtility.DisplayDialog("Pizza Tycoon — Scene Build",
                "Cena montada!\n\nIMPORTANTE — ajuste manual da câmera:\n" +
                "1. Selecione 'Main Camera' na Hierarchy\n" +
                "2. No script IsometricCameraFollow, defina:\n" +
                "   Offset: (0, 10, -8)\n" +
                "   Pitch: 35   Yaw: 0\n" +
                "3. Salve a cena (Ctrl+S)\n" +
                "4. Execute PizzaTycoon > 5. Setup UI", "OK");
        }

        private static void ClearScene()
        {
            Selection.activeObject = null;
            Selection.objects      = new Object[0];

            string[] toRemove = {
                "[Terrain]", "[Stations]", "[Managers]", "Player",
                "CustomerSpawner", "Directional Light", "Fill Light",
                "SceneBootstrapper", "Ground", "Grass", "Road"
            };
            foreach (string n in toRemove)
            {
                GameObject g = GameObject.Find(n);
                if (g != null) { Selection.activeObject = null; Object.DestroyImmediate(g); }
            }
        }

        private static void CreateTerrain()
        {
            var root = new GameObject("[Terrain]");
            CreatePlane("Ground",   root.transform, new Vector3(0, -0.01f, 0),  new Vector3(20, 1, 20),   "Mat_Ground");
            CreatePlane("Grass",    root.transform, new Vector3(-12, 0, 0),     new Vector3(10, 1, 10),   "Mat_Grass");
            CreatePlane("Road",     root.transform, new Vector3(0, 0, 14),      new Vector3(4, 1, 20),    "Mat_Road");
            CreatePlane("Sidewalk", root.transform, new Vector3(3, 0.005f, 14), new Vector3(1.5f, 1, 20), "Mat_Station_Silver");
        }

        private static void CreatePlane(string name, Transform parent, Vector3 pos, Vector3 scale, string mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position   = pos;
            go.transform.localScale = scale / 10f;
            SetMaterial(go, mat);
            var mc = go.GetComponent<MeshCollider>();
            if (mc) Object.DestroyImmediate(mc);
            var bc = go.AddComponent<BoxCollider>();
            bc.size = new Vector3(10f, 0.05f, 10f);
        }

        private static void PlaceStations()
        {
            var root = new GameObject("[Stations]");
            var list = new (string path, Vector3 pos)[]
            {
                ("Assets/_Project/Prefabs/Stations/Station_WheatField.prefab", new Vector3(-10, 0, 5)),
                ("Assets/_Project/Prefabs/Stations/Station_Dough.prefab",      new Vector3( -4, 0, 2)),
                ("Assets/_Project/Prefabs/Stations/Station_Assembly.prefab",   new Vector3(  0, 0, 0)),
                ("Assets/_Project/Prefabs/Stations/Station_Oven.prefab",       new Vector3(  4, 0, 2)),
                ("Assets/_Project/Prefabs/Stations/Station_Delivery.prefab",   new Vector3(  6, 0, 8)),
            };
            foreach (var (path, pos) in list)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"Prefab nao encontrado: {path}");
                    var ph = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    ph.name = System.IO.Path.GetFileNameWithoutExtension(path) + "_PH";
                    ph.transform.SetParent(root.transform);
                    ph.transform.position   = pos + Vector3.up * 0.5f;
                    ph.transform.localScale = new Vector3(1.5f, 1, 1.5f);
                    SetMaterial(ph, "Mat_Station_Blue");
                    continue;
                }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                if (inst == null) { Debug.LogError($"Falha ao instanciar: {path}"); continue; }
                inst.transform.position = pos;
                if (path.Contains("Delivery")) inst.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
        }

        private static void PlacePlayer()
        {
            const string path = "Assets/_Project/Prefabs/Player/Player.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject player = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : null;
            if (player == null)
            {
                player     = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.tag = "Player";
            }
            player.name = "Player";
            player.transform.position = new Vector3(0, 0.5f, -2);
        }

        // NÃO sobrescreve position/rotation/offset/pitch/yaw da câmera.
        // O IsometricCameraFollow.Awake() aplica pitch+yaw sozinho.
        // Aqui só garantimos: câmera existe, configurações básicas, e target conectado.
        private static void SetupCamera()
        {
            var mainCam = UnityEngine.Camera.main;
            if (mainCam == null)
            {
                var camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                mainCam   = camGO.AddComponent<UnityEngine.Camera>();
                camGO.AddComponent<AudioListener>();
            }

            mainCam.orthographic     = true;
            mainCam.orthographicSize = 8f;
            mainCam.nearClipPlane    = 0.1f;
            mainCam.farClipPlane     = 100f;
            mainCam.backgroundColor  = new Color(0.53f, 0.81f, 0.98f);

            var follow = mainCam.GetComponent<PizzaTycoon.Camera.IsometricCameraFollow>()
                      ?? mainCam.gameObject.AddComponent<PizzaTycoon.Camera.IsometricCameraFollow>();

            if (follow == null) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var so      = new SerializedObject(follow);
                var pTarget = so.FindProperty("_target");
                if (pTarget != null)
                {
                    pTarget.objectReferenceValue = player.transform;
                    so.ApplyModifiedProperties();
                }
            }
        }

        private static void SetupLighting()
        {
            foreach (var l in Object.FindObjectsOfType<Light>())
                if (l != null && l.type == LightType.Directional)
                { Selection.activeObject = null; Object.DestroyImmediate(l.gameObject); }

            var dir = new GameObject("Directional Light");
            dir.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var dl = dir.AddComponent<Light>();
            dl.type = LightType.Directional; dl.intensity = 1.2f;
            dl.color = new Color(1f, 0.95f, 0.85f);

            var fill = new GameObject("Fill Light");
            fill.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
            var fl = fill.AddComponent<Light>();
            fl.type = LightType.Directional; fl.intensity = 0.4f;
            fl.color = new Color(0.7f, 0.8f, 1f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            ColorUtility.TryParseHtmlString("#FFE4B5", out Color amb);
            RenderSettings.ambientLight     = amb * 0.5f;
            RenderSettings.fog              = true;
            RenderSettings.fogMode          = FogMode.Linear;
            RenderSettings.fogColor         = new Color(0.85f, 0.9f, 1f);
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance   = 80f;
        }

        private static void SetupManagers()
        {
            var root = new GameObject("[Managers]");
            AddC<GameManager>(root);
            AddC<SaveManager>(root);
            AddC<AudioManager>(root);
            AddC<SceneLoader>(root);
            AddC<MoneyManager>(root);
            AddC<PizzaTycoon.Items.ItemPool>(root);
            AddC<PizzaTycoon.Economy.UpgradeManager>(root);
        }

        private static T AddC<T>(GameObject g) where T : Component
        {
            var c = g.AddComponent<T>();
            if (c == null) Debug.LogError($"[SceneBootstrapper] Falha AddComponent<{typeof(T).Name}>");
            return c;
        }

        private static void SetupCustomerSpawner()
        {
            var go = new GameObject("CustomerSpawner");
            go.transform.position = new Vector3(0, 0, 16);
            var spawner = go.AddComponent<Customers.CustomerSpawner>();
            if (spawner == null) { Debug.LogError("Falha CustomerSpawner"); return; }

            var sp = new GameObject("SpawnPoint");
            sp.transform.SetParent(go.transform);
            sp.transform.position = new Vector3(0, 0, 18);

            var so = new SerializedObject(spawner);
            var p1 = so.FindProperty("_spawnPoint");      if (p1 != null) p1.objectReferenceValue = sp.transform;
            var p2 = so.FindProperty("_spawnInterval");   if (p2 != null) p2.floatValue = 8f;
            var p3 = so.FindProperty("_initialPoolSize"); if (p3 != null) p3.intValue   = 10;
            so.ApplyModifiedProperties();

            var queue = Object.FindObjectOfType<Customers.CustomerQueue>();
            if (queue != null)
            {
                so.Update();
                var pq = so.FindProperty("_customerQueue");
                if (pq != null) pq.objectReferenceValue = queue;
                so.ApplyModifiedProperties();
            }
        }

        private static void SetMaterial(GameObject go, string matName)
        {
            if (go == null) return;
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/_Project/Materials/{matName}.mat");
            if (mat != null) r.sharedMaterial = mat;
        }

        private static bool ConfirmBuild() =>
            EditorUtility.DisplayDialog("Pizza Tycoon — Build Scene",
                "Isso ira montar automaticamente a GameScene.\n" +
                "Objetos gerados serao removidos e recriados.\n\n" +
                "Execute antes: 1. Create Materials e 2. Create Prefabs",
                "Montar Cena", "Cancelar");
#endif
    }
}