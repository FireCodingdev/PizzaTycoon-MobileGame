using UnityEngine;
using UnityEditor;
using System.IO;
using PizzaTycoon.Items;
using PizzaTycoon.Economy;

namespace PizzaTycoon.Editor
{
    // Gera assets ScriptableObject de exemplo prontos para uso
    // Menu: PizzaTycoon > 4. Create ScriptableObjects
    public static class SOCreator
    {
        private const string ITEMS_SO_PATH  = "Assets/_Project/ScriptableObjects/ItemData";
        private const string UPGR_SO_PATH   = "Assets/_Project/ScriptableObjects/UpgradeData";
        private const string STATION_SO_PATH = "Assets/_Project/ScriptableObjects/StationData";

        [MenuItem("PizzaTycoon/4. Create ScriptableObjects")]
        public static void CreateAll()
        {
            EnsureDirectory(ITEMS_SO_PATH);
            EnsureDirectory(UPGR_SO_PATH);
            EnsureDirectory(STATION_SO_PATH);

            CreateItemDataAssets();
            CreateUpgradeDataAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Pizza Tycoon — ScriptableObjects",
                "✅ ScriptableObjects criados!\n\n" +
                "• 4x ItemData\n• 5x UpgradeData\n\n" +
                "Arraste os UpgradeData para o campo 'Available Upgrades' do UpgradeManager.",
                "OK");
        }

        // ─── ItemData ──────────────────────────────────────────────────────────

        private static void CreateItemDataAssets()
        {
            CreateItemData("ItemData_Wheat", ItemType.Wheat, "Trigo",
                "Ingrediente base colhido no campo.", new Color(0.957f, 0.816f, 0.247f), 0f);

            CreateItemData("ItemData_Dough", ItemType.Dough, "Massa de Pizza",
                "Trigo processado, pronto para montar a pizza.", new Color(0.961f, 0.796f, 0.655f), 2f);

            CreateItemData("ItemData_RawPizza", ItemType.RawPizza, "Pizza Crua",
                "Pizza montada aguardando o forno.", new Color(0.961f, 0.933f, 0.808f), 3f);

            CreateItemData("ItemData_CookedPizza", ItemType.CookedPizza, "Pizza Pronta",
                "Pizza assada, pronta para entrega!", new Color(0.902f, 0.494f, 0.133f), 10f);
        }

        private static void CreateItemData(string assetName, ItemType type, string displayName,
            string description, Color color, float baseValue)
        {
            string path = $"{ITEMS_SO_PATH}/{assetName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemData>(path) != null) return;

            ItemData data = ScriptableObject.CreateInstance<ItemData>();
            data.itemType    = type;
            data.displayName = displayName;
            data.description = description;
            data.itemColor   = color;
            data.baseValue   = baseValue;
            data.isStackable = true;

            AssetDatabase.CreateAsset(data, path);
        }

        // ─── UpgradeData ───────────────────────────────────────────────────────

        private static void CreateUpgradeDataAssets()
        {
            // Velocidade do jogador (PlayerSpeed)
            // valuesPerLevel = MoveSpeed real a aplicar
            CreateUpgradeData(
                assetName: "UpgradeData_Velocidade",
                upgradeId: "playerSpeed",
                displayName: "Velocidade",
                description: "Aumente a velocidade de movimento do pizzaiolo.",
                effectType: UpgradeEffectType.PlayerSpeed,
                maxLevel: 10,
                costs:  new[]{ 50f, 75f, 110f, 165f, 245f, 365f, 545f, 815f, 1220f, 1830f },
                values: new[]{ 6f,  7f,  7.5f, 8f,   8.5f, 9f,   9.5f, 10f,  11f,   12f   });

            // Capacidade de pilha (StackCapacity)
            // valuesPerLevel = MaxStackSize
            CreateUpgradeData(
                assetName: "UpgradeData_Capacidade",
                upgradeId: "stackCapacity",
                displayName: "Capacidade",
                description: "Carregue mais pizzas de uma so vez.",
                effectType: UpgradeEffectType.StackCapacity,
                maxLevel: 10,
                costs:  new[]{ 75f, 110f, 165f, 250f, 375f, 560f, 840f, 1260f, 1890f, 2835f },
                values: new[]{ 6f,  7f,   8f,   9f,   10f,  11f,  12f,  13f,   14f,   15f   });

            // Velocidade de colheita (ProductionSpeed — afeta intervalo das estações)
            // valuesPerLevel = 1/interval, ou seja, itens por segundo
            CreateUpgradeData(
                assetName: "UpgradeData_Colheita",
                upgradeId: "productionSpeed",
                displayName: "Velocidade de Colheita",
                description: "Estacoes produzem itens mais rapidamente.",
                effectType: UpgradeEffectType.ProductionSpeed,
                maxLevel: 8,
                costs:  new[]{ 100f, 180f, 320f, 570f, 1000f, 1800f, 3200f, 5700f },
                values: new[]{ 0.6f, 0.75f, 1.0f, 1.3f, 1.7f, 2.2f, 3.0f, 4.0f  });

            // Velocidade do forno (ProductionSpeed — separa upgradeId para uso futuro)
            CreateUpgradeData(
                assetName: "UpgradeData_Forno",
                upgradeId: "ovenSpeed",
                displayName: "Velocidade do Forno",
                description: "O forno assa pizzas mais rapidamente.",
                effectType: UpgradeEffectType.ProductionSpeed,
                maxLevel: 8,
                costs:  new[]{ 150f, 270f, 485f, 870f, 1565f, 2815f, 5065f, 9115f },
                values: new[]{ 0.5f, 0.65f, 0.85f, 1.1f, 1.45f, 1.9f, 2.5f, 3.3f });

            // Intervalo de clientes (SpawnInterval = seconds between spawns — menor = mais clientes)
            CreateUpgradeData(
                assetName: "UpgradeData_Funcionario",
                upgradeId: "spawnInterval",
                displayName: "Funcionario",
                description: "Contrate trabalhadores - clientes chegam com mais frequencia.",
                effectType: UpgradeEffectType.SpawnInterval,
                maxLevel: 3,
                costs:  new[]{ 500f, 1500f, 4000f },
                values: new[]{ 6f,   4f,    2.5f  }); // segundos entre clientes
        }

        private static void CreateUpgradeData(string assetName, string upgradeId, string displayName,
            string description, UpgradeEffectType effectType, int maxLevel,
            float[] costs, float[] values)
        {
            string path = $"{UPGR_SO_PATH}/{assetName}.asset";
            if (AssetDatabase.LoadAssetAtPath<UpgradeData>(path) != null) return;

            UpgradeData data = ScriptableObject.CreateInstance<UpgradeData>();
            data.upgradeId     = upgradeId;
            data.displayName   = displayName;
            data.description   = description;
            data.effectType    = effectType;
            data.maxLevel      = maxLevel;
            data.costsPerLevel = costs;
            data.valuesPerLevel = values;

            AssetDatabase.CreateAsset(data, path);
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
