using UnityEngine;
using PizzaTycoon.Items;

namespace PizzaTycoon.Items
{
    // ScriptableObject com dados visuais e configuráveis de cada tipo de item
    [CreateAssetMenu(fileName = "ItemData_New", menuName = "PizzaTycoon/Item Data", order = 0)]
    public class ItemData : ScriptableObject
    {
        [Header("Identificação")]
        public ItemType itemType;
        public string displayName;
        [TextArea] public string description;

        [Header("Visual")]
        public Sprite icon;
        public GameObject prefab;
        public Color itemColor = Color.white;

        [Header("Configurações")]
        public float baseValue = 1f;  // valor monetário base do item
        public bool isStackable = true;
    }
}
