using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
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
using PizzaTycoon.GameSystems;
using PizzaTycoon.VFX;
using PizzaTycoon.Map;
using PizzaTycoon.Recipes;
using PizzaTycoon.Achievements;
using PizzaTycoon.Events;
using PizzaTycoon.Seasons;
using PizzaTycoon.Leaderboard;
using PizzaTycoon.Notifications;
using PizzaTycoon.Performance;
using PizzaTycoon.Workers;
using System.IO;

namespace PizzaTycoon.Editor
{
    // Reconstroi a cena para replicar exatamente o layout do Pizza Ready.
    //
    // LAYOUT (eixo Z = sobe na tela portrait):
    //
    //  Z+ (topo da tela)
    //  [Balcao Drive-Thru]  Z = 21
    //  [Forno]              Z = 16
    //  [Mesa de Montagem]   Z = 11
    //  [Estacao de Massa]   Z =  6
    //  [Campo de Trigo]     Z =  1
    //  [Spawn do Player]    Z = -1
    //  Z- (base da tela)
    //
    // X estreito: pizzeria tem ~7u de largura.
    public static class SceneRebuilder
    {
        [MenuItem("PizzaTycoon/REBUILD — Main Menu")]
        public static void RebuildMainMenu() => BuildMainMenuScene();

        [MenuItem("PizzaTycoon/BUILD — Main Menu")]
        public static void BuildMainMenuMenuItem() => BuildMainMenuScene();

        [MenuItem("PizzaTycoon/Reset Save (Debug)")]
        public static void ResetSave()
        {
            const string key = "PizzaTycoon_SaveData";
            if (UnityEngine.PlayerPrefs.HasKey(key))
            {
                UnityEngine.PlayerPrefs.DeleteKey(key);
                UnityEngine.PlayerPrefs.Save();
                Debug.Log("[SceneRebuilder] Save deletado.");
                EditorUtility.DisplayDialog("Save Resetado",
                    "PlayerPrefs limpos.\nPressione PLAY para comecar do zero com R$0 e sem upgrades.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Reset Save", "Nenhum save encontrado — ja esta do zero.", "OK");
            }
        }

        [MenuItem("PizzaTycoon/Fix Build Settings")]
        public static void FixBuildSettings()
        {
            UpdateBuildSettings();
            EditorUtility.DisplayDialog("Build Settings",
                "Build Settings atualizados:\n0: Assets/Scenes/MainMenu.unity\n1: Assets/Scenes/SampleScene.unity",
                "OK");
        }

        [MenuItem("PizzaTycoon/REBUILD — Cena Identica ao Pizza Ready")]
        public static void RebuildScene()
        {
            if (!EditorUtility.DisplayDialog("Pizza Tycoon — Rebuild",
                "Isso vai APAGAR e recriar todos os objetos da cena.\n\nContinuar?",
                "Sim, Rebuild", "Cancelar")) return;

            Selection.activeObject = null;
            float p = 0f;

            Show("Limpando...",               p += 0.04f); ClearScene();
            Show("Limpando prefabs...",       p += 0.04f); CleanAllPrefabs();
            Show("Recriando Customer...",     p += 0.03f); RecreateCustomerPrefab();
            Show("Criando chao...",           p += 0.07f); BuildFloor();
            Show("Criando paredes...",        p += 0.05f); BuildWalls();
            Show("Criando rua...",            p += 0.04f); BuildStreet();
            Show("Construindo ambiente...",   p += 0.04f); BuildEnvironment();
            Show("Posicionando estacoes...",  p += 0.09f); BuildStations();
            Show("Area de mesas...",          p += 0.03f); BuildDiningArea();
            Show("Decoracao...",              p += 0.03f); BuildDecoration();
            Show("Criando player...",         p += 0.08f); BuildPlayer();
            Show("Configurando camera...",    p += 0.08f); BuildCamera();
            Show("Iluminacao...",             p += 0.06f); BuildLighting();
            Show("Criando managers...",       p += 0.08f); BuildManagers();
            Show("Sistema de clientes...",    p += 0.08f); BuildCustomerSystem();
            Show("HUD e Joystick...",         p += 0.08f); BuildUI();
            Show("Configurando animacoes...", p += 0.05f); SetupPlayerAnimator();
            Show("Conectando referencias...", p += 0.05f); WireAll();

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            UpdateBuildSettings();
            Debug.Log("[SceneRebuilder] Cena reconstruida — estilo Pizza Ready!");
            EditorUtility.DisplayDialog("Rebuild Completo",
                "Cena reconstruida!\n\n" +
                "* Camera perspectiva 65 graus (igual Pizza Ready)\n" +
                "* Layout portrait vertical\n" +
                "* Chao de madeira bege com listras\n" +
                "* Estacoes alinhadas em coluna (Z crescente)\n" +
                "* Rua com faixas amarelas\n" +
                "* ItemPool configurado com todos os prefabs\n" +
                "* ComboSystem, DailyGoalSystem e ParticleManager ativos\n\n" +
                "Mude o Game View para 9:16 e pressione PLAY.", "OK");
        }

        static void Show(string msg, float prog) =>
            EditorUtility.DisplayProgressBar("Pizza Tycoon Rebuild", msg, prog);

        // ══════════════════════════════════════════════════════════
        // LIMPAR CENA
        // ══════════════════════════════════════════════════════════
        static void ClearScene()
        {
            var scene = EditorSceneManager.GetActiveScene();

            // Duas passagens garantem que objetos que criam filhos ao serem destruídos
            // (event listeners, editor helpers) não deixem lixo para trás.
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (var root in scene.GetRootGameObjects())
                    Object.DestroyImmediate(root);
            }

            Debug.Log($"[SceneRebuilder] Cena limpa — {scene.GetRootGameObjects().Length} objetos restantes.");
        }

        // ══════════════════════════════════════════════════════════
        // CHAO
        // ══════════════════════════════════════════════════════════
        static void BuildFloor()
        {
            EnsureFloorMaterial();
            var root = new GameObject("[Floor]");

            // Chão principal: 20 largura (X: -2 a 18), 35 comprimento (Z: -3 a 32)
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Floor.mat");
            if (floorMat == null && shader != null)
            {
                floorMat = new Material(shader);
                var c = new Color(0.910f, 0.835f, 0.690f); // #E8D5B0
                floorMat.color = c;
                if (floorMat.HasProperty("_BaseColor")) floorMat.SetColor("_BaseColor", c);
            }
            if (floorMat != null && floorMat.HasProperty("_BaseMap"))
                floorMat.SetTextureScale("_BaseMap", new Vector2(6f, 10f));

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor_Main";
            floor.transform.SetParent(root.transform);
            floor.transform.position   = new Vector3(8f, 0f, 14.5f); // centro: X=(18-2)/2+(-2)=8, Z=(-3+32)/2=14.5
            floor.transform.localScale = new Vector3(2.0f, 1f, 3.5f); // Plane=10u, *2=20u, *3.5=35u
            Object.DestroyImmediate(floor.GetComponent<MeshCollider>());
            var floorCol = floor.AddComponent<BoxCollider>();
            floorCol.size   = new Vector3(10f, 0.05f, 10f);
            floorCol.center = Vector3.zero;
            if (floorMat != null) floor.GetComponent<Renderer>().sharedMaterial = floorMat;
        }

        // Cria (ou reusa) textura xadrez 32x32 Bege/Branco (TAREFA 9)
        static Texture2D GetOrCreateCheckerboardTexture()
        {
            const string texPath = "Assets/_Project/Textures/Floor_Checker.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (existing != null) return existing;

            string dir = Path.GetDirectoryName(texPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var beige = new Color(0.949f, 0.878f, 0.753f); // #F2E0C0
            var white = Color.white;                        // #FFFFFF
            const int tileSize = 8; // 4x4 grid de tiles
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    bool isLight = ((x / tileSize) + (y / tileSize)) % 2 == 0;
                    tex.SetPixel(x, y, isLight ? beige : white);
                }
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(Application.dataPath + "/_Project/Textures/Floor_Checker.png", bytes);
            AssetDatabase.Refresh();
            Debug.Log("[SceneRebuilder] Textura Floor_Checker.png criada.");
            return AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        }

        // Cria Assets/_Project/Materials/Mat_Floor.mat com URP Lit, cor #F5E6C8, tiling 4x4
        static void EnsureFloorMaterial()
        {
            const string matPath = "Assets/_Project/Materials/Mat_Floor.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null) return;

            string dir = System.IO.Path.GetDirectoryName(matPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) { Debug.LogWarning("[SceneRebuilder] Shader nao encontrado para Mat_Floor."); return; }

            var mat = new Material(shader) { name = "Mat_Floor" };
            var floorColor = new Color(0.961f, 0.902f, 0.784f); // #F5E6C8
            mat.color = floorColor;
            if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor", floorColor);
            if (mat.HasProperty("_BaseMap"))     mat.SetTextureScale("_BaseMap", new Vector2(4f, 4f));
            if (mat.HasProperty("_Smoothness"))  mat.SetFloat("_Smoothness", 0.25f);

            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneRebuilder] Mat_Floor.mat criado: " + matPath);
        }

        // ══════════════════════════════════════════════════════════
        // PAREDES
        // ══════════════════════════════════════════════════════════
        static void BuildWalls()
        {
            var root  = new GameObject("[Walls]");
            var wCol  = new Color(0.941f, 0.929f, 0.894f); // #F0EDE4
            const float H = 3.5f; // altura das paredes
            const float T = 0.3f; // espessura

            // Parede esquerda (X=-2), de Z=-3 a Z=32 → comprimento=35
            CreateBox("Wall_Left",  root.transform, new Vector3(-2f, H/2f, 14.5f), new Vector3(T, H, 35f), wCol);
            // Parede direita (X=18), de Z=-3 a Z=32
            CreateBox("Wall_Right", root.transform, new Vector3(18f, H/2f, 14.5f), new Vector3(T, H, 35f), wCol);

            // Parede traseira (Z=32) — sem porta
            CreateBox("Wall_Back",  root.transform, new Vector3(8f, H/2f, 32f), new Vector3(20f, H, T), wCol);

            // Parede frontal (Z=-3) com PORTA no centro (X:7-11, gap=4)
            // Lado esq: X=-2..7 → comprimento=9, centro=-2+(9/2)=2.5
            CreateBox("Wall_Front_Left",  root.transform, new Vector3(2.5f,  H/2f, -3f), new Vector3(9f,  H, T), wCol);
            // Lado dir: X=11..18 → comprimento=7, centro=11+(7/2)=14.5
            CreateBox("Wall_Front_Right", root.transform, new Vector3(14.5f, H/2f, -3f), new Vector3(7f,  H, T), wCol);
            // Verga da porta
            CreateBox("Door_Front_Lintel", root.transform, new Vector3(9f, H - 0.3f, -3f), new Vector3(4f, 0.6f, T), wCol);
        }

        // ══════════════════════════════════════════════════════════
        // RUA
        // ══════════════════════════════════════════════════════════
        static void BuildStreet()
        {
            var root = new GameObject("[Street]");

            CreatePlane("Sidewalk", root.transform,
                new Vector3(6f, 0.01f, 11), new Vector3(4, 1, 30),
                new Color(0.70f, 0.70f, 0.70f));

            CreatePlane("Road", root.transform,
                new Vector3(10.5f, 0, 11), new Vector3(6, 1, 34),
                new Color(0.28f, 0.28f, 0.28f));

            for (int i = 0; i < 8; i++)
                CreateBox($"RoadLine_{i}", root.transform,
                    new Vector3(10.5f, 0.01f, -5f + i * 4.5f),
                    new Vector3(0.12f, 0.01f, 2f),
                    new Color(0.95f, 0.90f, 0.20f));

            // ── Carros estacionados (Awb Low Poly Vehicles) ──────────
            string[] carPaths = {
                "Assets/Awb-Free Low Poly Vehicles/Prefabs/Hatchback Car_15.prefab",
                "Assets/Awb-Free Low Poly Vehicles/Prefabs/Sport Car_39.prefab",
                "Assets/Awb-Free Low Poly Vehicles/Prefabs/Classic Car_9.prefab",
                "Assets/Awb-Free Low Poly Vehicles/Prefabs/N Van_10.prefab",
            };
            float[] carZPositions = { 3f, 9f, 15f, 21f };
            Color[] carFallbackColors = {
                new Color(0.20f, 0.45f, 0.80f),
                new Color(0.80f, 0.20f, 0.25f),
                new Color(0.18f, 0.62f, 0.30f),
                new Color(0.55f, 0.55f, 0.18f),
            };
            for (int i = 0; i < carPaths.Length; i++)
            {
                var carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(carPaths[i]);
                if (carPrefab != null)
                {
                    var car = (GameObject)PrefabUtility.InstantiatePrefab(carPrefab);
                    car.name = $"Parked_Car_{i}";
                    car.transform.SetParent(root.transform);
                    car.transform.position = new Vector3(8.5f, 0f, carZPositions[i]);
                    car.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                    Debug.Log($"[SceneRebuilder] Carro {i}: {System.IO.Path.GetFileName(carPaths[i])}");
                }
                else
                {
                    CreateBox($"Car_Fallback_{i}", root.transform,
                        new Vector3(8.5f, 0.55f, carZPositions[i]),
                        new Vector3(1.8f, 1.1f, 3.8f), carFallbackColors[i]);
                    Debug.LogWarning($"[SceneRebuilder] Carro nao encontrado: {carPaths[i]} — usando primitiva.");
                }
            }
        }

        // ══════════════════════════════════════════════════════════
        // AMBIENTE — AREA DE REFEICOES E DECORACAO
        // ══════════════════════════════════════════════════════════
        static void BuildEnvironment()
        {
            var root = new GameObject("[Environment]");

            const string tablePrefabPath = "Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Table_01.prefab";
            const string chairPrefabPath = "Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Chair_01.prefab";
            const string shelfPrefabPath = "Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Shelf_01.prefab";

            var tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tablePrefabPath);
            var chairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(chairPrefabPath);
            var shelfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(shelfPrefabPath);

            Debug.Log($"[SceneRebuilder] Synty props: Table={tablePrefab != null}, Chair={chairPrefab != null}, Shelf={shelfPrefab != null}");

            // ── 2 mesas de refeicao + 4 cadeiras cada ─────────────────
            Vector3[] tablePositions = { new Vector3(4.2f, 0f, 19f), new Vector3(4.2f, 0f, 22f) };
            foreach (var tPos in tablePositions)
            {
                if (tablePrefab != null)
                {
                    var t = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab);
                    t.name = "Dining_Table";
                    t.transform.SetParent(root.transform);
                    t.transform.position = tPos;
                }
                else
                {
                    CreateBox("Dining_Table", root.transform, tPos + new Vector3(0, 0.42f, 0),
                        new Vector3(1.2f, 0.08f, 0.8f), new Color(0.7f, 0.55f, 0.35f));
                }

                var chairOffsets = new Vector3[] {
                    new Vector3(0f, 0f, -0.75f), new Vector3(0f, 0f, 0.75f),
                    new Vector3(-0.7f, 0f, 0f),  new Vector3(0.7f, 0f, 0f),
                };
                float[] chairYRots = { 0f, 180f, 90f, -90f };
                for (int ci = 0; ci < chairOffsets.Length; ci++)
                {
                    Vector3 cPos = tPos + chairOffsets[ci];
                    if (chairPrefab != null)
                    {
                        var c = (GameObject)PrefabUtility.InstantiatePrefab(chairPrefab);
                        c.name = "Dining_Chair";
                        c.transform.SetParent(root.transform);
                        c.transform.position = cPos;
                        c.transform.rotation = Quaternion.Euler(0f, chairYRots[ci], 0f);
                    }
                    else
                    {
                        CreateBox("Dining_Chair", root.transform, cPos + new Vector3(0, 0.22f, 0),
                            new Vector3(0.4f, 0.4f, 0.4f), new Color(0.45f, 0.32f, 0.18f));
                    }
                }
            }

            // ── Prateleiras na parede direita (X ~3.4) ────────────────
            Vector3[] shelfPositions = { new Vector3(3.4f, 1.1f, 5f), new Vector3(3.4f, 1.1f, 9f) };
            foreach (var sPos in shelfPositions)
            {
                if (shelfPrefab != null)
                {
                    var s = (GameObject)PrefabUtility.InstantiatePrefab(shelfPrefab);
                    s.name = "Wall_Shelf";
                    s.transform.SetParent(root.transform);
                    s.transform.position = sPos;
                    s.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else
                {
                    CreateBox("Wall_Shelf", root.transform, sPos,
                        new Vector3(0.1f, 0.08f, 1.2f), new Color(0.65f, 0.50f, 0.30f));
                }
            }

            // ── Plantas decorativas (vasinhos) ────────────────────────
            Vector3[] plantPositions = {
                new Vector3(-3.2f, 0f,  5f),
                new Vector3(-3.2f, 0f, 15f),
                new Vector3(-3.2f, 0f, 22f),
            };
            foreach (var pPos in plantPositions)
            {
                var plantRoot = new GameObject("Plant");
                plantRoot.transform.SetParent(root.transform);
                plantRoot.transform.position = pPos;
                CreateBoxChild("Pot",    plantRoot.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.3f, 0.3f, 0.3f), new Color(0.55f, 0.35f, 0.20f));
                CreateBoxChild("Leaves", plantRoot.transform, new Vector3(0f, 0.50f, 0f), new Vector3(0.38f, 0.38f, 0.38f), new Color(0.18f, 0.65f, 0.25f));
            }

            // ── Props de comida ithappy nas mesas ─────────────────────
            const string platePath = "Assets/ithappy/Food_Free/Prefabs/Plate_001.prefab";
            const string glassPath = "Assets/ithappy/Food_Free/Prefabs/Glass_001.prefab";
            var platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(platePath);
            var glassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(glassPath);
            Debug.Log($"[SceneRebuilder] ithappy food: Plate={platePrefab != null}, Glass={glassPrefab != null}");

            foreach (var tPos in tablePositions)
            {
                float tableTopY = tPos.y + 0.82f;
                if (platePrefab != null)
                {
                    var p = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
                    p.name = "Table_Plate";
                    p.transform.SetParent(root.transform);
                    p.transform.position = tPos + new Vector3(-0.25f, tableTopY - tPos.y, 0f);
                }
                if (glassPrefab != null)
                {
                    var g = (GameObject)PrefabUtility.InstantiatePrefab(glassPrefab);
                    g.name = "Table_Glass";
                    g.transform.SetParent(root.transform);
                    g.transform.position = tPos + new Vector3(0.25f, tableTopY - tPos.y, 0f);
                }
            }

            Debug.Log("[SceneRebuilder] Ambiente criado: mesas, cadeiras, prateleiras, plantas, props de comida.");
        }

        // ══════════════════════════════════════════════════════════
        // AREA DE MESAS PARA CLIENTES (lado direito, X:12-17)
        // ══════════════════════════════════════════════════════════
        static void BuildDiningArea()
        {
            var root = new GameObject("[DiningArea]");

            // Busca prefabs de mesa/cadeira no Synty
            string tablePrefabPath = FindSyntyPrefab("table", "chair");
            string chairPrefabPath = FindSyntyPrefab("chair");
            var tablePrefab = tablePrefabPath != null ? AssetDatabase.LoadAssetAtPath<GameObject>(tablePrefabPath) : null;
            var chairPrefab = chairPrefabPath != null ? AssetDatabase.LoadAssetAtPath<GameObject>(chairPrefabPath) : null;
            Debug.Log($"[SceneRebuilder] DiningArea — Mesa: {tablePrefabPath ?? "primitiva"}, Cadeira: {chairPrefabPath ?? "primitiva"}");

            var tablePositions = new Vector3[] {
                new Vector3(13f, 0f, 17f),
                new Vector3(16f, 0f, 17f),
                new Vector3(13f, 0f, 21f),
            };

            foreach (var tPos in tablePositions)
            {
                // Mesa
                if (tablePrefab != null)
                {
                    var t = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab);
                    t.name = "DiningTable"; t.transform.SetParent(root.transform); t.transform.position = tPos;
                }
                else
                {
                    var tRoot = new GameObject("DiningTable"); tRoot.transform.SetParent(root.transform); tRoot.transform.position = tPos;
                    CreateBoxChild("Top", tRoot.transform, new Vector3(0f, 0.45f, 0f), new Vector3(1.2f, 0.1f, 1.2f), new Color(0.545f, 0.416f, 0.078f));
                    CreateBoxChild("Leg", tRoot.transform, new Vector3(0f, 0.22f, 0f), new Vector3(0.1f, 0.44f, 0.1f),  new Color(0.45f, 0.33f, 0.10f));
                }
                // 2 Cadeiras (frente e costas)
                foreach (var cOff in new Vector3[] { new Vector3(0f,0f,-0.85f), new Vector3(0f,0f,0.85f) })
                {
                    Vector3 cPos = tPos + cOff;
                    if (chairPrefab != null)
                    {
                        var c = (GameObject)PrefabUtility.InstantiatePrefab(chairPrefab);
                        c.name = "DiningChair"; c.transform.SetParent(root.transform); c.transform.position = cPos;
                        c.transform.rotation = Quaternion.Euler(0f, cOff.z < 0 ? 0f : 180f, 0f);
                    }
                    else
                    {
                        CreateBox("DiningChair", root.transform, cPos + new Vector3(0f, 0.25f, 0f),
                            new Vector3(0.5f, 0.5f, 0.5f), new Color(0.365f, 0.251f, 0.216f));
                    }
                }
            }
            Debug.Log("[SceneRebuilder] BuildDiningArea: 3 mesas + 6 cadeiras criadas.");
        }

        // ══════════════════════════════════════════════════════════
        // DECORACAO DA PIZZARIA
        // ══════════════════════════════════════════════════════════
        static void BuildDecoration()
        {
            var root = new GameObject("[Decoration]");

            // 1. Plantas nos 4 cantos internos
            string plantPath = FindSyntyPrefab("plant", "flower", "bush");
            var plantPrefab = plantPath != null ? AssetDatabase.LoadAssetAtPath<GameObject>(plantPath) : null;
            var cornerPositions = new Vector3[] {
                new Vector3(1f, 0f, 30f), new Vector3(17f, 0f, 30f),
                new Vector3(1f, 0f,  0f), new Vector3(17f, 0f,  0f),
            };
            foreach (var pPos in cornerPositions)
            {
                if (plantPrefab != null)
                {
                    var p = (GameObject)PrefabUtility.InstantiatePrefab(plantPrefab);
                    p.name = "CornerPlant"; p.transform.SetParent(root.transform); p.transform.position = pPos;
                }
                else
                {
                    var pr = new GameObject("CornerPlant"); pr.transform.SetParent(root.transform); pr.transform.position = pPos;
                    CreateBoxChild("Pot",    pr.transform, new Vector3(0f,0.12f,0f), new Vector3(0.2f,0.24f,0.2f), new Color(0.55f,0.35f,0.20f));
                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.name = "Leaves"; sphere.transform.SetParent(pr.transform);
                    sphere.transform.localPosition = new Vector3(0f,0.55f,0f); sphere.transform.localScale = new Vector3(0.6f,0.8f,0.6f);
                    Object.DestroyImmediate(sphere.GetComponent<SphereCollider>());
                    ApplyColor(sphere, new Color(0.180f, 0.800f, 0.443f)); // #2ECC71
                }
            }

            // 2. Balcão de caixa perto da entrada (centro-baixo)
            var caixaRoot = new GameObject("Caixa"); caixaRoot.transform.SetParent(root.transform); caixaRoot.transform.position = new Vector3(9f, 0f, -1f);
            CreateBoxChild("Counter", caixaRoot.transform, new Vector3(0f,0.55f,0f), new Vector3(1.5f,1.1f,0.8f), new Color(0.204f,0.286f,0.369f)); // #34495E
            AddWorldLabel(caixaRoot.transform, "CAIXA", 1.3f);

            // 3. Quadro/menu na parede esquerda
            var board = GameObject.CreatePrimitive(PrimitiveType.Quad);
            board.name = "MenuBoard"; board.transform.SetParent(root.transform);
            board.transform.position = new Vector3(-1.85f, 2f, 15f);
            board.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            board.transform.localScale = new Vector3(2f, 1.5f, 1f);
            Object.DestroyImmediate(board.GetComponent<MeshCollider>());
            ApplyColor(board, new Color(0.102f, 0.145f, 0.184f)); // #1A252F

            Debug.Log($"[SceneRebuilder] Decoracao: plantas={plantPath ?? "primitiva"}, caixa, quadro criados.");
        }

        // Busca prefab no Synty que contenha qualquer uma das palavras-chave
        static string FindSyntyPrefab(params string[] keywords)
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Synty" });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string nameLow = Path.GetFileNameWithoutExtension(path).ToLower();
                foreach (var kw in keywords)
                    if (nameLow.Contains(kw.ToLower())) return path;
            }
            return null;
        }

        // ══════════════════════════════════════════════════════════
        // ESTACOES
        // ══════════════════════════════════════════════════════════
        static void BuildStations()
        {
            var root = new GameObject("[Stations]");

            // Paleta Pizza Ready (TAREFA 9)
            // ── 1. Campo de Trigo — canto inferior esquerdo ─────────────
            BuildStation<WheatFieldStation>("Station_WheatField", root.transform,
                pos: new Vector3(1f, 0f, 2f),
                visualSize: new Vector3(3f, 0.3f, 3f),
                color: new Color(0.545f, 0.765f, 0.290f), // #8BC34A
                label: "TRIGO",
                productionInterval: 0.25f,
                extraSetup: go =>
                {
                    var wf = go.GetComponent<WheatFieldStation>();
                    if (wf != null)
                    {
                        var so = new SerializedObject(wf);
                        so.FindProperty("_wheatRegenerateTime").floatValue = 0.3f;
                        so.FindProperty("_maxWheatStored").intValue = 20;
                        so.ApplyModifiedProperties();
                    }
                    go.AddComponent<StationFullIndicator>();
                    // Plantas decorativas ao redor
                    foreach (var off in new Vector3[] {
                        new Vector3(-0.8f,0f,-0.8f), new Vector3(0.8f,0f,-0.8f), new Vector3(0f,0f,0.9f) })
                    {
                        var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        p.name = "Wheat_Plant";
                        p.transform.SetParent(go.transform);
                        p.transform.localPosition = off + Vector3.up * 0.2f;
                        p.transform.localScale    = Vector3.one * 0.3f;
                        Object.DestroyImmediate(p.GetComponent<SphereCollider>());
                        ApplyColor(p, new Color(0.18f, 0.65f, 0.25f));
                    }
                });

            // ── 2. Estação de Massa — canto inferior direito ─────────────
            BuildStation<DoughStation>("Station_Dough", root.transform,
                pos: new Vector3(15f, 0f, 9f),
                visualSize: new Vector3(2f, 1f, 2f),
                color: new Color(0.831f, 0.663f, 0.416f), // #D4A96A
                label: "MASSA",
                productionInterval: 0.4f,
                extraSetup: go =>
                {
                    var dough = go.GetComponent<DoughStation>();
                    if (dough != null)
                    {
                        var so = new SerializedObject(dough);
                        so.FindProperty("_wheatPerDough").intValue = 1;
                        so.ApplyModifiedProperties();
                    }
                    go.AddComponent<StationFullIndicator>();
                });

            // ── 3. Mesa de Montagem — centro-esquerdo ────────────────────
            BuildStation<PizzaAssemblyStation>("Station_Assembly", root.transform,
                pos: new Vector3(2f, 0f, 16f),
                visualSize: new Vector3(2.5f, 1f, 2f),
                color: new Color(0.753f, 0.224f, 0.169f), // #C0392B
                label: "MONTAR",
                productionInterval: 0.4f,
                extraSetup: go => go.AddComponent<StationFullIndicator>());

            // ── 4. Forno — canto superior direito ────────────────────────
            BuildStation<OvenStation>("Station_Oven", root.transform,
                pos: new Vector3(15f, 0f, 24f),
                visualSize: new Vector3(2.5f, 2f, 2.5f),
                color: new Color(0.173f, 0.243f, 0.314f), // #2C3E50
                label: "FORNO",
                productionInterval: 0.4f,
                extraSetup: go =>
                {
                    var oven = go.GetComponent<OvenStation>();
                    if (oven != null)
                    {
                        var so = new SerializedObject(oven);
                        so.FindProperty("_cookingTime").floatValue = 4f;
                        so.FindProperty("_ovenSlots").intValue = 2;
                        so.ApplyModifiedProperties();
                    }
                    go.AddComponent<StationFullIndicator>();
                });

            // ── 5. Balcão de Entrega — canto superior esquerdo ───────────
            BuildStation<DeliveryStation>("Station_Delivery", root.transform,
                pos: new Vector3(2f, 0f, 29f),
                visualSize: new Vector3(4f, 1.2f, 1.5f),
                color: new Color(0.161f, 0.502f, 0.725f), // #2980B9
                label: "BALCAO",
                productionInterval: 0.3f,
                extraSetup: go =>
                {
                    go.AddComponent<StationFullIndicator>();
                    var dp = new GameObject("DeliveryPoint");
                    dp.transform.SetParent(go.transform);
                    dp.transform.localPosition = new Vector3(0f, 0f, -2f);
                });

            AddSyntyStationDecor(root.transform);
        }

        static void BuildStation<T>(
            string name, Transform parent, Vector3 pos,
            Vector3 visualSize, Color color, string label,
            float productionInterval = 1f,
            System.Action<GameObject> extraSetup = null) where T : BaseStation
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;

            CreateBoxChild("Visual", go.transform,
                new Vector3(0, visualSize.y / 2f, 0), visualSize, color);

            CreateBoxChild("Top", go.transform,
                new Vector3(0, visualSize.y + 0.02f, 0),
                new Vector3(visualSize.x + 0.1f, 0.06f, visualSize.z + 0.1f),
                Color.Lerp(color, Color.white, 0.4f));

            if (visualSize.y > 0.6f)
            {
                float lx = visualSize.x * 0.4f;
                float lz = visualSize.z * 0.4f;
                float legH = 0.3f;
                foreach (var corner in new Vector3[] {
                    new Vector3(-lx, legH / 2f, -lz), new Vector3(lx, legH / 2f, -lz),
                    new Vector3(-lx, legH / 2f,  lz), new Vector3(lx, legH / 2f,  lz) })
                {
                    CreateBoxChild("Leg", go.transform, corner,
                        new Vector3(0.18f, legH, 0.18f),
                        Color.Lerp(color, Color.black, 0.3f));
                }
            }

            AddWorldLabel(go.transform, label, visualSize.y + 1.2f);

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(visualSize.x + 1.2f, visualSize.y + 1f, visualSize.z + 1.2f);
            col.center = new Vector3(0, visualSize.y / 2f, 0);

            var station = go.AddComponent<T>();
            var so = new SerializedObject(station);
            so.FindProperty("_productionInterval").floatValue = productionInterval;
            var anchorProp = so.FindProperty("_itemStackAnchor");
            if (anchorProp != null)
            {
                var anchor = new GameObject("ItemAnchor");
                anchor.transform.SetParent(go.transform);
                anchor.transform.localPosition = new Vector3(0, visualSize.y + 0.4f, 0);
                anchorProp.objectReferenceValue = anchor.transform;
            }
            so.ApplyModifiedProperties();

            extraSetup?.Invoke(go);
        }

        // Coloca props Synty decorativos perto das estacoes (mesas, potes)
        static void AddSyntyStationDecor(Transform parent)
        {
            const string tablePath = "Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Table_01.prefab";
            const string potPath   = "Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Pot_01.prefab";
            const string breadPath = "Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Food_Bread_01.prefab";

            var tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tablePath);
            var potPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>(potPath);
            var breadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(breadPath);

            // Mesas laterais (X=1.5) ao lado das estacoes de producao
            var tableSpots = new (Vector3 pos, string name)[]
            {
                (new Vector3(1.8f, 0f,  1f),  "Decor_Table_Wheat"),
                (new Vector3(1.8f, 0f,  6f),  "Decor_Table_Dough"),
                (new Vector3(1.8f, 0f, 11f),  "Decor_Table_Assembly"),
            };
            foreach (var (pos, name) in tableSpots)
            {
                if (tablePrefab != null)
                {
                    var t = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab);
                    t.name = name;
                    t.transform.SetParent(parent);
                    t.transform.position = pos;
                    Debug.Log($"[SceneRebuilder] {name} colocado em {pos}");
                }
            }

            // Pote no forno (decorativo)
            if (potPrefab != null)
            {
                var pot = (GameObject)PrefabUtility.InstantiatePrefab(potPrefab);
                pot.name = "Decor_Pot_Oven";
                pot.transform.SetParent(parent);
                pot.transform.position = new Vector3(1.1f, 1.5f, 16f);
            }

            // Pao no balcao de entrega
            if (breadPrefab != null)
            {
                var bread = (GameObject)PrefabUtility.InstantiatePrefab(breadPrefab);
                bread.name = "Decor_Bread_Delivery";
                bread.transform.SetParent(parent);
                bread.transform.position = new Vector3(1.5f, 1.1f, 21f);
            }

            Debug.Log("[SceneRebuilder] Decoracao das estacoes concluida.");
        }

        // ══════════════════════════════════════════════════════════
        // PLAYER
        // ══════════════════════════════════════════════════════════
        static void BuildPlayer()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Player/Player.prefab");

            GameObject player = prefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
                : CreatePlayerFromScratch();

            player.name = "Player";
            player.transform.position = new Vector3(9f, 0.05f, 5f);
            if (!player.CompareTag("Player")) player.tag = "Player";

            var pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                var soPC = new SerializedObject(pc);
                var kb = soPC.FindProperty("_allowKeyboardInput");
                if (kb != null) kb.boolValue = true;
                var ms = soPC.FindProperty("_moveSpeed");
                if (ms != null && ms.floatValue < 5f) ms.floatValue = 6f;
                soPC.ApplyModifiedProperties();
            }

            var anim = player.GetComponent<PlayerAnimator>();
            if (anim != null)
            {
                var bodyChild = player.transform.Find("Body");
                if (bodyChild != null)
                {
                    var soAnim = new SerializedObject(anim);
                    var bodyProp = soAnim.FindProperty("_body");
                    if (bodyProp != null) bodyProp.objectReferenceValue = bodyChild;
                    soAnim.ApplyModifiedProperties();
                }
            }
        }

        static GameObject CreatePlayerFromScratch()
        {
            var root = new GameObject("Player");
            root.tag = "Player";

            CreateBoxChild("Body", root.transform,
                new Vector3(0, 0.45f, 0), new Vector3(0.42f, 0.65f, 0.30f),
                new Color(0.15f, 0.35f, 0.72f));

            CreateBoxChild("Head", root.transform,
                new Vector3(0, 1.02f, 0), new Vector3(0.35f, 0.35f, 0.30f),
                new Color(0.96f, 0.78f, 0.60f));

            CreateBoxChild("Hat_Brim", root.transform,
                new Vector3(0, 1.22f, 0), new Vector3(0.42f, 0.06f, 0.36f), Color.white);
            CreateBoxChild("Hat_Top", root.transform,
                new Vector3(0, 1.44f, 0), new Vector3(0.30f, 0.30f, 0.26f),
                new Color(0.90f, 0.18f, 0.18f));

            var stackPoint = new GameObject("StackPoint");
            stackPoint.transform.SetParent(root.transform);
            stackPoint.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            var rb = root.AddComponent<Rigidbody>();
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var col = root.AddComponent<CapsuleCollider>();
            col.height = 1.1f;
            col.radius = 0.22f;
            col.center = new Vector3(0, 0.55f, 0);

            var stacker = root.AddComponent<PlayerStacker>();
            var soS = new SerializedObject(stacker);
            soS.FindProperty("_stackAnchor").objectReferenceValue = stackPoint.transform;
            soS.FindProperty("_maxStackSize").intValue = 5;
            soS.FindProperty("_itemSpacingY").floatValue = 0.25f;
            soS.ApplyModifiedProperties();

            root.AddComponent<PlayerController>();
            root.AddComponent<PlayerAnimator>();
            return root;
        }

        // ══════════════════════════════════════════════════════════
        // CAMERA
        // ══════════════════════════════════════════════════════════
        static void BuildCamera()
        {
            var camGO = new GameObject("[Camera]");
            camGO.tag = "MainCamera";

            var cam = camGO.AddComponent<UnityEngine.Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 150f;
            cam.backgroundColor = new Color(0.65f, 0.85f, 0.95f);

            camGO.AddComponent<AudioListener>();
            camGO.transform.position = new Vector3(9f, 18f, 10f);
            camGO.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

            var follow = camGO.AddComponent<IsometricCameraFollow>();
            var so = new SerializedObject(follow);
            so.FindProperty("_height").floatValue   = 18f;
            so.FindProperty("_zOffset").floatValue  = -12f;
            so.FindProperty("_xOffset").floatValue  = 0f;
            so.FindProperty("_pitch").floatValue    = 55f;
            so.FindProperty("_yaw").floatValue      = 0f;
            so.FindProperty("_smoothTime").floatValue = 0.15f;
            so.FindProperty("_useBoundsZ").boolValue = false;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                so.FindProperty("_target").objectReferenceValue = player.transform;

            so.ApplyModifiedProperties();
        }

        // ══════════════════════════════════════════════════════════
        // ILUMINACAO
        // ══════════════════════════════════════════════════════════
        static void BuildLighting()
        {
            var sunGO = new GameObject("Directional Light");
            sunGO.transform.rotation = Quaternion.Euler(50f, -20f, 0f);
            var sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.4f;
            sun.color = new Color(1f, 0.97f, 0.88f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.6f;

            var fillGO = new GameObject("Fill Light");
            fillGO.transform.rotation = Quaternion.Euler(30f, 160f, 0f);
            var fill = fillGO.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.55f;
            fill.color = new Color(0.65f, 0.73f, 1f);
            fill.shadows = LightShadows.None;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.52f, 0.6f);
            RenderSettings.fog = false;
        }

        // ══════════════════════════════════════════════════════════
        // MANAGERS
        // ── ALTERADO: adicionados ComboSystem, DailyGoalSystem,
        //    ParticleManager e wiring de AudioClips via AssetDatabase
        // ══════════════════════════════════════════════════════════
        static void BuildManagers()
        {
            var root = new GameObject("[Managers]");
            root.AddComponent<GameManager>();
            root.AddComponent<SaveManager>();
            root.AddComponent<MoneyManager>();
            root.AddComponent<ItemPool>();
            root.AddComponent<UpgradeManager>();

            // ── Sistemas de gameplay ──────────────────────────────
            root.AddComponent<ComboSystem>();
            root.AddComponent<DailyGoalSystem>();
            root.AddComponent<ParticleManager>();

            // ── Managers adicionais (TAREFA 1) ────────────────────
            root.AddComponent<GameLoop>();
            root.AddComponent<TutorialManager>();
            root.AddComponent<MapExpansion>();
            root.AddComponent<RecipeSystem>();
            root.AddComponent<AchievementManager>();
            root.AddComponent<EventManager>();
            root.AddComponent<SeasonManager>();
            root.AddComponent<LeaderboardManager>();
            root.AddComponent<NotificationManager>();
            root.AddComponent<PerformanceManager>();
            root.AddComponent<MoneyPileSpawner>();

            // ── AudioManager com clips conectados ─────────────────
            var audio = root.AddComponent<AudioManager>();
            WireAudioClips(audio);

            // ── SceneLoader ───────────────────────────────────────
            root.AddComponent<SceneLoader>();

            // ── Workers (opcional — sem dependências obrigatórias) ─
            root.AddComponent<WorkerManager>();

            // ── NavMesh (opcional — pacote com.unity.ai.navigation) ─
            BuildNavMeshIfAvailable(root);

            // GameManager — começa direto no estado Playing
            var gm = root.GetComponent<GameManager>();
            if (gm != null)
            {
                var so = new SerializedObject(gm);
                var p = so.FindProperty("_initialState");
                if (p != null) { p.enumValueIndex = 1; so.ApplyModifiedProperties(); }
            }

            Debug.Log("[SceneRebuilder] Managers criados: GameManager, AudioManager, " +
                      "MoneyManager, ItemPool, UpgradeManager, ComboSystem, DailyGoalSystem, " +
                      "ParticleManager, GameLoop, TutorialManager, MapExpansion, RecipeSystem, " +
                      "AchievementManager, EventManager, SeasonManager, LeaderboardManager, " +
                      "NotificationManager, PerformanceManager, MoneyPileSpawner, WorkerManager.");
        }

        // Conecta os AudioClips dos arquivos WAV que existem no projeto
        static void WireAudioClips(AudioManager audio)
        {
            if (audio == null) return;
            var so = new SerializedObject(audio);

            TrySetClip(so, "_backgroundMusic",   "Assets/_Project/Audio/Music/Music_Background.wav");
            TrySetClip(so, "_coinPickupSFX",      "Assets/_Project/Audio/SFX/SFX_Coin.wav");
            TrySetClip(so, "_itemCollectSFX",     "Assets/_Project/Audio/SFX/SFX_ItemCollect.wav");
            TrySetClip(so, "_pizzaReadySFX",      "Assets/_Project/Audio/SFX/SFX_PizzaReady.wav");
            TrySetClip(so, "_customerHappySFX",   "Assets/_Project/Audio/SFX/SFX_CustomerHappy.wav");
            TrySetClip(so, "_buttonClickSFX",     "Assets/_Project/Audio/SFX/SFX_Click.wav");
            TrySetClip(so, "_upgradePurchaseSFX", "Assets/_Project/Audio/SFX/SFX_Upgrade.wav");

            so.ApplyModifiedProperties();
            Debug.Log("[SceneRebuilder] AudioManager: clips conectados automaticamente.");
        }

        static void TrySetClip(SerializedObject so, string fieldName, string assetPath)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null) return;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip != null)
                prop.objectReferenceValue = clip;
            else
                Debug.LogWarning($"[SceneRebuilder] AudioClip nao encontrado: {assetPath}");
        }

        // ══════════════════════════════════════════════════════════
        // SISTEMA DE CLIENTES
        // ══════════════════════════════════════════════════════════
        static void BuildCustomerSystem()
        {
            var root = new GameObject("[CustomerSystem]");

            var queueGO = new GameObject("CustomerQueue");
            queueGO.transform.SetParent(root.transform);
            queueGO.transform.position = new Vector3(2f, 0, 27f);

            var queue = queueGO.AddComponent<CustomerQueue>();
            var soQ = new SerializedObject(queue);

            var queueStart = new GameObject("QueueStart");
            queueStart.transform.SetParent(queueGO.transform);
            queueStart.transform.position = new Vector3(2f, 0, 27f);
            queueStart.transform.rotation = Quaternion.Euler(0, 180f, 0);

            soQ.FindProperty("_maxQueueSize").intValue = 5;
            soQ.FindProperty("_spacingBetweenCustomers").floatValue = 1.5f;
            soQ.FindProperty("_queueStart").objectReferenceValue = queueStart.transform;
            soQ.ApplyModifiedProperties();

            var delivery = Object.FindObjectOfType<DeliveryStation>();
            if (delivery != null)
            {
                var soD = new SerializedObject(delivery);
                soD.FindProperty("_customerQueue").objectReferenceValue = queue;
                soD.ApplyModifiedProperties();
            }

            var spawnerGO = new GameObject("CustomerSpawner");
            spawnerGO.transform.SetParent(root.transform);
            spawnerGO.transform.position = new Vector3(9f, 0f, 36f);

            var spawnPt = new GameObject("SpawnPoint");
            spawnPt.transform.SetParent(spawnerGO.transform);
            spawnPt.transform.position = new Vector3(9f, 0f, 36f);

            var spawner = spawnerGO.AddComponent<CustomerSpawner>();
            var soSp = new SerializedObject(spawner);

            var custPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Customers/Customer.prefab");
            if (custPrefab != null)
            {
                soSp.FindProperty("_customerPrefab").objectReferenceValue =
                    custPrefab.GetComponent<Customer>();
                Debug.Log("[SceneRebuilder] Customer prefab conectado.");
            }
            else
                Debug.LogError("[SceneRebuilder] Customer.prefab nao encontrado! Execute PizzaTycoon > 2. Create Prefabs.");

            soSp.FindProperty("_customerQueue").objectReferenceValue = queue;
            soSp.FindProperty("_spawnPoint").objectReferenceValue    = spawnPt.transform;
            soSp.FindProperty("_spawnInterval").floatValue           = 7f;
            soSp.FindProperty("_initialPoolSize").intValue           = 0;
            soSp.ApplyModifiedProperties();
        }

        // ══════════════════════════════════════════════════════════
        // HUD — Portrait Mobile (1080x1920)
        // ── ALTERADO: adicionados Daily Goal bar e Combo display
        // ══════════════════════════════════════════════════════════
        static void BuildUI()
        {
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var canvasGO = new GameObject("HUDCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Top bar (dinheiro) ──────────────────────────────────────
            var topBar = MakePanel(canvasGO.transform, "TopBar",
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                pivot: new Vector2(0.5f, 1f),
                anchPos: Vector2.zero, size: new Vector2(0, 100),
                color: new Color(0, 0, 0, 0.45f));

            var moneyText = MakeLabel(topBar.transform, "MoneyText", "$0",
                fontSize: 52, color: Color.white,
                anchMin: new Vector2(0.3f, 0), anchMax: new Vector2(0.9f, 1));
            moneyText.fontStyle = FontStyles.Bold;
            moneyText.alignment = TextAlignmentOptions.Center;

            // ── Daily Goal Bar (abaixo do top bar) ─────────────────────
            // Fundo da barra
            var goalBarBG = MakePanel(canvasGO.transform, "DailyGoalBG",
                anchorMin: new Vector2(0.05f, 1), anchorMax: new Vector2(0.95f, 1),
                pivot: new Vector2(0.5f, 1f),
                anchPos: new Vector2(0, -106f),
                size: new Vector2(0, 48f),
                color: new Color(0, 0, 0, 0.35f));

            // Preenchimento da barra (inicia com width = 0, HUDController atualiza via scale)
            // Estrutura do Slider: FillArea > Fill (necessário para Unity renderizar corretamente)
            var fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(goalBarBG.transform, false);
            var fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0, 0);
            fillAreaRT.anchorMax = new Vector2(1, 1);
            fillAreaRT.offsetMin = Vector2.zero;
            fillAreaRT.offsetMax = Vector2.zero;

            var goalFillGO = new GameObject("Fill");
            goalFillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillRT = goalFillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = new Vector2(0, 0);
            fillRT.anchorMax = new Vector2(0, 1);
            fillRT.pivot = new Vector2(0, 0.5f);
            fillRT.anchoredPosition = Vector2.zero;
            fillRT.sizeDelta = new Vector2(0, 0);
            var fillImg = goalFillGO.AddComponent<Image>();
            fillImg.color = new Color(0.25f, 0.82f, 0.35f);

            // Slider para fill proporcional
            var goalSlider = goalBarBG.AddComponent<Slider>();
            goalSlider.direction = Slider.Direction.LeftToRight;
            goalSlider.minValue = 0f;
            goalSlider.maxValue = 1f;
            goalSlider.value = 0f;
            goalSlider.interactable = false;
            goalSlider.fillRect = fillRT;

            // Texto da meta
            var goalText = MakeLabel(goalBarBG.transform, "DailyGoalText",
                "Meta: Entregar 10 pizzas",
                fontSize: 26, color: Color.white,
                anchMin: new Vector2(0.02f, 0), anchMax: new Vector2(0.98f, 1));
            goalText.alignment = TextAlignmentOptions.Center;

            // ── Combo display (canto superior esquerdo) ────────────────
            // Só aparece quando combo >= 2
            var comboGO = new GameObject("ComboDisplay");
            comboGO.transform.SetParent(canvasGO.transform, false);
            var comboRT = comboGO.AddComponent<RectTransform>();
            comboRT.anchorMin = new Vector2(0, 1);
            comboRT.anchorMax = new Vector2(0, 1);
            comboRT.pivot = new Vector2(0, 1);
            comboRT.anchoredPosition = new Vector2(20f, -160f);
            comboRT.sizeDelta = new Vector2(280f, 100f);
            var comboBG = comboGO.AddComponent<Image>();
            comboBG.color = new Color(0.95f, 0.60f, 0.05f, 0.88f);

            var comboText = MakeLabel(comboGO.transform, "ComboText", "x1.0",
                fontSize: 52, color: Color.white,
                anchMin: Vector2.zero, anchMax: Vector2.one);
            comboText.fontStyle = FontStyles.Bold;
            comboText.alignment = TextAlignmentOptions.Center;

            var comboCountText = MakeLabel(comboGO.transform, "ComboCountText", "COMBO",
                fontSize: 22, color: new Color(1f, 1f, 0.8f),
                anchMin: new Vector2(0, 0), anchMax: new Vector2(1, 0.3f));
            comboCountText.alignment = TextAlignmentOptions.Center;

            // Começa oculto — só aparece quando combo >= 2
            comboGO.SetActive(false);

            // ── Settings Button (topo-esquerda, TAREFA 5) ─────────────────
            var settingsGO = new GameObject("SettingsButton");
            settingsGO.transform.SetParent(canvasGO.transform, false);
            var settingsRT = settingsGO.AddComponent<RectTransform>();
            settingsRT.anchorMin = new Vector2(0, 1);
            settingsRT.anchorMax = new Vector2(0, 1);
            settingsRT.pivot     = new Vector2(0, 1);
            settingsRT.anchoredPosition = new Vector2(10f, -10f);
            settingsRT.sizeDelta = new Vector2(100, 100);
            var settingsImg = settingsGO.AddComponent<Image>();
            settingsImg.color = new Color(0.2f, 0.2f, 0.2f, 0.75f);
            var settingsBtn = settingsGO.AddComponent<Button>();
            settingsBtn.targetGraphic = settingsImg;
            var settingsTxtGO = new GameObject("Text");
            settingsTxtGO.transform.SetParent(settingsGO.transform, false);
            var settingsTxt = settingsTxtGO.AddComponent<TextMeshProUGUI>();
            settingsTxt.text      = "ST";
            settingsTxt.fontSize  = 36;
            settingsTxt.fontStyle = FontStyles.Bold;
            settingsTxt.color     = Color.white;
            settingsTxt.alignment = TextAlignmentOptions.Center;
            var settingsTxtRT = settingsTxtGO.GetComponent<RectTransform>();
            settingsTxtRT.anchorMin = Vector2.zero;
            settingsTxtRT.anchorMax = Vector2.one;
            settingsTxtRT.sizeDelta = Vector2.zero;

            // ── Map Button (abaixo do Settings, TAREFA 5) ─────────────────
            var mapGO = new GameObject("MapButton");
            mapGO.transform.SetParent(canvasGO.transform, false);
            var mapRT = mapGO.AddComponent<RectTransform>();
            mapRT.anchorMin = new Vector2(0, 1);
            mapRT.anchorMax = new Vector2(0, 1);
            mapRT.pivot     = new Vector2(0, 1);
            mapRT.anchoredPosition = new Vector2(10f, -120f);
            mapRT.sizeDelta = new Vector2(100, 100);
            var mapImg = mapGO.AddComponent<Image>();
            mapImg.color = new Color(0.945f, 0.769f, 0.059f); // #F1C40F
            var mapBtn = mapGO.AddComponent<Button>();
            mapBtn.targetGraphic = mapImg;
            var mapTxtGO = new GameObject("Text");
            mapTxtGO.transform.SetParent(mapGO.transform, false);
            var mapTxt = mapTxtGO.AddComponent<TextMeshProUGUI>();
            mapTxt.text      = "MAP";
            mapTxt.fontSize  = 32;
            mapTxt.fontStyle = FontStyles.Bold;
            mapTxt.color     = Color.white;
            mapTxt.alignment = TextAlignmentOptions.Center;
            var mapTxtRT = mapTxtGO.GetComponent<RectTransform>();
            mapTxtRT.anchorMin = Vector2.zero;
            mapTxtRT.anchorMax = Vector2.one;
            mapTxtRT.sizeDelta = Vector2.zero;

            // ── Tutorial Toast Panel (TAREFA 6) ───────────────────────────
            var tutPanel = new GameObject("TutorialPanel");
            tutPanel.transform.SetParent(canvasGO.transform, false);
            var tutPanelRT = tutPanel.AddComponent<RectTransform>();
            tutPanelRT.anchorMin        = new Vector2(0.05f, 0.12f);
            tutPanelRT.anchorMax        = new Vector2(0.95f, 0.22f);
            tutPanelRT.offsetMin        = Vector2.zero;
            tutPanelRT.offsetMax        = Vector2.zero;
            var tutBG = tutPanel.AddComponent<Image>();
            tutBG.color = new Color(0, 0, 0, 0.72f);

            var tutTextGO = new GameObject("TutorialText");
            tutTextGO.transform.SetParent(tutPanel.transform, false);
            var tutText = tutTextGO.AddComponent<TextMeshProUGUI>();
            tutText.text      = "Colete o trigo!";
            tutText.fontSize  = 38;
            tutText.color     = Color.white;
            tutText.alignment = TextAlignmentOptions.Center;
            var tutTextRT = tutTextGO.GetComponent<RectTransform>();
            tutTextRT.anchorMin = Vector2.zero;
            tutTextRT.anchorMax = Vector2.one;
            tutTextRT.offsetMin = new Vector2(10, 4);
            tutTextRT.offsetMax = new Vector2(-10, -4);

            tutPanel.SetActive(false);

            // ── Pause Panel (abre ao clicar Settings — TAREFA 5) ──────────
            var pausePanel = new GameObject("PausePanel");
            pausePanel.transform.SetParent(canvasGO.transform, false);
            var pausePanelRT = pausePanel.AddComponent<RectTransform>();
            pausePanelRT.anchorMin = Vector2.zero;
            pausePanelRT.anchorMax = Vector2.one;
            pausePanelRT.offsetMin = Vector2.zero;
            pausePanelRT.offsetMax = Vector2.zero;
            var pauseBG = pausePanel.AddComponent<Image>();
            pauseBG.color = new Color(0f, 0f, 0f, 0.75f);

            // Botao RETOMAR
            var resumeGO = new GameObject("ResumeButton");
            resumeGO.transform.SetParent(pausePanel.transform, false);
            var resumeRT = resumeGO.AddComponent<RectTransform>();
            resumeRT.anchorMin = new Vector2(0.25f, 0.52f);
            resumeRT.anchorMax = new Vector2(0.75f, 0.62f);
            resumeRT.offsetMin = Vector2.zero;
            resumeRT.offsetMax = Vector2.zero;
            var resumeImg = resumeGO.AddComponent<Image>();
            resumeImg.color = new Color(0.16f, 0.72f, 0.27f);
            var resumeBtn = resumeGO.AddComponent<Button>();
            resumeBtn.targetGraphic = resumeImg;
            var resumeTxtGO = new GameObject("Text");
            resumeTxtGO.transform.SetParent(resumeGO.transform, false);
            var resumeTxt = resumeTxtGO.AddComponent<TextMeshProUGUI>();
            resumeTxt.text = "RETOMAR";
            resumeTxt.fontSize = 44;
            resumeTxt.fontStyle = FontStyles.Bold;
            resumeTxt.color = Color.white;
            resumeTxt.alignment = TextAlignmentOptions.Center;
            var resumeTxtRT = resumeTxtGO.GetComponent<RectTransform>();
            resumeTxtRT.anchorMin = Vector2.zero;
            resumeTxtRT.anchorMax = Vector2.one;
            resumeTxtRT.sizeDelta = Vector2.zero;

            // Botao MENU PRINCIPAL
            var menuBtnGO = new GameObject("MainMenuButton");
            menuBtnGO.transform.SetParent(pausePanel.transform, false);
            var menuBtnRT = menuBtnGO.AddComponent<RectTransform>();
            menuBtnRT.anchorMin = new Vector2(0.25f, 0.40f);
            menuBtnRT.anchorMax = new Vector2(0.75f, 0.50f);
            menuBtnRT.offsetMin = Vector2.zero;
            menuBtnRT.offsetMax = Vector2.zero;
            var menuBtnImg = menuBtnGO.AddComponent<Image>();
            menuBtnImg.color = new Color(0.75f, 0.22f, 0.17f);
            var menuBtn = menuBtnGO.AddComponent<Button>();
            menuBtn.targetGraphic = menuBtnImg;
            var menuBtnTxtGO = new GameObject("Text");
            menuBtnTxtGO.transform.SetParent(menuBtnGO.transform, false);
            var menuBtnTxt = menuBtnTxtGO.AddComponent<TextMeshProUGUI>();
            menuBtnTxt.text = "MENU PRINCIPAL";
            menuBtnTxt.fontSize = 36;
            menuBtnTxt.fontStyle = FontStyles.Bold;
            menuBtnTxt.color = Color.white;
            menuBtnTxt.alignment = TextAlignmentOptions.Center;
            var menuBtnTxtRT = menuBtnTxtGO.GetComponent<RectTransform>();
            menuBtnTxtRT.anchorMin = Vector2.zero;
            menuBtnTxtRT.anchorMax = Vector2.one;
            menuBtnTxtRT.sizeDelta = Vector2.zero;

            pausePanel.SetActive(false);

            // ── HUDController — wire de todos os campos ─────────────────
            var hud = canvasGO.AddComponent<HUDController>();
            var soHUD = new SerializedObject(hud);
            soHUD.FindProperty("_moneyText").objectReferenceValue = moneyText;

            // Daily Goal
            var propGoalSlider  = soHUD.FindProperty("_dailyGoalSlider");
            var propGoalText    = soHUD.FindProperty("_dailyGoalText");
            if (propGoalSlider != null) propGoalSlider.objectReferenceValue = goalSlider;
            if (propGoalText   != null) propGoalText.objectReferenceValue   = goalText;

            // Combo
            var propComboRoot  = soHUD.FindProperty("_comboDisplay");
            var propComboMult  = soHUD.FindProperty("_comboMultiplierText");
            var propComboCount = soHUD.FindProperty("_comboCountText");
            if (propComboRoot  != null) propComboRoot.objectReferenceValue  = comboGO;
            if (propComboMult  != null) propComboMult.objectReferenceValue  = comboText;
            if (propComboCount != null) propComboCount.objectReferenceValue = comboCountText;

            soHUD.ApplyModifiedProperties();

            // ── Joystick ───────────────────────────────────────────────
            var joyPanel = new GameObject("JoystickPanel");
            joyPanel.transform.SetParent(canvasGO.transform, false);
            var joyRT = joyPanel.AddComponent<RectTransform>();
            joyRT.anchorMin = new Vector2(0, 0);
            joyRT.anchorMax = new Vector2(0.55f, 0.35f);
            joyRT.offsetMin = Vector2.zero;
            joyRT.offsetMax = Vector2.zero;
            var joyImg = joyPanel.AddComponent<Image>();
            joyImg.color = new Color(0, 0, 0, 0);

            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(joyPanel.transform, false);
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.sizeDelta = new Vector2(250, 250);
            bgRT.anchoredPosition = new Vector2(120, 120);
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(1, 1, 1, 0.20f);

            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(bgGO.transform, false);
            var handleRT = handleGO.AddComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(100, 100);
            handleRT.anchoredPosition = Vector2.zero;
            var handleImg = handleGO.AddComponent<Image>();
            handleImg.color = new Color(1, 1, 1, 0.55f);

            var joystick = joyPanel.AddComponent<JoystickController>();
            var soJoy = new SerializedObject(joystick);
            soJoy.FindProperty("_background").objectReferenceValue = bgRT;
            soJoy.FindProperty("_handle").objectReferenceValue = handleRT;
            soJoy.FindProperty("_dynamicPositioning").boolValue = true;
            soJoy.FindProperty("_deadZone").floatValue = 0.08f;
            soJoy.ApplyModifiedProperties();

            var pc = Object.FindObjectOfType<PlayerController>();
            if (pc != null)
            {
                var soPC = new SerializedObject(pc);
                soPC.FindProperty("_joystick").objectReferenceValue = joystick;
                soPC.ApplyModifiedProperties();
            }

            // ── Botao UPGRADE ──────────────────────────────────────────
            var upGO = new GameObject("UpgradeButton");
            upGO.transform.SetParent(canvasGO.transform, false);
            var upRT = upGO.AddComponent<RectTransform>();
            upRT.anchorMin = new Vector2(1, 0);
            upRT.anchorMax = new Vector2(1, 0);
            upRT.pivot = new Vector2(1, 0);
            upRT.anchoredPosition = new Vector2(-40, 60);
            upRT.sizeDelta = new Vector2(200, 90);
            var upImg = upGO.AddComponent<Image>();
            upImg.color = new Color(0.12f, 0.72f, 0.12f);
            var upBtn = upGO.AddComponent<Button>();
            upBtn.targetGraphic = upImg;

            var upTxtGO = new GameObject("Text");
            upTxtGO.transform.SetParent(upGO.transform, false);
            var upTxt = upTxtGO.AddComponent<TextMeshProUGUI>();
            upTxt.text = "UPGRADE";
            upTxt.fontSize = 36;
            upTxt.fontStyle = FontStyles.Bold;
            upTxt.color = Color.white;
            upTxt.alignment = TextAlignmentOptions.Center;
            var upTxtRT = upTxtGO.GetComponent<RectTransform>();
            upTxtRT.anchorMin = Vector2.zero;
            upTxtRT.anchorMax = Vector2.one;
            upTxtRT.sizeDelta = Vector2.zero;

            var hudButtonSo = new SerializedObject(hud);
            hudButtonSo.FindProperty("_upgradeButton").objectReferenceValue  = upBtn;
            var propSettings   = hudButtonSo.FindProperty("_settingsButton");
            var propMap        = hudButtonSo.FindProperty("_mapButton");
            var propPausePanel = hudButtonSo.FindProperty("_pausePanel");
            var propResume     = hudButtonSo.FindProperty("_resumeButton");
            var propMenuBtn    = hudButtonSo.FindProperty("_mainMenuButton");
            if (propSettings   != null) propSettings.objectReferenceValue   = settingsBtn;
            if (propMap        != null) propMap.objectReferenceValue         = mapBtn;
            if (propPausePanel != null) propPausePanel.objectReferenceValue  = pausePanel;
            if (propResume     != null) propResume.objectReferenceValue      = resumeBtn;
            if (propMenuBtn    != null) propMenuBtn.objectReferenceValue     = menuBtn;
            hudButtonSo.ApplyModifiedProperties();

            BuildUpgradePanel();

            var floatingTextGo = new GameObject("[FloatingText_Pool]");
            floatingTextGo.AddComponent<FloatingText>();

            Debug.Log("[SceneRebuilder] HUD criado com Daily Goal bar, Combo display, " +
                      "Settings/Map buttons, TutorialPanel e FloatingText.");
        }

        // ══════════════════════════════════════════════════════════
        // UPGRADE PANEL
        // ══════════════════════════════════════════════════════════
        static void BuildUpgradePanel()
        {
            var canvasGO = new GameObject("UpgradePanelCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;
            canvasGO.AddComponent<GraphicRaycaster>();

            var controllerGO = new GameObject("UpgradePanelController");
            controllerGO.transform.SetParent(canvasGO.transform, false);
            controllerGO.AddComponent<RectTransform>();

            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(canvasGO.transform, false);
            var ovRT = overlay.AddComponent<RectTransform>();
            ovRT.anchorMin = Vector2.zero;
            ovRT.anchorMax = Vector2.one;
            ovRT.offsetMin = Vector2.zero;
            ovRT.offsetMax = Vector2.zero;
            var ovImg = overlay.AddComponent<Image>();
            ovImg.color = new Color(0, 0, 0, 0.6f);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(overlay.transform, false);
            var pnRT = panel.AddComponent<RectTransform>();
            pnRT.anchorMin = new Vector2(0.5f, 0.5f);
            pnRT.anchorMax = new Vector2(0.5f, 0.5f);
            pnRT.pivot = new Vector2(0.5f, 0.5f);
            pnRT.sizeDelta = new Vector2(900, 1400);
            var pnImg = panel.AddComponent<Image>();
            pnImg.color = new Color(0.96f, 0.96f, 0.92f, 1f);

            var header = new GameObject("Header");
            header.transform.SetParent(panel.transform, false);
            var hdRT = header.AddComponent<RectTransform>();
            hdRT.anchorMin = new Vector2(0, 1);
            hdRT.anchorMax = new Vector2(1, 1);
            hdRT.pivot = new Vector2(0.5f, 1);
            hdRT.anchoredPosition = Vector2.zero;
            hdRT.sizeDelta = new Vector2(0, 120);
            var hdImg = header.AddComponent<Image>();
            hdImg.color = new Color(0.18f, 0.52f, 0.90f);

            var title = MakeLabel(header.transform, "Title", "UPGRADES", 56, Color.white,
                new Vector2(0, 0), new Vector2(1, 1));
            title.fontStyle = FontStyles.Bold;

            var closeGO = new GameObject("CloseButton");
            closeGO.transform.SetParent(header.transform, false);
            var clRT = closeGO.AddComponent<RectTransform>();
            clRT.anchorMin = new Vector2(1, 0.5f);
            clRT.anchorMax = new Vector2(1, 0.5f);
            clRT.pivot = new Vector2(1, 0.5f);
            clRT.anchoredPosition = new Vector2(-20, 0);
            clRT.sizeDelta = new Vector2(80, 80);
            var clImg = closeGO.AddComponent<Image>();
            clImg.color = new Color(0.90f, 0.20f, 0.20f);
            var clBtn = closeGO.AddComponent<Button>();
            clBtn.targetGraphic = clImg;

            var clTxt = MakeLabel(closeGO.transform, "X", "X", 48, Color.white,
                Vector2.zero, Vector2.one);
            clTxt.fontStyle = FontStyles.Bold;

            var scroll = new GameObject("Scroll");
            scroll.transform.SetParent(panel.transform, false);
            var scRT = scroll.AddComponent<RectTransform>();
            scRT.anchorMin = new Vector2(0, 0);
            scRT.anchorMax = new Vector2(1, 1);
            scRT.offsetMin = new Vector2(20, 20);
            scRT.offsetMax = new Vector2(-20, -130);
            var scImg = scroll.AddComponent<Image>();
            scImg.color = new Color(0, 0, 0, 0.05f);
            var sr = scroll.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            scroll.AddComponent<Mask>().showMaskGraphic = true;

            var content = new GameObject("Content");
            content.transform.SetParent(scroll.transform, false);
            var ctRT = content.AddComponent<RectTransform>();
            ctRT.anchorMin = new Vector2(0, 1);
            ctRT.anchorMax = new Vector2(1, 1);
            ctRT.pivot = new Vector2(0.5f, 1);
            ctRT.anchoredPosition = Vector2.zero;
            ctRT.sizeDelta = new Vector2(0, 0);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = ctRT;

            var template = BuildUpgradeItemTemplate(canvasGO.transform);
            template.SetActive(false);
            var templateUI = template.GetComponent<UpgradeItemUI>();

            var panelUI = controllerGO.AddComponent<UpgradePanelUI>();
            var soP = new SerializedObject(panelUI);
            soP.FindProperty("_panel").objectReferenceValue = overlay;
            soP.FindProperty("_upgradeListParent").objectReferenceValue = content.transform;
            soP.FindProperty("_upgradeItemPrefab").objectReferenceValue = templateUI;
            soP.FindProperty("_closeButton").objectReferenceValue = clBtn;
            soP.ApplyModifiedProperties();

            var ovBtn = overlay.AddComponent<Button>();
            ovBtn.targetGraphic = ovImg;
            UnityEventTools.AddPersistentListener(ovBtn.onClick, panelUI.Close);

            overlay.SetActive(false);
        }

        static GameObject BuildUpgradeItemTemplate(Transform parent)
        {
            var item = new GameObject("UpgradeItem_Template");
            item.transform.SetParent(parent, false);
            var itRT = item.AddComponent<RectTransform>();
            itRT.sizeDelta = new Vector2(800, 200);
            var bg = item.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 1f);
            var le = item.AddComponent<LayoutElement>();
            le.minHeight = 200;
            le.preferredHeight = 200;

            var name = MakeLabel(item.transform, "Name", "Nome", 38, new Color(0.15f, 0.15f, 0.15f),
                new Vector2(0, 0.6f), new Vector2(0.65f, 1f));
            name.fontStyle = FontStyles.Bold;
            name.alignment = TextAlignmentOptions.TopLeft;
            ((RectTransform)name.transform).offsetMin = new Vector2(20, 0);

            var desc = MakeLabel(item.transform, "Description", "Descricao", 24,
                new Color(0.35f, 0.35f, 0.35f),
                new Vector2(0, 0.2f), new Vector2(0.65f, 0.6f));
            desc.alignment = TextAlignmentOptions.TopLeft;
            ((RectTransform)desc.transform).offsetMin = new Vector2(20, 0);

            var lvl = MakeLabel(item.transform, "Level", "Nivel 0/5", 22,
                new Color(0.40f, 0.40f, 0.40f),
                new Vector2(0, 0), new Vector2(0.65f, 0.2f));
            lvl.alignment = TextAlignmentOptions.BottomLeft;
            ((RectTransform)lvl.transform).offsetMin = new Vector2(20, 8);

            var btnGO = new GameObject("BuyButton");
            btnGO.transform.SetParent(item.transform, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.65f, 0.15f);
            btnRT.anchorMax = new Vector2(1f, 0.85f);
            btnRT.offsetMin = new Vector2(10, 0);
            btnRT.offsetMax = new Vector2(-20, 0);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.18f, 0.72f, 0.18f);
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var btnColors = btn.colors;
            btnColors.disabledColor = new Color(0.6f, 0.6f, 0.6f);
            btn.colors = btnColors;

            var cost = MakeLabel(btnGO.transform, "Cost", "$100", 40, Color.white,
                Vector2.zero, Vector2.one);
            cost.fontStyle = FontStyles.Bold;

            var maxGO = new GameObject("MaxLevelLabel");
            maxGO.transform.SetParent(item.transform, false);
            var mxRT = maxGO.AddComponent<RectTransform>();
            mxRT.anchorMin = new Vector2(0.65f, 0.15f);
            mxRT.anchorMax = new Vector2(1f, 0.85f);
            mxRT.offsetMin = new Vector2(10, 0);
            mxRT.offsetMax = new Vector2(-20, 0);
            var mxImg = maxGO.AddComponent<Image>();
            mxImg.color = new Color(0.50f, 0.50f, 0.50f);
            var mxTxt = MakeLabel(maxGO.transform, "MaxText", "MAX", 44, Color.white,
                Vector2.zero, Vector2.one);
            mxTxt.fontStyle = FontStyles.Bold;
            maxGO.SetActive(false);

            var ui = item.AddComponent<UpgradeItemUI>();
            var soUI = new SerializedObject(ui);
            soUI.FindProperty("_nameText").objectReferenceValue = name;
            soUI.FindProperty("_descriptionText").objectReferenceValue = desc;
            soUI.FindProperty("_levelText").objectReferenceValue = lvl;
            soUI.FindProperty("_costText").objectReferenceValue = cost;
            soUI.FindProperty("_buyButton").objectReferenceValue = btn;
            soUI.FindProperty("_maxLevelLabel").objectReferenceValue = maxGO;
            soUI.ApplyModifiedProperties();

            return item;
        }

        // ══════════════════════════════════════════════════════════
        // RECRIAR CUSTOMER PREFAB COM VISUAL ATUALIZADO
        // ══════════════════════════════════════════════════════════
        static void RecreateCustomerPrefab()
        {
            const string prefabPath = "Assets/_Project/Prefabs/Customers/Customer.prefab";

            // Deleta o prefab existente para forçar recriação com o novo visual
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(prefabPath)))
            {
                AssetDatabase.DeleteAsset(prefabPath);
                AssetDatabase.Refresh();
                Debug.Log("[SceneRebuilder] Customer.prefab deletado para recriação.");
            }

            // Reconstrói diretamente (não chama PrefabBuilder para evitar dependência)
            var root = new GameObject("Customer");
            ColorUtility.TryParseHtmlString("#7A8FA6", out Color bodyColor);

            // Visual — corpo cápsula
            var bodyGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bodyGO.name = "Visual";
            bodyGO.transform.SetParent(root.transform);
            bodyGO.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            bodyGO.transform.localScale = new Vector3(0.4f, 0.7f, 0.4f);
            Object.DestroyImmediate(bodyGO.GetComponent<CapsuleCollider>());
            ApplyColor(bodyGO.GetComponent<MeshRenderer>(), bodyColor);

            // Visual — cabeça esfera
            var headGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headGO.name = "Head";
            headGO.transform.SetParent(root.transform);
            headGO.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            headGO.transform.localScale = Vector3.one * 0.35f;
            Object.DestroyImmediate(headGO.GetComponent<SphereCollider>());
            ApplyColor(headGO.GetComponent<MeshRenderer>(), new Color(0.90f, 0.75f, 0.60f));

            // Componentes lógicos
            root.AddComponent<Customer>();
            var col = root.AddComponent<CapsuleCollider>();
            col.height = 1.1f; col.radius = 0.22f;
            col.center = new Vector3(0f, 0.55f, 0f);
            col.isTrigger = false;

            // Barra de paciência (canvas world-space mínimo)
            var canvasGO = new GameObject("PatienceCanvas");
            canvasGO.transform.SetParent(root.transform);
            canvasGO.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            canvasGO.transform.localScale = Vector3.one * 0.01f;
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvasGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 30f);

            var sliderGO = new GameObject("PatienceSlider");
            sliderGO.transform.SetParent(canvasGO.transform);
            var slider = sliderGO.AddComponent<Slider>();
            var sliderRT = sliderGO.GetComponent<RectTransform>();
            sliderRT.anchorMin = Vector2.zero; sliderRT.anchorMax = Vector2.one;
            sliderRT.offsetMin = sliderRT.offsetMax = Vector2.zero;
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;

            var soC = new SerializedObject(root.GetComponent<Customer>());
            var patSliderProp = soC.FindProperty("_patienceSlider");
            if (patSliderProp != null) patSliderProp.objectReferenceValue = slider;
            soC.ApplyModifiedProperties();

            // Salva como prefab
            Directory.CreateDirectory("Assets/_Project/Prefabs/Customers");
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            Debug.Log("[SceneRebuilder] Customer.prefab recriado com visual azul-acinzentado.");
        }

        static void ApplyColor(MeshRenderer rend, Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                ?? Shader.Find("Standard"));
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            rend.sharedMaterial = mat;
        }

        // ══════════════════════════════════════════════════════════
        // ANIMATOR CONTROLLER DO PLAYER
        // ══════════════════════════════════════════════════════════
        static void SetupPlayerAnimator()
        {
            const string animFbx = "Assets/_Project/Models/Meshy_AI_Red_Cap_Explorer_biped_Animation_Walking_withSkin.fbx";
            const string ctrlPath = "Assets/_Project/Animations/PlayerAnimator.controller";

            // Garante pasta
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Animations"))
                AssetDatabase.CreateFolder("Assets/_Project", "Animations");

            // Encontra clip de caminhada no FBX
            AnimationClip walkClip = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(animFbx))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    walkClip = clip;
                    Debug.Log($"[SceneRebuilder] Clip de animação: '{clip.name}'");
                    break;
                }
            }
            if (walkClip == null)
                Debug.LogWarning("[SceneRebuilder] Clip de caminhada não encontrado em: " + animFbx);

            // Cria ou carrega o controller
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
            if (ctrl == null)
            {
                ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                Debug.Log("[SceneRebuilder] AnimatorController criado: " + ctrlPath);
            }

            // Garante parâmetro Speed
            bool hasSpeed = false;
            foreach (var p in ctrl.parameters)
                if (p.name == "Speed") { hasSpeed = true; break; }
            if (!hasSpeed)
                ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);

            // Limpa estados existentes e recria
            var sm = ctrl.layers[0].stateMachine;
            foreach (var s in sm.states) sm.RemoveState(s.state);
            foreach (var t in sm.anyStateTransitions) sm.RemoveAnyStateTransition(t);

            var idleState = sm.AddState("Idle");
            idleState.motion = null;

            var walkState = sm.AddState("Walk");
            if (walkClip != null) walkState.motion = walkClip;

            sm.defaultState = idleState;

            var toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0.15f;
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.15f;
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneRebuilder] AnimatorController configurado com estados Idle/Walk e parâmetro Speed.");

            // Conecta no prefab Player
            const string playerPrefab = "Assets/_Project/Prefabs/Player/Player.prefab";
            var contents = PrefabUtility.LoadPrefabContents(playerPrefab);
            if (contents == null)
            {
                Debug.LogError("[SceneRebuilder] Player.prefab não encontrado para conectar AnimatorController.");
                return;
            }

            // Animator pode estar no root ou num filho (o modelo FBX instanciado)
            var animator = contents.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = contents.AddComponent<Animator>();
                Debug.Log("[SceneRebuilder] Animator adicionado ao root do Player.");
            }

            var soAnim = new SerializedObject(animator);
            var ctrlProp = soAnim.FindProperty("m_Controller");
            if (ctrlProp != null)
            {
                ctrlProp.objectReferenceValue = ctrl;
                soAnim.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[SceneRebuilder] AnimatorController conectado ao Player.");
            }

            PrefabUtility.SaveAsPrefabAsset(contents, playerPrefab);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        // ══════════════════════════════════════════════════════════
        // LIMPAR PREFABS
        // ══════════════════════════════════════════════════════════
        static void CleanAllPrefabs()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs" });
            int totalRemoved = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int removed = RemoveBrokenScriptsRecursive(contents);
                    if (removed > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(contents, path);
                        totalRemoved += removed;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            if (totalRemoved > 0)
                Debug.Log($"[SceneRebuilder] Removidos {totalRemoved} scripts quebrados dos prefabs.");
        }

        static void CleanCustomerPrefab()
        {
            const string prefabPath = "Assets/_Project/Prefabs/Customers/Customer.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null) return;

            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                bool changed = false;

                // Remove NavMeshAgent (posicionamento via CustomerQueue, nao NavMesh)
                var agent = contents.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null)
                {
                    Object.DestroyImmediate(agent, true);
                    changed = true;
                }

                // BALANCEAMENTO: patience=40s (era 30s) — tempo adequado para o jogador
                var customer = contents.GetComponent<Customer>();
                if (customer != null)
                {
                    var so = new SerializedObject(customer);
                    var patienceProp = so.FindProperty("_patience");
                    if (patienceProp != null && !Mathf.Approximately(patienceProp.floatValue, 30f))
                    {
                        patienceProp.floatValue = 30f;
                        so.ApplyModifiedProperties();
                        changed = true;
                    }
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                    Debug.Log("[SceneRebuilder] Customer prefab atualizado: NavMeshAgent removido, patience=30s.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static int RemoveBrokenScriptsRecursive(GameObject root)
        {
            if (root == null) return 0;
            int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            foreach (Transform child in root.transform)
                count += RemoveBrokenScriptsRecursive(child.gameObject);
            return count;
        }

        // ══════════════════════════════════════════════════════════
        // NAVMESH OPCIONAL (TAREFA 10)
        // ══════════════════════════════════════════════════════════
        static void BuildNavMeshIfAvailable(GameObject managersRoot)
        {
            var surfaceType = System.Type.GetType(
                "Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (surfaceType == null)
            {
                Debug.Log("[SceneRebuilder] NavMesh skip - instale com.unity.ai.navigation");
                return;
            }

            // Adiciona NavMeshSurface ao chao
            var floorRoot = GameObject.Find("[Floor]");
            if (floorRoot != null)
            {
                // Adiciona o componente mas NAO faz bake automatico —
                // o bake automático inclui meshes do TextMeshPro e gera erros.
                // Use Window > AI > Navigation > Bake manualmente se necessário.
                // WorkerAI usa fallback de lerp quando não há NavMesh bakeado.
                floorRoot.AddComponent(surfaceType);
                Debug.Log("[SceneRebuilder] NavMeshSurface adicionado (sem bake automatico — use menu Navigation > Bake).");
            }
        }

        // ══════════════════════════════════════════════════════════
        // BUILD MAIN MENU SCENE (TAREFA 3)
        // ══════════════════════════════════════════════════════════
        static void BuildMainMenuScene()
        {
            if (!EditorUtility.DisplayDialog("Pizza Tycoon — Build Main Menu",
                "Criar/recriar Assets/Scenes/MainMenu.unity?\n" +
                "A cena atual sera salva e reaberta apos.\nContinuar?",
                "Sim, Build", "Cancelar")) return;

            var currentScene = EditorSceneManager.GetActiveScene();
            string originalPath = currentScene.path;

            if (currentScene.isDirty && !string.IsNullOrEmpty(originalPath))
                EditorSceneManager.SaveScene(currentScene);

            // Garante que a pasta Scenes existe
            const string scenesDir = "Assets/Scenes";
            if (!Directory.Exists(scenesDir)) Directory.CreateDirectory(scenesDir);

            // Cria nova cena vazia
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── EventSystem ──────────────────────────────────────────
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // ── Main Camera ortografica, fundo azul #3498DB ───────────
            var camGO = new GameObject("[MainCamera]");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<UnityEngine.Camera>();
            cam.orthographic     = true;
            cam.orthographicSize = 5f;
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = new Color(0.204f, 0.596f, 0.859f); // #3498DB
            cam.nearClipPlane    = 0.3f;
            cam.farClipPlane     = 100f;
            camGO.AddComponent<AudioListener>();
            camGO.transform.position = new Vector3(0, 0, -10f);

            // ── [Managers]: AudioManager + SceneLoader ────────────────
            var mgrsGO = new GameObject("[Managers]");
            var mmAudio = mgrsGO.AddComponent<AudioManager>();
            WireAudioClips(mmAudio);
            mgrsGO.AddComponent<SceneLoader>();
            mgrsGO.AddComponent<GameManager>();

            // ── Canvas portrait 1080x1920 ─────────────────────────────
            var canvasGO = new GameObject("MainMenuCanvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight   = 0f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Fundo degradê (painel simples)
            var bgPanel = new GameObject("Background");
            bgPanel.transform.SetParent(canvasGO.transform, false);
            var bgRT = bgPanel.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            var bgImg = bgPanel.AddComponent<Image>();
            bgImg.color = new Color(0.204f, 0.596f, 0.859f); // #3498DB

            // Logo "PIZZA TYCOON"
            var logoGO = new GameObject("LogoText");
            logoGO.transform.SetParent(canvasGO.transform, false);
            var logoRT = logoGO.AddComponent<RectTransform>();
            logoRT.anchorMin = new Vector2(0.1f, 0.65f);
            logoRT.anchorMax = new Vector2(0.9f, 0.85f);
            logoRT.offsetMin = Vector2.zero; logoRT.offsetMax = Vector2.zero;
            var logoTxt = logoGO.AddComponent<TextMeshProUGUI>();
            logoTxt.text      = "PIZZA TYCOON";
            logoTxt.fontSize  = 96;
            logoTxt.fontStyle = FontStyles.Bold;
            logoTxt.color     = Color.white;
            logoTxt.alignment = TextAlignmentOptions.Center;

            // Botão PLAY verde #27AE60
            var playGO = new GameObject("PlayButton");
            playGO.transform.SetParent(canvasGO.transform, false);
            var playRT = playGO.AddComponent<RectTransform>();
            playRT.anchorMin = new Vector2(0.25f, 0.42f);
            playRT.anchorMax = new Vector2(0.75f, 0.55f);
            playRT.offsetMin = Vector2.zero; playRT.offsetMax = Vector2.zero;
            var playImg = playGO.AddComponent<Image>();
            playImg.color = new Color(0.153f, 0.682f, 0.376f); // #27AE60
            var playBtn = playGO.AddComponent<Button>();
            playBtn.targetGraphic = playImg;
            var playTxtGO = new GameObject("Text");
            playTxtGO.transform.SetParent(playGO.transform, false);
            var playTxt = playTxtGO.AddComponent<TextMeshProUGUI>();
            playTxt.text      = "PLAY";
            playTxt.fontSize  = 72;
            playTxt.fontStyle = FontStyles.Bold;
            playTxt.color     = Color.white;
            playTxt.alignment = TextAlignmentOptions.Center;
            var playTxtRT = playTxtGO.GetComponent<RectTransform>();
            playTxtRT.anchorMin = Vector2.zero; playTxtRT.anchorMax = Vector2.one;
            playTxtRT.sizeDelta = Vector2.zero;

            // Botão Settings (canto superior direito)
            var mmSettingsGO = new GameObject("SettingsButton");
            mmSettingsGO.transform.SetParent(canvasGO.transform, false);
            var mmSRT = mmSettingsGO.AddComponent<RectTransform>();
            mmSRT.anchorMin = new Vector2(1, 1);
            mmSRT.anchorMax = new Vector2(1, 1);
            mmSRT.pivot     = new Vector2(1, 1);
            mmSRT.anchoredPosition = new Vector2(-20f, -20f);
            mmSRT.sizeDelta = new Vector2(100f, 100f);
            var mmSImg = mmSettingsGO.AddComponent<Image>();
            mmSImg.color = new Color(0.2f, 0.2f, 0.2f, 0.75f);
            var mmSBtn = mmSettingsGO.AddComponent<Button>();
            mmSBtn.targetGraphic = mmSImg;
            var mmSTxtGO = new GameObject("Text");
            mmSTxtGO.transform.SetParent(mmSettingsGO.transform, false);
            var mmSTxt = mmSTxtGO.AddComponent<TextMeshProUGUI>();
            mmSTxt.text      = "ST";
            mmSTxt.fontSize  = 36;
            mmSTxt.fontStyle = FontStyles.Bold;
            mmSTxt.color     = Color.white;
            mmSTxt.alignment = TextAlignmentOptions.Center;
            var mmSTxtRT = mmSTxtGO.GetComponent<RectTransform>();
            mmSTxtRT.anchorMin = Vector2.zero; mmSTxtRT.anchorMax = Vector2.one;
            mmSTxtRT.sizeDelta = Vector2.zero;

            // Painel de Settings (oculto)
            var mmSettingsPanel = new GameObject("SettingsPanel");
            mmSettingsPanel.transform.SetParent(canvasGO.transform, false);
            var mmSPRT = mmSettingsPanel.AddComponent<RectTransform>();
            mmSPRT.anchorMin = new Vector2(0.1f, 0.2f);
            mmSPRT.anchorMax = new Vector2(0.9f, 0.8f);
            mmSPRT.offsetMin = Vector2.zero; mmSPRT.offsetMax = Vector2.zero;
            var mmSPImg = mmSettingsPanel.AddComponent<Image>();
            mmSPImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            mmSettingsPanel.SetActive(false);

            // ── [MainMenu] com MainMenuUI ─────────────────────────────
            var mmGO = new GameObject("[MainMenu]");
            var mmUI = mmGO.AddComponent<MainMenuUI>();
            var soMM = new SerializedObject(mmUI);
            var propPlay = soMM.FindProperty("_playButton");
            var propSet  = soMM.FindProperty("_settingsButton");
            var propSetP = soMM.FindProperty("_settingsPanel");
            if (propPlay != null) propPlay.objectReferenceValue = playBtn;
            if (propSet  != null) propSet.objectReferenceValue  = mmSBtn;
            if (propSetP != null) propSetP.objectReferenceValue = mmSettingsPanel;
            soMM.ApplyModifiedProperties();

            // Wire Settings button → Open Settings Panel via persistent listener
            UnityEventTools.AddPersistentListener(mmSBtn.onClick, mmUI.OnSettingsClicked);

            // ── Salvar cena como MainMenu.unity ───────────────────────
            const string mainMenuPath = "Assets/Scenes/MainMenu.unity";
            EditorSceneManager.SaveScene(scene, mainMenuPath);

            // ── Atualizar Build Settings ──────────────────────────────
            // Prefere o path da cena original; fallback para SampleScene.unity se nao salva
            string gameScenePath = (!string.IsNullOrEmpty(originalPath) && File.Exists(originalPath))
                ? originalPath
                : "Assets/Scenes/SampleScene.unity";
            UpdateBuildSettings(gameScenePath);

            Debug.Log("[SceneRebuilder] MainMenu.unity criada e Build Settings atualizados.");
            EditorUtility.DisplayDialog("Build Completo",
                "MainMenu.unity criada!\n\nBuild Settings atualizados com:\n" +
                "0: Assets/Scenes/MainMenu.unity\n1: " + gameScenePath,
                "OK");

            // Reabrir cena original
            if (!string.IsNullOrEmpty(originalPath))
                EditorSceneManager.OpenScene(originalPath);
            else
                Debug.Log("[SceneRebuilder] Cena original nao encontrada — MainMenu permanece aberta.");
        }

        static void UpdateBuildSettings(string gameScenePath = "Assets/Scenes/SampleScene.unity")
        {
            var buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene(gameScenePath,                  true),
            };
            EditorBuildSettings.scenes = buildScenes;
            Debug.Log($"[SceneRebuilder] Build Settings: MainMenu(0) + {gameScenePath}(1).");
        }

        // ══════════════════════════════════════════════════════════
        // CONECTAR REFERENCIAS FINAIS
        // ══════════════════════════════════════════════════════════
        static void WireAll()
        {
            // ItemPool
            var itemPool = Object.FindObjectOfType<ItemPool>();
            if (itemPool != null)
            {
                var soIP = new SerializedObject(itemPool);
                var listProp = soIP.FindProperty("_itemPrefabs");
                if (listProp != null)
                {
                    listProp.ClearArray();
                    var mappings = new (string path, ItemType type)[]
                    {
                        ("Assets/_Project/Prefabs/Items/Item_Wheat.prefab",       ItemType.Wheat),
                        ("Assets/_Project/Prefabs/Items/Item_Dough.prefab",       ItemType.Dough),
                        ("Assets/_Project/Prefabs/Items/Item_RawPizza.prefab",    ItemType.RawPizza),
                        ("Assets/_Project/Prefabs/Items/Item_CookedPizza.prefab", ItemType.CookedPizza),
                    };

                    int count = 0;
                    foreach (var (path, type) in mappings)
                    {
                        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (go == null)
                        {
                            Debug.LogWarning($"[SceneRebuilder] Item prefab nao encontrado: {path}");
                            continue;
                        }
                        var stackable = go.GetComponent<StackableItem>();
                        if (stackable == null) continue;

                        listProp.InsertArrayElementAtIndex(count);
                        var entry = listProp.GetArrayElementAtIndex(count);
                        entry.FindPropertyRelative("type").enumValueIndex = (int)type;
                        entry.FindPropertyRelative("prefab").objectReferenceValue = stackable;
                        entry.FindPropertyRelative("initialPoolSize").intValue = 20;
                        count++;
                    }
                    soIP.ApplyModifiedProperties();
                    Debug.Log($"[SceneRebuilder] ItemPool configurado com {count} tipos de item.");
                }
            }

            // UpgradeManager
            var um = Object.FindObjectOfType<UpgradeManager>();
            if (um != null)
            {
                var so = new SerializedObject(um);
                var pc = Object.FindObjectOfType<PlayerController>();
                var ps = Object.FindObjectOfType<PlayerStacker>();
                var cs = Object.FindObjectOfType<CustomerSpawner>();
                var oven = Object.FindObjectOfType<OvenStation>();
                if (pc != null)   so.FindProperty("_playerController").objectReferenceValue = pc;
                if (ps != null)   so.FindProperty("_playerStacker").objectReferenceValue = ps;
                if (cs != null)   so.FindProperty("_customerSpawner").objectReferenceValue = cs;
                if (oven != null) so.FindProperty("_ovenStation").objectReferenceValue = oven;

                var upgradeList = so.FindProperty("_availableUpgrades");
                if (upgradeList != null)
                {
                    string[] paths = {
                        "Assets/_Project/ScriptableObjects/UpgradeData/UpgradeData_Velocidade.asset",
                        "Assets/_Project/ScriptableObjects/UpgradeData/UpgradeData_Capacidade.asset",
                        "Assets/_Project/ScriptableObjects/UpgradeData/UpgradeData_Forno.asset",
                        "Assets/_Project/ScriptableObjects/UpgradeData/UpgradeData_Colheita.asset",
                    };
                    upgradeList.ClearArray();
                    int i = 0;
                    foreach (var path in paths)
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
                        if (asset == null) continue;
                        upgradeList.InsertArrayElementAtIndex(i);
                        upgradeList.GetArrayElementAtIndex(i).objectReferenceValue = asset;
                        i++;
                    }
                }

                var allStations = Object.FindObjectsOfType<BaseStation>();
                var stProp = so.FindProperty("_allStations");
                if (stProp != null)
                {
                    stProp.ClearArray();
                    for (int j = 0; j < allStations.Length; j++)
                    {
                        stProp.InsertArrayElementAtIndex(j);
                        stProp.GetArrayElementAtIndex(j).objectReferenceValue = allStations[j];
                    }
                }
                so.ApplyModifiedProperties();
            }

            // Camera
            var camFollow = Object.FindObjectOfType<IsometricCameraFollow>();
            if (camFollow != null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    var so = new SerializedObject(camFollow);
                    so.FindProperty("_target").objectReferenceValue = player.transform;
                    so.ApplyModifiedProperties();
                }
            }

            // TutorialManager — wire do TutorialPanel e TutorialText (TAREFA 6)
            var tutMgr = Object.FindObjectOfType<TutorialManager>();
            if (tutMgr != null)
            {
                var tutPanelGO = GameObject.Find("TutorialPanel");
                if (tutPanelGO != null)
                {
                    var tutTmpText = tutPanelGO.GetComponentInChildren<TextMeshProUGUI>();
                    var soTut = new SerializedObject(tutMgr);
                    var panelProp = soTut.FindProperty("_tutorialPanel");
                    var stepProp  = soTut.FindProperty("_stepText");
                    var textProp  = soTut.FindProperty("_tutorialText");
                    if (panelProp != null) panelProp.objectReferenceValue = tutPanelGO;
                    if (stepProp  != null) stepProp.objectReferenceValue  = tutTmpText;
                    if (textProp  != null) textProp.objectReferenceValue  = tutTmpText;
                    soTut.ApplyModifiedProperties();
                    Debug.Log("[SceneRebuilder] TutorialManager: referencias conectadas.");
                }
            }

            Debug.Log("[SceneRebuilder] Todas as referencias conectadas.");
        }

        // ══════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════
        static void CreatePlane(string name, Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale / 10f;
            Object.DestroyImmediate(go.GetComponent<MeshCollider>());
            go.AddComponent<BoxCollider>().size = new Vector3(10f, 0.02f, 10f);
            ApplyColor(go, color);
        }

        static void CreateBox(string name, Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            ApplyColor(go, color);
        }

        static void CreateBoxChild(string name, Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            ApplyColor(go, color);
        }

        static void ApplyColor(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return;
            var mat = new Material(shader);
            mat.color = color;
            r.sharedMaterial = mat;
        }

        static void AddWorldLabel(Transform parent, string text, float yOffset)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(0, yOffset, 0);
            go.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 1.8f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        static GameObject MakePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchPos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchPos;
            rt.sizeDelta = size;
            go.AddComponent<Image>().color = color;
            return go;
        }

        static TextMeshProUGUI MakeLabel(Transform parent, string name, string text,
            float fontSize, Color color, Vector2 anchMin, Vector2 anchMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchMin; rt.anchorMax = anchMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }
    }
}