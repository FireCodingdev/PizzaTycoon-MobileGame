using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Stations;

namespace PizzaTycoon.Customers
{
    // Gerencia a fila de clientes na frente do balcão de entrega
    public class CustomerQueue : MonoBehaviour
    {
        [Header("Configurações de Fila")]
        [SerializeField] private int _maxQueueSize = 5;
        [SerializeField] private float _spacingBetweenCustomers = 1.2f;
        [SerializeField] private Transform _queueStart; // primeiro da fila (frente do balcão)

        private readonly List<Customer> _queue = new List<Customer>();

        public int Count => _queue.Count;
        public bool IsFull => _queue.Count >= _maxQueueSize;
        public bool HasCustomers => _queue.Count > 0;

        // Adiciona cliente ao final da fila
        public bool TryEnqueue(Customer customer)
        {
            if (IsFull || customer == null) return false;

            _queue.Add(customer);
            customer.OnCustomerLeft += OnCustomerLeft;
            RepositionQueue();
            return true;
        }

        // Retorna o próximo cliente aguardando (primeiro da fila)
        public Customer GetNextWaitingCustomer()
        {
            if (_queue.Count == 0) return null;
            return _queue[0];
        }

        private void OnCustomerLeft(Customer customer)
        {
            customer.OnCustomerLeft -= OnCustomerLeft;
            _queue.Remove(customer);
            RepositionQueue();
        }

        // Recalcula posições de todos na fila após mudança.
        // Em vez de teletransportar, chama Customer.MoveTo() — cliente caminha
        // gradualmente até o slot, com animação de walk.
        private void RepositionQueue()
        {
            if (_queueStart == null) return;

            for (int i = 0; i < _queue.Count; i++)
            {
                if (_queue[i] == null) continue;

                Vector3 targetPos = _queueStart.position - _queueStart.forward * (i * _spacingBetweenCustomers);
                _queue[i].MoveTo(targetPos, _queueStart.rotation);
            }
        }

#if UNITY_EDITOR
        // Visualizacao no Scene View: verde = primeiro slot (frente do balcao),
        // amarelo = slots seguintes, seta azul = direcao para a qual o cliente olha.
        // Se _queueStart estiver null, desenha aviso vermelho na posicao do objeto.
        private void OnDrawGizmos()
        {
            if (_queueStart == null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.5f);
                UnityEditor.Handles.color = Color.red;
                UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f,
                    "CustomerQueue: _queueStart NAO atribuido!");
                return;
            }

            Vector3 start  = _queueStart.position;
            Vector3 back   = -_queueStart.forward;
            Quaternion rot = _queueStart.rotation;

            for (int i = 0; i < _maxQueueSize; i++)
            {
                Vector3 slot = start + back * (i * _spacingBetweenCustomers);
                Gizmos.color = (i == 0) ? new Color(0.2f, 0.95f, 0.3f)
                                        : new Color(0.95f, 0.85f, 0.15f);
                Gizmos.DrawSphere(slot + Vector3.up * 0.5f, 0.3f);

                UnityEditor.Handles.color = Gizmos.color;
                UnityEditor.Handles.Label(slot + Vector3.up * 1.6f, $"Slot {i}");

                // Seta azul mostrando para onde o cliente olha (forward do queueStart).
                Gizmos.color = new Color(0.3f, 0.55f, 0.95f, 0.8f);
                Gizmos.DrawRay(slot + Vector3.up * 0.5f, rot * Vector3.forward * 1.2f);
            }

            // Linha conectando todos os slots.
            Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            for (int i = 0; i < _maxQueueSize - 1; i++)
            {
                Vector3 a = start + back * (i * _spacingBetweenCustomers) + Vector3.up * 0.5f;
                Vector3 b = start + back * ((i + 1) * _spacingBetweenCustomers) + Vector3.up * 0.5f;
                Gizmos.DrawLine(a, b);
            }
        }
#endif
    }
}
