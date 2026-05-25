using UnityEditor;
using UnityEngine;

namespace PizzaTycoon.Editor
{
    public static class PlayerModelSetup
    {
        private const string FbxPath    = "Assets/_Project/Models/Meshy_AI_Red_Cap_Explorer_biped_Character_output.fbx";
        private const string MatFolder  = "Assets/_Project/Models/Materials";
        private const string MatOut     = "Assets/_Project/Models/Materials/Mat_Player.mat";
        private const string PrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";

        // Textures já existem como arquivos separados na pasta do FBX
        private const string TexAlbedo   = "Assets/_Project/Models/Meshy_AI_Red_Cap_Explorer_biped_texture_0.png";
        private const string TexNormal   = "Assets/_Project/Models/Meshy_AI_Red_Cap_Explorer_biped_texture_0_normal.png";
        private const string TexMetallic = "Assets/_Project/Models/Meshy_AI_Red_Cap_Explorer_biped_texture_0_metallic.png";
        private const string TexRough    = "Assets/_Project/Models/Meshy_AI_Red_Cap_Explorer_biped_texture_0_roughness.png";

        // Faz tudo de uma vez: material + textura + animator controller
        [MenuItem("PizzaTycoon/Setup Player Model")]
        public static void SetupPlayerModel()
        {
            FixPlayerMaterial();
            // O animator já foi configurado pelo SceneRebuilder durante o rebuild.
            // Este menu serve como atalho para re-aplicar material após rebuild.
            Debug.Log("[PlayerModelSetup] Setup completo: material aplicado.");
            EditorUtility.DisplayDialog("Setup Player Model",
                "Material do player re-aplicado.\n\nSe o AnimatorController não estiver conectado,\nexecute PizzaTycoon/REBUILD primeiro.", "OK");
        }

        [MenuItem("PizzaTycoon/Fix Player Material")]
        public static void FixPlayerMaterial()
        {
            // ── 1. Verifica FBX ───────────────────────────────────────────────
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (fbx == null)
            {
                Debug.LogError("[PlayerModelSetup] FBX não encontrado: " + FbxPath);
                return;
            }
            Debug.Log("[PlayerModelSetup] FBX encontrado: " + FbxPath);

            // Log todos os sub-assets para diagnóstico
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(FbxPath))
                Debug.Log($"[PlayerModelSetup]   sub-asset: {a.GetType().Name} '{a.name}'");

            // ── 2. Carrega texturas externas ──────────────────────────────────
            var texAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TexAlbedo);
            if (texAlbedo == null)
            {
                Debug.LogError("[PlayerModelSetup] Textura albedo não encontrada: " + TexAlbedo);
                return;
            }
            Debug.Log("[PlayerModelSetup] Textura albedo carregada: " + TexAlbedo);

            var texNormal   = AssetDatabase.LoadAssetAtPath<Texture2D>(TexNormal);
            var texMetallic = AssetDatabase.LoadAssetAtPath<Texture2D>(TexMetallic);
            var texRough    = AssetDatabase.LoadAssetAtPath<Texture2D>(TexRough);

            // ── 3. Garante que o normal map tem TextureType correto ───────────
            if (texNormal != null)
                EnsureNormalMap(TexNormal);

            // ── 4. Cria pasta e material ──────────────────────────────────────
            EnsureFolder("Assets/_Project/Models", "Materials");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatOut);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                          ?? Shader.Find("Standard");
                if (shader == null)
                {
                    Debug.LogError("[PlayerModelSetup] Shader URP/Lit não encontrado.");
                    return;
                }
                mat = new Material(shader) { name = "Mat_Player" };
                AssetDatabase.CreateAsset(mat, MatOut);
                AssetDatabase.SaveAssets();
                Debug.Log("[PlayerModelSetup] Material criado: " + MatOut);
            }
            else
            {
                Debug.Log("[PlayerModelSetup] Material já existia, atualizando: " + MatOut);
            }

            // ── 5. Conecta texturas no material ───────────────────────────────
            // Albedo
            if (mat.HasProperty("_BaseMap"))  mat.SetTexture("_BaseMap",  texAlbedo);
            if (mat.HasProperty("_MainTex"))  mat.SetTexture("_MainTex",  texAlbedo);

            // Normal
            if (texNormal != null)
            {
                if (mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", texNormal);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            // Metallic / Smoothness (URP usa _MetallicGlossMap)
            if (texMetallic != null && mat.HasProperty("_MetallicGlossMap"))
            {
                mat.SetTexture("_MetallicGlossMap", texMetallic);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log("[PlayerModelSetup] Texturas conectadas ao material.");

            // ── 6. Abre o prefab ──────────────────────────────────────────────
            var prefabContents = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabContents == null)
            {
                Debug.LogError("[PlayerModelSetup] Prefab não encontrado: " + PrefabPath);
                return;
            }
            Debug.Log("[PlayerModelSetup] Prefab carregado: " + PrefabPath);

            // ── 7. Encontra SkinnedMeshRenderer ──────────────────────────────
            var smr = prefabContents.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr == null)
            {
                Debug.LogError("[PlayerModelSetup] SkinnedMeshRenderer não encontrado no prefab.");
                PrefabUtility.UnloadPrefabContents(prefabContents);
                return;
            }
            Debug.Log($"[PlayerModelSetup] SMR encontrado: '{smr.gameObject.name}'" +
                      $" — slots de material: {smr.sharedMaterials.Length}");

            // ── 8. Conecta material via SerializedObject ───────────────────────
            var smrSo = new SerializedObject(smr);
            smrSo.Update();

            var matsProp = smrSo.FindProperty("m_Materials");
            if (matsProp == null || !matsProp.isArray)
            {
                Debug.LogError("[PlayerModelSetup] Propriedade m_Materials não encontrada.");
                PrefabUtility.UnloadPrefabContents(prefabContents);
                return;
            }

            if (matsProp.arraySize == 0) matsProp.arraySize = 1;
            matsProp.GetArrayElementAtIndex(0).objectReferenceValue = mat;
            smrSo.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[PlayerModelSetup] Material conectado ao SMR Element 0.");

            // Captura nome antes do unload (objetos são destruídos depois)
            string smrName = smr.gameObject.name;

            // ── 9. Salva prefab ───────────────────────────────────────────────
            PrefabUtility.SaveAsPrefabAsset(prefabContents, PrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabContents);
            Debug.Log("[PlayerModelSetup] Prefab salvo: " + PrefabPath);

            // ── 10. Finaliza ──────────────────────────────────────────────────
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlayerModelSetup] Concluído com sucesso.");

            EditorUtility.DisplayDialog(
                "Player Material — OK",
                $"Material aplicado!\n\n" +
                $"• Albedo: {System.IO.Path.GetFileName(TexAlbedo)}\n" +
                $"• Normal: {(texNormal   != null ? "sim" : "não encontrado")}\n" +
                $"• Metallic: {(texMetallic != null ? "sim" : "não encontrado")}\n\n" +
                $"Material: {MatOut}\n" +
                $"SMR: {smrName}",
                "OK");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void EnsureFolder(string parent, string child)
        {
            string full = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(full))
            {
                AssetDatabase.CreateFolder(parent, child);
                Debug.Log("[PlayerModelSetup] Pasta criada: " + full);
            }
        }

        private static void EnsureNormalMap(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            if (importer.textureType == TextureImporterType.NormalMap) return;

            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
            Debug.Log("[PlayerModelSetup] Normal map reimportado: " + assetPath);
        }
    }
}
