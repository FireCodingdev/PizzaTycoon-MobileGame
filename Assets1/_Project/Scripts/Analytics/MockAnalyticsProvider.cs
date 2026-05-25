using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PizzaTycoon.Analytics
{
    // Provider mock — imprime eventos no console com prefixo [ANALYTICS]
    public class MockAnalyticsProvider : IAnalyticsProvider
    {
        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            IsInitialized = true;
            Debug.Log("[ANALYTICS] MockAnalyticsProvider inicializado.");
        }

        public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!IsInitialized) return;

            var sb = new StringBuilder($"[ANALYTICS] {eventName}");
            if (parameters != null && parameters.Count > 0)
            {
                sb.Append(" {");
                foreach (var kv in parameters)
                    sb.Append($" {kv.Key}={kv.Value},");
                sb.Append(" }");
            }
            Debug.Log(sb.ToString());
        }

        public void LogPurchase(string productId, decimal price, string currency)
        {
            Debug.Log($"[ANALYTICS] purchase: product={productId} price={price} {currency}");
        }

        public void SetUserProperty(string name, string value)
        {
            Debug.Log($"[ANALYTICS] user_property: {name}={value}");
        }
    }
}
