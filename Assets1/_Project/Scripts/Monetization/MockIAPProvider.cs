using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PizzaTycoon.Monetization
{
    // Provider mock de IAP para testes sem SDK real
    public class MockIAPProvider : IIAPProvider
    {
        public bool IsInitialized { get; private set; }

        private readonly HashSet<string>  _ownedProducts = new();
        private MonoBehaviour _coroutineRunner;

        public MockIAPProvider(MonoBehaviour runner)
        {
            _coroutineRunner = runner;
        }

        public void Initialize(string[] productIds, Action onSuccess, Action<string> onFail)
        {
            IsInitialized = true;
            Debug.Log($"[MockIAP] Inicializado com {productIds.Length} produtos.");
            onSuccess?.Invoke();
        }

        public void Purchase(string productId, Action<bool> onResult)
        {
            Debug.Log($"[MockIAP] Comprando: {productId}");
            _coroutineRunner.StartCoroutine(SimulatePurchase(productId, onResult));
        }

        public bool IsProductOwned(string productId) => _ownedProducts.Contains(productId);

        private IEnumerator SimulatePurchase(string productId, Action<bool> onResult)
        {
            yield return new WaitForSecondsRealtime(1f);

            bool success = UnityEngine.Random.value < 0.95f; // 95% sucesso no mock
            if (success)
                _ownedProducts.Add(productId);

            Debug.Log($"[MockIAP] Compra '{productId}': {(success ? "SUCESSO" : "FALHA")}");
            onResult?.Invoke(success);
        }
    }
}
