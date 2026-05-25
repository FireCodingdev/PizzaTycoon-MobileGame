using System;
using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Utils;
using PizzaTycoon.Managers;

namespace PizzaTycoon.GameSystems
{
    public enum MissionType
    {
        Tutorial,        // completar tutorial
        DeliverPizzas,   // vender N pizzas no balcao
        EarnMoney,       // acumular $N de ganhos
        ReachCombo,      // alcancar combo Nx
        ServeDriveThru,  // atender N carros no drive-thru
        Free             // missao customizavel
    }

    [Serializable]
    public class Mission
    {
        public string  Id;
        public string  Title;        // ex: "Venda 10 pizzas"
        public int     RequiredLevel;
        public MissionType Type;
        public float   Target;       // ex: 10
        public float   Current;      // progresso atual
        public int     XPReward;
        public int     MoneyReward;
        public bool    IsCompleted;

        public float Progress => Target > 0 ? Mathf.Clamp01(Current / Target) : 0f;
    }

    // Sistema de XP/Nivel + missoes por nivel.
    //
    // Eventos:
    //   OnXPChanged(currentXP, neededXP)
    //   OnLevelUp(newLevel)
    //   OnMissionProgress(mission)
    //   OnMissionCompleted(mission) — dispara depois que XP/$ ja foi creditado
    //   OnActiveMissionsChanged()   — dispara quando subiu de nivel e ativou novas missoes
    public class PlayerProgressionSystem : Singleton<PlayerProgressionSystem>
    {
        private int _currentXP;
        private int _currentLevel = 1;

        private readonly List<Mission> _allMissions    = new List<Mission>();
        private readonly List<Mission> _activeMissions = new List<Mission>();

        public int CurrentXP    => _currentXP;
        public int CurrentLevel => _currentLevel;
        public int XPForNextLevel => GetXPForLevel(_currentLevel);
        public IReadOnlyList<Mission> ActiveMissions => _activeMissions;

        public static event Action<int, int>  OnXPChanged;          // xpAtual, xpNecessario
        public static event Action<int>       OnLevelUp;
        public static event Action<Mission>   OnMissionProgress;
        public static event Action<Mission>   OnMissionCompleted;
        public static event Action            OnActiveMissionsChanged;

        protected override void Awake()
        {
            base.Awake();
            BuildMissionCatalog();
            ActivateMissionsForLevel(_currentLevel);
        }

        private void OnEnable()
        {
            // Hook automatico em eventos existentes do projeto.
            MoneyManager.OnMoneyAdded   += HandleMoneyEarned;
            ComboSystem.OnComboUpdated  += HandleComboUpdated;
        }

        private void OnDisable()
        {
            MoneyManager.OnMoneyAdded   -= HandleMoneyEarned;
            ComboSystem.OnComboUpdated  -= HandleComboUpdated;
        }

        private void HandleComboUpdated(int combo, float multiplier)
            => RegisterComboReached(combo);

        // ── Curva de XP por nivel ───────────────────────────────────────────
        // Level 1 -> 75 XP, Level 2 -> 200, Level 3 -> 375, Level 4 -> 600
        // Level 5 -> 875, Level 6 -> 1200, Level 7 -> 1575
        public static int GetXPForLevel(int level)
            => 50 * level + 25 * level * level;

        // ── API publica de registro de progresso ────────────────────────────
        // (chame estes metodos de onde quer que a acao acontece)
        public void RegisterTutorialComplete()      => Progress(MissionType.Tutorial, 1f);
        public void RegisterPizzaDelivered()        => Progress(MissionType.DeliverPizzas, 1f);
        public void RegisterDriveThruServed()       => Progress(MissionType.ServeDriveThru, 1f);
        public void RegisterComboReached(int combo) => SetMaxProgress(MissionType.ReachCombo, combo);
        public void RegisterMoneyEarned(float amt)  => Progress(MissionType.EarnMoney, amt);

        private void HandleMoneyEarned(float amount) => RegisterMoneyEarned(amount);

        // ── Internals ───────────────────────────────────────────────────────
        private void Progress(MissionType type, float amount)
        {
            for (int i = 0; i < _activeMissions.Count; i++)
            {
                var m = _activeMissions[i];
                if (m.Type != type || m.IsCompleted) continue;
                m.Current = Mathf.Min(m.Current + amount, m.Target);
                OnMissionProgress?.Invoke(m);
                if (m.Current >= m.Target) CompleteMission(m);
            }
        }

        private void SetMaxProgress(MissionType type, float value)
        {
            for (int i = 0; i < _activeMissions.Count; i++)
            {
                var m = _activeMissions[i];
                if (m.Type != type || m.IsCompleted) continue;
                if (value > m.Current)
                {
                    m.Current = Mathf.Min(value, m.Target);
                    OnMissionProgress?.Invoke(m);
                    if (m.Current >= m.Target) CompleteMission(m);
                }
            }
        }

        private void CompleteMission(Mission m)
        {
            if (m.IsCompleted) return;
            m.IsCompleted = true;

            // Recompensas
            if (m.XPReward > 0) AddXPInternal(m.XPReward);
            if (m.MoneyReward > 0)
                MoneyManager.Instance?.AddMoney(m.MoneyReward, transform.position);

            OnMissionCompleted?.Invoke(m);
        }

        // Adiciona XP e checa level-ups. Pode disparar varios level-ups em cascata.
        private void AddXPInternal(int amount)
        {
            _currentXP += amount;
            OnXPChanged?.Invoke(_currentXP, XPForNextLevel);

            while (_currentXP >= XPForNextLevel)
            {
                _currentXP -= XPForNextLevel;
                _currentLevel++;
                OnLevelUp?.Invoke(_currentLevel);
                ActivateMissionsForLevel(_currentLevel);
                OnXPChanged?.Invoke(_currentXP, XPForNextLevel);
            }
        }

        private void ActivateMissionsForLevel(int level)
        {
            _activeMissions.Clear();
            foreach (var m in _allMissions)
            {
                if (m.RequiredLevel == level && !m.IsCompleted)
                    _activeMissions.Add(m);
            }
            OnActiveMissionsChanged?.Invoke();
        }

        // ── Catalogo de missoes — edite aqui pra adicionar/mudar missoes ─────
        private void BuildMissionCatalog()
        {
            _allMissions.Clear();

            _allMissions.Add(new Mission {
                Id = "lvl1_tutorial",      Title = "Complete o tutorial",
                RequiredLevel = 1, Type = MissionType.Tutorial,
                Target = 1,   XPReward = 75,  MoneyReward = 50,
            });

            _allMissions.Add(new Mission {
                Id = "lvl2_sell10",        Title = "Venda 10 pizzas",
                RequiredLevel = 2, Type = MissionType.DeliverPizzas,
                Target = 10,  XPReward = 100, MoneyReward = 100,
            });

            _allMissions.Add(new Mission {
                Id = "lvl3_earn500",       Title = "Ganhe $500",
                RequiredLevel = 3, Type = MissionType.EarnMoney,
                Target = 500, XPReward = 150, MoneyReward = 150,
            });

            _allMissions.Add(new Mission {
                Id = "lvl4_combo5",        Title = "Faca combo x5",
                RequiredLevel = 4, Type = MissionType.ReachCombo,
                Target = 5,   XPReward = 200, MoneyReward = 200,
            });

            _allMissions.Add(new Mission {
                Id = "lvl5_drivethru5",    Title = "Atenda 5 carros no drive-thru",
                RequiredLevel = 5, Type = MissionType.ServeDriveThru,
                Target = 5,   XPReward = 250, MoneyReward = 300,
            });

            _allMissions.Add(new Mission {
                Id = "lvl6_sell25",        Title = "Venda 25 pizzas",
                RequiredLevel = 6, Type = MissionType.DeliverPizzas,
                Target = 25,  XPReward = 350, MoneyReward = 500,
            });

            _allMissions.Add(new Mission {
                Id = "lvl7_earn2000",      Title = "Ganhe $2.000",
                RequiredLevel = 7, Type = MissionType.EarnMoney,
                Target = 2000, XPReward = 500, MoneyReward = 750,
            });

            _allMissions.Add(new Mission {
                Id = "lvl8_combo10",       Title = "Faca combo x10",
                RequiredLevel = 8, Type = MissionType.ReachCombo,
                Target = 10,  XPReward = 700, MoneyReward = 1000,
            });

            _allMissions.Add(new Mission {
                Id = "lvl9_sell50",        Title = "Venda 50 pizzas",
                RequiredLevel = 9, Type = MissionType.DeliverPizzas,
                Target = 50,  XPReward = 1000, MoneyReward = 1500,
            });

            _allMissions.Add(new Mission {
                Id = "lvl10_drivethru20",  Title = "Atenda 20 carros no drive-thru",
                RequiredLevel = 10, Type = MissionType.ServeDriveThru,
                Target = 20, XPReward = 1400, MoneyReward = 2500,
            });
        }
    }
}
