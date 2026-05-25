using System;
using UnityEngine;
using PizzaTycoon.Utils;
using PizzaTycoon.Managers;

namespace PizzaTycoon.Notifications
{
    // Agendamento de 4 notificações locais ao sair do jogo
    public class NotificationManager : Singleton<NotificationManager>
    {
        // IDs fixos
        private const string ID_OFFLINE_BONUS  = "offline_bonus";
        private const string ID_DAILY_GOAL     = "daily_goal";
        private const string ID_SEASON_EXPIRY  = "season_expiry";
        private const string ID_COMEBACK       = "comeback";

        private INotificationProvider _provider;

        protected override void Awake()
        {
            base.Awake();
            _provider = new MockNotificationProvider();
            _provider.Initialize();
        }

        public void SetProvider(INotificationProvider provider)
        {
            _provider = provider;
            _provider.Initialize();
        }

        // Chamado por SaveManager ou ApplicationManager ao pausar/fechar o app
        public void ScheduleAll()
        {
            if (_provider == null || !_provider.IsInitialized) return;
            if (SaveManager.Instance?.CurrentData?.notificationsEnabled == false) return;

            _provider.CancelAll();

            // 1. Bônus offline disponível (4h)
            _provider.Schedule(ID_OFFLINE_BONUS,
                "Sua pizzaria está te esperando!",
                "Você tem bônus offline acumulado. Volte para coletar!",
                14_400);

            // 2. Meta diária disponível (24h se ainda não completada)
            _provider.Schedule(ID_DAILY_GOAL,
                "Nova meta diária desbloqueada!",
                "Complete as metas de hoje e ganhe XP extra no Season Pass!",
                86_400);

            // 3. Season Pass encerrando (avisa 3 dias antes)
            TimeSpan remaining = Seasons.SeasonManager.Instance?.TimeRemaining() ?? TimeSpan.Zero;
            if (remaining.TotalDays is > 0 and <= 3)
            {
                _provider.Schedule(ID_SEASON_EXPIRY,
                    "Season Pass encerrando!",
                    $"Faltam {remaining.Days}d {remaining.Hours}h para o fim da temporada. Resgate suas recompensas!",
                    3_600);
            }

            // 4. Comeback (2 dias sem jogar)
            _provider.Schedule(ID_COMEBACK,
                "Sua pizzaria precisa de você!",
                "Os clientes estão com saudade das suas pizzas. Volte e faça sucesso!",
                172_800);

            Debug.Log("[NotificationManager] Notificações agendadas.");
        }

        public void CancelAll() => _provider?.CancelAll();

        public void SetNotificationsEnabled(bool enabled)
        {
            var data = SaveManager.Instance?.CurrentData;
            if (data != null) data.notificationsEnabled = enabled;

            if (!enabled) CancelAll();
            else ScheduleAll();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) ScheduleAll();
            else _provider?.CancelAll();
        }
    }
}
