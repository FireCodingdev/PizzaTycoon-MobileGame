using System.Collections;
using UnityEngine;

namespace PizzaTycoon.Camera
{
    // Zoom in suave quando jogador fica parado, zoom out ao mover
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class CameraZoomOnIdle : MonoBehaviour
    {
        [SerializeField] private float _normalSize   = 8f;
        [SerializeField] private float _zoomedSize   = 6.5f;
        [SerializeField] private float _idleDelay    = 3f;
        [SerializeField] private float _zoomInTime   = 1.0f;
        [SerializeField] private float _zoomOutTime  = 0.5f;

        private UnityEngine.Camera _cam;
        private Transform          _playerTransform;
        private Vector3            _lastPlayerPos;
        private float              _idleTimer;
        private bool               _isZoomed;
        private Coroutine          _zoomCoroutine;

        private void Awake()
        {
            _cam = GetComponent<UnityEngine.Camera>();
        }

        public void SetPlayer(Transform player)
        {
            _playerTransform = player;
            _lastPlayerPos   = player.position;
        }

        private void Update()
        {
            if (_playerTransform == null) return;

            bool moving = Vector3.SqrMagnitude(_playerTransform.position - _lastPlayerPos) > 0.001f;
            _lastPlayerPos = _playerTransform.position;

            if (moving)
            {
                _idleTimer = 0f;
                if (_isZoomed) TriggerZoom(_normalSize, _zoomOutTime);
            }
            else
            {
                _idleTimer += Time.deltaTime;
                if (_idleTimer >= _idleDelay && !_isZoomed)
                    TriggerZoom(_zoomedSize, _zoomInTime);
            }
        }

        private void TriggerZoom(float targetSize, float duration)
        {
            _isZoomed = Mathf.Approximately(targetSize, _zoomedSize);
            if (_zoomCoroutine != null) StopCoroutine(_zoomCoroutine);
            _zoomCoroutine = StartCoroutine(ZoomRoutine(targetSize, duration));
        }

        private IEnumerator ZoomRoutine(float targetSize, float duration)
        {
            float startSize = _cam.orthographicSize;
            float elapsed   = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                _cam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
                yield return null;
            }
            _cam.orthographicSize = targetSize;
        }
    }
}
