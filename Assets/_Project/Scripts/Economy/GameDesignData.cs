using UnityEngine;

namespace PizzaTycoon.Economy
{
    // ScriptableObject central de balanceamento — todos os valores de design ficam aqui.
    // Crie o asset via: Assets > Create > PizzaTycoon > Game Design Data
    // Salve em Assets/_Project/Resources/GameDesignData.asset para acesso via Instance.
    [CreateAssetMenu(fileName = "GameDesignData", menuName = "PizzaTycoon/Game Design Data", order = 10)]
    public class GameDesignData : ScriptableObject
    {
        // ── Singleton via Resources ──────────────────────────────────────────
        private static GameDesignData _instance;
        public static GameDesignData Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<GameDesignData>("GameDesignData");
                if (_instance == null)
                {
                    Debug.LogWarning("[GameDesignData] Asset não encontrado em Resources/. Usando valores padrão.");
                    _instance = CreateInstance<GameDesignData>();
                }
                return _instance;
            }
        }

        // ── Jogador ──────────────────────────────────────────────────────────
        [Header("Jogador")]
        public float baseSpeed = 4.5f;
        public int   baseStackSize = 3;
        public float stackItemSpacingY = 0.3f;
        public float playerRotationSpeed = 10f;

        // ── Tempos de Produção (segundos) ────────────────────────────────────
        [Header("Estações — Timers (segundos)")]
        public float wheatRespawnTime   = 3.0f;   // tempo para cada trigo crescer
        public float wheatCollectRate   = 0.5f;   // intervalo de coleta no campo
        public float doughProcessTime   = 2.5f;   // trigo → massa
        public float assemblyTime       = 1.5f;   // massa → pizza crua
        public float ovenCookTime       = 5.0f;   // pizza crua → pizza pronta
        public int   wheatPerDough      = 2;      // trigros necessários por massa
        public int   ovenMaxSlots       = 2;      // slots simultâneos no forno

        // ── Economia ────────────────────────────────────────────────────────
        [Header("Economia")]
        public float pizzaBaseValue          = 15f;
        public float pizzaBonusIfFast        = 7f;   // bônus se paciência > 70%
        public float customerPatienceTime    = 12f;
        public float customerSpawnInterval   = 8f;
        public int   maxCustomersInQueue     = 4;
        public float startingMoney           = 0f;

        // ── Combo ────────────────────────────────────────────────────────────
        [Header("Combo")]
        public float comboTimeoutSeconds     = 5f;
        public int   combo5xThreshold        = 5;
        public int   combo10xThreshold       = 10;
        public int   combo20xThreshold       = 20;
        public float combo5xMultiplier       = 1.10f;
        public float combo10xMultiplier      = 1.25f;
        public float combo20xMultiplier      = 1.50f;

        // ── Progressão por Fase ──────────────────────────────────────────────
        [Header("Progressão")]
        [Tooltip("Custo em $ para desbloquear cada fase (índice = fase)")]
        public float[] phaseUnlockCosts = { 0f, 200f, 800f, 2000f };
        public float   upgradeBaseCostMultiplier = 1.8f;

        // ── Ganhos Offline ───────────────────────────────────────────────────
        [Header("Ganhos Offline")]
        [Range(0f, 1f)]
        public float offlineEarningMultiplier = 0.30f; // 30% do ganho normal
        public float maxOfflineHours          = 4f;    // máximo de horas contadas

        // ── Tutoriais / Metas ────────────────────────────────────────────────
        [Header("Metas Diárias")]
        public int   dailyGoalCount           = 3;
        public float dailyGoalReward1xMult    = 1.2f;
        public float dailyGoalReward2xMult    = 1.5f;
        public float dailyGoalReward3xMult    = 2.0f;
        public float goalRewardDurationSeconds = 60f;

        // ── VFX / Câmera ─────────────────────────────────────────────────────
        [Header("Câmera")]
        public float cameraShakeSell         = 0.08f;
        public float cameraShakeDurationSell = 0.15f;
        public float cameraShakeAngry        = 0.15f;
        public float cameraShakeDurationAngry= 0.20f;
        public float cameraShakeUpgrade      = 0.12f;
        public float cameraShakeDurationUpgr = 0.25f;
        public float cameraIdleZoomDelay     = 3.0f;
        public float cameraZoomInSize        = 6.5f;
        public float cameraDefaultSize       = 8.0f;

        // ── Estimativa de ganho/s (para cálculo offline) ────────────────────
        public float EstimateEarningsPerSecond()
        {
            return pizzaBaseValue / customerSpawnInterval;
        }
    }
}
