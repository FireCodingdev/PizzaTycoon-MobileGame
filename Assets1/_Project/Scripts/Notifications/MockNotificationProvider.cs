using System;
using UnityEngine;

namespace PizzaTycoon.Notifications
{
    // Provider de notificações para Editor/testes — apenas loga no console
    public class MockNotificationProvider : INotificationProvider
    {
        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            IsInitialized = true;
            Debug.Log("[MockNotification] Inicializado.");
        }

        public void RequestPermission(Action<bool> onResult)
        {
            Debug.Log("[MockNotification] Permissão concedida (mock).");
            onResult?.Invoke(true);
        }

        public void Schedule(string id, string title, string body, int delaySeconds)
        {
            Debug.Log($"[MockNotification] Agendada [{id}]: \"{title}\" em {delaySeconds}s — {body}");
        }

        public void Cancel(string id)
        {
            Debug.Log($"[MockNotification] Cancelada [{id}]");
        }

        public void CancelAll()
        {
            Debug.Log("[MockNotification] Todas as notificações canceladas.");
        }
    }
}
