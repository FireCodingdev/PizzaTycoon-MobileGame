using System.Reflection;
using UnityEngine;
using PizzaTycoon.Customers;
using PizzaTycoon.Stations;

namespace PizzaTycoon.GameSystems
{
    // Auto-conecta CustomerQueue, CustomerSpawner e DeliveryStation sem precisar
    // configurar nada no Inspector. Usa reflection para preencher campos privados.
    [DefaultExecutionOrder(20)]
    public class CustomerSceneAutoWirer : MonoBehaviour
    {
        private void Start()
        {
            // Auto-wire DESABILITADO. Refs de CustomerQueue/SpawnPoint/QueueStart
            // devem ser conectadas manualmente no Inspector.
        }

        private void AutoWire()
        {
            var delivery = FindObjectOfType<DeliveryStation>();
            if (delivery == null)
            {
                Debug.LogWarning("[AutoWire] DeliveryStation nao encontrada na cena.");
                return;
            }

            // Garante que existe uma CustomerQueue
            var queue = FindObjectOfType<CustomerQueue>();
            if (queue == null)
                queue = new GameObject("[CustomerQueue]").AddComponent<CustomerQueue>();

            // Cria queueStart (ponto de inicio da fila) na posicao da DeliveryStation
            if (RF<Transform>(queue, "_queueStart") == null)
            {
                var qsGO = new GameObject("[QueueStart]");
                Transform dp = delivery.DeliveryPoint;
                qsGO.transform.position = dp.position + dp.forward * 1.2f;
                qsGO.transform.rotation = dp.rotation;
                WF(queue, "_queueStart", qsGO.transform);
                Debug.Log("[AutoWire] QueueStart criado em " + qsGO.transform.position);
            }

            // Conecta DeliveryStation → CustomerQueue
            if (RF<CustomerQueue>(delivery, "_customerQueue") == null)
            {
                WF(delivery, "_customerQueue", queue);
                Debug.Log("[AutoWire] DeliveryStation conectada à CustomerQueue.");
            }

            // Conecta CustomerSpawner
            var spawner = FindObjectOfType<CustomerSpawner>();
            if (spawner != null)
            {
                if (RF<CustomerQueue>(spawner, "_customerQueue") == null)
                    WF(spawner, "_customerQueue", queue);

                // Cria spawnPoint se ausente (clientes vêm da frente da fila)
                if (RF<Transform>(spawner, "_spawnPoint") == null)
                {
                    var spGO = new GameObject("[CustomerSpawnPoint]");
                    Transform dp = delivery.DeliveryPoint;
                    spGO.transform.position = dp.position + dp.forward * 9f;
                    spGO.transform.rotation = Quaternion.LookRotation(-dp.forward);
                    WF(spawner, "_spawnPoint", spGO.transform);
                    Debug.Log("[AutoWire] SpawnPoint criado em " + spGO.transform.position);
                }
            }

            Debug.Log("[AutoWire] Clientes auto-conectados com sucesso.");
        }

        // ── Reflection helpers ─────────────────────────────────────────────────
        private static T RF<T>(object obj, string fieldName) where T : class
        {
            FieldInfo f = obj.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return f?.GetValue(obj) as T;
        }

        private static void WF(object obj, string fieldName, object value)
        {
            FieldInfo f = obj.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            f?.SetValue(obj, value);
        }
    }
}
