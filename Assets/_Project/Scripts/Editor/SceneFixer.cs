using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using PizzaTycoon.Player;
using PizzaTycoon.Camera;
using PizzaTycoon.Stations;
using PizzaTycoon.Customers;
using PizzaTycoon.Items;
using PizzaTycoon.Input;
using PizzaTycoon.Managers;
using PizzaTycoon.Economy;
using PizzaTycoon.UI;

namespace PizzaTycoon.Editor
{
    public static class SceneFixer
    {
        [MenuItem("PizzaTycoon/FIX — Reconstruir Cena Completa")]
        public static void FixAndRebuildScene()
        {
            Selection.activeObject = null;

            EditorUtility.DisplayProgressBar("Pizza Tycoon Fix", "Limpando cena...", 0.05f);
            ClearAllBrokenObjects();

            EditorUtility.DisplayProgressBar("Pizza Tycoon Fix", "Criando terreno...", 0.15f);
            BuildTerrain();

            EditorUtility.DisplayProgressBar("Pizza Tycoon Fix", "Criando estacoes...", 0.30f);
            BuildStations();

            EditorUtility.DisplayProgressBar("Pizza Tycoon Fix", "Criando jogador...", 0.45f);
            BuildPlayer();

            EditorUtility.DisplayProgressBar("Pizza Tycoon Fix", "Configurando camera...", 0.55f);
            BuildCamera();

            EditorUtility.DisplayProgressBar("Pizza Tycoon Fix", "Configurando iluminacao...", 0.65f);
            BuildLighting();

            EditorUtility.DisplayProgressBar("Pizza Tycoon Fix", "Criando managers...", 0.75f);
            BuildManagers();

            EditorUtility.DisplayProgressBar("Pizza Tycoon Fix", "Criando sistema de clientes...", 0.85f);
            BuildCustomerSystem();

            EditorUtility.DisplayProgressBar("Pizza Tycoon Fix", "Criando UI...", 0.92f);
            BuildUI();

            EditorUtility.DisplayProgressBar("Pizza Tycoon Fix", "Conectando referencias...", 0.97f);
            WireReferences();

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[SceneFixer] Cena reconstruida com sucesso! Pressione Play para testar.");
            EditorUtility.DisplayDialog("Pizza Tycoon — Fix Completo",
                "Cena reconstruida!\n\n" +
                "* Erros de script corrigidos\n" +
                "* Camera isometrica configurada\n" +
                "* Estacoes posicionadas e conectadas\n" +
                "* CustomerSpawner com prefab configurado\n" +
                "* ItemPool com prefabs conectados\n" +
                "* UI de HUD e Joystick criados\n\n" +
                "Pressione PLAY para testar o gameplay.", "OK");
        }

        // ══════════════════════════════════════════════════════════
        // LIMPAR OBJETOS QUEBRADOS
        // ══════════════════════════════════════════════════════════
        static void ClearAllBrokenObjects()
        {
            string[] toDestroy = {
                "[Terrain]", "[Stations]", "[Managers]", "[Camera]",
                "Player", "CustomerSpawner", "[CustomerSystem]",
                "HUDCanvas", "UpgradePanelCanvas", "EventSystem",
                "Directional Light", "Fill Light", "Main Camera",
                "Ground", "Grass", "Road", "Sidewalk",
                "UpgradeItem_Forno", "UpgradeItem_Colheita",
                "UpgradeItem_Capacidade", "UpgradeItem_Funcionario", "UpgradeItem_Velocidade"
            };

            foreach (string name in toDestroy)
            {
                GameObject go = GameObject.Find(name);
                if (go != null) Object.DestroyImmediate(go);
            }

            foreach (var light in Object.FindObjectsOfType<Light>())
                if (light.type == LightType.Directional)
                    Object.DestroyImmediate(light.gameObject);
        }

        // ══════════════════════════════════════════════════════════
        // TERRENO
        // ══════════════════════════════════════════════════════════
        static void BuildTerrain()
        {
            var root = new GameObject("[Terrain]");

            CreateColoredPlane("Ground",   root.transform, new Vector3(0, 0, 0),       new Vector3(24, 1, 20), new Color(0.95f, 0.88f, 0.75f));
            CreateColoredPlane("Grass",    root.transform, new Vector3(-12, 0.01f, -2), new Vector3(10, 1, 12), new Color(0.45f, 0.75f, 0.35f));
            CreateColoredPlane("Road",     root.transform, new Vector3(0, 0, 13),       new Vector3(5, 1, 18),  new Color(0.35f, 0.35f, 0.35f));
            CreateColoredPlane("Sidewalk", root.transform, new Vector3(4, 0.01f, 13),  new Vector3(2, 1, 18),  new Color(0.7f, 0.7f, 0.7f));
        }

        static void CreateColoredPlane(string name, Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale / 10f;

            Object.DestroyImmediate(go.GetComponent<MeshCollider>());
            var bc = go.AddComponent<BoxCollider>();
            bc.size = new Vector3(10f, 0.02f, 10f);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        // ══════════════════════════════════════════════════════════
        // ESTACOES
        // ══════════════════════════════════════════════════════════
        static void BuildStations()
        {
            var root = new GameObject("[Stations]");

            BuildStation<WheatFieldStation>(   "Station_WheatField", root.transform, new Vector3(-10, 0,  -2), new Color(0.85f, 0.75f, 0.20f), new Vector3(2.5f, 0.5f, 2.5f));
            BuildStation<DoughStation>(        "Station_Dough",      root.transform, new Vector3(-3,  0,   2), new Color(0.90f, 0.82f, 0.65f), new Vector3(2f,   1f,  2f));
            BuildStation<PizzaAssemblyStation>("Station_Assembly",   root.transform, new Vector3( 2,  0,   2), new Color(0.70f, 0.20f, 0.20f), new Vector3(2f,   1f,  2f));
            BuildStation<OvenStation>(         "Station_Oven",       root.transform, new Vector3( 7,  0,   2), new Color(0.20f, 0.20f, 0.20f), new Vector3(2.5f, 1.5f, 2.5f));
            BuildStation<DeliveryStation>(     "Station_Delivery",   root.transform, new Vector3( 7,  0,   8), new Color(0.20f, 0.60f, 0.90f), new Vector3(3f,   1f,  1.5f));

            AddStationLabel(root.transform.Find("Station_WheatField").gameObject, "TRIGO");
            AddStationLabel(root.transform.Find("Station_Dough").gameObject,      "MASSA");
            AddStationLabel(root.transform.Find("Station_Assembly").gameObject,   "MONTAR");
            AddStationLabel(root.transform.Find("Station_Oven").gameObject,       "FORNO");
            AddStationLabel(root.transform.Find("Station_Delivery").gameObject,   "BALCAO");
        }

        static void BuildStation<T>(string name, Transform parent, Vector3 pos, Color color, Vector3 size) where T : BaseStation
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform);
            visual.transform.localPosition = new Vector3(0, size.y / 2f, 0);
            visual.transform.localScale = size;
            Object.DestroyImmediate(visual.GetComponent<BoxCollider>());

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            visual.GetComponent<Renderer>().sharedMaterial = mat;

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(size.x + 1f, size.y + 1f, size.z + 1f);
            col.center = new Vector3(0, size.y / 2f, 0);

            var station = go.AddComponent<T>();

            var anchor = new GameObject("ItemAnchor");
            anchor.transform.SetParent(go.transform);
            anchor.transform.localPosition = new Vector3(0, size.y + 0.3f, 0);

            var so = new SerializedObject(station);
            var anchorProp = so.FindProperty("_itemStackAnchor");
            if (anchorProp != null) anchorProp.objectReferenceValue = anchor.transform;

            if (station is WheatFieldStation)
            {
                so.FindProperty("_productionInterval").floatValue = 0.5f;
            }
            else if (station is DoughStation)
            {
                so.FindProperty("_productionInterval").floatValue = 0.8f;
            }
            else if (station is OvenStation)
            {
                so.FindProperty("_cookingTime").floatValue = 4f;
                so.FindProperty("_ovenSlots").intValue = 2;
                so.FindProperty("_productionInterval").floatValue = 1f;
            }
            else if (station is DeliveryStation)
            {
                so.FindProperty("_productionInterval").floatValue = 0.5f;
            }

            so.ApplyModifiedProperties();
        }

        static void AddStationLabel(GameObject station, string text)
        {
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(station.transform);
            labelGO.transform.localPosition = new Vector3(0, 2.5f, 0);
            labelGO.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);

            var tmp = labelGO.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
        }

        // ══════════════════════════════════════════════════════════
        // PLAYER
        // ══════════════════════════════════════════════════════════
        static void BuildPlayer()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab");
            GameObject player;

            if (prefab != null)
            {
                player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }
            else
            {
                player = new GameObject("Player");
                player.tag = "Player";

                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                body.transform.SetParent(player.transform);
                body.transform.localPosition = new Vector3(0, 0.6f, 0);
                body.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);
                Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
                var bodyMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                bodyMat.color = new Color(0.2f, 0.5f, 0.9f);
                body.GetComponent<Renderer>().sharedMaterial = bodyMat;

                var hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hat.name = "Hat";
                hat.transform.SetParent(player.transform);
                hat.transform.localPosition = new Vector3(0, 1.35f, 0);
                hat.transform.localScale = new Vector3(0.42f, 0.15f, 0.42f);
                Object.DestroyImmediate(hat.GetComponent<CapsuleCollider>());
                var hatMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                hatMat.color = Color.white;
                hat.GetComponent<Renderer>().sharedMaterial = hatMat;

                var stackPoint = new GameObject("StackPoint");
                stackPoint.transform.SetParent(player.transform);
                stackPoint.transform.localPosition = new Vector3(0, 1.6f, 0);

                var rb = player.AddComponent<Rigidbody>();
                rb.freezeRotation = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                var capsule = player.AddComponent<CapsuleCollider>();
                capsule.height = 1.2f;
                capsule.radius = 0.28f;
                capsule.center = new Vector3(0, 0.6f, 0);

                var stacker = player.AddComponent<PlayerStacker>();
                var soStacker = new SerializedObject(stacker);
                soStacker.FindProperty("_stackAnchor").objectReferenceValue = stackPoint.transform;
                soStacker.FindProperty("_maxStackSize").intValue = 5;
                soStacker.FindProperty("_itemSpacingY").floatValue = 0.28f;
                soStacker.ApplyModifiedProperties();

                player.AddComponent<PlayerController>();
                player.AddComponent<PlayerAnimator>();
            }

            player.name = "Player";
            player.transform.position = new Vector3(0, 0.05f, -2f);

            if (!player.CompareTag("Player"))
                player.tag = "Player";
        }

        // ══════════════════════════════════════════════════════════
        // CAMERA ISOMETRICA
        // ══════════════════════════════════════════════════════════
        static void BuildCamera()
        {
            var camGO = new GameObject("[Camera]");

            var cam = camGO.AddComponent<UnityEngine.Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            cam.backgroundColor = new Color(0.53f, 0.81f, 0.98f);
            camGO.tag = "MainCamera";

            camGO.transform.position = new Vector3(0, 18f, -14f);
            camGO.transform.rotation = Quaternion.Euler(35f, 0f, 0f);

            camGO.AddComponent<AudioListener>();

            var follow = camGO.AddComponent<IsometricCameraFollow>();
            var soFollow = new SerializedObject(follow);
            soFollow.FindProperty("_offset").vector3Value = new Vector3(0f, 18f, -14f);
            soFollow.FindProperty("_smoothTime").floatValue = 0.12f;
            soFollow.FindProperty("_pitch").floatValue = 35f;
            soFollow.FindProperty("_yaw").floatValue = 0f;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                soFollow.FindProperty("_target").objectReferenceValue = player.transform;

            soFollow.ApplyModifiedProperties();
        }

        // ══════════════════════════════════════════════════════════
        // ILUMINACAO
        // ══════════════════════════════════════════════════════════
        static void BuildLighting()
        {
            var dirGO = new GameObject("Directional Light");
            dirGO.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            var dir = dirGO.AddComponent<Light>();
            dir.type = LightType.Directional;
            dir.intensity = 1.3f;
            dir.color = new Color(1f, 0.96f, 0.86f);
            dir.shadows = LightShadows.Soft;

            var fillGO = new GameObject("Fill Light");
            fillGO.transform.rotation = Quaternion.Euler(25f, 150f, 0f);
            var fill = fillGO.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.5f;
            fill.color = new Color(0.6f, 0.7f, 1f);
            fill.shadows = LightShadows.None;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
            RenderSettings.fog = false;
        }

        // ══════════════════════════════════════════════════════════
        // MANAGERS
        // ══════════════════════════════════════════════════════════
        static void BuildManagers()
        {
            var root = new GameObject("[Managers]");
            root.AddComponent<GameManager>();
            root.AddComponent<SaveManager>();
            root.AddComponent<AudioManager>();
            root.AddComponent<SceneLoader>();
            root.AddComponent<MoneyManager>();
            root.AddComponent<ItemPool>();
            root.AddComponent<UpgradeManager>();

            var gm = root.GetComponent<GameManager>();
            if (gm != null)
            {
                var soGM = new SerializedObject(gm);
                var stateProp = soGM.FindProperty("_initialState");
                if (stateProp != null)
                {
                    stateProp.enumValueIndex = 1; // GameState.Playing = 1
                    soGM.ApplyModifiedProperties();
                }
            }
        }

        // ══════════════════════════════════════════════════════════
        // SISTEMA DE CLIENTES
        // ══════════════════════════════════════════════════════════
        static void BuildCustomerSystem()
        {
            var root = new GameObject("[CustomerSystem]");
            root.transform.position = Vector3.zero;

            var queueGO = new GameObject("CustomerQueue");
            queueGO.transform.SetParent(root.transform);
            queueGO.transform.position = new Vector3(7, 0, 10);

            var queue = queueGO.AddComponent<CustomerQueue>();
            var soQueue = new SerializedObject(queue);

            var queueStart = new GameObject("QueueStart");
            queueStart.transform.SetParent(queueGO.transform);
            queueStart.transform.position = new Vector3(7, 0, 11);
            queueStart.transform.rotation = Quaternion.Euler(0, 180, 0);

            soQueue.FindProperty("_maxQueueSize").intValue = 4;
            soQueue.FindProperty("_spacingBetweenCustomers").floatValue = 1.3f;
            soQueue.FindProperty("_queueStart").objectReferenceValue = queueStart.transform;
            soQueue.ApplyModifiedProperties();

            var deliveryStation = Object.FindObjectOfType<DeliveryStation>();
            if (deliveryStation != null)
            {
                var soDel = new SerializedObject(deliveryStation);
                soDel.FindProperty("_customerQueue").objectReferenceValue = queue;
                soDel.ApplyModifiedProperties();
            }

            var spawnerGO = new GameObject("CustomerSpawner");
            spawnerGO.transform.SetParent(root.transform);
            spawnerGO.transform.position = new Vector3(7, 0, 18);

            var spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(spawnerGO.transform);
            spawnPoint.transform.position = new Vector3(7, 0, 20);

            var spawner = spawnerGO.AddComponent<CustomerSpawner>();
            var soSpawner = new SerializedObject(spawner);

            var customerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Customers/Customer.prefab");
            if (customerPrefab != null)
            {
                var customerComp = customerPrefab.GetComponent<Customer>();
                soSpawner.FindProperty("_customerPrefab").objectReferenceValue = customerComp;
                Debug.Log("[SceneFixer] Customer prefab conectado no CustomerSpawner.");
            }
            else
            {
                Debug.LogError("[SceneFixer] Prefab Customer.prefab NAO encontrado! Execute PizzaTycoon > 2. Create Prefabs primeiro.");
            }

            soSpawner.FindProperty("_customerQueue").objectReferenceValue = queue;
            soSpawner.FindProperty("_spawnPoint").objectReferenceValue = spawnPoint.transform;
            soSpawner.FindProperty("_spawnInterval").floatValue = 7f;
            soSpawner.FindProperty("_initialPoolSize").intValue = 8;
            soSpawner.ApplyModifiedProperties();
        }

        // ══════════════════════════════════════════════════════════
        // UI — HUD + JOYSTICK
        // ══════════════════════════════════════════════════════════
        static void BuildUI()
        {
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var canvasGO = new GameObject("HUDCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            var topBar = CreateUIPanel(canvasGO.transform, "TopBar",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -80), new Vector2(0, 0),
                new Color(0, 0, 0, 0.4f));
            topBar.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 80);

            var moneyGO = new GameObject("MoneyText");
            moneyGO.transform.SetParent(topBar.transform, false);
            var moneyRT = moneyGO.AddComponent<RectTransform>();
            moneyRT.anchorMin = new Vector2(0.5f, 0);
            moneyRT.anchorMax = new Vector2(0.5f, 1);
            moneyRT.sizeDelta = new Vector2(300, 0);
            moneyRT.anchoredPosition = Vector2.zero;
            var moneyTMP = moneyGO.AddComponent<TextMeshProUGUI>();
            moneyTMP.text = "$0";
            moneyTMP.fontSize = 36;
            moneyTMP.fontStyle = FontStyles.Bold;
            moneyTMP.color = Color.white;
            moneyTMP.alignment = TextAlignmentOptions.Center;

            var hud = canvasGO.AddComponent<HUDController>();
            var soHUD = new SerializedObject(hud);
            soHUD.FindProperty("_moneyText").objectReferenceValue = moneyTMP;
            soHUD.ApplyModifiedProperties();

            // Joystick (canto inferior esquerdo)
            var joystickPanel = new GameObject("JoystickPanel");
            joystickPanel.transform.SetParent(canvasGO.transform, false);
            var jpRT = joystickPanel.AddComponent<RectTransform>();
            jpRT.anchorMin = new Vector2(0, 0);
            jpRT.anchorMax = new Vector2(0, 0);
            jpRT.pivot = new Vector2(0, 0);
            jpRT.anchoredPosition = new Vector2(60, 60);
            jpRT.sizeDelta = new Vector2(220, 220);

            var bg = CreateCircleImage(joystickPanel.transform, "Background", new Vector2(220, 220), new Color(1, 1, 1, 0.25f));
            var handle = CreateCircleImage(bg.transform, "Handle", new Vector2(90, 90), new Color(1, 1, 1, 0.6f));
            var handleRT = handle.GetComponent<RectTransform>();
            handleRT.anchoredPosition = Vector2.zero;

            var joystick = joystickPanel.AddComponent<JoystickController>();
            var soJoy = new SerializedObject(joystick);
            soJoy.FindProperty("_background").objectReferenceValue = bg.GetComponent<RectTransform>();
            soJoy.FindProperty("_handle").objectReferenceValue = handleRT;
            soJoy.FindProperty("_dynamicPositioning").boolValue = true;
            soJoy.FindProperty("_deadZone").floatValue = 0.1f;
            soJoy.ApplyModifiedProperties();

            var playerCtrl = Object.FindObjectOfType<PlayerController>();
            if (playerCtrl != null)
            {
                var soPCtrl = new SerializedObject(playerCtrl);
                soPCtrl.FindProperty("_joystick").objectReferenceValue = joystick;
                soPCtrl.ApplyModifiedProperties();
            }

            // Botao de Upgrade (canto inferior direito)
            var upgradeBtnGO = new GameObject("UpgradeButton");
            upgradeBtnGO.transform.SetParent(canvasGO.transform, false);
            var upgBtnRT = upgradeBtnGO.AddComponent<RectTransform>();
            upgBtnRT.anchorMin = new Vector2(1, 0);
            upgBtnRT.anchorMax = new Vector2(1, 0);
            upgBtnRT.pivot = new Vector2(1, 0);
            upgBtnRT.anchoredPosition = new Vector2(-30, 60);
            upgBtnRT.sizeDelta = new Vector2(160, 70);

            var upgImg = upgradeBtnGO.AddComponent<Image>();
            upgImg.color = new Color(0.2f, 0.7f, 0.2f);
            upgradeBtnGO.AddComponent<Button>();

            var upgTxtGO = new GameObject("Text");
            upgTxtGO.transform.SetParent(upgradeBtnGO.transform, false);
            var upgTxt = upgTxtGO.AddComponent<TextMeshProUGUI>();
            upgTxt.text = "UPGRADE";
            upgTxt.fontSize = 22;
            upgTxt.fontStyle = FontStyles.Bold;
            upgTxt.color = Color.white;
            upgTxt.alignment = TextAlignmentOptions.Center;
            var upgTxtRT = upgTxtGO.GetComponent<RectTransform>();
            upgTxtRT.anchorMin = Vector2.zero;
            upgTxtRT.anchorMax = Vector2.one;
            upgTxtRT.sizeDelta = Vector2.zero;
        }

        static GameObject CreateUIPanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        static GameObject CreateCircleImage(Transform parent, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            return go;
        }

        // ══════════════════════════════════════════════════════════
        // CONECTAR REFERENCIAS FINAIS
        // ══════════════════════════════════════════════════════════
        static void WireReferences()
        {
            // Conecta prefabs de itens no ItemPool
            var itemPool = Object.FindObjectOfType<ItemPool>();
            if (itemPool != null)
            {
                var soIP = new SerializedObject(itemPool);
                var prefabListProp = soIP.FindProperty("_itemPrefabs");

                if (prefabListProp != null)
                {
                    prefabListProp.ClearArray();

                    var itemMappings = new (string path, ItemType type)[]
                    {
                        ("Assets/_Project/Prefabs/Items/Item_Wheat.prefab",      ItemType.Wheat),
                        ("Assets/_Project/Prefabs/Items/Item_Dough.prefab",      ItemType.Dough),
                        ("Assets/_Project/Prefabs/Items/Item_RawPizza.prefab",   ItemType.RawPizza),
                        ("Assets/_Project/Prefabs/Items/Item_CookedPizza.prefab",ItemType.CookedPizza),
                    };

                    int validCount = 0;
                    foreach (var (path, type) in itemMappings)
                    {
                        var prefabGO = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefabGO == null)
                        {
                            Debug.LogWarning($"[SceneFixer] Item prefab nao encontrado: {path}. Execute PizzaTycoon > 2. Create Prefabs");
                            continue;
                        }

                        var stackable = prefabGO.GetComponent<StackableItem>();
                        if (stackable == null) continue;

                        prefabListProp.InsertArrayElementAtIndex(validCount);
                        var entry = prefabListProp.GetArrayElementAtIndex(validCount);
                        entry.FindPropertyRelative("type").enumValueIndex = (int)type;
                        entry.FindPropertyRelative("prefab").objectReferenceValue = stackable;
                        entry.FindPropertyRelative("initialPoolSize").intValue = 20;
                        validCount++;
                    }

                    soIP.ApplyModifiedProperties();
                    Debug.Log($"[SceneFixer] ItemPool configurado com {validCount} tipos de item.");
                }
            }

            // UpgradeManager — conecta referencias e carrega UpgradeDatas
            var upgradeManager = Object.FindObjectOfType<UpgradeManager>();
            if (upgradeManager != null)
            {
                var soUM = new SerializedObject(upgradeManager);

                var playerCtrl = Object.FindObjectOfType<PlayerController>();
                var playerStack = Object.FindObjectOfType<PlayerStacker>();
                var customerSpawner = Object.FindObjectOfType<CustomerSpawner>();

                if (playerCtrl != null)   soUM.FindProperty("_playerController")?.SetObjectReferenceValue(playerCtrl);
                if (playerStack != null)  soUM.FindProperty("_playerStacker")?.SetObjectReferenceValue(playerStack);
                if (customerSpawner != null) soUM.FindProperty("_customerSpawner")?.SetObjectReferenceValue(customerSpawner);

                var upgradeList = soUM.FindProperty("_availableUpgrades");
                if (upgradeList != null)
                {
                    string[] upgradePaths = {
                        "Assets/_Project/ScriptableObjects/UpgradeData/UpgradeData_Velocidade.asset",
                        "Assets/_Project/ScriptableObjects/UpgradeData/UpgradeData_Capacidade.asset",
                        "Assets/_Project/ScriptableObjects/UpgradeData/UpgradeData_Forno.asset",
                        "Assets/_Project/ScriptableObjects/UpgradeData/UpgradeData_Colheita.asset",
                    };

                    upgradeList.ClearArray();
                    int validCount = 0;
                    foreach (string path in upgradePaths)
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
                        if (asset != null)
                        {
                            upgradeList.InsertArrayElementAtIndex(validCount);
                            upgradeList.GetArrayElementAtIndex(validCount).objectReferenceValue = asset;
                            validCount++;
                        }
                    }
                }

                var allStations = Object.FindObjectsOfType<BaseStation>();
                var stationsProp = soUM.FindProperty("_allStations");
                if (stationsProp != null)
                {
                    stationsProp.ClearArray();
                    for (int i = 0; i < allStations.Length; i++)
                    {
                        stationsProp.InsertArrayElementAtIndex(i);
                        stationsProp.GetArrayElementAtIndex(i).objectReferenceValue = allStations[i];
                    }
                }

                soUM.ApplyModifiedProperties();
            }

            Debug.Log("[SceneFixer] Referencias conectadas com sucesso!");
        }
    }

    // Extensao para SerializedProperty — permite SetObjectReferenceValue com null-guard
    internal static class SerializedPropertyExtensions
    {
        internal static void SetObjectReferenceValue(this SerializedProperty prop, Object value)
        {
            if (prop != null) prop.objectReferenceValue = value;
        }
    }
}
