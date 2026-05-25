#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PizzaTycoon.Editor
{
    // Combina materiais de cor sólida em um atlas para reduzir draw calls
    // Antes: ~25 materiais separados. Depois: 1 atlas + ~3 materiais
    public static class TextureAtlas
    {
        private const int   ATLAS_SIZE     = 512;
        private const int   PIXEL_SIZE     = 32; // cada cor ocupa 32x32px no atlas
        private const string ATLAS_PATH    = "Assets/_Project/Materials/Atlas/ColorAtlas.png";
        private const string MAT_PATH      = "Assets/_Project/Materials/Atlas/Mat_Atlas.mat";

        [MenuItem("PizzaTycoon/7. Bake Texture Atlas", priority = 107)]
        public static void BakeAtlas()
        {
            // 1. Coletar todos os materiais de cor sólida
            var materials = CollectSolidColorMaterials();
            if (materials.Count == 0)
            {
                EditorUtility.DisplayDialog("Atlas", "Nenhum material de cor sólida encontrado.", "OK");
                return;
            }

            // 2. Criar textura atlas
            Texture2D atlas = new Texture2D(ATLAS_SIZE, ATLAS_SIZE, TextureFormat.RGBA32, false);
            FillTransparent(atlas);

            var uvRects = new Dictionary<Material, Rect>();
            int col = 0, row = 0;
            int perRow = ATLAS_SIZE / PIXEL_SIZE;

            foreach (var mat in materials)
            {
                Color c = GetMaterialColor(mat);

                // Preenche bloco PIXEL_SIZE×PIXEL_SIZE com a cor
                int x = col * PIXEL_SIZE;
                int y = row * PIXEL_SIZE;
                FillBlock(atlas, x, y, PIXEL_SIZE, c);

                // UV rect normalizado
                float u  = (float)x / ATLAS_SIZE;
                float v  = (float)y / ATLAS_SIZE;
                float sz = (float)PIXEL_SIZE / ATLAS_SIZE;
                uvRects[mat] = new Rect(u + sz * 0.5f, v + sz * 0.5f, 0f, 0f); // ponto central

                col++;
                if (col >= perRow) { col = 0; row++; }
            }

            atlas.Apply();

            // 3. Salvar atlas como PNG
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ATLAS_PATH));
            System.IO.File.WriteAllBytes(ATLAS_PATH, atlas.EncodeToPNG());
            AssetDatabase.Refresh();

            // 4. Configurar importer
            var importer = (TextureImporter)AssetImporter.GetAtPath(ATLAS_PATH);
            if (importer != null)
            {
                importer.textureType         = TextureImporterType.Default;
                importer.mipmapEnabled       = false;
                importer.filterMode          = FilterMode.Point;
                importer.textureCompression  = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }

            // 5. Criar material atlas
            Texture2D atlasAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(ATLAS_PATH);
            CreateAtlasMaterial(atlasAsset);

            // 6. Atualizar referências (nota: UV offset por objeto precisa de MaterialPropertyBlock em runtime)
            Debug.Log($"[TextureAtlas] Atlas criado: {materials.Count} cores combinadas → {ATLAS_PATH}");
            EditorUtility.DisplayDialog("Atlas criado",
                $"Atlas gerado com {materials.Count} cores.\n\n" +
                "Para uso em runtime, aplique o material Atlas e configure UV offset via MaterialPropertyBlock.",
                "OK");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static List<Material> CollectSolidColorMaterials()
        {
            var result = new List<Material>();
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project/Materials" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Atlas")) continue; // pula o atlas em si

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                // Considera "sólido" materiais que usam apenas cor base (sem texture)
                bool hasTexture = mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null;
                bool hasColor   = mat.HasProperty("_BaseColor");

                if (!hasTexture && hasColor)
                    result.Add(mat);
            }
            return result;
        }

        private static Color GetMaterialColor(Material mat)
        {
            if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
            if (mat.HasProperty("_Color"))     return mat.GetColor("_Color");
            return Color.white;
        }

        private static void FillTransparent(Texture2D tex)
        {
            Color[] pixels = new Color[tex.width * tex.height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
            tex.SetPixels(pixels);
        }

        private static void FillBlock(Texture2D tex, int x, int y, int size, Color color)
        {
            for (int px = x; px < x + size; px++)
                for (int py = y; py < y + size; py++)
                    tex.SetPixel(px, py, color);
        }

        private static void CreateAtlasMaterial(Texture2D atlas)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(MAT_PATH));

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetTexture("_BaseMap", atlas);

            AssetDatabase.CreateAsset(mat, MAT_PATH);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
