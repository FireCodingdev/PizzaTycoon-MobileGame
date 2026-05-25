using System;
using UnityEngine;
using PizzaTycoon.Utils;

namespace PizzaTycoon.Events
{
    // Determina qual evento está ativo com base em DateTime.Now
    // Schedule fixo por hora do dia (horário local)
    public class EventScheduler : Singleton<EventScheduler>
    {
        [Header("Configuração")]
        [SerializeField] private bool _autoSchedule = true;

        // Horários de início de cada evento (hora local)
        //   Rush Hour    : 12h, 18h, 21h  (3x/dia, 30 min)
        //   Coin Rain    : 10h, 15h        (2x/dia, 15 min)
        //   RareIngred.  : 08h             (1x/dia, 1h)
        //   Lightning    : 14h, 20h        (2x/dia, 10 min)
        private static readonly (GameEventType type, int[] hours)[] _schedule =
        {
            (GameEventType.RushHour,          new[] { 12, 18, 21 }),
            (GameEventType.CoinRain,          new[] { 10, 15 }),
            (GameEventType.RareIngredients,   new[] { 8  }),
            (GameEventType.LightningChallenge,new[] { 14, 20 }),
        };

        private int _lastCheckedMinute = -1;

        private void Update()
        {
            if (!_autoSchedule) return;

            int minute = DateTime.Now.Minute;
            if (minute == _lastCheckedMinute) return;
            _lastCheckedMinute = minute;

            CheckSchedule();
        }

        private void CheckSchedule()
        {
            int hour = DateTime.Now.Hour;
            int min  = DateTime.Now.Minute;
            if (min != 0) return; // dispara apenas no início da hora

            foreach (var (type, hours) in _schedule)
            {
                foreach (int h in hours)
                {
                    if (h == hour)
                    {
                        TriggerEvent(type);
                        return;
                    }
                }
            }
        }

        private void TriggerEvent(GameEventType type)
        {
            var mgr = EventManager.Instance;
            if (mgr == null) return;

            // Não substitui evento ativo por outro do mesmo tipo
            if (mgr.IsEventActive && mgr.ActiveEvent?.type == type) return;

            mgr.StartEvent(type);
        }

        // ── API manual (debug / editor / tutorial) ────────────────────────────

        public void ForceStartEvent(GameEventType type) => EventManager.Instance?.StartEvent(type);
        public void ForceEndEvent()                     => EventManager.Instance?.EndEvent();

        // Retorna o próximo evento do schedule e o tempo em segundos até ele
        public (GameEventType type, float secondsUntil) GetNextScheduledEvent()
        {
            DateTime now  = DateTime.Now;
            float    best = float.MaxValue;
            GameEventType bestType = GameEventType.RushHour;

            foreach (var (type, hours) in _schedule)
            {
                foreach (int h in hours)
                {
                    DateTime candidate = new DateTime(now.Year, now.Month, now.Day, h, 0, 0);
                    if (candidate <= now) candidate = candidate.AddDays(1);
                    float secs = (float)(candidate - now).TotalSeconds;
                    if (secs < best) { best = secs; bestType = type; }
                }
            }

            return (bestType, best);
        }
    }
}
