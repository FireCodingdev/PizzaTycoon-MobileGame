using UnityEngine;

namespace PizzaTycoon.Items
{
    // Representa um item físico que pode ser empilhado pelo jogador ou nas estações
    public class StackableItem : MonoBehaviour
    {
        [SerializeField] private ItemType _type = ItemType.None;
        [SerializeField] private MeshRenderer _meshRenderer;

        public ItemType Type => _type;

        // Inicializa o item ao sair do pool
        public void Initialize(ItemType type)
        {
            _type = type;
        }

        // Reseta estado ao retornar ao pool
        public void ResetItem()
        {
            transform.SetParent(null);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        // Move suavemente até uma posição alvo (usado ao empilhar)
        public void AnimateTo(Vector3 worldTarget, float duration = 0.15f)
        {
            StopAllCoroutines();
            StartCoroutine(MoveRoutine(worldTarget, duration));
        }

        private System.Collections.IEnumerator MoveRoutine(Vector3 target, float duration)
        {
            Vector3 start = transform.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            transform.position = target;
        }
    }
}
