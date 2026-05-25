using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Items;

namespace PizzaTycoon.Player
{
    // Gerencia a pilha de itens sobre o jogador com animação de wobble
    public class PlayerStacker : MonoBehaviour
    {
        [Header("Configurações de Pilha")]
        [SerializeField] private Transform _stackAnchor;   // ponto base onde a pilha começa (ex: cabeça)
        [SerializeField] private float _itemSpacingY = 0.3f;
        [SerializeField] private int _maxStackSize = 5;

        [Header("Wobble")]
        [SerializeField] private float _wobbleAmount = 5f;
        [SerializeField] private float _wobbleSpeed = 3f;

        private readonly List<StackableItem> _stack = new List<StackableItem>();
        private bool _isMoving = false;

        public int CurrentCount => _stack.Count;
        public int MaxStackSize { get => _maxStackSize; set => _maxStackSize = value; }
        public bool HasItems => _stack.Count > 0;
        public bool IsFull => _stack.Count >= _maxStackSize;

        private void Update()
        {
            UpdateItemPositions();
        }

        public bool TryAddItem(StackableItem item)
        {
            if (IsFull || item == null) return false;

            item.transform.SetParent(_stackAnchor != null ? _stackAnchor : transform);
            item.transform.localRotation = Quaternion.identity;
            _stack.Add(item);

            // Posiciona o item imediatamente
            SnapItemToPosition(_stack.Count - 1);
            return true;
        }

        public StackableItem RemoveTopItem()
        {
            if (_stack.Count == 0) return null;

            StackableItem topItem = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
            topItem.transform.SetParent(null);
            return topItem;
        }

        public StackableItem PeekTopItem()
        {
            return _stack.Count > 0 ? _stack[_stack.Count - 1] : null;
        }

        public bool ContainsType(ItemType type)
        {
            return _stack.Exists(item => item.Type == type);
        }

        public StackableItem RemoveItemOfType(ItemType type)
        {
            StackableItem found = _stack.FindLast(item => item.Type == type);
            if (found == null) return null;

            _stack.Remove(found);
            found.transform.SetParent(null);

            // Reposiciona os itens restantes
            RefreshAllPositions();
            return found;
        }

        public void SetMoving(bool moving, bool hasItems = false)
        {
            _isMoving = moving;
        }

        private void UpdateItemPositions()
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                if (_stack[i] == null) continue;

                Vector3 targetPos = GetItemLocalPosition(i);

                if (_isMoving && _stack.Count > 0)
                {
                    // Efeito wobble baseado em seno para simular balançar
                    float wobbleX = Mathf.Sin(Time.time * _wobbleSpeed + i * 0.5f) * _wobbleAmount;
                    float wobbleZ = Mathf.Cos(Time.time * _wobbleSpeed + i * 0.5f) * _wobbleAmount * 0.5f;
                    Quaternion wobbleRotation = Quaternion.Euler(wobbleX, 0f, wobbleZ);
                    _stack[i].transform.localRotation = Quaternion.Lerp(
                        _stack[i].transform.localRotation, wobbleRotation, Time.deltaTime * 5f);
                }
                else
                {
                    _stack[i].transform.localRotation = Quaternion.Lerp(
                        _stack[i].transform.localRotation, Quaternion.identity, Time.deltaTime * 5f);
                }

                // Suaviza movimento para posição alvo
                _stack[i].transform.localPosition = Vector3.Lerp(
                    _stack[i].transform.localPosition, targetPos, Time.deltaTime * 10f);
            }
        }

        private void SnapItemToPosition(int index)
        {
            if (index < 0 || index >= _stack.Count) return;
            _stack[index].transform.localPosition = GetItemLocalPosition(index);
        }

        private void RefreshAllPositions()
        {
            for (int i = 0; i < _stack.Count; i++)
                SnapItemToPosition(i);
        }

        private Vector3 GetItemLocalPosition(int index)
        {
            return new Vector3(0f, index * _itemSpacingY, 0f);
        }

        public void ClearStack()
        {
            foreach (var item in _stack)
                if (item != null)
                    ItemPool.Instance.Return(item);

            _stack.Clear();
        }
    }
}
