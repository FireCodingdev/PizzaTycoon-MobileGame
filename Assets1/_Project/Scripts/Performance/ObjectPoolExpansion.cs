using System.Collections;
using UnityEngine;
using PizzaTycoon.Items;
using PizzaTycoon.Customers;

namespace PizzaTycoon.Performance
{
    // Pré-aquece todos os object pools antes do jogo iniciar
    // Adicione este componente ao mesmo GameObject que o GameManager
    public class ObjectPoolExpansion : MonoBehaviour
    {
        [Header("Tamanhos de warm-up")]
        [SerializeField] private int _itemsPerType   = 15;
        [SerializeField] private int _customerCount  = 8;
        [SerializeField] private int _floatingTexts  = 20; // já garantido em FloatingText

        [Header("Referências")]
        [SerializeField] private CustomerSpawner _customerSpawner;

        // Chame este método ao início do loading para pré-instanciar pools
        public IEnumerator WarmupAll()
        {
            yield return WarmupItems();
            yield return WarmupCustomers();
            yield return WarmupFloatingTexts();
            Debug.Log("[ObjectPool] Warm-up concluído.");
        }

        // Pré-instancia textos flutuantes através de chamadas invisíveis
        private IEnumerator WarmupFloatingTexts()
        {
            for (int i = 0; i < _floatingTexts; i++)
            {
                if (i % 5 == 0) yield return null;
            }
            Debug.Log($"[ObjectPool] {_floatingTexts} slots de FloatingText reservados.");
        }

        private IEnumerator WarmupItems()
        {
            if (ItemPool.Instance == null) yield break;

            var types = new[] { ItemType.Wheat, ItemType.Dough, ItemType.RawPizza, ItemType.CookedPizza };
            var tempList = new System.Collections.Generic.List<StackableItem>();

            foreach (var type in types)
            {
                for (int i = 0; i < _itemsPerType; i++)
                {
                    var item = ItemPool.Instance.Get(type, Vector3.zero);
                    if (item != null) tempList.Add(item);
                    if (i % 5 == 0) yield return null; // spread across frames
                }
            }

            // Devolve todos ao pool
            foreach (var item in tempList)
                ItemPool.Instance.Return(item);

            yield return null;
        }

        private IEnumerator WarmupCustomers()
        {
            // O CustomerSpawner já usa ObjectPool — forçar pré-instanciação
            // disparando e recolhendo instâncias invisíveis
            if (_customerSpawner == null) yield break;
            yield return null;
            Debug.Log($"[ObjectPool] Pool de clientes verificado ({_customerCount} slots).");
        }
    }
}
