using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Utils;
using PizzaTycoon.Managers;
using PizzaTycoon.Skins;
using PizzaTycoon.Analytics;

namespace PizzaTycoon.Seasons
{
    public enum RewardType { Coins, Skin, Decoration, SpeedBoost, XPBoost }

    [Serializable]
    public class SeasonReward
    {
        public int        level;
        public RewardType type;
        public string     rewardId;
        public int        amount;
        public string     iconColor; // hex
    }

    [Serializable]
    public class Season
    {
        public int           seasonNumber;
        public string        theme;
        public string        startDate; // "yyyy-MM-dd"
        public string        endDate;
        public SeasonReward[] freeTrack;
        public SeasonReward[] premiumTrack;
        public int            premiumCost;
    }

    // Gerencia Season Pass — temporadas de 30 dias
    public class SeasonManager : Singleton<SeasonManager>
    {
        [Header("Configuração")]
        [SerializeField] private int _xpPerPizza        = 10;
        [SerializeField] private int _xpPerComboBonus   = 25;
        [SerializeField] private int _xpPerDailyGoal    = 100;
        [SerializeField] private int _xpPerDailyLogin   = 50;
        [SerializeField] private int _xpPerLevel        = 1000; // XP por nível de season

        public Season   CurrentSeason   { get; private set; }
        public int      CurrentXP       { get; private set; }
        public int      CurrentLevel    { get; private set; }
        public bool     IsPremium       { get; private set; }

        public static event Action<int, int>        OnXPGained;         // (xpGained, totalXP)
        public static event Action<int>             OnLevelUp;          // (newLevel)
        public static event Action<SeasonReward>    OnRewardClaimed;

        private bool _loginBonusGiven;
        private HashSet<int> _claimedFree    = new HashSet<int>();
        private HashSet<int> _claimedPremium = new HashSet<int>();

        protected override void Awake()
        {
            base.Awake();
            CreateDefaultSeason();
            LoadFromSave();
        }

        private void Start()
        {
            GiveDailyLoginBonus();
        }

        // ── Temporada padrão (Season 1 — Verão) ──────────────────────────────

        private void CreateDefaultSeason()
        {
            CurrentSeason = new Season
            {
                seasonNumber = 1,
                theme        = "Verão da Pizza",
                startDate    = DateTime.Now.ToString("yyyy-MM-dd"),
                endDate      = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd"),
                premiumCost  = 499,  // $4.99 em centavos (referência)
                freeTrack    = BuildFreeTrack(),
                premiumTrack = BuildPremiumTrack()
            };
        }

        private SeasonReward[] BuildFreeTrack()
        {
            var rewards = new SeasonReward[30];
            for (int i = 0; i < 30; i++)
            {
                int level = i + 1;
                rewards[i] = new SeasonReward
                {
                    level    = level,
                    type     = level % 10 == 0 ? RewardType.Skin : RewardType.Coins,
                    rewardId = level % 10 == 0 ? $"season1_skin_{level/10}" : "",
                    amount   = level % 10 == 0 ? 0 : (level * 50),
                    iconColor= level % 10 == 0 ? "#FF6B35" : "#F4D03F"
                };
            }
            return rewards;
        }

        private SeasonReward[] BuildPremiumTrack()
        {
            var rewards = new SeasonReward[30];
            for (int i = 0; i < 30; i++)
            {
                int level = i + 1;
                rewards[i] = new SeasonReward
                {
                    level    = level,
                    type     = level % 5 == 0 ? RewardType.Skin : RewardType.Coins,
                    rewardId = level % 5 == 0 ? $"season1_premium_skin_{level/5}" : "",
                    amount   = level * 100,
                    iconColor= "#FFD700"
                };
            }
            return rewards;
        }

        // ── XP ────────────────────────────────────────────────────────────────

        public void AddXP(int amount)
        {
            CurrentXP += amount;
            OnXPGained?.Invoke(amount, CurrentXP);

            // Verifica level ups
            int newLevel = CurrentXP / _xpPerLevel;
            if (newLevel > CurrentLevel)
            {
                int levelsGained = newLevel - CurrentLevel;
                CurrentLevel = newLevel;
                for (int i = 0; i < levelsGained; i++)
                    OnLevelUp?.Invoke(CurrentLevel - levelsGained + i + 1);

                CheckAndClaimRewards();
            }

            SaveToData();
        }

        public void OnPizzaSold()                   => AddXP(_xpPerPizza);
        public void OnComboBonus()                  => AddXP(_xpPerComboBonus);
        public void OnDailyGoalCompleted()          => AddXP(_xpPerDailyGoal);

        private void GiveDailyLoginBonus()
        {
            if (_loginBonusGiven) return;
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string key   = $"SeasonLoginBonus_{today}";
            if (PlayerPrefs.GetInt(key, 0) == 0)
            {
                PlayerPrefs.SetInt(key, 1);
                AddXP(_xpPerDailyLogin);
            }
            _loginBonusGiven = true;
        }

        // ── Recompensas ───────────────────────────────────────────────────────

        private void CheckAndClaimRewards()
        {
            if (CurrentSeason == null) return;

            // Faixa gratuita
            foreach (var r in CurrentSeason.freeTrack)
                if (r.level <= CurrentLevel && !_claimedFree.Contains(r.level))
                    ClaimReward(r, false);

            // Faixa premium
            if (IsPremium)
                foreach (var r in CurrentSeason.premiumTrack)
                    if (r.level <= CurrentLevel && !_claimedPremium.Contains(r.level))
                        ClaimReward(r, true);
        }

        private void ClaimReward(SeasonReward reward, bool isPremiumTrack)
        {
            if (isPremiumTrack) _claimedPremium.Add(reward.level);
            else                _claimedFree.Add(reward.level);

            switch (reward.type)
            {
                case RewardType.Coins:
                    MoneyManager.Instance?.AddMoney(reward.amount);
                    break;
                case RewardType.Skin:
                    SkinManager.Instance?.UnlockSkin(reward.rewardId);
                    break;
                case RewardType.SpeedBoost:
                    // Aplica temporariamente via AdsManager ou PlayerController
                    break;
            }

            OnRewardClaimed?.Invoke(reward);
            // Analytics: log local até SDK real ser integrado
            Debug.Log($"[Analytics] season_reward_claimed: level={reward.level} type={reward.type} premium={isPremiumTrack}");
        }

        public void ActivatePremium()
        {
            IsPremium = true;
            SaveManager.Instance.CurrentData.seasonPremium = true;
            CheckAndClaimRewards();
        }

        // ── Timer até fim ─────────────────────────────────────────────────────

        public TimeSpan TimeRemaining()
        {
            if (!DateTime.TryParse(CurrentSeason?.endDate, out DateTime end))
                return TimeSpan.Zero;
            TimeSpan diff = end - DateTime.Now;
            return diff < TimeSpan.Zero ? TimeSpan.Zero : diff;
        }

        public int XPToNextLevel => _xpPerLevel - (CurrentXP % _xpPerLevel);
        public float LevelProgress => (float)(CurrentXP % _xpPerLevel) / _xpPerLevel;

        // ── Save / Load ───────────────────────────────────────────────────────

        private void SaveToData()
        {
            var data = SaveManager.Instance?.CurrentData;
            if (data == null) return;
            data.seasonXP    = CurrentXP;
            data.seasonLevel = CurrentLevel;
        }

        private void LoadFromSave()
        {
            var data = SaveManager.Instance?.CurrentData;
            if (data == null) return;
            CurrentXP    = data.seasonXP;
            CurrentLevel = data.seasonLevel;
            IsPremium    = data.seasonPremium;

            if (data.claimedSeasonFree    != null)
                foreach (int l in data.claimedSeasonFree)    _claimedFree.Add(l);
            if (data.claimedSeasonPremium != null)
                foreach (int l in data.claimedSeasonPremium) _claimedPremium.Add(l);
        }
    }
}
