using System;
using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Utils;
using PizzaTycoon.Managers;
using PizzaTycoon.GameSystems;
using PizzaTycoon.Monetization;

namespace PizzaTycoon.Analytics
{
    // Centraliza todos os eventos de analytics — provider é injetável
    public class AnalyticsManager : Singleton<AnalyticsManager>
    {
        private IAnalyticsProvider _provider;
        private float _sessionStart;

        protected override void Awake()
        {
            base.Awake();
            _provider = new MockAnalyticsProvider();
            _provider.Initialize();
        }

        private void Start()
        {
            _sessionStart = Time.realtimeSinceStartup;

            SubscribeToEvents();
            LogSessionStart();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            LogSessionEnd();
        }

        // ── Injeção de provider ───────────────────────────────────────────────

        public void SetProvider(IAnalyticsProvider provider)
        {
            _provider = provider;
            if (!provider.IsInitialized) provider.Initialize();
        }

        // ── Subscriptions ─────────────────────────────────────────────────────

        private void SubscribeToEvents()
        {
            GameLoop.OnPhaseUnlocked          += OnPhaseUnlocked;
            ComboSystem.OnComboUpdated        += OnComboUpdated;
            DailyGoalSystem.OnAllGoalsCompleted += _ => Log(GameEvents.ALL_GOALS_COMPLETED);
            DailyGoalSystem.OnGoalProgress    += OnGoalProgress;
            IAPManager.OnPurchaseCompleted    += OnPurchaseCompleted;
            IAPManager.OnPurchaseFailed       += OnPurchaseFailed;
            AdsManager.OnAdStarted            += () => Log(GameEvents.AD_SHOWN);
            AdsManager.OnAdFinished           += () => Log(GameEvents.AD_COMPLETED);
            SaveManager.OnOfflineEarningsApplied += OnOfflineReturn;
        }

        private void UnsubscribeFromEvents()
        {
            GameLoop.OnPhaseUnlocked          -= OnPhaseUnlocked;
            ComboSystem.OnComboUpdated        -= OnComboUpdated;
            IAPManager.OnPurchaseCompleted    -= OnPurchaseCompleted;
            IAPManager.OnPurchaseFailed       -= OnPurchaseFailed;
            SaveManager.OnOfflineEarningsApplied -= OnOfflineReturn;
        }

        // ── Handlers ─────────────────────────────────────────────────────────

        private void OnPhaseUnlocked(int phase) =>
            Log(GameEvents.PHASE_UNLOCKED, new() { { GameParams.PHASE, phase } });

        private void OnComboUpdated(int combo, float mult)
        {
            if (combo == 5 || combo == 10 || combo == 20)
                Log(GameEvents.COMBO_ACHIEVED, new() { { GameParams.COMBO, combo } });
        }

        private void OnGoalProgress(DailyGoal goal)
        {
            if (goal.Completed)
                Log(GameEvents.DAILY_GOAL_COMPLETED, new() { { GameParams.TYPE, goal.Type.ToString() } });
        }

        private void OnPurchaseCompleted(string productId) =>
            Log(GameEvents.PURCHASE_COMPLETED, new() { { "product_id", productId } });

        private void OnPurchaseFailed(string productId) =>
            Log(GameEvents.PURCHASE_FAILED, new() { { "product_id", productId } });

        private void OnOfflineReturn(float earnings) =>
            Log(GameEvents.OFFLINE_RETURN, new() { { GameParams.EARNINGS, Mathf.FloorToInt(earnings) } });

        // ── API pública ───────────────────────────────────────────────────────

        public void LogUpgradePurchased(string upgradeType, int level, float cost) =>
            Log(GameEvents.UPGRADE_PURCHASED, new()
            {
                { GameParams.TYPE,  upgradeType },
                { GameParams.LEVEL, level },
                { GameParams.COST,  Mathf.FloorToInt(cost) }
            });

        public void LogPizzaDelivered(float payment, int phase) =>
            Log(GameEvents.PIZZA_DELIVERED, new()
            {
                { "payment", Mathf.FloorToInt(payment) },
                { GameParams.PHASE, phase }
            });

        public void LogTutorialStep(int step) =>
            Log(GameEvents.TUTORIAL_STEP, new() { { GameParams.STEP, step } });

        public void LogTutorialCompleted() => Log(GameEvents.TUTORIAL_COMPLETED);
        public void LogTutorialSkipped()   => Log(GameEvents.TUTORIAL_SKIPPED);

        // ── Sessão ────────────────────────────────────────────────────────────

        private void LogSessionStart()
        {
            int dayNumber = SaveManager.Instance?.CurrentData?.consecutiveDaysPlayed ?? 1;
            Log(GameEvents.SESSION_START, new() { { GameParams.DAY, dayNumber } });
        }

        private void LogSessionEnd()
        {
            float duration = Time.realtimeSinceStartup - _sessionStart;
            var data = SaveManager.Instance?.CurrentData;
            Log(GameEvents.SESSION_END, new()
            {
                { GameParams.DURATION, Mathf.FloorToInt(duration) },
                { GameParams.PIZZAS,   data?.totalPizzasSold ?? 0  }
            });
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private void Log(string eventName, Dictionary<string, object> parameters = null)
        {
            _provider?.LogEvent(eventName, parameters);
        }
    }
}
