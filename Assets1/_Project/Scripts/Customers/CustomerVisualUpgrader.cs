using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PizzaTycoon.Customers
{
    [RequireComponent(typeof(Customer))]
    public class CustomerVisualUpgrader : MonoBehaviour
    {
        // ithappy asset paths
        private const string BaseMeshPath = "Assets/ithappy/Creative_Characters_FREE/Prefabs/Base_Mesh.prefab";
        private static readonly string[] OutwearPaths = {
            "Assets/ithappy/Creative_Characters_FREE/Prefabs/Outwear/Outwear_004.prefab",
            "Assets/ithappy/Creative_Characters_FREE/Prefabs/Outwear/Outwear_043.prefab",
            "Assets/ithappy/Creative_Characters_FREE/Prefabs/Outwear/Outwear_050.prefab",
        };
        private static readonly string[] PantsPaths = {
            "Assets/ithappy/Creative_Characters_FREE/Prefabs/Pants/Pants_009.prefab",
            "Assets/ithappy/Creative_Characters_FREE/Prefabs/Pants/Pants_010.prefab",
        };
        private static readonly string[] HairPaths = {
            "Assets/ithappy/Creative_Characters_FREE/Prefabs/Hairstyle/Hairstyle_Male_001.prefab",
            "Assets/ithappy/Creative_Characters_FREE/Prefabs/Hairstyle/Hairstyle_Male_005.prefab",
            "Assets/ithappy/Creative_Characters_FREE/Prefabs/Hairstyle Single/Hairstyle_Male_Single_006.prefab",
        };
        private static readonly string[] ShoePaths = {
            "Assets/ithappy/Creative_Characters_FREE/Prefabs/Shoes/Shoe_Sneakers_009.prefab",
            "Assets/ithappy/Creative_Characters_FREE/Prefabs/Shoes/Shoe_Slippers_002.prefab",
        };

        // Fallback palettes
        private static readonly Color[] ShirtPalette = {
            new Color(0.85f, 0.20f, 0.20f), new Color(0.20f, 0.50f, 0.85f),
            new Color(0.95f, 0.75f, 0.10f), new Color(0.30f, 0.70f, 0.30f),
            new Color(0.70f, 0.30f, 0.85f), new Color(0.95f, 0.55f, 0.20f),
        };
        private static readonly Color[] SkinPalette = {
            new Color(0.95f, 0.80f, 0.65f), new Color(0.80f, 0.60f, 0.45f),
            new Color(0.60f, 0.40f, 0.28f), new Color(0.42f, 0.28f, 0.18f),
        };
        private static readonly Color[] PantsPalette = {
            new Color(0.20f, 0.20f, 0.45f), new Color(0.15f, 0.15f, 0.15f),
            new Color(0.42f, 0.28f, 0.18f), new Color(0.55f, 0.50f, 0.45f),
        };

        private void Start()
        {
            HideExistingMesh();
            if (!TryBuildIthappyCharacter())
                BuildFallbackCharacter();
        }

        private void HideExistingMesh()
        {
            foreach (var r in GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.gameObject.name.StartsWith("_PT_")) continue;
                r.enabled = false;
            }
        }

        // ── ithappy modular character ─────────────────────────────────────────

        private bool TryBuildIthappyCharacter()
        {
#if UNITY_EDITOR
            var basePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(BaseMeshPath);
            if (basePrefab == null) return false;

            var baseInst = Instantiate(basePrefab);
            baseInst.name = "_PT_Char";
            baseInst.transform.SetParent(transform, false);
            baseInst.transform.localPosition = Vector3.zero;
            baseInst.transform.localScale    = Vector3.one * 0.85f;

            // Bone map from base skeleton
            var boneMap = new Dictionary<string, Transform>();
            foreach (var t in baseInst.GetComponentsInChildren<Transform>(true))
                if (!boneMap.ContainsKey(t.name)) boneMap[t.name] = t;

            // Find root bone (first SMR's rootBone)
            Transform rootBone = null;
            var baseSmr = baseInst.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (baseSmr != null && baseSmr.rootBone != null)
                rootBone = baseSmr.rootBone;

            AttachPart(OutwearPaths[Random.Range(0, OutwearPaths.Length)], baseInst.transform, boneMap, rootBone);
            AttachPart(PantsPaths[Random.Range(0, PantsPaths.Length)],     baseInst.transform, boneMap, rootBone);
            AttachPart(HairPaths[Random.Range(0, HairPaths.Length)],       baseInst.transform, boneMap, rootBone);
            AttachPart(ShoePaths[Random.Range(0, ShoePaths.Length)],       baseInst.transform, boneMap, rootBone);

            StartCoroutine(IdleBob(baseInst.transform));
            return true;
#else
            return false;
#endif
        }

#if UNITY_EDITOR
        private static void AttachPart(string path, Transform baseRoot,
            Dictionary<string, Transform> boneMap, Transform rootBone)
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            var inst = Instantiate(prefab);
            inst.name = "_PT_Part";
            inst.transform.SetParent(baseRoot, false);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale    = Vector3.one;

            foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                RetargetBones(smr, boneMap, rootBone);
        }

        private static void RetargetBones(SkinnedMeshRenderer smr,
            Dictionary<string, Transform> boneMap, Transform rootBone)
        {
            var bones = smr.bones;
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null && boneMap.TryGetValue(bones[i].name, out Transform t))
                    bones[i] = t;
            smr.bones = bones;

            if (smr.rootBone != null && boneMap.TryGetValue(smr.rootBone.name, out Transform rb))
                smr.rootBone = rb;
            else if (rootBone != null)
                smr.rootBone = rootBone;
        }
#endif

        // ── Fallback primitives ───────────────────────────────────────────────

        private void BuildFallbackCharacter()
        {
            Color skin  = SkinPalette[Random.Range(0, SkinPalette.Length)];
            Color shirt = ShirtPalette[Random.Range(0, ShirtPalette.Length)];
            Color pants = PantsPalette[Random.Range(0, PantsPalette.Length)];
            Color hair  = new Color(0.18f, 0.10f, 0.05f);

            var root = new GameObject("_PT_Char");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.zero;

            Build(root, "_PT_Head",  PrimitiveType.Sphere,
                new Vector3(0f, 1.55f, 0f),     new Vector3(0.35f, 0.35f, 0.35f), skin);
            Build(root, "_PT_Hair",  PrimitiveType.Sphere,
                new Vector3(0f, 1.62f, -0.02f), new Vector3(0.37f, 0.22f, 0.37f), hair);
            Build(root, "_PT_Torso", PrimitiveType.Capsule,
                new Vector3(0f, 1.05f, 0f),     new Vector3(0.45f, 0.40f, 0.30f), shirt);
            Build(root, "_PT_ArmL",  PrimitiveType.Capsule,
                new Vector3(-0.28f, 1.05f, 0f), new Vector3(0.13f, 0.30f, 0.13f), shirt);
            Build(root, "_PT_ArmR",  PrimitiveType.Capsule,
                new Vector3( 0.28f, 1.05f, 0f), new Vector3(0.13f, 0.30f, 0.13f), shirt);
            Build(root, "_PT_LegL",  PrimitiveType.Capsule,
                new Vector3(-0.13f, 0.45f, 0f), new Vector3(0.15f, 0.30f, 0.15f), pants);
            Build(root, "_PT_LegR",  PrimitiveType.Capsule,
                new Vector3( 0.13f, 0.45f, 0f), new Vector3(0.15f, 0.30f, 0.15f), pants);

            StartCoroutine(IdleBob(root.transform));
        }

        private static void Build(GameObject parent, string name, PrimitiveType type,
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

        private IEnumerator IdleBob(Transform body)
        {
            Vector3 basePos = body.localPosition;
            float phase = Random.Range(0f, Mathf.PI * 2f);
            while (true)
            {
                phase += Time.deltaTime * 2.5f;
                body.localPosition = basePos + Vector3.up * (Mathf.Sin(phase) * 0.04f);
                yield return null;
            }
        }
    }
}
