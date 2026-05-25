using System.Reflection;
using UnityEngine;

namespace PizzaTycoon.Achievements
{
    // Popula AchievementManager._allAchievements em runtime via reflection,
    // já que não existem .asset files criados no projeto.
    [DefaultExecutionOrder(35)]
    public class AchievementRuntimeSetup : MonoBehaviour
    {
        private void Start()
        {
            var manager = AchievementManager.Instance;
            if (manager == null) return;

            var field = typeof(AchievementManager).GetField("_allAchievements",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var existing = field?.GetValue(manager) as AchievementData[];
            if (existing != null && existing.Length > 0) return;

            field?.SetValue(manager, BuildAchievements());
            Debug.Log("[AchievementSetup] Conquistas carregadas em runtime.");
        }

        private static AchievementData[] BuildAchievements() => new[]
        {
            Make("pizza_made",       "Pizzaiolo",         "Faça {0} pizzas",               AchievementCategory.Producao,
                new[]{10,50,200,500,1000},       new[]{50,150,400,1000,2500},  "#FF6B35"),

            Make("pizza_baked",      "Mestre do Forno",   "Asse {0} pizzas no forno",      AchievementCategory.Producao,
                new[]{10,50,200,500,1000},       new[]{50,150,400,1000,2500},  "#FF4500"),

            Make("wheat_collected",  "Colheiteiro",       "Colete {0} de trigo",           AchievementCategory.Producao,
                new[]{20,100,500,1000,5000},     new[]{30,100,300,700,2000},   "#F5C842"),

            Make("money_earned",     "Empreendedor",      "Ganhe R${0} no total",          AchievementCategory.Vendas,
                new[]{100,500,2000,10000,50000}, new[]{50,200,750,3000,15000}, "#00C853"),

            Make("customer_happy",   "Simpático",         "Atenda {0} clientes felizes",   AchievementCategory.Vendas,
                new[]{5,25,100,500,2000},        new[]{30,100,350,1500,5000},  "#42A5F5"),

            Make("best_combo",       "Combo King",        "Alcance combo x{0}",            AchievementCategory.Combo,
                new[]{5,10,20,50,100},           new[]{50,150,500,2000,10000}, "#7B1FA2"),

            Make("upgrades_bought",  "Upgrade Addict",    "Compre {0} upgrades",           AchievementCategory.Progressao,
                new[]{1,5,15,30,50},             new[]{30,100,300,700,1500},   "#1565C0"),

            Make("phase_reached",    "Expansionista",     "Alcance a fase {0}",            AchievementCategory.Progressao,
                new[]{2,3,4,5,6},               new[]{100,250,500,1000,2500}, "#EF6C00"),

            Make("skins_unlocked",   "Fashionista",       "Desbloqueie {0} visuais",       AchievementCategory.Especial,
                new[]{1,3,6,10,15},             new[]{50,150,400,1000,3000},  "#E91E63"),

            Make("no_complaints_reset", "Zero a Zero",   "Sirva {0} sem perder cliente",  AchievementCategory.Especial,
                new[]{10,30,100,300,1000},       new[]{75,200,600,2000,8000},  "#00BCD4", hidden: true),
        };

        private static AchievementData Make(string id, string title, string description,
            AchievementCategory category, int[] milestones, int[] rewards, string iconColor,
            bool hidden = false)
        {
            var d = ScriptableObject.CreateInstance<AchievementData>();
            d.id          = id;
            d.title       = title;
            d.description = description;
            d.category    = category;
            d.milestones  = milestones;
            d.coinRewards = rewards;
            d.iconColor   = iconColor;
            d.isHidden    = hidden;
            return d;
        }
    }
}
