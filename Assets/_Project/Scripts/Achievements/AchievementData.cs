using UnityEngine;

namespace PizzaTycoon.Achievements
{
    public enum AchievementCategory
    {
        Producao, Vendas, Combo, Progressao, Especial
    }

    [CreateAssetMenu(menuName = "PizzaTycoon/Achievement", fileName = "Achievement_New")]
    public class AchievementData : ScriptableObject
    {
        public string              id;
        public string              title;
        [TextArea(2, 4)]
        public string              description;
        public AchievementCategory category;
        public int[]               milestones;    // ex: {10, 50, 100, 500, 1000}
        public int[]               coinRewards;   // ex: {50, 100, 200, 500, 1000}
        public string              iconColor;     // hex: "#FF6B35"
        public bool                isHidden;      // oculta até desbloquear

        public int MilestoneCount => milestones?.Length ?? 0;

        public string GetDescription(int milestoneIndex)
        {
            if (milestones == null || milestoneIndex >= milestones.Length) return description;
            return string.Format(description, milestones[milestoneIndex]);
        }

        public Color GetIconColor()
        {
            if (ColorUtility.TryParseHtmlString(iconColor, out Color c)) return c;
            return Color.gray;
        }
    }
}
