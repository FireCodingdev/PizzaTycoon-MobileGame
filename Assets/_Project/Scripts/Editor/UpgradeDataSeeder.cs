using UnityEditor;
using UnityEngine;
using PizzaTycoon.Economy;

namespace PizzaTycoon.Editor
{
    public static class UpgradeDataSeeder
    {
        [MenuItem("PizzaTycoon/Seed UpgradeData Values")]
        public static void SeedAll()
        {
            SeedAsset("UpgradeData_Velocidade",  "playerSpeed",     UpgradeEffectType.PlayerSpeed,
                new float[] { 50, 150, 350, 750, 1500 },   new float[] { 6, 7, 8.5f, 10, 12 }, 5);

            SeedAsset("UpgradeData_Capacidade",  "stackCapacity",   UpgradeEffectType.StackCapacity,
                new float[] { 75, 200, 450, 900, 2000 },   new float[] { 7, 9, 11, 14, 18 }, 5);

            SeedAsset("UpgradeData_Colheita",    "productionSpeed", UpgradeEffectType.ProductionSpeed,
                new float[] { 100, 250, 500, 1000, 2500 }, new float[] { 1.5f, 2, 2.5f, 3, 4 }, 5);

            SeedAsset("UpgradeData_Forno",       "ovenSlots",       UpgradeEffectType.OvenSlots,
                new float[] { 300, 800, 2000 },            new float[] { 3, 4, 6 }, 3);

            AssetDatabase.SaveAssets();
            Debug.Log("[UpgradeDataSeeder] UpgradeData assets preenchidos com sucesso!");
        }

        static void SeedAsset(string assetName, string id, UpgradeEffectType effect,
            float[] costs, float[] values, int maxLvl)
        {
            string path = $"Assets/_Project/ScriptableObjects/UpgradeData/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
            if (asset == null)
            {
                Debug.LogWarning($"[UpgradeDataSeeder] Nao encontrado: {path}");
                return;
            }

            asset.upgradeId    = id;
            asset.maxLevel     = maxLvl;
            asset.costsPerLevel  = costs;
            asset.valuesPerLevel = values;
            asset.effectType   = effect;
            EditorUtility.SetDirty(asset);
        }
    }
}
