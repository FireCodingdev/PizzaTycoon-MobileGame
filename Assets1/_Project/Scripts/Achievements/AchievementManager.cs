using System;
using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Utils;
using PizzaTycoon.Managers;
using PizzaTycoon.Analytics;

namespace PizzaTycoon.Achievements
{
    [Serializable]
    public class AchievementSaveEntry
    {
        public string id;
        public float  currentValue;
        public int    milestoneIndex; // próximo milestone a atingir
    }

    // Rastreia progresso de todas as conquistas do jogo
    public class AchievementManager : Singleton<AchievementManager>
    {
        [Header("Dados")]
        [SerializeField] private AchievementData[] _allAchievements;

        private Dictionary<string, AchievementSaveEntry> _progress = new Dictionary<string, AchievementSaveEntry>();

        public static event Action<AchievementData, int> OnAchievementUnlocked;
        // (achievementData, milestoneIndex) — milestoneIndex=0..n-1

        protected override void Awake()
        {
            base.Awake();
            LoadProgress();
        }

        // ── Incremento de valores ─────────────────────────────────────────────

        public void TrackPizzaMade()        => Add("pizza_made",       1);
        public void TrackPizzaBaked()       => Add("pizza_baked",      1);
        public void TrackWheatCollected()   => Add("wheat_collected",  1);
        public void TrackMoneyEarned(float amount) => Add("money_earned", amount);
        public void TrackCustomerServed()   => Add("customer_happy",   1);
        public void TrackCustomerLost()     => Add("no_complaints_reset", 1);
        public void TrackComboReached(int combo)
        {
            SetMax("best_combo", combo);
            // Conquista de "manter combo" registrada via tempo (simplificado)
        }
        public void TrackUpgradeBought()    => Add("upgrades_bought",  1);
        public void TrackSkinUnlocked()     => Add("skins_unlocked",   1);

        // Chamado pelo GameLoop
        public void TrackPhaseReached(int phase) => SetMax("phase_reached", phase);

        // Sessão consecutiva sem cliente perdido (resetado ao perder)
        private float _noLossTimer;
        public void OnPizzaDelivered()
        {
            Add("no_complaints_timer", Time.deltaTime); // simplificado
        }
        public void OnCustomerAngry() => ResetValue("no_complaints_timer");

        // ── Progresso ─────────────────────────────────────────────────────────

        private void Add(string id, float amount)
        {
            var entry = GetOrCreate(id);
            entry.currentValue += amount;
            CheckMilestones(id, entry);
        }

        private void SetMax(string id, float value)
        {
            var entry = GetOrCreate(id);
            if (value > entry.currentValue)
            {
                entry.currentValue = value;
                CheckMilestones(id, entry);
            }
        }

        private void ResetValue(string id)
        {
            var entry = GetOrCreate(id);
            entry.currentValue = 0f;
        }

        private AchievementSaveEntry GetOrCreate(string id)
        {
            if (!_progress.TryGetValue(id, out var entry))
            {
                entry = new AchievementSaveEntry { id = id };
                _progress[id] = entry;
            }
            return entry;
        }

        private void CheckMilestones(string id, AchievementSaveEntry entry)
        {
            AchievementData data = FindAchievement(id);
            if (data == null || data.milestones == null) return;

            while (entry.milestoneIndex < data.milestones.Length
                   && entry.currentValue >= data.milestones[entry.milestoneIndex])
            {
                int idx = entry.milestoneIndex;
                entry.milestoneIndex++;

                // Recompensa em coins
                int reward = (data.coinRewards != null && idx < data.coinRewards.Length)
                    ? data.coinRewards[idx] : 0;
                if (reward > 0)
                    MoneyManager.Instance?.AddMoney(reward);

                OnAchievementUnlocked?.Invoke(data, idx);

                // Analytics: usar método wrapper público
                Debug.Log($"[Analytics] achievement_unlocked: id={id} milestone={idx+1}");

                Debug.Log($"[Achievement] {data.title} — nível {idx + 1} desbloqueado!");
            }
        }

        private AchievementData FindAchievement(string id)
        {
            if (_allAchievements == null) return null;
            foreach (var a in _allAchievements)
                if (a != null && a.id == id) return a;
            return null;
        }

        // ── Consultas ─────────────────────────────────────────────────────────

        public float GetProgress(string id)
        {
            return _progress.TryGetValue(id, out var e) ? e.currentValue : 0f;
        }

        public int GetMilestoneIndex(string id)
        {
            return _progress.TryGetValue(id, out var e) ? e.milestoneIndex : 0;
        }

        public AchievementData[] GetAll() => _allAchievements;

        // ── Persistência ──────────────────────────────────────────────────────

        public AchievementSaveEntry[] GetSaveData()
        {
            var list = new AchievementSaveEntry[_progress.Count];
            _progress.Values.CopyTo(list, 0);
            return list;
        }

        public void LoadProgress()
        {
            _progress.Clear();
            var data = SaveManager.Instance?.CurrentData?.achievementProgress;
            if (data == null) return;
            foreach (var entry in data)
                if (!string.IsNullOrEmpty(entry.id))
                    _progress[entry.id] = entry;
        }
    }
}
