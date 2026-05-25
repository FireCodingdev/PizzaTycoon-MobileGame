using System;
using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Utils;

namespace PizzaTycoon.GameSystems
{
    public enum GoalType { EarnMoney, DeliverPizzas, ReachCombo }

    [Serializable]
    public class DailyGoal
    {
        public GoalType Type;
        public float    Target;
        public float    Current;
        public bool     Completed;
        public float    RewardMultiplier;

        public float Progress => Mathf.Clamp01(Current / Target);
    }

    [Serializable]
    public class DailyGoalSaveData
    {
        public string   SaveDate;  // "yyyy-MM-dd"
        public DailyGoal[] Goals;
        public bool     RewardClaimed;
    }

    // Gera 3 metas diárias aleatórias e rastreia progresso
    public class DailyGoalSystem : Singleton<DailyGoalSystem>
    {
        public DailyGoal[] Goals { get; private set; }

        public static event Action<DailyGoal>   OnGoalProgress;
        public static event Action<DailyGoal[]> OnAllGoalsCompleted;

        private bool _allCompleted;

        protected override void Awake()
        {
            base.Awake();
            Goals = GenerateGoals();
        }

        private DailyGoal[] GenerateGoals()
        {
            var allTypes = (GoalType[])Enum.GetValues(typeof(GoalType));
            var chosen   = new List<GoalType>(allTypes);

            // Embaralha e pega 3
            for (int i = chosen.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (chosen[i], chosen[j]) = (chosen[j], chosen[i]);
            }

            var goals = new DailyGoal[3];
            for (int i = 0; i < 3; i++)
            {
                goals[i] = new DailyGoal
                {
                    Type             = chosen[i],
                    Target           = GetTarget(chosen[i]),
                    RewardMultiplier = UnityEngine.Random.Range(1.1f, 1.4f)
                };
            }
            return goals;
        }

        private float GetTarget(GoalType type) => type switch
        {
            GoalType.EarnMoney      => Mathf.Round(UnityEngine.Random.Range(150f, 500f) / 50f) * 50f,
            GoalType.DeliverPizzas  => UnityEngine.Random.Range(5, 20),
            GoalType.ReachCombo     => UnityEngine.Random.Range(5, 15),
            _                       => 10f
        };

        // ── Registro de progresso ────────────────────────────────────────────

        public void RegisterMoneyEarned(float amount)   => AddProgress(GoalType.EarnMoney,     amount);
        public void RegisterPizzaDelivered()            => AddProgress(GoalType.DeliverPizzas, 1f);
        public void RegisterComboReached(int combo)     => SetMaxProgress(GoalType.ReachCombo, combo);

        private void AddProgress(GoalType type, float amount)
        {
            foreach (var goal in Goals)
            {
                if (goal.Type != type || goal.Completed) continue;
                goal.Current = Mathf.Min(goal.Current + amount, goal.Target);
                CheckCompletion(goal);
                OnGoalProgress?.Invoke(goal);
            }
        }

        private void SetMaxProgress(GoalType type, float value)
        {
            foreach (var goal in Goals)
            {
                if (goal.Type != type || goal.Completed) continue;
                if (value > goal.Current)
                {
                    goal.Current = Mathf.Min(value, goal.Target);
                    CheckCompletion(goal);
                    OnGoalProgress?.Invoke(goal);
                }
            }
        }

        private void CheckCompletion(DailyGoal goal)
        {
            if (goal.Completed || goal.Current < goal.Target) return;
            goal.Completed = true;

            if (!_allCompleted && AllCompleted())
            {
                _allCompleted = true;
                OnAllGoalsCompleted?.Invoke(Goals);
            }
        }

        private bool AllCompleted()
        {
            foreach (var g in Goals)
                if (!g.Completed) return false;
            return true;
        }

        // ── Serialização ─────────────────────────────────────────────────────

        public DailyGoalSaveData GetSaveData() => new DailyGoalSaveData
        {
            SaveDate      = DateTime.Now.ToString("yyyy-MM-dd"),
            Goals         = Goals,
            RewardClaimed = _allCompleted
        };

        public void LoadSaveData(DailyGoalSaveData data)
        {
            if (data == null) return;
            if (data.SaveDate != DateTime.Now.ToString("yyyy-MM-dd"))
            {
                Goals = GenerateGoals(); // novo dia, novas metas
                return;
            }
            Goals         = data.Goals ?? GenerateGoals();
            _allCompleted = data.RewardClaimed;
        }
    }
}
