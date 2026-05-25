using UnityEngine;

namespace PizzaTycoon.Workers
{
    public enum WorkerType { Collector, Baker, Deliverer, Cleaner }

    [CreateAssetMenu(menuName = "PizzaTycoon/Worker Data", fileName = "Worker_New")]
    public class WorkerData : ScriptableObject
    {
        public string     workerId;
        public string     displayName;
        public WorkerType type;
        public int        hiringCost;
        public int        upgradeCostPerLevel;
        public int        maxLevel;
        public float      baseSpeed;        // m/s
        public float      speedPerLevel;    // bônus por nível
        public Color      uniformColor;
        [TextArea(1, 2)]
        public string     description;

        public float GetSpeed(int level) => baseSpeed + speedPerLevel * (level - 1);
        public int GetUpgradeCost(int currentLevel) => upgradeCostPerLevel * currentLevel;
    }
}
