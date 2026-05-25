using System.Collections;
using UnityEngine;

namespace PizzaTycoon.Camera
{
    // Adiciona shake à câmera para feedback de eventos importantes.
    // Adicione este componente ao mesmo GameObject da câmera principal.
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        private Coroutine _shakeCoroutine;
        private Vector3 _shakeOffset = Vector3.zero;

        private void Awake()
        {
            Instance = this;
        }

        // Aplica shake na câmera.
        // intensity: 0.05 (sutil) → 0.5 (forte)
        // duration: em segundos
        public void Shake(float intensity, float duration)
        {
            if (_shakeCoroutine != null)
                StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ShakeRoutine(intensity, duration));
        }

        // Atalhos pré-configurados para os eventos do jogo
        public void ShakeOnSell()    => Shake(GameDesignData(), 0.15f);
        public void ShakeOnAngry()   => Shake(GameDesignData(angry: true), 0.20f);
        public void ShakeOnUpgrade() => Shake(0.12f, 0.25f);

        private float GameDesignData(bool angry = false)
        {
            var gd = Economy.GameDesignData.Instance;
            if (gd == null) return angry ? 0.15f : 0.08f;
            return angry ? gd.cameraShakeAngry : gd.cameraShakeSell;
        }

        private IEnumerator ShakeRoutine(float intensity, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float remaining = 1f - (elapsed / duration);
                float currentIntensity = intensity * remaining;

                // Offset aleatório decrescente ao longo do tempo
                _shakeOffset = Random.insideUnitSphere * currentIntensity;
                _shakeOffset.z = 0f; // câmera isométrica — não mover profundidade

                transform.localPosition += _shakeOffset;

                elapsed += Time.deltaTime;
                yield return null;

                // Desfaz o offset antes do próximo frame
                transform.localPosition -= _shakeOffset;
            }

            _shakeOffset = Vector3.zero;
        }
    }
}
