namespace PizzaTycoon.Analytics
{
    // Constantes de nomes de eventos para garantir consistência
    public static class GameEvents
    {
        // ── Tutorial ─────────────────────────────────────────────────────────
        public const string TUTORIAL_STARTED   = "tutorial_started";
        public const string TUTORIAL_COMPLETED = "tutorial_completed";
        public const string TUTORIAL_SKIPPED   = "tutorial_skipped";
        public const string TUTORIAL_STEP      = "tutorial_step";       // {step: 1-6}

        // ── Monetização ───────────────────────────────────────────────────────
        public const string AD_SHOWN             = "ad_shown";          // {type: rewarded/interstitial/banner}
        public const string AD_COMPLETED         = "ad_completed";      // {type, reward}
        public const string AD_SKIPPED           = "ad_skipped";
        public const string PURCHASE_INITIATED   = "purchase_initiated"; // {product_id}
        public const string PURCHASE_COMPLETED   = "purchase_completed"; // {product_id, value}
        public const string PURCHASE_FAILED      = "purchase_failed";
        public const string RESTORE_PURCHASES    = "restore_purchases";

        // ── Gameplay ──────────────────────────────────────────────────────────
        public const string PHASE_UNLOCKED       = "phase_unlocked";   // {phase: 1-4}
        public const string UPGRADE_PURCHASED    = "upgrade_purchased"; // {type, level, cost}
        public const string COMBO_ACHIEVED       = "combo_achieved";   // {combo: 5/10/20}
        public const string PIZZA_DELIVERED      = "pizza_delivered";  // {payment, phase}
        public const string CUSTOMER_ANGRY       = "customer_angry";

        // ── Sessão ────────────────────────────────────────────────────────────
        public const string SESSION_START        = "session_start";
        public const string SESSION_END          = "session_end";      // {duration_s, revenue, pizzas}
        public const string APP_OPEN             = "app_open";         // {day_number}
        public const string OFFLINE_RETURN       = "offline_return";   // {hours_away, earnings}

        // ── Goals ─────────────────────────────────────────────────────────────
        public const string DAILY_GOAL_COMPLETED = "daily_goal_completed"; // {type}
        public const string ALL_GOALS_COMPLETED  = "all_goals_completed";

        // ── Acessibilidade ────────────────────────────────────────────────────
        public const string ACCESSIBILITY_CHANGED = "accessibility_changed"; // {setting, value}
    }

    // Chaves de parâmetros padronizadas
    public static class GameParams
    {
        public const string TYPE     = "type";
        public const string PHASE    = "phase";
        public const string LEVEL    = "level";
        public const string COST     = "cost";
        public const string COMBO    = "combo";
        public const string DURATION = "duration_s";
        public const string REVENUE  = "revenue";
        public const string PIZZAS   = "pizzas";
        public const string STEP     = "step";
        public const string VALUE    = "value";
        public const string REWARD   = "reward";
        public const string HOURS    = "hours_away";
        public const string EARNINGS = "earnings";
        public const string DAY      = "day_number";
    }
}
