using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using PizzaTycoon.Map;
using PizzaTycoon.Customers;
using PizzaTycoon.Stations;
using PizzaTycoon.Camera;

namespace PizzaTycoon.Editor
{
    // MenuItems aditivos para enriquecer map_scene.unity SEM destruir o trabalho manual.
    // Cada item e idempotente: roda novamente apenas atualiza, nao duplica.
    public static class MapEnhancer
    {
        private const string DriveThruRootName    = "[DriveThru]";
        private const string NeighborhoodRootName = "[Neighborhood]";
        private const string RoadExtRootName      = "[RoadExtension]";
        private const string WheatVisRootName     = "[WheatFieldVisuals]";

        // ============ Aliases de nível raiz (sem /MAP/) ============

        [MenuItem("PizzaTycoon/Setup DriveThru",         priority = 1)]
        public static void SetupDriveThruRoot()       => SetupDriveThru();

        [MenuItem("PizzaTycoon/Setup Neighborhood",      priority = 2)]
        public static void SetupNeighborhoodRoot()    => SetupNeighborhood();

        [MenuItem("PizzaTycoon/Fix Station Positions",   priority = 3)]
        public static void FixStationPositionsRoot()  => FixStationPositions();

        [MenuItem("PizzaTycoon/Fix Player Setup",        priority = 4)]
        public static void FixPlayerSetupRoot()       => FixPlayerSetup();

        // ============ Master ============

        // Roda apenas as enhancements ADITIVAS — nao mexe nas posicoes ja feitas a mao.
        [MenuItem("PizzaTycoon/MAP/Run All (Safe — aditivo)", priority = 1)]
        public static void RunAll()
        {
            FixStationPositions();   // alinha stations com labels manuais
            SetupDriveThru();
            SetupNeighborhood();
            ExtendRoad();
            SetupWheatFieldVisuals();
            FixPlayerSetup();
            FixCustomerSpawn();
            FixCamera();
            FixOvenStation();
            MarkSceneDirty();
            Debug.Log("[MapEnhancer] Run All concluido (aditivo).");
        }

        // ============ Stations (alinha com Labels) ============

        [MenuItem("PizzaTycoon/MAP/Fix Station Positions", priority = 10)]
        public static void FixStationPositions()
        {
            // Os Labels_* sao filhos UI (RectTransform) das proprias estacoes — nao sao
            // marcadores independentes. Em vez disso, derivamos as posicoes-alvo a partir
            // das paredes reais do predio, colocando cada estacao em posicao logica dentro
            // do espaco interior.
            GameObject wallFL = FindByName("Wall_Front_Left");
            GameObject wallFR = FindByName("Wall_Front_Right");
            GameObject wallL  = FindByName("Wall_Left");
            GameObject wallR  = FindByName("Wall_Right");

            // Dimensoes reais do interior do predio.
            float frontZ  = -3f;
            float backZ   = 25f;
            float leftX   = -31.8f;
            float rightX  = -11.8f;
            float gapX    = -20.75f;  // centro do vao (janela drive-thru / porta)

            if (wallFL != null && wallFR != null)
            {
                frontZ = (wallFL.transform.position.z + wallFR.transform.position.z) * 0.5f;
                float eastOfLeft  = wallFL.transform.position.x + wallFL.transform.lossyScale.x * 0.5f;
                float westOfRight = wallFR.transform.position.x - wallFR.transform.lossyScale.x * 0.5f;
                gapX   = (eastOfLeft + westOfRight) * 0.5f;
                rightX = wallFR.transform.position.x + wallFR.transform.lossyScale.x * 0.5f;
                leftX  = wallFL.transform.position.x - wallFL.transform.lossyScale.x * 0.5f;
            }
            if (wallL != null)
            {
                leftX = wallL.transform.position.x - wallL.transform.lossyScale.x * 0.5f;
                backZ = wallL.transform.position.z + wallL.transform.lossyScale.z * 0.5f;
            }
            if (wallR != null)
                rightX = wallR.transform.position.x + wallR.transform.lossyScale.x * 0.5f;

            // Margens internas (afastar das paredes).
            float margin = 2f;
            float nearX  = leftX  + margin;   // faixa oeste interna
            float farX   = rightX - margin;   // faixa leste interna
            float nearZ  = frontZ + margin;   // faixa sul interna (perto da entrada)
            float midZ   = frontZ + (backZ - frontZ) * 0.5f;
            float deepZ  = backZ  - margin;   // faixa norte interna (fundo)

            // Posicoes-alvo para cada estacao dentro do predio.
            // Fluxo: colheita(fundo-esq) -> massa(fundo-dir) -> montagem(meio-dir) ->
            //        forno(meio-esq) -> balcao(frente-centro)
            var targets = new (string stationName, Vector3 pos)[]
            {
                ("Station_WheatField", new Vector3(nearX, 0f, deepZ)),
                ("Station_Dough",      new Vector3(farX,  0f, deepZ)),
                ("Station_Assembly",   new Vector3(farX,  0f, midZ)),
                ("Station_Oven",       new Vector3(nearX, 0f, midZ)),
                ("Station_Delivery",   new Vector3(gapX,  0f, nearZ)),
            };

            int moved = 0;
            foreach (var entry in targets)
            {
                GameObject station = FindByName(entry.stationName);
                if (station == null) continue;

                Undo.RegisterFullObjectHierarchyUndo(station, "Move Station");

                // Cache mundial dos filhos para preservar posicoes de anchors/visuals.
                var childTransforms = station.GetComponentsInChildren<Transform>(true);
                var cachedPositions = new Dictionary<Transform, (Vector3 pos, Quaternion rot)>();
                foreach (Transform t in childTransforms)
                {
                    if (t == station.transform) continue;
                    cachedPositions[t] = (t.position, t.rotation);
                }

                station.transform.position = entry.pos;

                foreach (var kv in cachedPositions)
                {
                    if (kv.Key == null) continue;
                    kv.Key.position = kv.Value.pos;
                    kv.Key.rotation = kv.Value.rot;
                }

                moved++;
                EnsureStationTrigger(station);
            }

            // Garante BoxCollider em mesas Synty para o player nao atravessar.
            int tableCols = 0;
            foreach (GameObject root in GetSceneRoots())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    string n = t.gameObject.name.ToLowerInvariant();
                    if (!n.Contains("table") && !n.Contains("dining_table")) continue;
                    if (t.GetComponent<Collider>() != null) continue;
                    var col = t.gameObject.AddComponent<BoxCollider>();
                    col.size   = new Vector3(1.2f, 1f, 1.2f);
                    col.center = new Vector3(0f, 0.5f, 0f);
                    tableCols++;
                }
            }

            Debug.Log($"[MapEnhancer] Fix Station Positions: {moved} estacoes movidas para o interior | {tableCols} colliders em mesas.");
            MarkSceneDirty();
        }

        private static void EnsureStationTrigger(GameObject station)
        {
            BoxCollider box = station.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = station.AddComponent<BoxCollider>();
                box.size   = new Vector3(3f, 2f, 3f);
                box.center = new Vector3(0f, 1f, 0f);
            }
            box.isTrigger = true;
        }

        // ============ DriveThru ============

        [MenuItem("PizzaTycoon/MAP/Setup DriveThru", priority = 20)]
        public static void SetupDriveThru()
        {
            GameObject root = FindByName(DriveThruRootName);
            if (root == null)
            {
                root = new GameObject(DriveThruRootName);
                Undo.RegisterCreatedObjectUndo(root, "Create DriveThru Root");
            }
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;

            // Lê a geometria real das paredes frontais para colocar a faixa FORA do prédio.
            // Wall_Front_Left  = parede esquerda da fachada (oeste)
            // Wall_Front_Right = parede direita da fachada (leste, do lado da rua)
            // O vão entre elas é a janela do drive-thru.
            GameObject wallFL = FindByName("Wall_Front_Left");
            GameObject wallFR = FindByName("Wall_Front_Right");
            GameObject wallL  = FindByName("Wall_Left");

            // Valores de fallback baseados na geometria conhecida do projeto.
            float frontZ     = -3f;
            float gapCenterX = -20.75f;
            float rightEdgeX = -11.8f;   // borda leste do prédio (lado da rua)
            float westEdgeX  = -31.7f;   // borda oeste do prédio

            if (wallFL != null && wallFR != null)
            {
                frontZ = (wallFL.transform.position.z + wallFR.transform.position.z) * 0.5f;

                // Borda do vão: leste da parede esquerda, oeste da parede direita.
                float eastOfLeft  = wallFL.transform.position.x + wallFL.transform.lossyScale.x * 0.5f;
                float westOfRight = wallFR.transform.position.x - wallFR.transform.lossyScale.x * 0.5f;
                gapCenterX = (eastOfLeft + westOfRight) * 0.5f;

                // Borda leste = leste da parede direita (entrada da rua).
                rightEdgeX = wallFR.transform.position.x + wallFR.transform.lossyScale.x * 0.5f;
                // Borda oeste = oeste da parede esquerda.
                westEdgeX  = wallFL.transform.position.x - wallFL.transform.lossyScale.x * 0.5f;
            }
            else if (wallL != null)
            {
                westEdgeX = wallL.transform.position.x - wallL.transform.lossyScale.x * 0.5f;
            }

            // Faixa do drive-thru: paralela à fachada, 3 u na frente (Z negativo = sul).
            // Mantemos 3u de distância para carros não encostarem na parede.
            // O player sai pela porta (gap) e fica a ~3-4u do OrderPoint — dentro do
            // _deliveryRadius aumentado para 5.5 abaixo.
            float laneZ = frontZ - 3f;
            float y     = 0.05f;

            Transform spawn = EnsureChild(root.transform, "SpawnPoint",  new Vector3(rightEdgeX + 5f,      y, laneZ));
            Transform q2    = EnsureChild(root.transform, "Queue_2",     new Vector3(gapCenterX  + 8f,      y, laneZ));
            Transform q1    = EnsureChild(root.transform, "Queue_1",     new Vector3(gapCenterX  + 4f,      y, laneZ));
            Transform order = EnsureChild(root.transform, "OrderPoint",  new Vector3(gapCenterX,            y, laneZ));
            Transform exit  = EnsureChild(root.transform, "ExitPoint",   new Vector3(westEdgeX   - 6f,      y, laneZ));

            DriveThruSystem sys = root.GetComponent<DriveThruSystem>();
            if (sys == null) sys = Undo.AddComponent<DriveThruSystem>(root);

            GameObject[] cars = LoadCarPrefabs();

            var so = new SerializedObject(sys);
            so.FindProperty("_spawnPoint").objectReferenceValue   = spawn;
            so.FindProperty("_orderPoint").objectReferenceValue   = order;
            so.FindProperty("_exitPoint").objectReferenceValue    = exit;
            var queueProp = so.FindProperty("_queuePositions");
            queueProp.arraySize = 2;
            queueProp.GetArrayElementAtIndex(0).objectReferenceValue = q1;
            queueProp.GetArrayElementAtIndex(1).objectReferenceValue = q2;

            var prefabsProp = so.FindProperty("_carPrefabs");
            prefabsProp.arraySize = cars.Length;
            for (int i = 0; i < cars.Length; i++)
                prefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = cars[i];

            // Raio de entrega maior: player sai pela porta e fica a ~3-4 u do carro.
            var radiusProp = so.FindProperty("_deliveryRadius");
            if (radiusProp != null) radiusProp.floatValue = 5.5f;

            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[MapEnhancer] DriveThru: faixa Z={laneZ:F1} | janela X={gapCenterX:F1} | spawn X={rightEdgeX+5f:F1} | saida X={westEdgeX-6f:F1} | raio entrega=5.5 | {cars.Length} carros.");
            MarkSceneDirty();
        }

        private static GameObject[] LoadCarPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Awb-Free Low Poly Vehicles/Prefabs" });
            var list = new List<GameObject>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string lower = path.ToLowerInvariant();
                if (lower.Contains("air plane") || lower.Contains("monster")) continue;
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null) list.Add(go);
            }
            return list.ToArray();
        }

        // ============ Neighborhood ============

        [MenuItem("PizzaTycoon/MAP/Setup Neighborhood", priority = 30)]
        public static void SetupNeighborhood()
        {
            GameObject root = FindByName(NeighborhoodRootName);
            if (root == null)
            {
                root = new GameObject(NeighborhoodRootName);
                Undo.RegisterCreatedObjectUndo(root, "Create Neighborhood Root");
            }
            root.transform.position = Vector3.zero;

            // Limpa filhos para idempotencia.
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

            // Localiza estrada existente.
            GameObject street = FindByName("[Street]");
            Vector3 streetCenter = street != null ? street.transform.position : new Vector3(0f, 0f, -10f);

            // Pizzaria principal centrada perto de (0..15, *, 0..30). Estrada na frente (Z negativo).
            // Cria prédios no lado oposto da estrada (Z mais negativo).
            float zFar = streetCenter.z - 18f;

            string[] candidates = new[]
            {
                "SM_Bld_Base_Floor_Combined_01",
                "SM_Bld_Base_Wall_01",
                "SM_Bld_Base_Wall_Window_01",
            };

            // Tenta usar prefabs Synty; se nao encontrar nenhum util, cai para cubos brancos.
            // Para simplicidade, sempre usa cubos altos coloridos — fica visualmente confiavel.
            float[] heights = { 4f, 6f, 5f, 7f, 4.5f, 6.5f };
            for (int i = 0; i < 7; i++)
            {
                float x = -25f + i * 8f;
                float h = heights[i % heights.Length];
                GameObject bld = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bld.name = $"Building_{i}";
                bld.transform.SetParent(root.transform, false);
                bld.transform.position = new Vector3(x, h * 0.5f, zFar);
                bld.transform.localScale = new Vector3(6f, h, 5f);
                ApplyColor(bld, new Color(0.92f, 0.92f, 0.88f));

                // janelas
                int windowsPerFloor = 3;
                int floors = Mathf.Max(1, Mathf.FloorToInt(h / 2f));
                for (int f = 0; f < floors; f++)
                {
                    for (int w = 0; w < windowsPerFloor; w++)
                    {
                        GameObject win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        win.name = "Window";
                        win.transform.SetParent(bld.transform, false);
                        win.transform.localScale = new Vector3(0.18f, 0.18f, 1.05f);
                        float wx = -0.33f + w * 0.33f;
                        float wy = -0.4f + f * 0.25f;
                        win.transform.localPosition = new Vector3(wx, wy, 0.45f);
                        ApplyColor(win, new Color(0.45f, 0.7f, 0.95f, 1f));
                        Object.DestroyImmediate(win.GetComponent<Collider>());
                    }
                }
            }

            // Postes de luz a cada ~10u na beira da rua.
            for (int i = 0; i < 6; i++)
            {
                float x = -25f + i * 10f;
                GameObject lamp = new GameObject($"Lamp_{i}");
                lamp.transform.SetParent(root.transform, false);
                lamp.transform.position = new Vector3(x, 0f, streetCenter.z - 3f);

                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = "Post";
                post.transform.SetParent(lamp.transform, false);
                post.transform.localPosition = new Vector3(0f, 2f, 0f);
                post.transform.localScale    = new Vector3(0.15f, 2f, 0.15f);
                ApplyColor(post, new Color(0.3f, 0.3f, 0.3f));

                GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bulb.name = "Bulb";
                bulb.transform.SetParent(lamp.transform, false);
                bulb.transform.localPosition = new Vector3(0f, 4f, 0f);
                bulb.transform.localScale    = new Vector3(0.4f, 0.4f, 0.4f);
                ApplyColor(bulb, new Color(1f, 0.95f, 0.5f));
                Object.DestroyImmediate(bulb.GetComponent<Collider>());

                Light l = bulb.AddComponent<Light>();
                l.type      = LightType.Point;
                l.range     = 10f;
                l.intensity = 1.2f;
                l.color     = new Color(1f, 0.92f, 0.6f);
            }

            // Carros estaticos de decoracao na faixa oposta.
            GameObject[] cars = LoadCarPrefabs();
            if (cars.Length > 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    GameObject src = cars[i % cars.Length];
                    GameObject go  = (GameObject)PrefabUtility.InstantiatePrefab(src);
                    go.name = $"DecorCar_{i}";
                    go.transform.SetParent(root.transform, false);
                    go.transform.position = new Vector3(-15f + i * 14f, 0f, streetCenter.z - 8f);
                    go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                }
            }

            Debug.Log("[MapEnhancer] Neighborhood criado.");
            MarkSceneDirty();
        }

        private static void ApplyColor(GameObject go, Color c)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r == null) return;
            // Usa shared material para nao instanciar 1 material por cubo,
            // mas precisamos colorir individualmente: cria material instance.
            Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.color = c;
            r.sharedMaterial = m;
        }

        // ============ Customer Spawn na porta ============

        [MenuItem("PizzaTycoon/MAP/Fix Customer Spawn", priority = 40)]
        public static void FixCustomerSpawn()
        {
            CustomerSpawner spawner = Object.FindObjectOfType<CustomerSpawner>();
            if (spawner == null)
            {
                Debug.LogWarning("[MapEnhancer] CustomerSpawner nao encontrado.");
                return;
            }

            // Usa as paredes frontais reais para calcular posição da porta.
            GameObject wallFL = FindByName("Wall_Front_Left");
            GameObject wallFR = FindByName("Wall_Front_Right");

            float frontZ     = -3f;
            float gapCenterX = -20.75f;

            if (wallFL != null && wallFR != null)
            {
                frontZ = (wallFL.transform.position.z + wallFR.transform.position.z) * 0.5f;
                float eastOfLeft  = wallFL.transform.position.x + wallFL.transform.lossyScale.x * 0.5f;
                float westOfRight = wallFR.transform.position.x - wallFR.transform.lossyScale.x * 0.5f;
                gapCenterX = (eastOfLeft + westOfRight) * 0.5f;
            }

            // Spawner fica do lado de fora da porta.
            spawner.transform.position = new Vector3(gapCenterX, 0f, frontZ - 3f);

            var sso = new SerializedObject(spawner);
            var spawnIntervalProp = sso.FindProperty("_spawnInterval");
            if (spawnIntervalProp != null) spawnIntervalProp.floatValue = 8f;
            sso.ApplyModifiedPropertiesWithoutUndo();

            // Configura CustomerQueue: move o objeto para dentro do prédio e
            // cria o QueueStart que CustomerQueue.RepositionQueue() precisa para
            // posicionar os clientes corretamente (sem QueueStart eles ficam parados
            // no spawner sem ocupar posicoes de fila).
            GameObject queueGo = FindByName("CustomerQueue");
            if (queueGo != null)
            {
                CustomerQueue cq = queueGo.GetComponent<CustomerQueue>();

                // Objeto da fila fica alinhado à porta, levemente dentro do prédio.
                queueGo.transform.position = new Vector3(gapCenterX, 0f, frontZ + 2f);
                queueGo.transform.rotation = Quaternion.identity;

                // Cria (ou reutiliza) filho QueueStart — primeiro slot da fila.
                Transform qsTransform = queueGo.transform.Find("QueueStart");
                if (qsTransform == null)
                {
                    var qsGo = new GameObject("QueueStart");
                    Undo.RegisterCreatedObjectUndo(qsGo, "Create QueueStart");
                    qsGo.transform.SetParent(queueGo.transform, true);
                    qsTransform = qsGo.transform;
                }

                // QueueStart olha para o norte (+Z = dentro do prédio / balcão).
                // RepositionQueue posiciona clientes em: queueStart.pos - forward * i * spacing
                // Com forward = +Z: clientes ficam em Z decrescente (fila vai para o sul/porta).
                // Slot 0: na porta. Slot 1: 1.2u atrás. Slot 2: 2.4u fora etc.
                qsTransform.position = new Vector3(gapCenterX, 0f, frontZ + 1f);
                qsTransform.rotation = Quaternion.Euler(0f, 0f, 0f); // forward = +Z

                if (cq != null)
                {
                    var qso = new SerializedObject(cq);
                    var maxProp   = qso.FindProperty("_maxQueueSize");
                    var startProp = qso.FindProperty("_queueStart");
                    if (maxProp   != null) maxProp.intValue                = 4;
                    if (startProp != null) startProp.objectReferenceValue  = qsTransform;
                    qso.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            Debug.Log($"[MapEnhancer] CustomerSpawner: porta X={gapCenterX:F1} Z={frontZ-3f:F1} | QueueStart: X={gapCenterX:F1} Z={frontZ+1f:F1} | intervalo=8s, fila=4.");
            MarkSceneDirty();
        }

        // ============ Player Setup ============

        [MenuItem("PizzaTycoon/MAP/Fix Player Setup", priority = 50)]
        public static void FixPlayerSetup()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = FindByName("Player");
            if (player == null)
            {
                Debug.LogWarning("[MapEnhancer] Player nao encontrado na cena.");
                return;
            }

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb == null) rb = player.AddComponent<Rigidbody>();
            rb.constraints   = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.useGravity = true;

            CapsuleCollider cap = player.GetComponent<CapsuleCollider>();
            if (cap == null) cap = player.AddComponent<CapsuleCollider>();
            cap.radius = 0.3f;
            cap.height = 1.8f;
            cap.center = new Vector3(0f, 0.9f, 0f);

            Debug.Log("[MapEnhancer] Player setup OK (Rigidbody constraints + CapsuleCollider 0.3 x 1.8).");
            MarkSceneDirty();
        }

        // ============ Extensão da Pista ============

        [MenuItem("PizzaTycoon/MAP/Extend Road", priority = 60)]
        public static void ExtendRoad()
        {
            // Reutiliza ou cria raiz de extensão.
            GameObject root = FindByName(RoadExtRootName);
            if (root == null)
            {
                root = new GameObject(RoadExtRootName);
                Undo.RegisterCreatedObjectUndo(root, "Create RoadExtension Root");
            }
            else
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            }

            // Descobre os limites da rua atual.
            GameObject street    = FindByName("[Street]");
            Bounds     roadBounds = ComputeGroupBounds(street);

            // Se não achou nada útil, usa estimativa a partir da posição da câmera.
            if (roadBounds.size == Vector3.zero)
            {
                roadBounds.center = new Vector3(-8f, 0f, 15f);
                roadBounds.size   = new Vector3(9f,  0.1f, 50f);
            }

            float roadCenterX = roadBounds.center.x;
            float roadWidth   = roadBounds.size.x;
            float roadMinZ    = roadBounds.min.z;
            float roadMaxZ    = roadBounds.max.z;
            float extLen      = 30f;

            Color roadCol     = new Color(0.28f, 0.28f, 0.28f);
            Color sideCol     = new Color(0.70f, 0.70f, 0.70f);
            Color lineCol     = new Color(0.95f, 0.90f, 0.20f);

            // Extensão SUL (Z-) e extensão NORTE (Z+).
            Vector3 southCenter = new Vector3(roadCenterX, 0f, roadMinZ - extLen * 0.5f);
            Vector3 northCenter = new Vector3(roadCenterX, 0f, roadMaxZ + extLen * 0.5f);

            CreatePlane("Road_ExtSouth", root.transform, southCenter, new Vector3(roadWidth / 10f, 1f, extLen / 10f), roadCol);
            CreatePlane("Road_ExtNorth", root.transform, northCenter, new Vector3(roadWidth / 10f, 1f, extLen / 10f), roadCol);

            // Calçadas dos dois lados (estende a calçada existente).
            float sideW    = 3f;
            float leftX    = roadCenterX - roadWidth * 0.5f - sideW * 0.5f;
            float rightX   = roadCenterX + roadWidth * 0.5f + sideW * 0.5f;
            foreach (float sideX in new[] { leftX, rightX })
            {
                CreatePlane("Sidewalk_ExtSouth", root.transform,
                    new Vector3(sideX, 0.01f, roadMinZ - extLen * 0.5f),
                    new Vector3(sideW / 10f, 1f, extLen / 10f), sideCol);
                CreatePlane("Sidewalk_ExtNorth", root.transform,
                    new Vector3(sideX, 0.01f, roadMaxZ + extLen * 0.5f),
                    new Vector3(sideW / 10f, 1f, extLen / 10f), sideCol);
            }

            // Faixas amarelas contínuas (extensão sul).
            for (int i = 0; i < 5; i++)
            {
                float z = roadMinZ - 3f - i * 5.5f;
                CreateBox($"DashS_{i}", root.transform,
                    new Vector3(roadCenterX, 0.02f, z),
                    new Vector3(0.15f, 0.02f, 2f), lineCol);
            }
            // Faixas amarelas (extensão norte).
            for (int i = 0; i < 5; i++)
            {
                float z = roadMaxZ + 3f + i * 5.5f;
                CreateBox($"DashN_{i}", root.transform,
                    new Vector3(roadCenterX, 0.02f, z),
                    new Vector3(0.15f, 0.02f, 2f), lineCol);
            }

            // Carros estáticos decorativos nas extensões.
            GameObject[] cars = LoadCarPrefabs();
            float[] decoZ     = { roadMinZ - 8f, roadMinZ - 18f, roadMaxZ + 8f };
            for (int i = 0; i < decoZ.Length; i++)
            {
                if (cars.Length > 0)
                {
                    var carSrc = cars[i % cars.Length];
                    var go     = (GameObject)PrefabUtility.InstantiatePrefab(carSrc);
                    go.name    = $"StaticCar_Ext_{i}";
                    go.transform.SetParent(root.transform, false);
                    go.transform.position = new Vector3(roadCenterX + 1.5f, 0f, decoZ[i]);
                    go.transform.rotation = Quaternion.Euler(0f, i % 2 == 0 ? 0f : 180f, 0f);
                }
                else
                {
                    CreateBox($"StaticCar_Ext_{i}", root.transform,
                        new Vector3(roadCenterX + 1.5f, 0.55f, decoZ[i]),
                        new Vector3(1.8f, 1.1f, 3.8f), new Color(0.3f, 0.5f, 0.8f));
                }
            }

            Debug.Log($"[MapEnhancer] ExtendRoad: +30u sul/norte em Z=[{roadMinZ:F1}, {roadMaxZ:F1}] centrado X={roadCenterX:F1}.");
            MarkSceneDirty();
        }

        // Calcula os bounds do grupo varrendo todos os Renderer filhos.
        private static Bounds ComputeGroupBounds(GameObject groupRoot)
        {
            if (groupRoot == null) return new Bounds();
            var renderers = groupRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds();
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }

        // ============ Visual do Campo de Trigo ============

        [MenuItem("PizzaTycoon/MAP/Setup Wheat Field Visuals", priority = 65)]
        public static void SetupWheatFieldVisuals()
        {
            // Reutiliza ou cria raiz.
            GameObject root = FindByName(WheatVisRootName);
            if (root == null)
            {
                root = new GameObject(WheatVisRootName);
                Undo.RegisterCreatedObjectUndo(root, "Create WheatFieldVisuals Root");
            }
            else
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            }

            // Encontra o WheatFieldStation para ancorar a posição.
            GameObject wheatStation = FindByName("Station_WheatField");
            // Encontra também o Label_TRIGO como referência alternativa.
            GameObject labelTrigo   = FindByName("Label_TRIGO");

            Vector3 fieldCenter;
            if (labelTrigo != null)
                fieldCenter = labelTrigo.transform.position;
            else if (wheatStation != null)
                fieldCenter = wheatStation.transform.position;
            else
            {
                Debug.LogWarning("[MapEnhancer] WheatFieldStation e Label_TRIGO nao encontrados — usando posicao padrao.");
                fieldCenter = new Vector3(1f, 0f, 2f);
            }

            // Força Y=0 para ficar no chão.
            fieldCenter.y = 0f;

            float fieldW = 8f;
            float fieldD = 8f;

            // Plano verde-amarelado cobrindo ~40% da area de plantacao.
            CreatePlane("WheatField_Ground", root.transform,
                fieldCenter + new Vector3(0f, 0.005f, 0f),
                new Vector3(fieldW / 10f, 1f, fieldD / 10f),
                new Color(0.61f, 0.76f, 0.25f));

            // Cerca ao redor (postes espaçados de 1 unidade).
            Color fenceColor = new Color(0.545f, 0.416f, 0.078f); // #8B6A14
            float hw = fieldW * 0.5f;
            float hd = fieldD * 0.5f;

            for (float x = -hw; x <= hw + 0.1f; x += 2f)
            {
                CreateBox($"Fence_N_{x:F0}", root.transform,
                    fieldCenter + new Vector3(x, 0.5f, hd),
                    new Vector3(0.1f, 1f, 0.1f), fenceColor);
                CreateBox($"Fence_S_{x:F0}", root.transform,
                    fieldCenter + new Vector3(x, 0.5f, -hd),
                    new Vector3(0.1f, 1f, 0.1f), fenceColor);
            }
            for (float z = -hd + 2f; z <= hd - 1.9f; z += 2f)
            {
                CreateBox($"Fence_E_{z:F0}", root.transform,
                    fieldCenter + new Vector3(hw, 0.5f, z),
                    new Vector3(0.1f, 1f, 0.1f), fenceColor);
                CreateBox($"Fence_W_{z:F0}", root.transform,
                    fieldCenter + new Vector3(-hw, 0.5f, z),
                    new Vector3(0.1f, 1f, 0.1f), fenceColor);
            }

            // 6 plantas de trigo (esferas verdes-amarelas) distribuídas no campo.
            Color plantColor = new Color(0.45f, 0.72f, 0.10f);
            var plantOffsets = new Vector3[]
            {
                new Vector3(-2.5f, 0f, -2.5f), new Vector3( 0f, 0f, -2.5f), new Vector3( 2.5f, 0f, -2.5f),
                new Vector3(-2.5f, 0f,  1f),   new Vector3( 0f, 0f,  1f),   new Vector3( 2.5f, 0f,  2f),
            };
            foreach (var off in plantOffsets)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                p.name = "WheatPlant";
                p.transform.SetParent(root.transform);
                p.transform.position   = fieldCenter + off + Vector3.up * 0.2f;
                p.transform.localScale = Vector3.one * 0.4f;
                Object.DestroyImmediate(p.GetComponent<SphereCollider>());
                ApplyColor(p, plantColor);
            }

            Debug.Log($"[MapEnhancer] SetupWheatFieldVisuals: campo + cerca + 6 plantas em {fieldCenter}.");
            MarkSceneDirty();
        }

        // ============ Câmera ============

        [MenuItem("PizzaTycoon/MAP/Fix Camera", priority = 70)]
        public static void FixCamera()
        {
            IsometricCameraFollow cam = Object.FindObjectOfType<IsometricCameraFollow>();
            if (cam == null)
            {
                Debug.LogWarning("[MapEnhancer] IsometricCameraFollow nao encontrado na cena.");
                return;
            }

            var so = new SerializedObject(cam);

            // Offset pedido: (0, 14, -10) → height=14, zOffset=-10, xOffset=0.
            var height    = so.FindProperty("_height");
            var zOff      = so.FindProperty("_zOffset");
            var xOff      = so.FindProperty("_xOffset");
            var pitch     = so.FindProperty("_pitch");
            var smoothT   = so.FindProperty("_smoothTime");
            var target    = so.FindProperty("_target");

            if (height    != null) height.floatValue    = 14f;
            if (zOff      != null) zOff.floatValue      = -10f;
            if (xOff      != null) xOff.floatValue      = 0f;
            if (pitch     != null) pitch.floatValue      = 50f;
            if (smoothT   != null) smoothT.floatValue    = 0.2f;  // 1/smoothSpeed ≈ 1/5

            // Conecta o player se ainda não estiver configurado.
            if (target != null && target.objectReferenceValue == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player") ?? FindByName("Player");
                if (player != null)
                    target.objectReferenceValue = player.transform;
            }

            so.ApplyModifiedProperties();
            Debug.Log("[MapEnhancer] Camera: height=14, zOffset=-10, pitch=50, smoothTime=0.2.");
            MarkSceneDirty();
        }

        // ============ Forno ============

        [MenuItem("PizzaTycoon/MAP/Fix Oven Station", priority = 75)]
        public static void FixOvenStation()
        {
            OvenStation oven = Object.FindObjectOfType<OvenStation>();
            if (oven == null)
            {
                Debug.LogWarning("[MapEnhancer] OvenStation nao encontrada.");
                return;
            }
            var so = new SerializedObject(oven);
            var ct = so.FindProperty("_cookingTime");
            var sl = so.FindProperty("_ovenSlots");
            if (ct != null) ct.floatValue = 4f;
            if (sl != null) sl.intValue   = 2;
            so.ApplyModifiedProperties();
            Debug.Log("[MapEnhancer] OvenStation: cookingTime=4, ovenSlots=2.");
            MarkSceneDirty();
        }

        // ============ Util ============

        private static GameObject FindByName(string n)
        {
            foreach (GameObject go in GetAllSceneGameObjects())
            {
                if (go != null && go.name == n) return go;
            }
            return null;
        }

        private static IEnumerable<GameObject> GetAllSceneGameObjects()
        {
            var scene = EditorSceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                yield return root;
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    yield return t.gameObject;
            }
        }

        private static IEnumerable<GameObject> GetSceneRoots()
        {
            return EditorSceneManager.GetActiveScene().GetRootGameObjects();
        }

        private static Transform EnsureChild(Transform parent, string name, Vector3 worldPos)
        {
            Transform t = parent.Find(name);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, true);
                Undo.RegisterCreatedObjectUndo(go, "Create DriveThru waypoint");
            }
            else
            {
                go = t.gameObject;
            }
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.identity;
            return go.transform;
        }

        private static void MarkSceneDirty()
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        // ── Primitivas coloridas ─────────────────────────────────────────────

        private static void CreatePlane(string name, Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position   = pos;
            go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            ApplyColor(go, color);
        }

        private static void CreateBox(string name, Transform parent, Vector3 pos, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position   = pos;
            go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            ApplyColor(go, color);
        }
    }
}
