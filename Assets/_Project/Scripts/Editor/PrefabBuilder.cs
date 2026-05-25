using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;
using System.IO;
using PizzaTycoon.Player;
using PizzaTycoon.Items;
using PizzaTycoon.Stations;
using PizzaTycoon.Customers;

namespace PizzaTycoon.Editor
{
    // Gera todos os Prefabs do projeto usando apenas primitivas Unity
    // Menu: PizzaTycoon > 2. Create Prefabs
    // ATENÇÃO: execute PizzaTycoon > 1. Create Materials antes deste script
    public static class PrefabBuilder
    {
        private const string PREFAB_PLAYER    = "Assets/_Project/Prefabs/Player";
        private const string PREFAB_ITEMS     = "Assets/_Project/Prefabs/Items";
        private const string PREFAB_STATIONS  = "Assets/_Project/Prefabs/Stations";
        private const string PREFAB_CUSTOMERS = "Assets/_Project/Prefabs/Customers";
        private const string PREFAB_UI        = "Assets/_Project/Prefabs/UI";
        private const string ANIMS_PATH       = "Assets/_Project/Animations";

        [MenuItem("PizzaTycoon/2. Create Prefabs")]
        public static void CreateAllPrefabs()
        {
            EnsureDirectories();

            EditorUtility.DisplayProgressBar("Pizza Tycoon", "Criando Player...", 0.1f);
            CreatePlayerPrefab();

            EditorUtility.DisplayProgressBar("Pizza Tycoon", "Criando Itens...", 0.25f);
            CreateItemPrefabs();

            EditorUtility.DisplayProgressBar("Pizza Tycoon", "Criando Estações...", 0.50f);
            CreateStationPrefabs();

            EditorUtility.DisplayProgressBar("Pizza Tycoon", "Criando Clientes e Carro...", 0.75f);
            CreateCustomerPrefab();
            CreateCarPrefab();

            EditorUtility.DisplayProgressBar("Pizza Tycoon", "Criando UI Prefabs...", 0.90f);
            CreateFloatingTextPrefab();

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Pizza Tycoon — Prefabs",
                "✅ Todos os Prefabs criados!\n\n" +
                "Próximo passo: PizzaTycoon > 3. Build Scene\n\n" +
                "Lembre-se de criar um AnimatorController para o Player\n" +
                "em Assets/_Project/Animations/ e conectar no Inspector.",
                "OK");
        }

        // ══════════════════════════════════════════════════════════════════════
        // PLAYER
        // ══════════════════════════════════════════════════════════════════════
        private static void CreatePlayerPrefab()
        {
            const string path = PREFAB_PLAYER + "/Player.prefab";
            if (AssetExists(path)) return;

            GameObject root = new GameObject("Player");

            // ── Corpo (Capsule) ──
            GameObject body = CreatePrimitive(PrimitiveType.Capsule, "Body", root.transform);
            body.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            body.transform.localScale    = new Vector3(0.5f, 0.5f, 0.5f);
            SetMaterial(body, "Mat_Player_Body");

            // ── Cabeça (Sphere) ──
            GameObject head = CreatePrimitive(PrimitiveType.Sphere, "Head", root.transform);
            head.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            head.transform.localScale    = Vector3.one * 0.40f;
            SetMaterial(head, "Mat_Player_Skin");

            // ── Chapéu (Cylinder achatado) ──
            GameObject hat = CreatePrimitive(PrimitiveType.Cylinder, "Hat", root.transform);
            hat.transform.localPosition = new Vector3(0f, 1.36f, 0f);
            hat.transform.localScale    = new Vector3(0.45f, 0.08f, 0.45f);
            SetMaterial(hat, "Mat_Player_Hat");

            // ── StackPoint (ponto de ancoragem dos itens empilhados) ──
            GameObject stackPoint = new GameObject("StackPoint");
            stackPoint.transform.SetParent(root.transform);
            stackPoint.transform.localPosition = new Vector3(0f, 1.55f, 0f);

            // ── Física ──
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX |
                             RigidbodyConstraints.FreezeRotationY |
                             RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.height = 1.2f;
            col.radius = 0.25f;
            col.center = new Vector3(0f, 0.5f, 0f);

            // ── Scripts de jogador ──
            PlayerStacker stacker = root.AddComponent<PlayerStacker>();

            // Conecta o StackPoint via SerializedObject (reflection-free Editor API)
            SerializedObject so = new SerializedObject(stacker);
            so.FindProperty("_stackAnchor").objectReferenceValue = stackPoint.transform;
            so.FindProperty("_maxStackSize").intValue = 5;
            so.FindProperty("_itemSpacingY").floatValue = 0.3f;
            so.ApplyModifiedProperties();

            PlayerController ctrl = root.AddComponent<PlayerController>();
            root.AddComponent<PlayerAnimator>();

            // ── Animator + Controller ──
            Animator anim = body.AddComponent<Animator>();
            AnimatorController controller = CreatePlayerAnimatorController();
            if (controller != null) anim.runtimeAnimatorController = controller;

            // Tag do Unity Player
            root.tag = "Player";

            SavePrefab(root, path);
        }

        private static AnimatorController CreatePlayerAnimatorController()
        {
            string path = $"{ANIMS_PATH}/PlayerAnimator.controller";
            EnsureDirectory(ANIMS_PATH);

            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing != null) return existing;

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);

            ctrl.AddParameter("IsWalking",  AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("IsCarrying", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Pickup",     AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Deliver",    AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("SpeedMult",  AnimatorControllerParameterType.Float);

            // Estado base (Idle)
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            AnimatorState idle = sm.AddState("Idle");
            AnimatorState walk = sm.AddState("Walk");
            idle.speed = 1f;
            walk.speed = 1f;

            // Idle → Walk quando IsWalking = true
            AnimatorStateTransition toWalk = idle.AddTransition(walk);
            toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsWalking");
            toWalk.hasExitTime = false;
            toWalk.duration = 0.1f;

            // Walk → Idle quando IsWalking = false
            AnimatorStateTransition toIdle = walk.AddTransition(idle);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWalking");
            toIdle.hasExitTime = false;
            toIdle.duration = 0.1f;

            sm.defaultState = idle;

            // Define SpeedMult padrão = 1.0
            foreach (var p in ctrl.parameters)
                if (p.name == "SpeedMult")
                    ctrl.parameters[0].defaultFloat = 1f;

            AssetDatabase.SaveAssets();
            return ctrl;
        }

        // ══════════════════════════════════════════════════════════════════════
        // ITENS
        // ══════════════════════════════════════════════════════════════════════
        private static void CreateItemPrefabs()
        {
            CreateWheatPrefab();
            CreateDoughPrefab();
            CreateRawPizzaPrefab();
            CreateCookedPizzaPrefab();
        }

        private static void CreateWheatPrefab()
        {
            const string path = PREFAB_ITEMS + "/Item_Wheat.prefab";
            if (AssetExists(path)) return;

            GameObject root = new GameObject("Item_Wheat");
            GameObject mesh = CreatePrimitive(PrimitiveType.Cube, "Mesh", root.transform);
            mesh.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f);
            SetMaterial(mesh, "Mat_Wheat");

            StackableItem item = root.AddComponent<StackableItem>();
            SetSerializedField(item, "_type", (int)ItemType.Wheat);

            SavePrefab(root, path);
        }

        private static void CreateDoughPrefab()
        {
            const string path = PREFAB_ITEMS + "/Item_Dough.prefab";
            if (AssetExists(path)) return;

            GameObject root = new GameObject("Item_Dough");
            GameObject mesh = CreatePrimitive(PrimitiveType.Cylinder, "Mesh", root.transform);
            mesh.transform.localScale = new Vector3(0.25f, 0.06f, 0.25f);
            SetMaterial(mesh, "Mat_Dough");

            StackableItem item = root.AddComponent<StackableItem>();
            SetSerializedField(item, "_type", (int)ItemType.Dough);

            SavePrefab(root, path);
        }

        private static void CreateRawPizzaPrefab()
        {
            const string path = PREFAB_ITEMS + "/Item_RawPizza.prefab";
            if (AssetExists(path)) return;

            GameObject root = new GameObject("Item_RawPizza");
            // Base da pizza (Cylinder achatado)
            GameObject mesh = CreatePrimitive(PrimitiveType.Cylinder, "Mesh", root.transform);
            mesh.transform.localScale = new Vector3(0.3f, 0.05f, 0.3f);
            SetMaterial(mesh, "Mat_RawPizza");

            StackableItem item = root.AddComponent<StackableItem>();
            SetSerializedField(item, "_type", (int)ItemType.RawPizza);

            SavePrefab(root, path);
        }

        private static void CreateCookedPizzaPrefab()
        {
            const string path = PREFAB_ITEMS + "/Item_CookedPizza.prefab";
            if (AssetExists(path)) return;

            GameObject root = new GameObject("Item_CookedPizza");
            // Base (Cylinder laranja)
            GameObject mesh = CreatePrimitive(PrimitiveType.Cylinder, "Base", root.transform);
            mesh.transform.localScale = new Vector3(0.3f, 0.07f, 0.3f);
            SetMaterial(mesh, "Mat_Pizza_Cooked");

            // 3 Toppings (Spheres pequenas)
            Vector3[] toppingOffsets = {
                new Vector3(0f,    0.08f,  0.08f),
                new Vector3(-0.07f,0.08f, -0.04f),
                new Vector3( 0.07f,0.08f, -0.04f)
            };
            foreach (Vector3 offset in toppingOffsets)
            {
                GameObject topping = CreatePrimitive(PrimitiveType.Sphere, "Topping", root.transform);
                topping.transform.localPosition = offset;
                topping.transform.localScale = Vector3.one * 0.06f;
                SetMaterial(topping, "Mat_Pizza_Topping");
            }

            StackableItem item = root.AddComponent<StackableItem>();
            SetSerializedField(item, "_type", (int)ItemType.CookedPizza);

            SavePrefab(root, path);
        }

        // ══════════════════════════════════════════════════════════════════════
        // ESTAÇÕES
        // ══════════════════════════════════════════════════════════════════════
        private static void CreateStationPrefabs()
        {
            CreateWheatFieldPrefab();
            CreateDoughStationPrefab();
            CreateAssemblyStationPrefab();
            CreateOvenStationPrefab();
            CreateDeliveryStationPrefab();
        }

        private static void CreateWheatFieldPrefab()
        {
            const string path = PREFAB_STATIONS + "/Station_WheatField.prefab";
            if (AssetExists(path)) return;

            GameObject root = new GameObject("Station_WheatField");

            // Chão de terra
            GameObject ground = CreatePrimitive(PrimitiveType.Cube, "Ground", root.transform);
            ground.transform.localScale = new Vector3(3f, 0.1f, 3f);
            ground.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            SetMaterial(ground, "Mat_Station_Wood");

            // 6 palheiros de trigo em posições variadas
            float[,] wheatPos = {
                {-0.8f, 0.3f,  -0.8f}, {0.0f,  0.45f, -0.6f}, {0.8f, 0.55f, -0.9f},
                {-0.9f, 0.5f,   0.5f}, {0.3f,  0.35f,  0.9f}, {0.9f, 0.6f,   0.3f}
            };
            for (int i = 0; i < 6; i++)
            {
                GameObject stalk = CreatePrimitive(PrimitiveType.Cube, $"Wheat_{i}", root.transform);
                stalk.transform.localPosition = new Vector3(wheatPos[i, 0], wheatPos[i, 1] * 0.5f, wheatPos[i, 2]);
                stalk.transform.localScale = new Vector3(0.12f, wheatPos[i, 1], 0.12f);
                SetMaterial(stalk, "Mat_Wheat");
            }

            // Rótulo visual (ícone de trigo — Cube achatado acima)
            GameObject label = CreatePrimitive(PrimitiveType.Cube, "Label", root.transform);
            label.transform.localPosition = new Vector3(0f, 1.2f, -1.4f);
            label.transform.localScale    = new Vector3(0.8f, 0.4f, 0.05f);
            SetMaterial(label, "Mat_Wheat");

            // Collider trigger de interação
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.size   = new Vector3(3.5f, 2f, 3.5f);
            trigger.center = new Vector3(0f, 1f, 0f);
            trigger.isTrigger = true;

            root.AddComponent<WheatFieldStation>();

            SavePrefab(root, path);
        }

        private static void CreateDoughStationPrefab()
        {
            const string path = PREFAB_STATIONS + "/Station_Dough.prefab";
            if (AssetExists(path)) return;

            GameObject root = new GameObject("Station_Dough");

            // Bancada base
            GameObject body = CreatePrimitive(PrimitiveType.Cube, "Body", root.transform);
            body.transform.localScale = new Vector3(1.5f, 1f, 1.5f);
            body.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            SetMaterial(body, "Mat_Station_Blue");

            // Detalhe no topo (misturador)
            GameObject mixer = CreatePrimitive(PrimitiveType.Cylinder, "Mixer", root.transform);
            mixer.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            mixer.transform.localScale = new Vector3(0.5f, 0.3f, 0.5f);
            SetMaterial(mixer, "Mat_Station_Silver");

            // Perna da bancada (lateral)
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject leg = CreatePrimitive(PrimitiveType.Cube, $"Leg_{side}", root.transform);
                leg.transform.localPosition = new Vector3(side * 0.6f, 0.25f, 0f);
                leg.transform.localScale = new Vector3(0.15f, 0.5f, 1.5f);
                SetMaterial(leg, "Mat_Station_Dark");
            }

            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.size      = new Vector3(2.5f, 2.5f, 2.5f);
            trigger.center    = new Vector3(0f, 1f, 0f);
            trigger.isTrigger = true;

            root.AddComponent<DoughStation>();

            SavePrefab(root, path);
        }

        private static void CreateAssemblyStationPrefab()
        {
            const string path = PREFAB_STATIONS + "/Station_Assembly.prefab";
            if (AssetExists(path)) return;

            GameObject root = new GameObject("Station_Assembly");

            // Mesa principal
            GameObject top = CreatePrimitive(PrimitiveType.Cube, "TableTop", root.transform);
            top.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            top.transform.localScale    = new Vector3(2f, 0.1f, 1.5f);
            SetMaterial(top, "Mat_Station_Silver");

            // Corpo da mesa
            GameObject body = CreatePrimitive(PrimitiveType.Cube, "Body", root.transform);
            body.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            body.transform.localScale    = new Vector3(1.8f, 0.9f, 1.3f);
            SetMaterial(body, "Mat_Station_Blue");

            // Perna de apoio (4)
            Vector3[] legPositions = {
                new Vector3(-0.8f, 0f,  0.5f), new Vector3(0.8f, 0f,  0.5f),
                new Vector3(-0.8f, 0f, -0.5f), new Vector3(0.8f, 0f, -0.5f)
            };
            foreach (var pos in legPositions)
            {
                GameObject leg = CreatePrimitive(PrimitiveType.Cube, "Leg", root.transform);
                leg.transform.localPosition = pos;
                leg.transform.localScale    = new Vector3(0.1f, 0.9f, 0.1f);
                SetMaterial(leg, "Mat_Station_Dark");
            }

            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.size      = new Vector3(2.5f, 2.5f, 2.5f);
            trigger.center    = new Vector3(0f, 1f, 0f);
            trigger.isTrigger = true;

            root.AddComponent<PizzaAssemblyStation>();

            SavePrefab(root, path);
        }

        private static void CreateOvenStationPrefab()
        {
            const string path = PREFAB_STATIONS + "/Station_Oven.prefab";
            if (AssetExists(path)) return;

            GameObject root = new GameObject("Station_Oven");

            // Corpo principal do forno
            GameObject body = CreatePrimitive(PrimitiveType.Cube, "Body", root.transform);
            body.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            body.transform.localScale    = new Vector3(1.5f, 1.2f, 1.5f);
            SetMaterial(body, "Mat_Station_Dark");

            // Abertura do forno (na frente)
            GameObject door = CreatePrimitive(PrimitiveType.Cube, "Door", root.transform);
            door.transform.localPosition = new Vector3(0f, 0.6f, 0.76f);
            door.transform.localScale    = new Vector3(0.8f, 0.6f, 0.05f);
            // Usa Mat_Station_Dark como base para a abertura do forno
            if (door.GetComponent<Renderer>() != null)
            {
                Material blackMat = MaterialCreator.Load("Mat_Station_Dark");
                if (blackMat != null)
                {
                    Material darkCopy = new Material(blackMat) { color = Color.black };
                    door.GetComponent<Renderer>().sharedMaterial = darkCopy;
                }
            }

            // Indicador de status (Sphere — vermelho=cozinhando, verde=pronto)
            GameObject indicator = CreatePrimitive(PrimitiveType.Sphere, "StatusLight", root.transform);
            indicator.transform.localPosition = new Vector3(0.55f, 1.3f, 0.55f);
            indicator.transform.localScale    = Vector3.one * 0.15f;
            SetMaterial(indicator, "Mat_Indicator_Red");

            // Chaminé
            GameObject chimney = CreatePrimitive(PrimitiveType.Cylinder, "Chimney", root.transform);
            chimney.transform.localPosition = new Vector3(-0.4f, 1.6f, -0.4f);
            chimney.transform.localScale    = new Vector3(0.2f, 0.4f, 0.2f);
            SetMaterial(chimney, "Mat_Station_Dark");

            // Barra de progresso placeholder (Cube achatado)
            GameObject progressBG = CreatePrimitive(PrimitiveType.Cube, "ProgressBG", root.transform);
            progressBG.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            progressBG.transform.localScale    = new Vector3(1f, 0.08f, 0.08f);
            SetMaterial(progressBG, "Mat_Station_Dark");

            GameObject progressFill = CreatePrimitive(PrimitiveType.Cube, "ProgressFill", progressBG.transform);
            progressFill.transform.localPosition = new Vector3(-0.5f, 0f, 0f);
            progressFill.transform.localScale    = new Vector3(0.5f, 1f, 1f);
            SetMaterial(progressFill, "Mat_Indicator_Red");

            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.size      = new Vector3(2.5f, 2.5f, 2.5f);
            trigger.center    = new Vector3(0f, 1f, 0f);
            trigger.isTrigger = true;

            root.AddComponent<OvenStation>();

            SavePrefab(root, path);
        }

        private static void CreateDeliveryStationPrefab()
        {
            const string path = PREFAB_STATIONS + "/Station_Delivery.prefab";
            if (AssetExists(path)) return;

            GameObject root = new GameObject("Station_Delivery");

            // Balcão
            GameObject counter = CreatePrimitive(PrimitiveType.Cube, "Counter", root.transform);
            counter.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            counter.transform.localScale    = new Vector3(2f, 0.9f, 1f);
            SetMaterial(counter, "Mat_Station_Blue");

            // Superfície do balcão
            GameObject surface = CreatePrimitive(PrimitiveType.Cube, "Surface", root.transform);
            surface.transform.localPosition = new Vector3(0f, 0.925f, 0f);
            surface.transform.localScale    = new Vector3(2.1f, 0.05f, 1.1f);
            SetMaterial(surface, "Mat_Station_Silver");

            // Seta direcional (3 cubos formando "▶")
            BuildArrow(root.transform, new Vector3(0f, 1.3f, 1.2f));

            // Ponto de enfileiramento dos clientes (Transform vazio)
            GameObject queueStart = new GameObject("QueueStart");
            queueStart.transform.SetParent(root.transform);
            queueStart.transform.localPosition = new Vector3(0f, 0f, 2f);

            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.size      = new Vector3(3f, 2.5f, 3f);
            trigger.center    = new Vector3(0f, 1f, 0f);
            trigger.isTrigger = true;

            DeliveryStation delivery = root.AddComponent<DeliveryStation>();
            CustomerQueue queue = root.AddComponent<CustomerQueue>();

            // Conecta CustomerQueue e ponto de entrega
            SerializedObject soD = new SerializedObject(delivery);
            soD.FindProperty("_customerQueue").objectReferenceValue = queue;
            soD.FindProperty("_deliveryPoint").objectReferenceValue = queueStart.transform;
            soD.ApplyModifiedProperties();

            SerializedObject soQ = new SerializedObject(queue);
            soQ.FindProperty("_queueStart").objectReferenceValue = queueStart.transform;
            soQ.ApplyModifiedProperties();

            SavePrefab(root, path);
        }

        private static void BuildArrow(Transform parent, Vector3 center)
        {
            // Haste da seta
            GameObject shaft = CreatePrimitive(PrimitiveType.Cube, "Arrow_Shaft", parent);
            shaft.transform.localPosition = center + new Vector3(-0.2f, 0f, 0f);
            shaft.transform.localScale = new Vector3(0.4f, 0.12f, 0.12f);
            SetMaterial(shaft, "Mat_Arrow");

            // Ponta (triângulo simulado com dois cubos rotacionados)
            GameObject tip1 = CreatePrimitive(PrimitiveType.Cube, "Arrow_Tip1", parent);
            tip1.transform.localPosition = center + new Vector3(0.15f, 0.1f, 0f);
            tip1.transform.localScale    = new Vector3(0.25f, 0.1f, 0.1f);
            tip1.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            SetMaterial(tip1, "Mat_Arrow");

            GameObject tip2 = CreatePrimitive(PrimitiveType.Cube, "Arrow_Tip2", parent);
            tip2.transform.localPosition = center + new Vector3(0.15f, -0.1f, 0f);
            tip2.transform.localScale    = new Vector3(0.25f, 0.1f, 0.1f);
            tip2.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            SetMaterial(tip2, "Mat_Arrow");
        }

        // ══════════════════════════════════════════════════════════════════════
        // CUSTOMER
        // ══════════════════════════════════════════════════════════════════════
        private static void CreateCustomerPrefab()
        {
            const string path = PREFAB_CUSTOMERS + "/Customer.prefab";
            if (AssetExists(path)) return;

            GameObject root = new GameObject("Customer");

            // Corpo (Capsule)
            GameObject body = CreatePrimitive(PrimitiveType.Capsule, "Body", root.transform);
            body.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            body.transform.localScale    = new Vector3(0.45f, 0.45f, 0.45f);
            SetMaterial(body, "Mat_Customer_A");

            // Cabeça (Sphere)
            GameObject head = CreatePrimitive(PrimitiveType.Sphere, "Head", root.transform);
            head.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            head.transform.localScale    = Vector3.one * 0.35f;
            SetMaterial(head, "Mat_Player_Skin");

            // Canvas World-Space com barra de paciência
            GameObject canvasGO = new GameObject("PatienceCanvas");
            canvasGO.transform.SetParent(root.transform);
            canvasGO.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            canvasGO.transform.localScale    = Vector3.one * 0.01f;

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(200f, 30f);

            // Fundo da barra
            GameObject bgGO = new GameObject("BarBG");
            bgGO.transform.SetParent(canvasGO.transform);
            Image bg = bgGO.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            RectTransform bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

            // Slider de paciência
            GameObject sliderGO = new GameObject("PatienceSlider");
            sliderGO.transform.SetParent(canvasGO.transform);
            Slider slider = sliderGO.AddComponent<Slider>();

            RectTransform sliderRT = sliderGO.GetComponent<RectTransform>();
            sliderRT.anchorMin = Vector2.zero;
            sliderRT.anchorMax = Vector2.one;
            sliderRT.offsetMin = new Vector2(4f, 4f);
            sliderRT.offsetMax = new Vector2(-4f, -4f);
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            // Fill da barra
            GameObject fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderGO.transform);
            RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.offsetMin = fillAreaRT.offsetMax = Vector2.zero;

            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillArea.transform);
            Image fill = fillGO.AddComponent<Image>();
            fill.color = Color.green;
            RectTransform fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

            slider.fillRect = fillRT;

            // NavMeshAgent
            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.speed           = 2f;
            agent.radius          = 0.4f;
            agent.stoppingDistance = 0.5f;
            agent.height           = 1.5f;

            // Script Customer + Animator
            Customer customer = root.AddComponent<Customer>();
            SerializedObject so = new SerializedObject(customer);
            so.FindProperty("_patienceBar").objectReferenceValue = slider;
            so.FindProperty("_patienceFill").objectReferenceValue = fill;
            so.FindProperty("_patience").floatValue = 30f;
            so.FindProperty("_basePayment").floatValue = 10f;
            so.FindProperty("_bonusPaymentFast").floatValue = 5f;
            so.ApplyModifiedProperties();

            SavePrefab(root, path);
        }

        // ══════════════════════════════════════════════════════════════════════
        // CAR
        // ══════════════════════════════════════════════════════════════════════
        private static void CreateCarPrefab()
        {
            const string path = PREFAB_CUSTOMERS + "/Car.prefab";
            if (AssetExists(path)) return;

            GameObject root = new GameObject("Car");

            // Corpo do carro
            GameObject body = CreatePrimitive(PrimitiveType.Cube, "Body", root.transform);
            body.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            body.transform.localScale    = new Vector3(1.5f, 0.6f, 2.5f);
            SetMaterial(body, "Mat_Car_Body");

            // Cabine (teto)
            GameObject roof = CreatePrimitive(PrimitiveType.Cube, "Roof", root.transform);
            roof.transform.localPosition = new Vector3(0f, 0.85f, -0.2f);
            roof.transform.localScale    = new Vector3(1.3f, 0.4f, 1.4f);
            SetMaterial(roof, "Mat_Car_Body");

            // Parabrisa
            GameObject windshield = CreatePrimitive(PrimitiveType.Cube, "Windshield", root.transform);
            windshield.transform.localPosition = new Vector3(0f, 0.85f, 0.52f);
            windshield.transform.localScale    = new Vector3(1.1f, 0.35f, 0.05f);
            SetMaterial(windshield, "Mat_Car_Glass");

            // 4 Rodas (Cylinders rotacionados 90° no eixo Z)
            Vector3[] wheelPositions = {
                new Vector3(-0.75f, 0.22f,  0.9f),
                new Vector3( 0.75f, 0.22f,  0.9f),
                new Vector3(-0.75f, 0.22f, -0.9f),
                new Vector3( 0.75f, 0.22f, -0.9f)
            };
            foreach (Vector3 wPos in wheelPositions)
            {
                GameObject wheel = CreatePrimitive(PrimitiveType.Cylinder, "Wheel", root.transform);
                wheel.transform.localPosition = wPos;
                wheel.transform.localScale    = new Vector3(0.3f, 0.12f, 0.3f);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                SetMaterial(wheel, "Mat_Car_Wheel");
            }

            SavePrefab(root, path);
        }

        // ══════════════════════════════════════════════════════════════════════
        // FLOATING TEXT
        // ══════════════════════════════════════════════════════════════════════
        private static void CreateFloatingTextPrefab()
        {
            const string path = PREFAB_UI + "/FloatingText.prefab";
            if (AssetExists(path)) return;

            // FloatingText gerencia seu próprio pool internamente — só precisa do componente
            GameObject root = new GameObject("FloatingText");
            root.AddComponent<PizzaTycoon.UI.FloatingText>();

            SavePrefab(root, path);
        }

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════════════

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one;

            // Remove o collider padrão da primitiva (cada station tem o seu)
            Collider col = go.GetComponent<Collider>();
            if (col != null && parent.GetComponent<Collider>() != null)
                Object.DestroyImmediate(col);

            return go;
        }

        private static void SetMaterial(GameObject go, string matName)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r == null) return;
            Material mat = MaterialCreator.Load(matName);
            if (mat != null) r.sharedMaterial = mat;
        }

        // Seta campo privado serializado via SerializedObject (sem reflection direta)
        private static void SetSerializedField(MonoBehaviour target, string fieldName, object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) { Debug.LogWarning($"Campo '{fieldName}' não encontrado em {target.GetType().Name}"); return; }

            if (value is int   i) prop.intValue = i;
            else if (value is float f) prop.floatValue = f;
            else if (value is bool  b) prop.boolValue = b;
            else if (value is string s) prop.stringValue = s;
            so.ApplyModifiedProperties();
        }

        private static void SavePrefab(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static bool AssetExists(string path) =>
            AssetDatabase.LoadAssetAtPath<Object>(path) != null;

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        private static void EnsureDirectories()
        {
            EnsureDirectory(PREFAB_PLAYER);
            EnsureDirectory(PREFAB_ITEMS);
            EnsureDirectory(PREFAB_STATIONS);
            EnsureDirectory(PREFAB_CUSTOMERS);
            EnsureDirectory(PREFAB_UI);
            EnsureDirectory(ANIMS_PATH);
        }
    }
}
