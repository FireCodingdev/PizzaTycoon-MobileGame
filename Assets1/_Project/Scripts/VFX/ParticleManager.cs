using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Utils;

namespace PizzaTycoon.VFX
{
    // Singleton que gerencia todos os efeitos de partículas do jogo via pools.
    // Cria instâncias VFX em runtime sem necessidade de prefabs.
    public class ParticleManager : Singleton<ParticleManager>
    {
        [Header("Tamanho dos Pools")]
        [SerializeField] private int _collectPoolSize   = 8;
        [SerializeField] private int _moneyPoolSize     = 6;
        [SerializeField] private int _cookPoolSize      = 4;
        [SerializeField] private int _pizzaReadyPoolSize= 5;
        [SerializeField] private int _happyPoolSize     = 6;
        [SerializeField] private int _angryPoolSize     = 4;

        private Pool<VFX_Collect>       _collectPool;
        private Pool<VFX_Money>         _moneyPool;
        private Pool<VFX_Cook>          _cookPool;
        private Pool<VFX_PizzaReady>    _pizzaReadyPool;
        private Pool<VFX_CustomerHappy> _happyPool;
        private Pool<VFX_CustomerAngry> _angryPool;

        private Transform _poolParent;

        protected override void Awake()
        {
            base.Awake();
            _poolParent = new GameObject("[VFX_Pool]").transform;
            _poolParent.SetParent(transform);

            _collectPool    = new Pool<VFX_Collect>   (_collectPoolSize,   "VFX_Collect",    _poolParent);
            _moneyPool      = new Pool<VFX_Money>     (_moneyPoolSize,     "VFX_Money",      _poolParent);
            _cookPool       = new Pool<VFX_Cook>      (_cookPoolSize,      "VFX_Cook",       _poolParent);
            _pizzaReadyPool = new Pool<VFX_PizzaReady>(_pizzaReadyPoolSize,"VFX_PizzaReady", _poolParent);
            _happyPool      = new Pool<VFX_CustomerHappy>(_happyPoolSize,  "VFX_Happy",      _poolParent);
            _angryPool      = new Pool<VFX_CustomerAngry>(_angryPoolSize,  "VFX_Angry",      _poolParent);
        }

        // ── API pública ──────────────────────────────────────────────────────

        // Efeito de coleta de item (trigo, massa, pizza)
        public void PlayCollect(Vector3 worldPos)
            => _collectPool.Get()?.Play(worldPos);

        // Efeito ao receber dinheiro
        public void PlayMoney(Vector3 worldPos)
            => _moneyPool.Get()?.Play(worldPos);

        // Inicia fumaça do forno (loop). Retorna o VFX para poder parar depois.
        public VFX_Cook PlayCookLoop(Vector3 worldPos)
        {
            VFX_Cook vfx = _cookPool.Get();
            vfx?.PlayLoop(worldPos);
            return vfx;
        }

        // Para a fumaça do forno
        public void StopCook(VFX_Cook vfx) => vfx?.StopLoop();

        // Burst de estrelinhas ao pizza sair do forno
        public void PlayPizzaReady(Vector3 worldPos)
            => _pizzaReadyPool.Get()?.Play(worldPos);

        // Corações ao cliente ser atendido
        public void PlayCustomerHappy(Vector3 worldPos)
            => _happyPool.Get()?.Play(worldPos);

        // Partículas de raiva ao cliente sair insatisfeito
        public void PlayCustomerAngry(Vector3 worldPos)
            => _angryPool.Get()?.Play(worldPos);

        // ── Pool interno genérico ────────────────────────────────────────────
        private class Pool<T> where T : BaseVFX
        {
            private readonly Queue<T> _available = new Queue<T>();
            private readonly Transform _parent;

            public Pool(int size, string name, Transform parent)
            {
                _parent = parent;
                for (int i = 0; i < size; i++)
                {
                    T vfx = CreateInstance(name);
                    vfx.gameObject.SetActive(false);
                    _available.Enqueue(vfx);
                }
            }

            public T Get()
            {
                // Verifica se algum inativo pode ser reutilizado
                foreach (T v in _available)
                    if (v.IsAvailable)
                        return v;

                // Cria novo se todos estiverem em uso
                return CreateInstance(typeof(T).Name);
            }

            private T CreateInstance(string name)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(_parent);
                go.AddComponent<ParticleSystem>();
                T vfx = go.AddComponent<T>();
                return vfx;
            }
        }
    }
}
