using System;
using System.Collections.Generic;

namespace PizzaTycoon.Monetization
{
    // Interface para qualquer SDK de IAP (Unity IAP, Google Play Billing, etc.)
    public interface IIAPProvider
    {
        void Initialize(string[] productIds, Action onSuccess, Action<string> onFail);
        void Purchase(string productId, Action<bool> onResult);
        bool IsProductOwned(string productId);
        bool IsInitialized { get; }
    }
}
