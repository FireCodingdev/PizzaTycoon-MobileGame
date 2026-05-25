using System.Collections.Generic;
using UnityEngine;

namespace PizzaTycoon.Utils
{
    // Pool de objetos genérico — evita Instantiate/Destroy constante em runtime
    public class ObjectPool<T> where T : MonoBehaviour
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _available = new Queue<T>();

        public int CountInactive => _available.Count;

        public ObjectPool(T prefab, int initialSize, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;

            for (int i = 0; i < initialSize; i++)
            {
                T obj = CreateNew();
                obj.gameObject.SetActive(false);
                _available.Enqueue(obj);
            }
        }

        // Retira um objeto do pool (ou cria novo se estiver vazio)
        public T Get(Vector3 position = default, Quaternion rotation = default)
        {
            T obj = _available.Count > 0 ? _available.Dequeue() : CreateNew();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.gameObject.SetActive(true);
            return obj;
        }

        // Devolve objeto ao pool
        public void Return(T obj)
        {
            if (obj == null) return;
            obj.gameObject.SetActive(false);
            _available.Enqueue(obj);
        }

        private T CreateNew()
        {
            T obj = Object.Instantiate(_prefab, _parent);
            return obj;
        }
    }
}
