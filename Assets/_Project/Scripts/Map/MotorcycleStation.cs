using System.Collections;
using UnityEngine;
using PizzaTycoon.Items;

namespace PizzaTycoon.Map
{
    // Station de entrega por moto — aceita PackagedPizza e entrega em zonas mais distantes
    // Multiplica o valor da entrega em 1.5x (distância extra)
    public class MotorcycleStation : MonoBehaviour
    {
        [Header("Configuração")]
        [SerializeField] private float _deliveryTime      = 4f;   // segundos por entrega
        [SerializeField] private float _valueMultiplier   = 1.5f;
        [SerializeField] private int   _maxQueue          = 3;

        [Header("Visual")]
        [SerializeField] private Transform _bikeTransform;
        [SerializeField] private Renderer  _bikeRenderer;

        private int  _queueCount;
        private bool _delivering;

        private void Start()
        {
            // Colorir a moto com MaterialPropertyBlock
            if (_bikeRenderer != null)
            {
                var mpb = new MaterialPropertyBlock();
                mpb.SetColor("_BaseColor", new Color(0.8f, 0.2f, 0.1f));
                _bikeRenderer.SetPropertyBlock(mpb);
            }
        }

        public bool CanAccept() => _queueCount < _maxQueue;

        public bool TryDeliver(float pizzaValue)
        {
            if (!CanAccept()) return false;
            _queueCount++;
            StartCoroutine(DeliverRoutine(pizzaValue));
            return true;
        }

        private IEnumerator DeliverRoutine(float baseValue)
        {
            if (!_delivering && _bikeTransform != null)
                StartCoroutine(AnimateBike());

            _delivering = true;
            yield return new WaitForSeconds(_deliveryTime);

            float earned = baseValue * _valueMultiplier;
            Managers.MoneyManager.Instance?.AddMoney((int)earned);
            UI.FloatingText.Show($"+${(int)earned}", transform.position, UI.FloatingTextType.Money);

            _queueCount = Mathf.Max(0, _queueCount - 1);
            if (_queueCount == 0) _delivering = false;
        }

        private IEnumerator AnimateBike()
        {
            if (_bikeTransform == null) yield break;
            Vector3 startPos = _bikeTransform.localPosition;
            Vector3 away     = startPos + Vector3.forward * 3f;

            // Sai
            for (float t = 0f; t < _deliveryTime * 0.3f; t += Time.deltaTime)
            {
                float p = t / (_deliveryTime * 0.3f);
                _bikeTransform.localPosition = Vector3.Lerp(startPos, away, p);
                yield return null;
            }
            _bikeTransform.localPosition = away;

            yield return new WaitForSeconds(_deliveryTime * 0.4f);

            // Volta
            for (float t = 0f; t < _deliveryTime * 0.3f; t += Time.deltaTime)
            {
                float p = t / (_deliveryTime * 0.3f);
                _bikeTransform.localPosition = Vector3.Lerp(away, startPos, p);
                yield return null;
            }
            _bikeTransform.localPosition = startPos;
        }

        public int QueueCount => _queueCount;
        public float Multiplier => _valueMultiplier;
    }
}
