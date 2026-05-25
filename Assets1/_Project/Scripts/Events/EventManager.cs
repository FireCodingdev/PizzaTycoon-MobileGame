using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Utils;

namespace PizzaTycoon.Events
{
    public enum GameEventType
    {
        RushHour,           // Clientes 3x mais rápidos, valor +50%
        RareIngredients,    // Pizza especial vale 5x
        CoinRain,           // 20% chance de dropar 2x-5x coins
        LightningChallenge  // Vender X pizzas em 10 min para ganhar recompensa
    }

    [Serializable]
    public class GameEvent
    {
        public GameEventType type;
        public string        displayName;
        public string        description;
        public float         durationSeconds;
        public string        iconColor;       // hex
        public int           challengeTarget; // apenas LightningChallenge
        public int           challengeCoins;  // recompensa em coins
        public int           challengeXP;     // recompensa em XP season
    }

    // Gerencia eventos de tempo limitado que alteram mecânicas do jogo
    public class EventManager : Singleton<EventManager>
    {
        public GameEvent     ActiveEvent     { get; private set; }
        public bool          IsEventActive   => ActiveEvent != null && _timeRemaining > 0f;
        public float         TimeRemaining   => _timeRemaining;
        public int           ChallengeCount  { get; private set; }

        public static event Action<GameEvent>       OnEventStarted;
        public static event Action<GameEvent>       OnEventEnded;
        public static event Action<GameEvent, int>  OnChallengeProgress; // (event, current)
        public static event Action<GameEvent>       OnChallengeCompleted;

        private float   _timeRemaining;
        private bool    _challengeCompleted;
        private Coroutine _timerCoroutine;

        private static readonly List<GameEvent> _allEvents = new()
        {
            new GameEvent
            {
                type            = GameEventType.RushHour,
                displayName     = "Hora do Rush!",
                description     = "Clientes chegam 3x mais rápido e pagam 50% a mais!",
                durationSeconds = 1800f,
                iconColor       = "#FF4136",
                challengeTarget = 0
            },
            new GameEvent
            {
                type            = GameEventType.RareIngredients,
                displayName     = "Ingredientes Raros",
                description     = "A pizza especial vale 5x o preço normal!",
                durationSeconds = 3600f,
                iconColor       = "#7B68EE",
                challengeTarget = 0
            },
            new GameEvent
            {
                type            = GameEventType.CoinRain,
                displayName     = "Chuva de Coins!",
                description     = "20% de chance de ganhar 2x–5x coins a cada venda!",
                durationSeconds = 900f,
                iconColor       = "#FFD700",
                challengeTarget = 0
            },
            new GameEvent
            {
                type            = GameEventType.LightningChallenge,
                displayName     = "Desafio Relâmpago",
                description     = "Venda {0} pizzas em 10 minutos e ganhe uma recompensa!",
                durationSeconds = 600f,
                iconColor       = "#00BFFF",
                challengeTarget = 20,
                challengeCoins  = 500,
                challengeXP     = 200
            }
        };

        public static GameEvent GetEventData(GameEventType type)
        {
            foreach (var e in _allEvents)
                if (e.type == type) return e;
            return null;
        }

        public static IReadOnlyList<GameEvent> AllEvents => _allEvents;

        // ── Iniciar / Parar ───────────────────────────────────────────────────

        public void StartEvent(GameEventType type)
        {
            if (IsEventActive) EndEvent();

            GameEvent data = GetEventData(type);
            if (data == null) return;

            ActiveEvent        = data;
            _timeRemaining     = data.durationSeconds;
            _challengeCompleted = false;
            ChallengeCount     = 0;

            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _timerCoroutine = StartCoroutine(TickTimer());

            OnEventStarted?.Invoke(ActiveEvent);
            Debug.Log($"[EventManager] Evento iniciado: {data.displayName} ({data.durationSeconds}s)");
        }

        public void EndEvent()
        {
            if (ActiveEvent == null) return;
            var ended = ActiveEvent;
            ActiveEvent    = null;
            _timeRemaining = 0f;
            if (_timerCoroutine != null) { StopCoroutine(_timerCoroutine); _timerCoroutine = null; }
            OnEventEnded?.Invoke(ended);
            Debug.Log($"[EventManager] Evento encerrado: {ended.displayName}");
        }

        private IEnumerator TickTimer()
        {
            while (_timeRemaining > 0f)
            {
                yield return null;
                _timeRemaining -= Time.deltaTime;
            }
            EndEvent();
        }

        // ── Modificadores de jogo ─────────────────────────────────────────────

        // Retorna multiplicador de velocidade dos clientes (spawn rate)
        public float CustomerSpawnMultiplier =>
            IsEventActive && ActiveEvent.type == GameEventType.RushHour ? 3f : 1f;

        // Retorna multiplicador de valor da pizza
        public float PizzaValueMultiplier()
        {
            if (!IsEventActive) return 1f;
            return ActiveEvent.type switch
            {
                GameEventType.RushHour          => 1.5f,
                GameEventType.RareIngredients   => 5f,
                _                               => 1f
            };
        }

        // Coin Rain — chama ao vender pizza; retorna multiplicador final
        public float RollCoinMultiplier()
        {
            if (!IsEventActive || ActiveEvent.type != GameEventType.CoinRain) return 1f;
            if (UnityEngine.Random.value <= 0.20f)
                return UnityEngine.Random.Range(2f, 5.01f);
            return 1f;
        }

        // ── Desafio Relâmpago ─────────────────────────────────────────────────

        public void RegisterPizzaSoldForChallenge()
        {
            if (!IsEventActive || ActiveEvent.type != GameEventType.LightningChallenge) return;
            if (_challengeCompleted) return;

            ChallengeCount++;
            OnChallengeProgress?.Invoke(ActiveEvent, ChallengeCount);

            if (ChallengeCount >= ActiveEvent.challengeTarget)
            {
                _challengeCompleted = true;
                OnChallengeCompleted?.Invoke(ActiveEvent);
                AwardChallengeReward(ActiveEvent);
                Debug.Log($"[EventManager] Desafio concluído! Recompensa: {ActiveEvent.challengeCoins} coins");
            }
        }

        private void AwardChallengeReward(GameEvent ev)
        {
            Managers.MoneyManager.Instance?.AddMoney(ev.challengeCoins);
            Seasons.SeasonManager.Instance?.AddXP(ev.challengeXP);
        }

        // ── Formatação de tempo restante ──────────────────────────────────────

        public string FormattedTimeRemaining()
        {
            if (!IsEventActive) return "00:00";
            int total = Mathf.CeilToInt(_timeRemaining);
            int m     = total / 60;
            int s     = total % 60;
            return $"{m:D2}:{s:D2}";
        }
    }
}
