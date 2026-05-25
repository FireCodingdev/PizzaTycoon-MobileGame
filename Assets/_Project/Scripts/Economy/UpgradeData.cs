using UnityEngine;

namespace PizzaTycoon.Economy
{
    // ScriptableObject que define um upgrade — configurável no Inspector sem código
    [CreateAssetMenu(fileName = "UpgradeData_New", menuName = "PizzaTycoon/Upgrade Data", order = 1)]
    public class UpgradeData : ScriptableObject
    {
        [Header("Identificação")]
        public string upgradeId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Níveis e Custos")]
        public int maxLevel = 5;
        public float[] costsPerLevel;       // custo em $ para cada nível
        public float[] valuesPerLevel;      // valor do efeito em cada nível

        [Header("Tipo de Efeito")]
        public UpgradeEffectType effectType;

        public bool IsMaxLevel(int currentLevel) => currentLevel >= maxLevel;

        public float GetCostForLevel(int nextLevel)
        {
            int index = Mathf.Clamp(nextLevel - 1, 0, costsPerLevel.Length - 1);
            return costsPerLevel.Length > 0 ? costsPerLevel[index] : 0f;
        }

        public float GetValueForLevel(int level)
        {
            int index = Mathf.Clamp(level - 1, 0, valuesPerLevel.Length - 1);
            return valuesPerLevel.Length > 0 ? valuesPerLevel[index] : 0f;
        }
    }

    public enum UpgradeEffectType
    {
        PlayerSpeed,        // aumenta velocidade do jogador
        StackCapacity,      // aumenta limite de empilhamento
        ProductionSpeed,    // reduz tempo de produção das estações
        SpawnInterval,      // reduz intervalo entre clientes
        OvenSlots,          // adiciona slots ao forno
    }
}
