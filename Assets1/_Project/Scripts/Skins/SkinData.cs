using UnityEngine;

namespace PizzaTycoon.Skins
{
    public enum SkinCategory { Player, Restaurant, Items }

    [CreateAssetMenu(menuName = "PizzaTycoon/Skin Data", fileName = "Skin_New")]
    public class SkinData : ScriptableObject
    {
        public string      skinId;
        public string      displayName;
        public SkinCategory category;
        public Color       primaryColor;
        public Color       secondaryColor;
        public Color       accentColor;
        public int         coinCost;        // 0 = gratuito/season reward
        public bool        isPremium;       // requer VIP ou IAP
        public string      unlockCondition; // "season_1_level_10" / "iap_starter" / ""
        [TextArea(1, 2)]
        public string      description;
    }
}
