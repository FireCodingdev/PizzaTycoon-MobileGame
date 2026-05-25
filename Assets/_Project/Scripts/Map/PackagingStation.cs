using System.Collections;
using UnityEngine;
using PizzaTycoon.Items;

namespace PizzaTycoon.Map
{
    // Station que empacota CookedPizza → PackagedPizza para a MotorcycleStation
    // Adiciona 20% de valor ao empacotar
    public class PackagingStation : MonoBehaviour
    {
        [Header("Configuração")]
        [SerializeField] private float _packagingTime     = 1.5f;
        [SerializeField] private float _valueBonus        = 0.20f; // +20%

        [Header("Visual")]
        [SerializeField] private Transform _conveyor;
        [SerializeField] private Renderer  _boxRenderer;

        private bool _busy;

        private void Start()
        {
            if (_boxRenderer != null)
            {
                var mpb = new MaterialPropertyBlock();
                mpb.SetColor("_BaseColor", new Color(0.95f, 0.85f, 0.55f)); // caixa kraft
                _boxRenderer.SetPropertyBlock(mpb);
            }
        }

        public bool IsBusy => _busy;

        public bool TryPackage(float pizzaValue, System.Action<float> onComplete)
        {
            if (_busy) return false;
            StartCoroutine(PackageRoutine(pizzaValue, onComplete));
            return true;
        }

        private IEnumerator PackageRoutine(float baseValue, System.Action<float> onComplete)
        {
            _busy = true;

            // Animação do esteirar girando
            if (_conveyor != null)
                StartCoroutine(SpinConveyor());

            yield return new WaitForSeconds(_packagingTime);

            float packedValue = baseValue * (1f + _valueBonus);
            onComplete?.Invoke(packedValue);
            _busy = false;
        }

        private IEnumerator SpinConveyor()
        {
            if (_conveyor == null) yield break;
            float elapsed = 0f;
            while (elapsed < _packagingTime)
            {
                _conveyor.Rotate(Vector3.right, 120f * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        public float PackagingTime => _packagingTime;
        public float ValueBonus    => _valueBonus;
    }
}
