using System;
using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Utils;
using PizzaTycoon.Managers;

namespace PizzaTycoon.Recipes
{
    // Gerencia receitas disponíveis e desbloqueadas
    public class RecipeSystem : Singleton<RecipeSystem>
    {
        [Header("Catálogo")]
        [SerializeField] private RecipeData[] _allRecipes;

        private HashSet<string> _unlockedRecipes = new();

        public static event Action<RecipeData> OnRecipeUnlocked;

        protected override void Awake()
        {
            base.Awake();
            LoadFromSave();
        }

        public bool IsUnlocked(string recipeId) => _unlockedRecipes.Contains(recipeId);

        public bool TryUnlock(string recipeId)
        {
            RecipeData data = Find(recipeId);
            if (data == null || IsUnlocked(recipeId)) return false;

            if (data.unlockCost > 0 && !MoneyManager.Instance.SpendMoney(data.unlockCost))
                return false;

            _unlockedRecipes.Add(recipeId);
            SaveManager.Instance?.CurrentData?.unlockedRecipeIds.Add(recipeId);
            OnRecipeUnlocked?.Invoke(data);
            return true;
        }

        // Retorna o multiplicador ativo para os itens fornecidos
        public float GetMultiplierForItems(List<int> itemTypes)
        {
            if (_allRecipes == null) return 1f;
            float best = 1f;
            foreach (var recipe in _allRecipes)
            {
                if (recipe == null || !IsUnlocked(recipe.recipeId)) continue;
                if (MatchesRecipe(recipe, itemTypes) && recipe.valueMultiplier > best)
                    best = recipe.valueMultiplier;
            }
            return best;
        }

        private bool MatchesRecipe(RecipeData recipe, List<int> itemTypes)
        {
            if (recipe.requiredItemTypes == null || recipe.requiredItemTypes.Length == 0) return false;
            foreach (int req in recipe.requiredItemTypes)
                if (!itemTypes.Contains(req)) return false;
            return true;
        }

        public RecipeData[] GetAll() => _allRecipes;

        public RecipeData Find(string id)
        {
            if (_allRecipes == null) return null;
            foreach (var r in _allRecipes)
                if (r != null && r.recipeId == id) return r;
            return null;
        }

        private void LoadFromSave()
        {
            var data = SaveManager.Instance?.CurrentData;
            if (data?.unlockedRecipeIds == null) return;

            // Receita base sempre desbloqueada
            _unlockedRecipes.Add("margherita");

            foreach (var id in data.unlockedRecipeIds)
                _unlockedRecipes.Add(id);
        }
    }
}
