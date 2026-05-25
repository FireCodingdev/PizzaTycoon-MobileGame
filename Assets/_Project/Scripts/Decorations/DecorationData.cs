using UnityEngine;

namespace PizzaTycoon.Decorations
{
    public enum DecorationCategory { Interior, Exterior, Equipment, Seasonal }

    public enum BonusType { None, CoinMultiplier, CustomerSpeed, XPMultiplier, PizzaSpeed }

    [CreateAssetMenu(menuName = "PizzaTycoon/Decoration Data", fileName = "Decoration_New")]
    public class DecorationData : ScriptableObject
    {
        public string             decorationId;
        public string             displayName;
        public DecorationCategory category;
        public int                coinCost;
        public BonusType          bonusType;
        [Range(0f, 1f)]
        public float              bonusAmount;   // ex: 0.10 = +10%
        public string             bonusDescription;
        public Color              previewColor;
        [TextArea(1, 2)]
        public string             description;
    }
}
