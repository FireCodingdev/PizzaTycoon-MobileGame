using UnityEngine;
using UnityEngine.EventSystems;

namespace PizzaTycoon.Input
{
    // Joystick virtual flutuante para controle mobile
    // Attach: no painel de UI do joystick. Requer EventSystem na cena.
    public class JoystickController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Referências UI")]
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _handle;

        [Header("Configurações")]
        [SerializeField] private float _handleRange = 1f;     // multiplicador do raio máximo do handle
        [SerializeField] private float _deadZone = 0.15f;     // zona morta para ignorar inputs mínimos
        [SerializeField] private bool _dynamicPositioning = true; // reposiciona joystick no toque

        private Canvas _canvas;
        private UnityEngine.Camera _camera;
        private Vector2 _input = Vector2.zero;
        private Vector2 _joystickCenter;
        private float _handleRadius;

        // Direção normalizada do input (-1 a 1 em X e Y)
        public Vector2 Direction => _input;
        public float Horizontal => _input.x;
        public float Vertical => _input.y;
        public bool IsActive => _input.magnitude > _deadZone;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>(true);
            if (_canvas == null)
            {
                Debug.LogWarning("[JoystickController] Canvas nao encontrado na hierarquia — joystick desabilitado.");
                enabled = false;
                return;
            }

            _camera = _canvas.renderMode == RenderMode.ScreenSpaceCamera ? _canvas.worldCamera : null;
            _handleRadius = _background != null ? _background.sizeDelta.x * 0.5f * _handleRange : 100f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_dynamicPositioning)
                MoveJoystickTo(eventData.position);

            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background, eventData.position, _camera, out Vector2 localPoint))
                return;

            // Normaliza para range [-1, 1]
            Vector2 direction = localPoint / (_background.sizeDelta * 0.5f);
            _input = direction.magnitude > 1f ? direction.normalized : direction;

            // Move o handle visualmente
            _handle.anchoredPosition = _input * _handleRadius * _handleRange;

            // Aplica dead zone
            if (_input.magnitude < _deadZone)
                _input = Vector2.zero;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _input = Vector2.zero;
            _handle.anchoredPosition = Vector2.zero;

            if (_dynamicPositioning)
                ResetJoystickPosition();
        }

        private void MoveJoystickTo(Vector2 screenPosition)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform, screenPosition, _camera, out Vector2 localPoint))
            {
                _background.anchoredPosition = localPoint;
            }
        }

        private void ResetJoystickPosition()
        {
            _background.anchoredPosition = _joystickCenter;
        }

        private void Start()
        {
            _joystickCenter = _background.anchoredPosition;
        }
    }
}
