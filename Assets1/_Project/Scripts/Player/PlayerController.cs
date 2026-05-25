using UnityEngine;
using PizzaTycoon.Input;
using PizzaTycoon.Stations;
using PizzaTycoon.VFX;
using PizzaTycoon.Camera;

namespace PizzaTycoon.Player
{
    // Controla movimento do jogador via joystick + WASD/setas, colisoes com estacoes e fisica Rigidbody
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimento")]
        [Tooltip("Velocidade base de andar (em m/s). Aumente para personagem mais rapido.")]
        [SerializeField] private float _moveSpeed         = 10f;
        [Tooltip("Velocidade de rotacao do personagem (graus por frame)")]
        [SerializeField] private float _rotationSpeed     = 15f;

        [Header("Corrida (Shift)")]
        [Tooltip("Multiplicador de velocidade quando Shift esta pressionado")]
        [SerializeField] private float _runSpeedMultiplier = 1.8f;

        [Header("Pulo (Space)")]
        [SerializeField] private float _jumpForce          = 6f;
        [Tooltip("Raio da checagem de chao (CheckSphere)")]
        [SerializeField] private float _groundCheckRadius  = 0.25f;
        [Tooltip("Layers consideradas chao — deixe Default se nao souber")]
        [SerializeField] private LayerMask _groundMask     = -1;

        [Header("Referencias")]
        [SerializeField] private JoystickController _joystick;

        [Header("Configuracao Isometrica")]
        [Tooltip("Habilita controles WASD/setas como fallback quando joystick nao esta ativo")]
        [SerializeField] private bool _allowKeyboardInput = true;

        private Rigidbody        _rigidbody;
        private PlayerStacker    _stacker;
        private PlayerAnimator   _playerAnimator;
        private BaseStation      _currentStation;
        private CameraZoomOnIdle _cameraZoom;
        private bool             _isGrounded;
        private bool             _isJumping;

        public float MoveSpeed { get => _moveSpeed; set => _moveSpeed = value; }

        private void Awake()
        {
            _rigidbody      = GetComponent<Rigidbody>();
            _stacker        = GetComponent<PlayerStacker>();
            _playerAnimator = GetComponent<PlayerAnimator>();
            _cameraZoom     = FindObjectOfType<CameraZoomOnIdle>();

            _rigidbody.freezeRotation = true;
            _rigidbody.interpolation  = RigidbodyInterpolation.Interpolate;

            // Garante tag "Player" para que estacoes detectem o trigger
            if (!gameObject.CompareTag("Player"))
            {
                try { gameObject.tag = "Player"; }
                catch { Debug.LogWarning("[PlayerController] Tag 'Player' nao existe no projeto."); }
            }

            if (_cameraZoom != null) _cameraZoom.SetPlayer(transform);
        }

        private void Update()
        {
            CheckGround();
            HandleJump();
            _playerAnimator?.SetSpeed(_rigidbody.velocity.magnitude);
        }

        private void FixedUpdate()
        {
            HandleMovement();
        }

        private void CheckGround()
        {
            Vector3 origin = transform.position + Vector3.up * (_groundCheckRadius + 0.05f);
            _isGrounded = Physics.CheckSphere(origin, _groundCheckRadius, _groundMask,
                QueryTriggerInteraction.Ignore);

            if (_isGrounded && _isJumping && _rigidbody.velocity.y <= 0.05f)
            {
                _isJumping = false;
                _playerAnimator?.SetJumping(false);
            }
        }

        private void HandleJump()
        {
            if (!_allowKeyboardInput) return;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space) && _isGrounded && !_isJumping)
            {
                _isJumping = true;
                _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
                _playerAnimator?.SetJumping(true);
            }
        }

        private void HandleMovement()
        {
            Vector2 raw      = ReadInput();
            bool isRunning   = _allowKeyboardInput && UnityEngine.Input.GetKey(KeyCode.LeftShift);
            float speed      = _moveSpeed * (isRunning ? _runSpeedMultiplier : 1f);
            bool carrying    = _stacker != null && _stacker.HasItems;

            // Valores inteiros de direção para o Animator (-1 / 0 / 1)
            int hor  = raw.x > 0.1f ? 1 : raw.x < -0.1f ? -1 : 0;
            int vert = raw.y > 0.1f ? 1 : raw.y < -0.1f ? -1 : 0;

            Vector3 direction = new Vector3(raw.x, 0f, raw.y);
            if (direction.sqrMagnitude > 1f) direction.Normalize();

            if (direction.magnitude > 0.1f)
            {
                Vector3 worldDir = ConvertToWorldDirection(direction);
                _rigidbody.velocity = new Vector3(
                    worldDir.x * speed,
                    _rigidbody.velocity.y,
                    worldDir.z * speed);
                RotateTowards(worldDir);
                _playerAnimator?.SetMovingFull(true, isRunning, carrying, hor, vert);
            }
            else
            {
                _rigidbody.velocity = new Vector3(0f, _rigidbody.velocity.y, 0f);
                _playerAnimator?.SetMovingFull(false, false, carrying, 0, 0);
            }
        }

        // Le entrada de joystick virtual ou teclado (WASD/setas) como fallback
        private Vector2 ReadInput()
        {
            float h = 0f, v = 0f;

            // Joystick (mobile/touch) tem prioridade quando ativo
            if (_joystick != null)
            {
                h = _joystick.Horizontal;
                v = _joystick.Vertical;
            }

            // Teclado (desktop) — usa GetKey direto para garantir funcionamento
            // mesmo se o Input Manager classico nao estiver configurado ou se o
            // projeto usar apenas o New Input System.
            if (_allowKeyboardInput && Mathf.Abs(h) < 0.01f && Mathf.Abs(v) < 0.01f)
            {
                if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow))  h -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow)) h += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow))  v -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow))    v += 1f;
            }

            return new Vector2(h, v);
        }

        // Chamado por estacoes apos adicionar item a pilha do jogador
        public void OnItemCollected(Vector3 worldPos)
        {
            ParticleManager.Instance?.PlayCollect(worldPos);
            CameraShake.Instance?.Shake(0.08f, 0.15f);
            _playerAnimator?.TriggerPickup();
        }

        // Chamado por DeliveryStation apos entrega bem-sucedida
        public void OnItemDelivered(Vector3 worldPos)
        {
            _playerAnimator?.TriggerDeliver();
        }

        // Converte o input local (com Z = forward) para direcao no mundo
        // alinhada com o yaw da camera isometrica. Ex.: yaw=45 rotaciona o input 45 no Y.
        private Vector3 ConvertToWorldDirection(Vector3 inputDirection)
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null)
                return new Vector3(inputDirection.x, 0f, inputDirection.z).normalized;

            // Pega apenas o yaw da camera (ignora pitch) para mapear input → mundo
            float yaw = cam.transform.eulerAngles.y;
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 worldDir = rotation * new Vector3(inputDirection.x, 0f, inputDirection.z);
            worldDir.y = 0f;
            return worldDir.sqrMagnitude > 0.001f ? worldDir.normalized : Vector3.zero;
        }

        private void RotateTowards(Vector3 direction)
        {
            if (direction == Vector3.zero) return;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                _rotationSpeed * Time.fixedDeltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            BaseStation station = other.GetComponent<BaseStation>();
            if (station != null)
            {
                _currentStation = station;
                station.OnPlayerEnter(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            BaseStation station = other.GetComponent<BaseStation>();
            if (station != null && station == _currentStation)
            {
                station.OnPlayerExit(this);
                _currentStation = null;
            }
        }

        public PlayerStacker Stacker => _stacker;
    }
}
