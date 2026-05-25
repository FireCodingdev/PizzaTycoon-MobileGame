using UnityEngine;

namespace PizzaTycoon.Recipes
{
    public enum RecipeDifficulty { Easy, Medium, Hard }

    [CreateAssetMenu(menuName = "PizzaTycoon/Recipe Data", fileName = "Recipe_New")]
    public class RecipeData : ScriptableObject
    {
        public string           recipeId;
        public string           displayName;
        public RecipeDifficulty difficulty;
        public float            valueMultiplier;  // ex: 1.3x, 1.5x, 2.0x
        public int              unlockCost;       // 0 = desbloqueada desde o início
        public Color            previewColor;
        [TextArea(1, 2)]
        public string           description;

        // Ingredientes necessários (por ItemType int value)
        public int[] requiredItemTypes;
    }
}
