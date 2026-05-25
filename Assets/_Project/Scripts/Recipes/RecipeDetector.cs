using System.Collections.Generic;
using UnityEngine;

namespace PizzaTycoon.Recipes
{
    // Acoplado a uma PizzaAssemblyStation — detecta combinação de ingredientes
    // e retorna o multiplicador de receita correspondente
    public class RecipeDetector : MonoBehaviour
    {
        private List<int> _currentItemTypes = new();

        public void AddIngredient(int itemTypeValue)
        {
            _currentItemTypes.Add(itemTypeValue);
        }

        public void RemoveIngredient(int itemTypeValue)
        {
            _currentItemTypes.Remove(itemTypeValue);
        }

        public void ClearIngredients() => _currentItemTypes.Clear();

        // Retorna o multiplicador de preço para os ingredientes atuais
        public float GetCurrentMultiplier()
        {
            var sys = RecipeSystem.Instance;
            return sys != null
                ? sys.GetMultiplierForItems(_currentItemTypes)
                : 1f;
        }

        // Retorna a receita ativa (a de maior multiplicador) para mostrar na UI
        public RecipeData GetActiveRecipe()
        {
            var sys = RecipeSystem.Instance;
            if (sys == null) return null;

            RecipeData best     = null;
            float      bestMult = 1f;

            foreach (var recipe in sys.GetAll())
            {
                if (recipe == null || !sys.IsUnlocked(recipe.recipeId)) continue;
                if (recipe.valueMultiplier <= bestMult) continue;

                bool match = true;
                foreach (int req in recipe.requiredItemTypes)
                    if (!_currentItemTypes.Contains(req)) { match = false; break; }

                if (match) { best = recipe; bestMult = recipe.valueMultiplier; }
            }
            return best;
        }
    }
}
