using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Items;
using PizzaTycoon.Player;

namespace PizzaTycoon.Stations
{
    // Classe base abstrata para todas as estações de trabalho do jogo
    // Subclasses implementam a lógica específica de cada estação
    public abstract class BaseStation : MonoBehaviour
    {
        [Header("Configurações Base")]
        [SerializeField] protected string _stationId;
        [SerializeField] protected float _productionInterval = 2f;
        [SerializeField] protected int _maxItemsInStation = 5;

        [Header("Visual")]
        [SerializeField] protected Transform _itemStackAnchor; // onde os itens aparecem visualmente
        [SerializeField] protected GameObject _productionIndicator; // indicador de progresso (ex: barra)

        protected readonly List<StackableItem> _itemsInStation = new List<StackableItem>();
        protected PlayerController _playerInRange;
        protected Coroutine _productionCoroutine;
        protected bool _isActive = true;

        public bool IsActive { get => _isActive; set => _isActive = value; }
        public float ProductionInterval { get => _productionInterval; set => _productionInterval = value; }
        public bool IsFull => _itemsInStation.Count >= _maxItemsInStation;

        protected virtual void Awake()
        {
            EnsureTriggerCollider();
        }

        // Garante que a estacao possui um Collider configurado como trigger
        // para detectar a entrada do jogador. Cria BoxCollider padrao se ausente.
        private void EnsureTriggerCollider()
        {
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(3f, 2f, 3f);
                box.center = new Vector3(0f, 1f, 0f);
                box.isTrigger = true;
                Debug.Log($"[BaseStation] BoxCollider trigger adicionado a {gameObject.name}");
            }
            else if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.Log($"[BaseStation] Collider de {gameObject.name} convertido para trigger.");
            }
        }

        protected virtual void OnEnable()
        {
            if (_productionIndicator != null)
                _productionIndicator.SetActive(false);
        }

        // Chamado pelo PlayerController ao entrar no trigger da estação
        public virtual void OnPlayerEnter(PlayerController player)
        {
            _playerInRange = player;
            StartInteraction();
        }

        // Chamado pelo PlayerController ao sair do trigger
        public virtual void OnPlayerExit(PlayerController player)
        {
            _playerInRange = null;
            StopInteraction();
        }

        protected virtual void StartInteraction()
        {
            if (_productionCoroutine != null)
                StopCoroutine(_productionCoroutine);
            _productionCoroutine = StartCoroutine(ProductionLoop());
        }

        protected virtual void StopInteraction()
        {
            if (_productionCoroutine != null)
            {
                StopCoroutine(_productionCoroutine);
                _productionCoroutine = null;
            }

            if (_productionIndicator != null)
                _productionIndicator.SetActive(false);
        }

        // Loop de produção que executa enquanto o jogador está na área
        protected virtual IEnumerator ProductionLoop()
        {
            if (_productionIndicator != null)
                _productionIndicator.SetActive(true);

            while (_playerInRange != null && _isActive)
            {
                yield return new WaitForSeconds(_productionInterval);

                if (_playerInRange != null)
                    ProcessItem(_playerInRange);
            }

            if (_productionIndicator != null)
                _productionIndicator.SetActive(false);
        }

        // Lógica principal da estação — implementada por cada subclasse
        protected abstract void ProcessItem(PlayerController player);

        // Posiciona item visualmente na pilha da estação
        protected void AddItemVisual(StackableItem item)
        {
            if (_itemStackAnchor == null) return;
            item.transform.SetParent(_itemStackAnchor);
            item.transform.localPosition = new Vector3(0f, _itemsInStation.Count * 0.25f, 0f);
            item.transform.localRotation = Quaternion.identity;
        }

        protected void ClearItemVisuals()
        {
            foreach (var item in _itemsInStation)
                if (item != null)
                    ItemPool.Instance.Return(item);
            _itemsInStation.Clear();
        }
    }
}
