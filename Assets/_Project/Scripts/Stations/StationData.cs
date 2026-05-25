using UnityEngine;
using PizzaTycoon.Items;

namespace PizzaTycoon.Stations
{
    // ScriptableObject com dados configuráveis de cada estação de trabalho
    [CreateAssetMenu(fileName = "StationData_New", menuName = "PizzaTycoon/Station Data", order = 2)]
    public class StationData : ScriptableObject
    {
        [Header("Identificação")]
        public string stationId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Produção")]
        public ItemType inputItemType;    // item que a estação consome
        public ItemType outputItemType;   // item que a estação produz
        public float productionTime = 2f; // segundos por item
        public int maxCapacity = 5;       // máximo de itens na fila

        [Header("Desbloqueio")]
        public bool isLockedByDefault = false;
        public float unlockCost = 0f;

        [Header("Upgrade")]
        public float[] productionTimesPerLevel; // tempo de produção em cada nível de upgrade
    }
}
