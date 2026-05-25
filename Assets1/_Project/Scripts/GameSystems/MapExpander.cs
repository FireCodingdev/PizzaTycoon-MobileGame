using UnityEngine;
using PizzaTycoon.Stations;

namespace PizzaTycoon.GameSystems
{
    // Expande o mapa ao redor do restaurante em runtime.
    // Cria: chão amplo, calçada, faixa de asfalto, marcações de pista,
    //       prédios de fundo e postes de luz para dar sensação de cidade.
    [DefaultExecutionOrder(20)]
    public class MapExpander : MonoBehaviour
    {
        // ── Paleta ─────────────────────────────────────────────────────────────
        private static readonly Color GrassGreen    = new Color(0.28f, 0.56f, 0.22f);
        private static readonly Color Asphalt       = new Color(0.22f, 0.22f, 0.24f);
        private static readonly Color Sidewalk      = new Color(0.82f, 0.80f, 0.76f);
        private static readonly Color RoadLine      = new Color(0.95f, 0.90f, 0.25f);
        private static readonly Color RoadLineDash  = new Color(0.92f, 0.92f, 0.92f);
        private static readonly Color BuildingBase  = new Color(0.76f, 0.72f, 0.68f);
        private static readonly Color WindowBlue    = new Color(0.55f, 0.75f, 0.95f, 0.85f);
        private static readonly Color LampPost      = new Color(0.25f, 0.25f, 0.28f);
        private static readonly Color LampGlow      = new Color(0.98f, 0.95f, 0.72f);
        private static readonly Color FenceColor    = new Color(0.55f, 0.48f, 0.40f);
        private static readonly Color RestFloor     = new Color(0.90f, 0.88f, 0.84f);

        private void Start()
        {
            // Auto-spawn DESABILITADO. O cenario eh editado manualmente no Scene View.
            // Os metodos privados abaixo permanecem caso voce queira chama-los seletivamente.
        }

        // ── Bounds/direction helpers ──────────────────────────────────────────

        private static Bounds DetectRestaurantBounds()
        {
            var stations = FindObjectsOfType<BaseStation>();
            if (stations.Length > 0)
            {
                var b = new Bounds(stations[0].transform.position, Vector3.one * 3f);
                foreach (var s in stations)
                    b.Encapsulate(new Bounds(s.transform.position, Vector3.one * 3f));
                b.Expand(new Vector3(4f, 0f, 4f));
                return b;
            }
            return new Bounds(Vector3.zero, new Vector3(22f, 2f, 16f));
        }

        private static Vector3 DetectRoadDir(Vector3 center)
        {
            var delivery = FindObjectOfType<DeliveryStation>();
            if (delivery != null) return delivery.transform.forward;
            return Vector3.forward;
        }

        // ── Restaurant floor ──────────────────────────────────────────────────

        private void ExpandRestaurantFloor(Bounds rest)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "[RestFloor]";
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(rest.center.x, -0.05f, rest.center.z);
            go.transform.localScale = new Vector3(rest.size.x + 2f, 0.10f, rest.size.z + 2f);
            SetMat(go, RestFloor);
            Destroy(go.GetComponent<Collider>());
        }

        // ── Grass ground ──────────────────────────────────────────────────────

        private void BuildGround(Bounds rest, Vector3 roadDir, Vector3 perpDir)
        {
            // Large ground plane centered slightly behind restaurant
            Vector3 center = rest.center - roadDir * 20f;
            center.y = -0.15f;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "[Ground]";
            go.transform.SetParent(transform, false);
            go.transform.position   = center;
            go.transform.localScale = new Vector3(100f, 0.10f, 80f);
            go.transform.rotation   = Quaternion.LookRotation(roadDir);
            SetMat(go, GrassGreen);
            Destroy(go.GetComponent<Collider>());
        }

        // ── Sidewalk ─────────────────────────────────────────────────────────

        private void BuildSidewalk(Bounds rest, Vector3 roadDir, Vector3 perpDir)
        {
            // Strip between restaurant front and road
            Vector3 swCenter = rest.center
                + roadDir * (rest.extents.magnitude * 0.65f + 3.0f);
            swCenter.y = -0.02f;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "[Sidewalk]";
            go.transform.SetParent(transform, false);
            go.transform.position   = swCenter;
            go.transform.localScale = new Vector3(40f, 0.06f, 6.0f);
            go.transform.rotation   = Quaternion.LookRotation(roadDir);
            SetMat(go, Sidewalk);
            Destroy(go.GetComponent<Collider>());

            // Curb (raised edge between sidewalk and road)
            var curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curb.name = "[Curb]";
            curb.transform.SetParent(transform, false);
            curb.transform.position = swCenter + roadDir * 3.1f;
            curb.transform.rotation = Quaternion.LookRotation(roadDir);
            curb.transform.localScale = new Vector3(40f, 0.14f, 0.25f);
            SetMat(curb, new Color(0.70f, 0.68f, 0.64f));
            Destroy(curb.GetComponent<Collider>());
        }

        // ── Road ─────────────────────────────────────────────────────────────

        private void BuildRoad(Bounds rest, Vector3 roadDir, Vector3 perpDir)
        {
            float roadOffset = rest.extents.magnitude * 0.65f + 9.5f;
            Vector3 roadCenter = rest.center + roadDir * roadOffset;
            roadCenter.y = -0.01f;

            // Asphalt surface
            var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "[Road]";
            road.transform.SetParent(transform, false);
            road.transform.position   = roadCenter;
            road.transform.localScale = new Vector3(60f, 0.08f, 14f);
            road.transform.rotation   = Quaternion.LookRotation(roadDir);
            SetMat(road, Asphalt);
            Destroy(road.GetComponent<Collider>());

            // Center yellow lines
            MakeLine(roadCenter + perpDir * 0.35f, roadDir, RoadLine,  new Vector3(0.12f, 0.01f, 58f));
            MakeLine(roadCenter - perpDir * 0.35f, roadDir, RoadLine,  new Vector3(0.12f, 0.01f, 58f));

            // Side white dashes
            for (int i = -5; i <= 5; i++)
            {
                Vector3 dashPos = roadCenter + perpDir * (i * 10f);
                dashPos.y = 0f;
                MakeLine(dashPos + roadDir * 3f,  roadDir, RoadLineDash, new Vector3(0.18f, 0.01f, 2.8f));
                MakeLine(dashPos - roadDir * 3f,  roadDir, RoadLineDash, new Vector3(0.18f, 0.01f, 2.8f));
            }
        }

        private void MakeLine(Vector3 pos, Vector3 roadDir, Color color, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "_PT";
            go.transform.SetParent(transform, false);
            go.transform.position   = pos;
            go.transform.localScale = scale;
            go.transform.rotation   = Quaternion.LookRotation(roadDir);
            SetMat(go, color);
            Destroy(go.GetComponent<Collider>());
        }

        // ── Background buildings ──────────────────────────────────────────────

        private void BuildBackgroundBuildings(Bounds rest, Vector3 roadDir, Vector3 perpDir)
        {
            var root = new GameObject("[Buildings]");
            root.transform.SetParent(transform, false);

            // Buildings on the far side of the road
            float buildingDepth = rest.extents.magnitude * 0.65f + 26f;
            Vector3 rowCenter = rest.center + roadDir * buildingDepth;
            rowCenter.y = 0f;

            // Row of varied buildings
            var configs = new (float w, float h, float d, float xOff, Color wall)[]
            {
                (5f, 9f,  5f, -22f, new Color(0.80f, 0.74f, 0.68f)),
                (7f, 14f, 6f, -13f, new Color(0.68f, 0.72f, 0.78f)),
                (4f, 7f,  4f,  -4f, new Color(0.88f, 0.82f, 0.72f)),
                (6f, 12f, 5f,   5f, new Color(0.72f, 0.78f, 0.70f)),
                (8f, 10f, 6f,  15f, new Color(0.76f, 0.68f, 0.78f)),
                (5f, 8f,  4f,  24f, new Color(0.80f, 0.76f, 0.68f)),
            };

            foreach (var (w, h, d, xOff, wall) in configs)
            {
                Vector3 pos = rowCenter + perpDir * xOff + Vector3.up * (h * 0.5f);
                BuildBuilding(root.transform, pos, w, h, d, wall, roadDir);
            }

            // Also add some on the lateral sides
            for (int side = -1; side <= 1; side += 2)
            {
                float lateralOff = rest.extents.x + 18f;
                for (int i = 0; i < 3; i++)
                {
                    float h = Random.Range(5f, 13f);
                    Vector3 pos = rest.center
                        + perpDir * (side * (lateralOff + i * 9f))
                        + roadDir * Random.Range(-8f, 8f)
                        + Vector3.up * (h * 0.5f);
                    BuildBuilding(root.transform, pos, 5.5f, h, 5.5f,
                        BuildingBase, roadDir);
                }
            }
        }

        private void BuildBuilding(Transform parent, Vector3 pos,
            float w, float h, float d, Color wallColor, Vector3 roadDir)
        {
            var building = new GameObject("Building");
            building.transform.SetParent(parent, false);
            building.transform.position = pos;

            // Main body
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "_PT_Body";
            body.transform.SetParent(building.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale    = new Vector3(w, h, d);
            body.transform.rotation      = Quaternion.LookRotation(roadDir);
            SetMat(body, wallColor);
            Destroy(body.GetComponent<Collider>());

            // Roof detail
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "_PT_Roof";
            roof.transform.SetParent(building.transform, false);
            roof.transform.localPosition = Vector3.up * (h * 0.5f + 0.15f);
            roof.transform.localScale    = new Vector3(w + 0.3f, 0.3f, d + 0.3f);
            roof.transform.rotation      = Quaternion.LookRotation(roadDir);
            SetMat(roof, wallColor * 0.85f);
            Destroy(roof.GetComponent<Collider>());

            // Windows grid — faces toward road
            int rows = Mathf.FloorToInt(h / 2.2f);
            int cols = Mathf.FloorToInt(w / 1.8f);
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                float wx = (c - (cols - 1) * 0.5f) * 1.7f;
                float wy = -h * 0.5f + 1.4f + r * 2.2f;
                Vector3 wPos = pos + roadDir * (-d * 0.5f - 0.06f)
                                   + Vector3.Cross(roadDir, Vector3.up) * wx
                                   + Vector3.up * wy;
                var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.name = "_PT_Win";
                win.transform.SetParent(building.transform, false);
                win.transform.position   = wPos;
                win.transform.rotation   = Quaternion.LookRotation(-roadDir);
                win.transform.localScale = new Vector3(0.85f, 1.1f, 0.05f);
                SetMat(win, WindowBlue);
                Destroy(win.GetComponent<Collider>());
            }
        }

        // ── Lamp posts ────────────────────────────────────────────────────────

        private void BuildLampPosts(Bounds rest, Vector3 roadDir, Vector3 perpDir)
        {
            var root = new GameObject("[LampPosts]");
            root.transform.SetParent(transform, false);

            float sidewalkEdge = rest.extents.magnitude * 0.65f + 5.5f;
            for (int side = -1; side <= 1; side += 2)
            for (int i = -3; i <= 3; i++)
            {
                Vector3 pos = rest.center
                    + perpDir * (side * 18f)
                    + roadDir * (sidewalkEdge + i * 9f);
                pos.y = 0f;
                BuildLamp(root.transform, pos, -side);
            }
        }

        private static void BuildLamp(Transform parent, Vector3 pos, float side)
        {
            var lamp = new GameObject("Lamp");
            lamp.transform.SetParent(parent, false);
            lamp.transform.position = pos;

            // Pole
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "_PT_Pole";
            pole.transform.SetParent(lamp.transform, false);
            pole.transform.localPosition = Vector3.up * 2.5f;
            pole.transform.localScale    = new Vector3(0.08f, 2.5f, 0.08f);
            SetMat(pole, LampPost);
            Destroy(pole.GetComponent<Collider>());

            // Arm
            var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "_PT_Arm";
            arm.transform.SetParent(lamp.transform, false);
            arm.transform.localPosition = new Vector3(side * 0.5f, 5.1f, 0f);
            arm.transform.localScale    = new Vector3(1.05f, 0.07f, 0.07f);
            SetMat(arm, LampPost);
            Destroy(arm.GetComponent<Collider>());

            // Bulb
            var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "_PT_Bulb";
            bulb.transform.SetParent(lamp.transform, false);
            bulb.transform.localPosition = new Vector3(side * 1.0f, 5.0f, 0f);
            bulb.transform.localScale    = new Vector3(0.22f, 0.16f, 0.22f);
            SetMat(bulb, LampGlow);
            Destroy(bulb.GetComponent<Collider>());
        }

        // ── Perimeter fence ───────────────────────────────────────────────────

        private void BuildPerimeterFence(Bounds rest, Vector3 roadDir, Vector3 perpDir)
        {
            var root = new GameObject("[Fence]");
            root.transform.SetParent(transform, false);

            float backOff  = rest.extents.z + 6f;
            float sideOff  = rest.extents.x + 16f;

            // Back wall
            BuildFenceSegment(root.transform,
                rest.center - roadDir * backOff,
                Quaternion.LookRotation(perpDir),
                new Vector3(sideOff * 2f, 1.2f, 0.18f));

            // Side walls
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 sideCenter = rest.center
                    + perpDir * (s * sideOff)
                    - roadDir * (backOff * 0.5f - rest.extents.z * 0.5f);
                BuildFenceSegment(root.transform, sideCenter,
                    Quaternion.LookRotation(roadDir),
                    new Vector3(backOff, 1.2f, 0.18f));
            }

            // Fence posts
            float postSpacing = 3.5f;
            int postCount = Mathf.CeilToInt(sideOff * 2f / postSpacing);
            for (int i = 0; i <= postCount; i++)
            {
                float t = -sideOff + i * postSpacing;
                Vector3 postPos = rest.center - roadDir * backOff + perpDir * t;
                postPos.y = 0f;
                BuildFencePost(root.transform, postPos);
            }
        }

        private static void BuildFenceSegment(Transform parent, Vector3 pos,
            Quaternion rot, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "_PT_Fence";
            go.transform.SetParent(parent, false);
            go.transform.position   = pos + Vector3.up * (scale.y * 0.5f);
            go.transform.rotation   = rot;
            go.transform.localScale = scale;
            SetMat(go, FenceColor);
            Destroy(go.GetComponent<Collider>());
        }

        private static void BuildFencePost(Transform parent, Vector3 pos)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "_PT_Post";
            post.transform.SetParent(parent, false);
            post.transform.position   = pos + Vector3.up * 0.75f;
            post.transform.localScale = new Vector3(0.22f, 1.5f, 0.22f);
            SetMat(post, FenceColor * 0.85f);
            Destroy(post.GetComponent<Collider>());
        }

        // ── Shared helper ─────────────────────────────────────────────────────

        private static void SetMat(GameObject go, Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                ?? Shader.Find("Standard"));
            mat.color = color;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
