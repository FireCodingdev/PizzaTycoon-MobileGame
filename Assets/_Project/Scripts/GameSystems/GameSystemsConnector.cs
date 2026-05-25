using System.Collections;
using UnityEngine;
using PizzaTycoon.Managers;
using PizzaTycoon.Economy;
using PizzaTycoon.Achievements;
using PizzaTycoon.Seasons;
using PizzaTycoon.Leaderboard;
using PizzaTycoon.Events;

namespace PizzaTycoon.GameSystems
{
    [DefaultExecutionOrder(25)]
    public class GameSystemsConnector : MonoBehaviour
    {
        private float _lastMoney = 0f;
        private const float EventCheckInterval = 600f;

        private void OnEnable()
        {
            MoneyManager.OnMoneyChanged       += OnMoneyChanged;
            ComboSystem.OnComboUpdated        += OnComboUpdated;
            ComboSystem.OnComboReset          += OnComboReset;
            DailyGoalSystem.OnGoalProgress    += OnGoalProgress;
            DailyGoalSystem.OnAllGoalsCompleted += OnAllGoalsCompleted;
            GameLoop.OnPhaseUnlocked          += OnPhaseUnlocked;
        }

        private void OnDisable()
        {
            MoneyManager.OnMoneyChanged       -= OnMoneyChanged;
            ComboSystem.OnComboUpdated        -= OnComboUpdated;
            ComboSystem.OnComboReset          -= OnComboReset;
            DailyGoalSystem.OnGoalProgress    -= OnGoalProgress;
            DailyGoalSystem.OnAllGoalsCompleted -= OnAllGoalsCompleted;
            GameLoop.OnPhaseUnlocked          -= OnPhaseUnlocked;
        }

        private void Start()
        {
            // Snapshot do dinheiro inicial para calcular delta
            _lastMoney = MoneyManager.Instance?.CurrentMoney ?? 0f;
            // Inicia loop de eventos
            StartCoroutine(EventLoop());
        }

        // ── Handlers ─────────────────────────────────────────────────────────

        private void OnMoneyChanged(float total)
        {
            float delta = total - _lastMoney;
            _lastMoney  = total;

            if (delta > 0f)
            {
                AchievementManager.Instance?.TrackMoneyEarned(delta);
                int xp = Mathf.Max(1, Mathf.RoundToInt(delta / 5f));
                SeasonManager.Instance?.AddXP(xp);
            }

            LeaderboardManager.Instance?.Submit(LeaderboardCategory.TotalCoins, (long)total);
        }

        private void OnComboUpdated(int combo, float multiplier)
        {
            AchievementManager.Instance?.TrackComboReached(combo);

            if (combo >= 10)       SeasonManager.Instance?.AddXP(25);
            else if (combo >= 5)   SeasonManager.Instance?.AddXP(10);

            LeaderboardManager.Instance?.Submit(LeaderboardCategory.BestCombo, (long)combo);
        }

        private void OnComboReset()
        {
            // ComboSystem.OnComboReset é Action — combo final não é passado
        }

        private void OnGoalProgress(DailyGoal goal)
        {
            if (goal.Type == GoalType.DeliverPizzas)
            {
                AchievementManager.Instance?.TrackCustomerServed();
                SeasonManager.Instance?.AddXP(10);
                LeaderboardManager.Instance?.Submit(LeaderboardCategory.PizzasSold, (long)goal.Current);
            }
        }

        private void OnAllGoalsCompleted(DailyGoal[] goals)
        {
            SeasonManager.Instance?.AddXP(100);
            AchievementManager.Instance?.TrackUpgradeBought();
            Debug.Log("[Connector] Todas as metas diárias concluídas! +100 XP Season Pass");
        }

        private void OnPhaseUnlocked(int phase)
        {
            AchievementManager.Instance?.TrackPhaseReached(phase);
            SeasonManager.Instance?.AddXP(200);
            LeaderboardManager.Instance?.SubmitCurrentStats();
            Debug.Log($"[Connector] Fase {phase} desbloqueada! +200 XP Season Pass");
        }

        // ── Loop de eventos aleatórios ────────────────────────────────────────

        private IEnumerator EventLoop()
        {
            // Aguarda um pouco antes de começar a checar eventos
            yield return new WaitForSeconds(120f);

            while (true)
            {
                yield return new WaitForSeconds(EventCheckInterval);

                var em = EventManager.Instance;
                if (em == null) yield break;
                if (em.IsEventActive) { yield return null; continue; }

                // Sorteia um evento aleatório
                var types = new[] {
                    GameEventType.RushHour,
                    GameEventType.CoinRain,
                    GameEventType.RareIngredients,
                    GameEventType.LightningChallenge
                };
                GameEventType picked = types[Random.Range(0, types.Length)];
                em.StartEvent(picked);
                Debug.Log($"[Connector] Evento iniciado: {picked}");
            }
        }
    }
}
