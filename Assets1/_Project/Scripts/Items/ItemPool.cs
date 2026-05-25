using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Utils;

namespace PizzaTycoon.Items
{
    // Pool centralizado para todos os StackableItems do jogo
    // Evita instanciação/destruição constante de objetos durante gameplay
    [DefaultExecutionOrder(-50)]
    public class ItemPool : Singleton<ItemPool>
    {
        [System.Serializable]
        private class ItemPrefabEntry
        {
            public ItemType type;
            public StackableItem prefab;
            public int initialPoolSize = 20;
        }

        [SerializeField] private List<ItemPrefabEntry> _itemPrefabs = new List<ItemPrefabEntry>();

        private readonly Dictionary<ItemType, Queue<StackableItem>> _pools =
            new Dictionary<ItemType, Queue<StackableItem>>();

        private Transform _poolParent;

        protected override void Awake()
        {
            base.Awake();
            _poolParent = new GameObject("[ItemPool]").transform;
            _poolParent.SetParent(transform);
            InitializePools();
        }

        private void InitializePools()
        {
            foreach (var entry in _itemPrefabs)
            {
                if (entry.prefab == null)
                {
                    Debug.LogWarning($"[ItemPool] Prefab nulo para tipo {entry.type}");
                    continue;
                }

                Queue<StackableItem> queue = new Queue<StackableItem>();
                for (int i = 0; i < entry.initialPoolSize; i++)
                {
                    StackableItem item = Instantiate(entry.prefab, _poolParent);
                    item.gameObject.SetActive(false);
                    queue.Enqueue(item);
                }
                _pools[entry.type] = queue;
            }
        }

        public StackableItem Get(ItemType type, Vector3 position = default)
        {
            if (!_pools.ContainsKey(type))
            {
                Debug.LogError($"[ItemPool] Tipo {type} não registrado no pool!");
                return null;
            }

            StackableItem item;
            if (_pools[type].Count > 0)
            {
                item = _pools[type].Dequeue();
            }
            else
            {
                // Crescimento dinâmico do pool
                var entry = _itemPrefabs.Find(e => e.type == type);
                if (entry == null || entry.prefab == null) return null;
                item = Instantiate(entry.prefab, _poolParent);
            }

            item.transform.position = position;
            item.Initialize(type);
            item.gameObject.SetActive(true);
            return item;
        }

        public void Return(StackableItem item)
        {
            if (item == null) return;
            item.ResetItem();
            item.transform.SetParent(_poolParent);
            item.gameObject.SetActive(false);

            if (_pools.ContainsKey(item.Type))
                _pools[item.Type].Enqueue(item);
            else
                Destroy(item.gameObject);
        }
    }
}
