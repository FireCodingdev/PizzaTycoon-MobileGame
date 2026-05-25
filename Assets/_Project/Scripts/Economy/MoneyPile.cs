using System.Collections;
using UnityEngine;
using PizzaTycoon.Managers;
using PizzaTycoon.VFX;

namespace PizzaTycoon.Economy
{
    // Pilha de dinheiro coletavel no chao - estilo Pizza Ready / Supercent.
    // Criado pelo MoneyPileSpawner ao cliente pagar.
    public class MoneyPile : MonoBehaviour
    {
        private float _value;
        private float _lifetime = 30f;
        private bool  _canPickup;
        private Vector3 _groundPos;

        private const float PickupDelay    = 0.35f;
        private const float BounceHeight   = 1.2f;
        private const float BounceDuration = 0.45f;
        private const float ScatterRadius  = 0.6f;
        private const float RotateSpeed    = 180f;
        private const float BobAmplitude   = 0.08f;
        private const float BobSpeed       = 3f;

        public void Initialize(float value, float lifetime = 30f)
        {
            _value    = value;
            _lifetime = lifetime;
        }

        private void Start()
        {
            StartCoroutine(BounceArc());
            StartCoroutine(AutoDestroy());
        }

        private IEnumerator BounceArc()
        {
            Vector3 start  = transform.position;
            Vector3 offset = new Vector3(
                Random.Range(-ScatterRadius, ScatterRadius),
                0f,
                Random.Range(-ScatterRadius * 0.5f, ScatterRadius * 0.6f));
            Vector3 end = start + offset;
            end.y = start.y;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / BounceDuration;
                float tc  = Mathf.Clamp01(t);
                float arc = Mathf.Sin(tc * Mathf.PI) * BounceHeight;
                transform.position = Vector3.Lerp(start, end, tc) + Vector3.up * arc;
                transform.Rotate(0f, RotateSpeed * Time.deltaTime, 0f);
                yield return null;
            }

            _groundPos = end;
            transform.position = _groundPos;

            yield return new WaitForSeconds(PickupDelay);
            _canPickup = true;
            StartCoroutine(BobAndRotate());
        }

        private IEnumerator BobAndRotate()
        {
            float time = 0f;
            while (this != null)
            {
                time += Time.deltaTime;
                float y = Mathf.Sin(time * BobSpeed) * BobAmplitude;
                transform.position = _groundPos + Vector3.up * y;
                transform.Rotate(0f, RotateSpeed * Time.deltaTime, 0f);
                yield return null;
            }
        }

        private IEnumerator AutoDestroy()
        {
            yield return new WaitForSeconds(_lifetime);
            if (this != null) Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_canPickup) return;
            if (!other.CompareTag("Player")) return;

            MoneyManager.Instance?.AddMoney(_value, transform.position);
            ParticleManager.Instance?.PlayMoney(transform.position);
            Destroy(gameObject);
        }
    }
}
