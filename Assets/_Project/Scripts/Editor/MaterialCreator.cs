using UnityEngine;
using UnityEditor;
using System.IO;

namespace PizzaTycoon.Editor
{
    // Gera todos os Materials URP do projeto via menu
    // Deve ser executado primeiro: PizzaTycoon > 1. Create Materials
    public static class MaterialCreator
    {
        private const string MATS_PATH = "Assets/_Project/Materials";

        [MenuItem("PizzaTycoon/1. Create Materials")]
        public static void CreateAllMaterials()
        {
            EnsureDirectory(MATS_PATH);
            int created = 0;

            // --- Terreno ---
            if (Create("Mat_Ground",      HexToColor("E8D5B0"), 0.10f)) created++;
            if (Create("Mat_Grass",       HexToColor("7DC855"), 0.20f)) created++;
            if (Create("Mat_Road",        HexToColor("808080"), 0.15f)) created++;

            // --- Jogador ---
            if (Create("Mat_Player_Body", HexToColor("C0392B"), 0.30f)) created++;
            if (Create("Mat_Player_Skin", HexToColor("FDBCB4"), 0.20f)) created++;
            if (Create("Mat_Player_Hat",  HexToColor("C0392B"), 0.15f)) created++;

            // --- Estações ---
            if (Create("Mat_Station_Blue",  HexToColor("3498DB"), 0.50f)) created++;
            if (Create("Mat_Station_Dark",  HexToColor("2C3E50"), 0.40f)) created++;
            if (Create("Mat_Station_Wood",  HexToColor("8B6914"), 0.10f)) created++;
            if (Create("Mat_Station_Silver",HexToColor("BDC3C7"), 0.70f)) created++;

            // --- Itens ---
            if (Create("Mat_Wheat",         HexToColor("F4D03F"), 0.10f)) created++;
            if (Create("Mat_Dough",         HexToColor("F5CBA7"), 0.20f)) created++;
            if (Create("Mat_RawPizza",      HexToColor("F5F0CF"), 0.15f)) created++;
            if (Create("Mat_Pizza_Cooked",  HexToColor("E67E22"), 0.30f)) created++;
            if (Create("Mat_Pizza_Topping", HexToColor("B7170B"), 0.20f)) created++;

            // --- UI / Efeitos ---
            if (Create("Mat_Money",          HexToColor("27AE60"), 0.60f)) created++;
            if (Create("Mat_Indicator_Green",HexToColor("2ECC71"), 0.90f, emissive: true)) created++;
            if (Create("Mat_Indicator_Red",  HexToColor("E74C3C"), 0.90f, emissive: true)) created++;
            if (Create("Mat_Customer_A",     HexToColor("9B59B6"), 0.25f)) created++;
            if (Create("Mat_Customer_B",     HexToColor("E91E63"), 0.25f)) created++;
            if (Create("Mat_Customer_C",     HexToColor("00BCD4"), 0.25f)) created++;
            if (Create("Mat_Arrow",          HexToColor("3498DB"), 0.50f, emissive: true)) created++;
            if (Create("Mat_Car_Body",       HexToColor("E74C3C"), 0.40f)) created++;
            if (Create("Mat_Car_Wheel",      HexToColor("2C3E50"), 0.20f)) created++;
            if (Create("Mat_Car_Glass",      HexToColor("AED6F1"), 0.90f)) created++;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Pizza Tycoon — Materials",
                $"✅ {created} materiais criados em {MATS_PATH}\n\nPróximo passo: PizzaTycoon > 2. Create Prefabs",
                "OK");
        }

        // Cria um material URP/Lit e salva em disco; retorna false se já existia
        private static bool Create(string name, Color color, float smoothness,
            bool emissive = false)
        {
            string path = $"{MATS_PATH}/{name}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return false;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogWarning($"[MaterialCreator] Shader URP/Lit não encontrado. Usando Standard.");
                shader = Shader.Find("Standard");
            }

            Material mat = new Material(shader) { name = name };
            mat.color = color;
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);

            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.6f);
            }

            AssetDatabase.CreateAsset(mat, path);
            return true;
        }

        // Carrega material criado anteriormente (para uso em PrefabBuilder)
        public static Material Load(string name)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MATS_PATH}/{name}.mat");
            if (mat == null)
                Debug.LogWarning($"[MaterialCreator] Material '{name}' não encontrado. Execute 1. Create Materials primeiro.");
            return mat;
        }

        private static Color HexToColor(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
