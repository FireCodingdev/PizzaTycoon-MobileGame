using System.Collections.Generic;

namespace PizzaTycoon.Analytics
{
    // Interface para qualquer SDK de analytics (Firebase, GameAnalytics, Amplitude, etc.)
    public interface IAnalyticsProvider
    {
        void Initialize();
        void LogEvent(string eventName, Dictionary<string, object> parameters = null);
        void LogPurchase(string productId, decimal price, string currency);
        void SetUserProperty(string name, string value);
        bool IsInitialized { get; }
    }
}
