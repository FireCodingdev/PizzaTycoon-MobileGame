using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Utils;

namespace PizzaTycoon.Customers
{
    // Spawna clientes em intervalos regulares — suporta múltiplas variantes de prefab
    public class CustomerSpawner : MonoBehaviour
    {
        [Header("Prefab Base (usado se Variantes estiver vazio)")]
        [SerializeField] private Customer _customerPrefab;

        [Header("Variantes de Prefab (spawn aleatório — sobrescreve o campo acima)")]
        [Tooltip("Adicione aqui Customer_A, Customer_B, Customer_C para spawn variado")]
        [SerializeField] private Customer[] _customerPrefabVariants;

        [Header("Configurações de Spawn")]
        [SerializeField] private CustomerQueue _customerQueue;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private float _spawnInterval = 8f;
        [SerializeField] private int _initialPoolSize = 10;

        private ObjectPool<Customer>[] _pools;
        private readonly Dictionary<Customer, int> _customerPoolIndex = new();
        private Coroutine _spawnCoroutine;

        public float SpawnInterval { get => _spawnInterval; set => _spawnInterval = value; }

        private void Awake()
        {
            Customer[] prefabs = (_customerPrefabVariants != null && _customerPrefabVariants.Length > 0)
                ? _customerPrefabVariants
                : (_customerPrefab != null ? new[] { _customerPrefab } : null);

            if (prefabs == null || prefabs.Length == 0)
            {
                Debug.LogError("[CustomerSpawner] Nenhum prefab de cliente definido!");
                return;
            }

            int poolSize = Mathf.Max(2, _initialPoolSize / prefabs.Length);
            _pools = new ObjectPool<Customer>[prefabs.Length];
            for (int i = 0; i < prefabs.Length; i++)
                _pools[i] = new ObjectPool<Customer>(prefabs[i], poolSize, transform);
        }

        private void OnEnable()
        {
            _spawnCoroutine = StartCoroutine(SpawnLoop());
        }

        private void OnDisable()
        {
            if (_spawnCoroutine != null)
                StopCoroutine(_spawnCoroutine);
        }

        private IEnumerator SpawnLoop()
        {
            yield return new WaitForSeconds(2f);
            while (true)
            {
                yield return new WaitForSeconds(_spawnInterval);
                if (!_customerQueue.IsFull)
                    SpawnCustomer();
            }
        }

        private void SpawnCustomer()
        {
            if (_spawnPoint == null || _pools == null || _pools.Length == 0) return;

            int idx = Random.Range(0, _pools.Length);
            Customer customer = _pools[idx].Get(_spawnPoint.position, _spawnPoint.rotation);
            _customerPoolIndex[customer] = idx;
            customer.OnCustomerLeft += OnCustomerReturned;

            if (_customerQueue.TryEnqueue(customer))
            {
                // NAO chama StartWaiting aqui — o Customer.Update() inicia a paciencia
                // automaticamente quando o cliente chega caminhando no slot.
                // Isso evita perder paciencia enquanto ele ainda esta andando.
            }
            else
            {
                customer.OnCustomerLeft -= OnCustomerReturned;
                _customerPoolIndex.Remove(customer);
                _pools[idx].Return(customer);
            }
        }

        private void OnCustomerReturned(Customer customer)
        {
            customer.OnCustomerLeft -= OnCustomerReturned;
            if (_customerPoolIndex.TryGetValue(customer, out int idx))
            {
                _customerPoolIndex.Remove(customer);
                _pools[idx].Return(customer);
            }
        }
    }
}
