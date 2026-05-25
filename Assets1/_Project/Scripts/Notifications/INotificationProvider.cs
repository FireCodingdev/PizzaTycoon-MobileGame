using System;

namespace PizzaTycoon.Notifications
{
    public interface INotificationProvider
    {
        void Initialize();
        void RequestPermission(Action<bool> onResult);
        void Schedule(string id, string title, string body, int delaySeconds);
        void Cancel(string id);
        void CancelAll();
        bool IsInitialized { get; }
    }
}
