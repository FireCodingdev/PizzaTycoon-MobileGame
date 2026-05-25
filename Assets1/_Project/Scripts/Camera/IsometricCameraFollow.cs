using UnityEngine;

namespace PizzaTycoon.Camera
{
    // Camera que replica o estilo do Pizza Ready (Supercent):
    // - Perspectiva (NAO ortografica)
    // - Pitch alto (~65 graus) — olha de quase cima
    // - Yaw 0 graus — sem rotacao lateral
    // - Layout portrait: o mapa se estende no eixo Z
    // - FOV ~50 para dar a sensacao de profundidade
    // - Camera segue apenas o Z do player levemente
    public class IsometricCameraFollow : MonoBehaviour
    {
        [Header("Alvo")]
        [SerializeField] private Transform _target;

        [Header("Configuracao Pizza Ready Style")]
        [Tooltip("Altura da camera acima do chao")]
        [SerializeField] private float _height = 14f;
        [Tooltip("Distancia atras do player no eixo Z")]
        [SerializeField] private float _zOffset = -9f;
        [Tooltip("Offset X — centraliza ou desloca levemente")]
        [SerializeField] private float _xOffset = 0f;
        [Tooltip("Suavidade do follow. Menor = mais suave")]
        [SerializeField] private float _smoothTime = 0.18f;

        [Header("Rotacao")]
        [Tooltip("Pitch: 60-70 para visual de cima. Pizza Ready usa ~65")]
        [SerializeField] private float _pitch = 65f;
        [Tooltip("Yaw: 0 = sem rotacao lateral (portrait puro)")]
        [SerializeField] private float _yaw = 0f;

        [Header("Limites de camera (opcional)")]
        [SerializeField] private bool _useBoundsZ = false;
        [SerializeField] private float _boundsZMin = -10f;
        [SerializeField] private float _boundsZMax = 60f;

        private Vector3 _velocity = Vector3.zero;
        private UnityEngine.Camera _cam;

        private void Awake()
        {
            _cam = GetComponent<UnityEngine.Camera>();
            ConfigureCamera();
            ApplyRotation();
        }

        private void Start()
        {
            if (_target == null)
            {
                var playerGO = GameObject.FindWithTag("Player")
                            ?? GameObject.Find("Player");
                if (playerGO != null)
                {
                    _target = playerGO.transform;
                    SnapToTarget();
                }
                else
                    Debug.LogWarning("[CameraFollow] Player nao encontrado na cena.");
            }
            else
                SnapToTarget();
        }

        private void LateUpdate()
        {
            if (_target == null) return;
            FollowTarget();
        }

        private void ConfigureCamera()
        {
            if (_cam == null) return;
            _cam.orthographic = false;    // Pizza Ready usa perspectiva
            _cam.fieldOfView = 50f;
            _cam.nearClipPlane = 0.3f;
            _cam.farClipPlane = 200f;
        }

        private void ApplyRotation()
        {
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void FollowTarget()
        {
            float targetZ = _target.position.z + _zOffset;
            if (_useBoundsZ)
                targetZ = Mathf.Clamp(targetZ, _boundsZMin, _boundsZMax);

            float targetX = _target.position.x + _xOffset;
            float targetY = _height;

            Vector3 desiredPos = new Vector3(targetX, targetY, targetZ);
            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPos, ref _velocity, _smoothTime);

            ApplyRotation();
        }

        public void SetTarget(Transform t)
        {
            _target = t;
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (_target == null) return;
            float snapZ = _target.position.z + _zOffset;
            if (_useBoundsZ) snapZ = Mathf.Clamp(snapZ, _boundsZMin, _boundsZMax);
            transform.position = new Vector3(_target.position.x + _xOffset, _height, snapZ);
            _velocity = Vector3.zero;
            ApplyRotation();
        }

        // Alias para compatibilidade com CameraZoomOnIdle
        public void SetPlayer(Transform t) => SetTarget(t);

        [ContextMenu("Aplicar Configuracao Pizza Ready")]
        public void ApplyPizzaReadyPreset()
        {
            _pitch = 65f;
            _yaw = 0f;
            _height = 14f;
            _zOffset = -9f;
            _xOffset = 0f;
            ConfigureCamera();
            ApplyRotation();
            if (_target != null) SnapToTarget();
        }
    }
}
