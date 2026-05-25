using System.Collections;
using UnityEngine;

namespace PizzaTycoon.Items
{
    // Anima cada item na pilha: wobble contínuo + punch ao adicionar + drop ao remover
    [RequireComponent(typeof(StackableItem))]
    public class ItemStackAnimator : MonoBehaviour
    {
        [SerializeField] private float _wobbleHz  = 1.8f;
        [SerializeField] private float _wobbleAmp = 3.5f; // graus

        private int    _stackIndex;
        private float  _phaseOffset;
        private bool   _wobbling;

        private Vector3 _baseLocalPos;
        private Coroutine _punchCoroutine;

        private void Awake()
        {
            _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void OnEnable()
        {
            _baseLocalPos = transform.localPosition;
            _wobbling     = true;
        }

        private void OnDisable()
        {
            _wobbling = false;
        }

        private void Update()
        {
            if (!_wobbling) return;

            float phase = Time.time * _wobbleHz * Mathf.PI * 2f + _phaseOffset + _stackIndex * 0.4f;
            // Amplitude sobe com índice (topo da pilha oscila mais)
            float amp   = _wobbleAmp * (1f + _stackIndex * 0.15f);
            float tilt  = Mathf.Sin(phase) * amp;

            transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
        }

        // Chamado por PlayerStacker ao empilhar
        public void OnAddedToStack(int index)
        {
            _stackIndex   = index;
            _baseLocalPos = transform.localPosition;

            if (_punchCoroutine != null) StopCoroutine(_punchCoroutine);
            _punchCoroutine = StartCoroutine(ElasticPunch());
        }

        // Chamado por PlayerStacker ao remover
        public void OnRemovedFromStack()
        {
            if (_punchCoroutine != null) StopCoroutine(_punchCoroutine);
            _punchCoroutine = StartCoroutine(DropPop());
        }

        // Elastic punch: escala 0 → 1.25 → 1.0
        private IEnumerator ElasticPunch()
        {
            Vector3 target = transform.localScale;
            transform.localScale = Vector3.zero;
            float duration = 0.25f;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float p = t / duration;
                // Elastic overshoot manual
                float scale = p < 0.7f
                    ? Mathf.Lerp(0f, 1.25f, p / 0.7f)
                    : Mathf.Lerp(1.25f, 1f, (p - 0.7f) / 0.3f);
                transform.localScale = target * scale;
                yield return null;
            }
            transform.localScale = target;
        }

        // Pop ao ser removido: squash rápido
        private IEnumerator DropPop()
        {
            Vector3 original = transform.localScale;
            float   half     = 0.06f;

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                float p = t / half;
                transform.localScale = new Vector3(
                    original.x * Mathf.Lerp(1f, 1.3f, p),
                    original.y * Mathf.Lerp(1f, 0.7f, p),
                    original.z * Mathf.Lerp(1f, 1.3f, p));
                yield return null;
            }
            transform.localScale = original;
        }
    }
}
