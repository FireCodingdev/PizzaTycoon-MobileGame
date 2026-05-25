using UnityEngine;
using PizzaTycoon.Stations;

namespace PizzaTycoon.GameSystems
{
    // Spawna decoração visual em runtime sem prefabs nem Inspector.
    // Detecta automaticamente onde é o restaurante (via BaseStation) e onde
    // fica a estrada (por nome de objeto ou DeliveryStation.forward) para
    // posicionar mesas, estacionamento e plantas nos lados corretos.
    public class WorldDecorationSpawner : MonoBehaviour
    {
        // ── Paleta ─────────────────────────────────────────────────────────────
        private static readonly Color TableWood   = new Color(0.78f, 0.50f, 0.24f);
        private static readonly Color TableLeg    = new Color(0.38f, 0.22f, 0.10f);
        private static readonly Color ChairBlue   = new Color(0.18f, 0.40f, 0.82f);
        private static readonly Color ChairOrange = new Color(0.92f, 0.50f, 0.10f);
        private static readonly Color PotBrown    = new Color(0.52f, 0.28f, 0.12f);
        private static readonly Color PlantGreen  = new Color(0.16f, 0.62f, 0.20f);

        private static readonly Color[] CarColors = {
            new Color(0.92f, 0.78f, 0.08f),
            new Color(0.15f, 0.42f, 0.80f),
            new Color(0.85f, 0.15f, 0.15f),
            new Color(0.85f, 0.85f, 0.88f),
            new Color(0.22f, 0.22f, 0.22f),
        };

        private void Start()
        {
            // Auto-spawn DESABILITADO. Mesas, estacionamento e decoracoes externas
            // devem ser editadas manualmente no Scene View.
        }

        // ── Detecção de bounds do restaurante ─────────────────────────────────
        private Bounds DetectRestaurantBounds()
        {
            var stations = FindObjectsOfType<BaseStation>();
            if (stations.Length > 0)
            {
                var b = new Bounds(stations[0].transform.position, Vector3.one * 3f);
                foreach (var s in stations)
                    b.Encapsulate(new Bounds(s.transform.position, Vector3.one * 3f));
                b.Expand(new Vector3(3f, 0f, 3f));
                return b;
            }
            var player = FindObjectOfType<PizzaTycoon.Player.PlayerController>();
            if (player != null)
                return new Bounds(player.transform.position + new Vector3(0f, 0f, 4f),
                                  new Vector3(20f, 2f, 14f));
            return new Bounds(Vector3.zero, new Vector3(20f, 2f, 14f));
        }

        // ── Detecção da direção da estrada ────────────────────────────────────
        // Estratégia 1: procura objeto chamado "Road", "Rua", "Street" etc.
        // Estratégia 2: usa DeliveryStation.forward (direção dos clientes).
        // Estratégia 3: fallback para Vector3.forward.
        private Vector3 DetectRoadDirection(Vector3 restaurantCenter)
        {
            float   bestDist = 0f;
            Vector3 bestDir  = Vector3.zero;

            foreach (var r in FindObjectsOfType<Renderer>())
            {
                string n = r.gameObject.name.ToLower();
                if (!n.Contains("road") && !n.Contains("rua") &&
                    !n.Contains("street") && !n.Contains("estrada") &&
                    !n.Contains("asphalt") && !n.Contains("sidewalk")) continue;

                Vector3 dir = r.bounds.center - restaurantCenter;
                dir.y = 0f;
                if (dir.magnitude > bestDist)
                {
                    bestDist = dir.magnitude;
                    bestDir  = dir.normalized;
                }
            }
            if (bestDir.sqrMagnitude > 0.01f) return bestDir;

            // Fallback: DeliveryStation.forward aponta para os clientes (= estrada)
            var delivery = FindObjectOfType<DeliveryStation>();
            if (delivery != null)
                return delivery.transform.forward;

            return Vector3.forward;
        }

        // ── Área de mesas (lado da estrada / entrada dos clientes) ────────────
        private void BuildOutdoorSeating(Bounds rest, Vector3 roadDir, Vector3 perpDir)
        {
            var root = new GameObject("[OutdoorSeating]");
            root.transform.SetParent(transform, false);

            // Centro da área de mesas: a ~3 u além da parede frontal
            Vector3 frontWall = rest.center + roadDir * rest.extents.magnitude * 0.6f;
            frontWall.y = 0f;

            for (int row = 0; row < 2; row++)
            for (int col = 0; col < 2; col++)
            {
                Vector3 pos = frontWall
                    + perpDir  * ((col - 0.5f) * 4.2f)
                    + roadDir  * (row * -4.0f + 3f);
                pos.y = 0f;
                Color c = (col + row) % 2 == 0 ? ChairBlue : ChairOrange;
                BuildOutdoorTable(root.transform, pos, c);
            }
        }

        // ithappy food props placed on tables
        private static readonly string[] FoodPropPaths = {
            "Assets/ithappy/Food_Free/Prefabs/Plate_001.prefab",
            "Assets/ithappy/Food_Free/Prefabs/Glass_001.prefab",
            "Assets/ithappy/Food_Free/Prefabs/Cup_003.prefab",
            "Assets/ithappy/Food_Free/Prefabs/Plate_007.prefab",
            "Assets/ithappy/Food_Free/Prefabs/Glass_005.prefab",
        };
        private static int _foodIndex = 0;

        private void BuildOutdoorTable(Transform parent, Vector3 origin, Color chairColor)
        {
            var t = new GameObject("Table");
            t.transform.SetParent(parent, false);
            t.transform.position = origin;

            MakePrim(t.transform, PrimitiveType.Cylinder,
                new Vector3(0f, 0.80f, 0f), new Vector3(0.88f, 0.055f, 0.88f), TableWood);
            MakePrim(t.transform, PrimitiveType.Cylinder,
                new Vector3(0f, 0.40f, 0f), new Vector3(0.10f, 0.42f, 0.10f), TableLeg);
            MakePrim(t.transform, PrimitiveType.Cylinder,
                new Vector3(0f, 0.03f, 0f), new Vector3(0.48f, 0.03f, 0.48f), TableLeg);

            // Food props on table surface
            PlaceFoodProp(t.transform, new Vector3(-0.18f, 0.86f, -0.12f));
            PlaceFoodProp(t.transform, new Vector3( 0.18f, 0.86f,  0.12f));

            BuildChair(parent, origin + new Vector3(0f, 0f, -1.05f),   0f, chairColor);
            BuildChair(parent, origin + new Vector3(0f, 0f,  1.05f), 180f, chairColor);
        }

        private void PlaceFoodProp(Transform tableTop, Vector3 localPos)
        {
#if UNITY_EDITOR
            string path = FoodPropPaths[_foodIndex % FoodPropPaths.Length];
            _foodIndex++;
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                var inst = Instantiate(prefab);
                inst.name = "_PT_Food";
                inst.transform.SetParent(tableTop, false);
                inst.transform.localPosition = localPos;
                inst.transform.localScale    = Vector3.one * 0.55f;
                inst.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                return;
            }
#endif
            // Fallback: small sphere as placeholder
            MakePrim(tableTop, PrimitiveType.Sphere, localPos, Vector3.one * 0.12f,
                new Color(0.85f, 0.75f, 0.60f));
        }

        private void BuildChair(Transform parent, Vector3 origin, float yaw, Color color)
        {
            var c = new GameObject("Chair");
            c.transform.SetParent(parent, false);
            c.transform.position = origin;
            c.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            MakePrim(c.transform, PrimitiveType.Cube,
                new Vector3(0f, 0.44f, 0f), new Vector3(0.54f, 0.07f, 0.50f), color);
            MakePrim(c.transform, PrimitiveType.Cube,
                new Vector3(0f, 0.78f, -0.22f), new Vector3(0.54f, 0.52f, 0.07f), color);

            float[] xs = { -0.21f, 0.21f }, zs = { -0.19f, 0.19f };
            foreach (float x in xs) foreach (float z in zs)
                MakePrim(c.transform, PrimitiveType.Cube,
                    new Vector3(x, 0.22f, z), new Vector3(0.05f, 0.44f, 0.05f), TableLeg);
        }

        // ── Estacionamento (lateral, paralelo à estrada) ──────────────────────

        private static readonly string[] AwbVehiclePaths = {
            "Assets/Awb-Free Low Poly Vehicles/Prefabs/Hatchback Car_15.prefab",
            "Assets/Awb-Free Low Poly Vehicles/Prefabs/Classic Car_9.prefab",
            "Assets/Awb-Free Low Poly Vehicles/Prefabs/N Van_10.prefab",
            "Assets/Awb-Free Low Poly Vehicles/Prefabs/Pick Up_11.prefab",
            "Assets/Awb-Free Low Poly Vehicles/Prefabs/Sport Car_39.prefab",
        };

        private void BuildParkingLot(Bounds rest, Vector3 roadDir, Vector3 perpDir)
        {
            var root = new GameObject("[Parking]");
            root.transform.SetParent(transform, false);

            float sideOff = rest.extents.x + 5.5f;
            Vector3 start = rest.center
                + perpDir * sideOff
                + roadDir * (rest.extents.z * 0.15f);
            start.y = 0f;

            // 4 parking spots
            for (int i = 0; i < 4; i++)
            {
                Vector3 pos = start + roadDir * ((i - 1.5f) * 5.8f);
                pos.y = 0f;

                GameObject car = TrySpawnVehicle(root.transform, i);
                car.transform.position = pos;
                car.transform.rotation = Quaternion.LookRotation(perpDir);
            }
        }

        private static GameObject TrySpawnVehicle(Transform parent, int index)
        {
#if UNITY_EDITOR
            string path = AwbVehiclePaths[index % AwbVehiclePaths.Length];
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                var inst = Instantiate(prefab);
                inst.name = "Car_" + index;
                inst.transform.SetParent(parent, false);
                // Remove Rigidbody/Collider logic that may interfere with scene
                foreach (var rb in inst.GetComponentsInChildren<Rigidbody>(true))
                    rb.isKinematic = true;
                return inst;
            }
#endif
            return BuildFallbackCar(parent, CarColors[index % CarColors.Length]);
        }

        private static GameObject BuildFallbackCar(Transform parent, Color body)
        {
            var car = new GameObject("Car");
            car.transform.SetParent(parent, false);

            MakePrim(car.transform, PrimitiveType.Cube,
                new Vector3(0f, 0.28f, 0f), new Vector3(1.0f, 0.38f, 2.20f), body);
            MakePrim(car.transform, PrimitiveType.Cube,
                new Vector3(0f, 0.72f, 0.10f), new Vector3(0.82f, 0.36f, 1.35f), body * 0.88f);
            MakePrim(car.transform, PrimitiveType.Cube,
                new Vector3(0f, 0.80f, -0.52f), new Vector3(0.78f, 0.22f, 0.07f),
                new Color(0.55f, 0.78f, 0.92f));
            Quaternion wr = Quaternion.Euler(90f, 0f, 0f);
            float[] wxs = { -0.46f, 0.46f }, wzs = { -0.78f, 0.78f };
            foreach (float wx in wxs) foreach (float wz in wzs)
                MakePrim(car.transform, PrimitiveType.Cylinder,
                    new Vector3(wx, 0.16f, wz), new Vector3(0.30f, 0.10f, 0.30f),
                    new Color(0.08f, 0.08f, 0.08f), wr);
            return car;
        }

        // ── Plantas e lixeiras externas ───────────────────────────────────────
        private void SpawnExteriorDecorations(Bounds rest, Vector3 roadDir, Vector3 perpDir)
        {
            var root = new GameObject("[Exterior]");
            root.transform.SetParent(transform, false);

            float cx = rest.center.x, cy = 0f, cz = rest.center.z;
            float ex = rest.extents.x + 1f, ez = rest.extents.z + 1f;

            // Plantas nos 4 cantos externos + 2 no centro de cada parede lateral
            Vector3[] spots = {
                new Vector3(cx + ex, cy, cz + ez),
                new Vector3(cx - ex, cy, cz + ez),
                new Vector3(cx + ex, cy, cz - ez),
                new Vector3(cx - ex, cy, cz - ez),
                new Vector3(cx + ex, cy, cz),
                new Vector3(cx - ex, cy, cz),
            };
            foreach (var p in spots) BuildPlant(root.transform, p);

            // Lixeiras perto da área de mesas (lado da estrada)
            Vector3 frontBase = rest.center + roadDir * (rest.extents.magnitude * 0.6f + 4f);
            frontBase.y = 0f;
            BuildTrashCan(root.transform, frontBase + perpDir *  5.5f);
            BuildTrashCan(root.transform, frontBase + perpDir * -5.5f);
        }

        private void BuildPlant(Transform parent, Vector3 origin)
        {
            var go = new GameObject("Plant");
            go.transform.SetParent(parent, false);
            go.transform.position = origin;

            MakePrim(go.transform, PrimitiveType.Cylinder,
                new Vector3(0f, 0.25f, 0f), new Vector3(0.38f, 0.25f, 0.38f), PotBrown);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad;
                MakePrim(go.transform, PrimitiveType.Sphere,
                    new Vector3(Mathf.Cos(a) * 0.16f, 0.70f + i * 0.12f, Mathf.Sin(a) * 0.16f),
                    new Vector3(0.40f, 0.52f, 0.40f), PlantGreen);
            }
        }

        private void BuildTrashCan(Transform parent, Vector3 origin)
        {
            var go = new GameObject("TrashCan");
            go.transform.SetParent(parent, false);
            go.transform.position = origin;

            MakePrim(go.transform, PrimitiveType.Cylinder,
                new Vector3(0f, 0.36f, 0f), new Vector3(0.28f, 0.36f, 0.28f),
                new Color(0.20f, 0.20f, 0.22f));
            MakePrim(go.transform, PrimitiveType.Cylinder,
                new Vector3(0f, 0.74f, 0f), new Vector3(0.31f, 0.04f, 0.31f),
                new Color(0.30f, 0.30f, 0.32f));
        }

        // ── Pads de upgrade/unlock em frente a cada estação ───────────────────
        private void SpawnUpgradePads()
        {
            var stations = FindObjectsOfType<BaseStation>();
            if (stations.Length == 0) return;

            // Centróide de todas as estações (linha de trabalho)
            Vector3 centroid = Vector3.zero;
            foreach (var s in stations) centroid += s.transform.position;
            centroid /= stations.Length;

            // Direção do corredor do jogador = para fora do centróide das estações
            // Usa a perpendicular à linha de estações detectada pela PCA simplificada
            Vector3 rowDir = Vector3.right; // padrão
            if (stations.Length >= 2)
            {
                // Direção dominante da linha de estações
                Vector3 span = stations[stations.Length - 1].transform.position
                             - stations[0].transform.position;
                span.y = 0f;
                if (span.sqrMagnitude > 0.01f)
                    rowDir = span.normalized;
            }
            // Corredor do jogador fica perpendicular à linha de estações
            Vector3 corridorDir = Vector3.Cross(rowDir, Vector3.up).normalized;

            // Se o corredor aponta para longe do centróide, inverte
            var player = FindObjectOfType<PizzaTycoon.Player.PlayerController>();
            if (player != null)
            {
                Vector3 toPlayer = player.transform.position - centroid;
                toPlayer.y = 0f;
                if (Vector3.Dot(corridorDir, toPlayer) < 0f)
                    corridorDir = -corridorDir;
            }

            // Determina quantas estações ficam bloqueadas (últimas da linha)
            int lockCount = Mathf.Max(0, stations.Length - 3);

            for (int i = 0; i < stations.Length; i++)
            {
                var station = stations[i];

                // Evita duplicar pad se já existe
                if (station.GetComponentInChildren<StationUpgradePad>() != null) continue;

                // Pad fica à frente da estação no corredor do jogador
                Vector3 padPos = station.transform.position
                               + corridorDir * 1.6f;
                padPos.y = 0f;

                bool locked = (i >= stations.Length - lockCount);

                var padGO = new GameObject("[UpgradePad]");
                padGO.transform.SetParent(station.transform.parent);
                var pad = padGO.AddComponent<StationUpgradePad>();
                pad.Init(station, padPos, locked);
            }
        }

        // ── Helper: cria primitiva colorida sem collisor ───────────────────────
        private static GameObject MakePrim(Transform parent, PrimitiveType type,
            Vector3 localPos, Vector3 localScale, Color color,
            Quaternion localRot = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = "_PT";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            if (localRot != default) go.transform.localRotation = localRot;

            Destroy(go.GetComponent<Collider>());

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                ?? Shader.Find("Standard"));
            mat.color = color;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
    }
}
