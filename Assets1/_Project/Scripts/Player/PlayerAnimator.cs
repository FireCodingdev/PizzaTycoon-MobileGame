using System.Collections;
using UnityEngine;

namespace PizzaTycoon.Player
{
    // Animações do player — dois modos:
    //   1. Animator Controller (rig humanoide) — SetInteger/SetBool conforme os params do controller
    //   2. Procedural (capsulas/primitivas) — bob + lean
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("Referências")]
        [Tooltip("Filho com a mesh — arraste o modelo 3D aqui (raiz do personagem ithappy/Synty)")]
        [SerializeField] private Transform _body;
        [Tooltip("Animator do rig — preenchido auto se houver AnimatorController no filho")]
        [SerializeField] private Animator  _animator;

        [Header("Idle Bob (modo procedural)")]
        [SerializeField] private float _idleBobHz  = 1.5f;
        [SerializeField] private float _idleBobAmp = 0.05f;

        [Header("Walk Bob (modo procedural)")]
        [SerializeField] private float _walkBobHz    = 2.5f;
        [SerializeField] private float _walkBobAmp   = 0.08f;
        [SerializeField] private float _walkLeanDeg  = 5f;
        [SerializeField] private float _carryLeanDeg = 8f;

        [Header("Estilo do andar")]
        [Tooltip("Velocidade base do PlayerController no início do jogo (antes dos upgrades). Usada para escalar a animação automaticamente.")]
        [SerializeField] private float _baseMoveSpeed = 5f;

        [Header("Tuning (resolve pés deslizando / animação travando)")]
        [Tooltip("Damp time do SetFloat 'Vert' — menor = transições mais snappy. " +
                 "Default 0.1f. Se a animação 'demora' a iniciar quando você anda, baixe para 0.05f ou 0f.")]
        [Range(0f, 0.3f)] [SerializeField] private float _vertDamp = 0.1f;
        [Tooltip("Quando true, _animator.speed é multiplicado pela razão (velocidade atual / base). " +
                 "Se DESMARCAR, a animação roda sempre na velocidade nativa — útil quando os pés deslizam ao correr.")]
        [SerializeField] private bool _scaleAnimSpeedWithMoveSpeed = true;
        [Tooltip("Multiplicador FIXO aplicado à animação ao correr (Shift), usado SOMENTE quando " +
                 "_scaleAnimSpeedWithMoveSpeed está desmarcado. 1.0 = mesma velocidade que andar.")]
        [Range(0.5f, 3f)] [SerializeField] private float _runAnimSpeed = 1.4f;

        [Header("Parâmetros do Animator (nomes exatos do AnimatorController)")]
        [Tooltip("Float 0-1: velocidade vertical — Character_Movement usa 'Vert'")]
        [SerializeField] private string _paramVert  = "Vert";
        [Tooltip("Float 0-1: velocidade horizontal — Character_Movement usa 'Hor'")]
        [SerializeField] private string _paramHor   = "Hor";
        [Tooltip("Float: 0=Idle 1=Walk 2=Run — Character_Movement usa 'State'")]
        [SerializeField] private string _paramState = "State";
        [Tooltip("Bool: true no ar — Character_Movement usa 'IsJump'")]
        [SerializeField] private string _paramJump  = "IsJump";
        [Tooltip("Deixe vazio se o controller nao tiver este param")]
        [SerializeField] private string _paramCarry = "";

        // Hashes e flags de existência
        private int  _vertHash, _horHash, _stateHash, _jumpHash, _carryHash, _speedHash;
        private bool _hasVert, _hasHor, _hasState, _hasJump, _hasCarry, _hasSpeed;

        private PlayerController _playerController;

        // Estado interno para modo procedural
        private bool  _isMoving;
        private bool  _isCarrying;
        private float _bobPhase;
        private float _targetLean;
        private float _currentLean;
        private Vector3   _bodyBaseLocalPos;
        private Coroutine _punchCoroutine;
        private Coroutine _jumpCoroutine;

        private static readonly string[] BodyCandidates =
            { "Body", "Geometry", "Character", "Mesh", "Model", "Root", "Armature" };

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
            if (_animator != null && _animator.runtimeAnimatorController == null)
                _animator = null;

            if (_body == null)
            {
                foreach (string n in BodyCandidates)
                {
                    Transform found = transform.Find(n);
                    if (found != null) { _body = found; break; }
                }
                if (_body == null && transform.childCount > 0)
                    _body = transform.GetChild(0);
            }

            if (_animator == null && _body == null)
            {
                Debug.LogWarning("[PlayerAnimator] Sem AnimatorController nem mesh filho — animacao desativada.");
                enabled = false;
                return;
            }

            if (_body != null) _bodyBaseLocalPos = _body.localPosition;
            if (_animator != null) CacheAnimatorParams();

            _playerController = GetComponent<PlayerController>();
        }

        private void CacheAnimatorParams()
        {
            _vertHash  = Hash(_paramVert);
            _horHash   = Hash(_paramHor);
            _stateHash = Hash(_paramState);
            _jumpHash  = Hash(_paramJump);
            _carryHash = Hash(_paramCarry);
            _speedHash = Animator.StringToHash("Speed");

            foreach (var p in _animator.parameters)
            {
                if (_vertHash  != 0 && p.nameHash == _vertHash)  _hasVert  = true;
                if (_horHash   != 0 && p.nameHash == _horHash)   _hasHor   = true;
                if (_stateHash != 0 && p.nameHash == _stateHash) _hasState = true;
                if (_jumpHash  != 0 && p.nameHash == _jumpHash)  _hasJump  = true;
                if (_carryHash != 0 && p.nameHash == _carryHash) _hasCarry = true;
                if (p.nameHash == _speedHash)                    _hasSpeed = true;
            }
        }

        private static int Hash(string name) =>
            string.IsNullOrEmpty(name) ? 0 : Animator.StringToHash(name);

        private void Update()
        {
            if (_animator != null) return;
            UpdateBob();
            UpdateLean();
        }

        // ── API pública ──────────────────────────────────────────────────────

        // Chamado pelo PlayerController a cada FixedUpdate
        // hor/vert: -1, 0 ou 1 (direção do input raw)
        // running: true quando Shift pressionado
        public void SetMovingFull(bool moving, bool running, bool carrying, int hor, int vert)
        {
            _isMoving   = moving;
            _isCarrying = carrying;

            if (_animator != null)
            {
                // BlendTree do Synty e 2D Cartesian: motions estao nas POSICOES
                // (0,0)=Idle, (0,1)=Walk-front, (0,-1)=Walk-back, (+/-1,0)=Walk-side.
                // Portanto Vert tem que ser 1.0 ao andar — qualquer valor menor mistura
                // com Idle e gera aquela animacao em camera lenta.
                // O PlayerController rotaciona o corpo, entao Hor sempre = 0.
                float vertVal = moving ? 1f : 0f;

                if (_hasVert)  _animator.SetFloat(_vertHash,  vertVal, _vertDamp, Time.deltaTime);
                if (_hasHor)   _animator.SetFloat(_horHash,   0f,      _vertDamp, Time.deltaTime);
                if (_hasState) _animator.SetFloat(_stateHash, !moving ? 0f : (running ? 2f : 1f));
                if (_hasCarry) _animator.SetBool(_carryHash, carrying);

                if (_scaleAnimSpeedWithMoveSpeed)
                {
                    float currentSpeed = _playerController != null ? _playerController.MoveSpeed : _baseMoveSpeed;
                    _animator.speed = moving ? (currentSpeed / _baseMoveSpeed) : 1f;
                }
                else
                {
                    _animator.speed = moving && running ? _runAnimSpeed : 1f;
                }
                return;
            }

            // Modo procedural
            if (carrying)    _targetLean = _carryLeanDeg;
            else if (moving) _targetLean = _walkLeanDeg * (running ? 1.4f : 1f);
            else             _targetLean = 0f;
        }

        public void SetMoving(bool moving, bool carrying) =>
            SetMovingFull(moving, false, carrying, 0, 0);

        public void SetJumping(bool jumping)
        {
            if (_animator != null && _hasJump)
                _animator.SetBool(_jumpHash, jumping);
        }

        public void TriggerPickup()
        {
            if (_animator != null) return; // controller cuida disso automaticamente
            if (_punchCoroutine != null) StopCoroutine(_punchCoroutine);
            _punchCoroutine = StartCoroutine(PunchScale());
        }

        public void TriggerDeliver()
        {
            if (_animator != null) return;
            if (_jumpCoroutine != null) StopCoroutine(_jumpCoroutine);
            _jumpCoroutine = StartCoroutine(JumpJoy());
        }

        public void SetAnimationSpeed(float s) { }

        // Chamado pelo PlayerController.Update() com a magnitude da velocidade.
        // Alimenta o parâmetro "Speed" do AnimatorController (Idle/Walk simple blend).
        public void SetSpeed(float speed)
        {
            if (_animator == null || !_hasSpeed) return;
            _animator.SetFloat(_speedHash, speed);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        // ── Animações procedurais ────────────────────────────────────────────

        private void UpdateBob()
        {
            if (_body == null) return;
            float hz  = _isMoving ? _walkBobHz  : _idleBobHz;
            float amp = _isMoving ? _walkBobAmp : _idleBobAmp;
            _bobPhase += hz * Mathf.PI * 2f * Time.deltaTime;
            _body.localPosition = _bodyBaseLocalPos + new Vector3(0f, Mathf.Sin(_bobPhase) * amp, 0f);
        }

        private void UpdateLean()
        {
            if (_body == null) return;
            _currentLean = Mathf.Lerp(_currentLean, _targetLean, Time.deltaTime * 10f);
            _body.localRotation = Quaternion.Euler(_currentLean, 0f, 0f);
        }

        private IEnumerator PunchScale()
        {
            if (_body == null) yield break;
            Vector3 original = _body.localScale;
            float half = 0.1f;
            for (float t = 0f; t < half; t += Time.deltaTime)
            { _body.localScale = Vector3.Lerp(original, original * 1.3f, t / half); yield return null; }
            for (float t = 0f; t < half; t += Time.deltaTime)
            { _body.localScale = Vector3.Lerp(original * 1.3f, original, t / half); yield return null; }
            _body.localScale = original;
        }

        private IEnumerator JumpJoy()
        {
            if (_body == null) yield break;
            float half = 0.1f;
            Vector3 basePos = _bodyBaseLocalPos;
            for (float t = 0f; t < half; t += Time.deltaTime)
            { _bodyBaseLocalPos = basePos + new Vector3(0f, 0.3f * (t / half), 0f); yield return null; }
            for (float t = 0f; t < half; t += Time.deltaTime)
            { _bodyBaseLocalPos = basePos + new Vector3(0f, 0.3f * (1f - t / half), 0f); yield return null; }
            _bodyBaseLocalPos = basePos;
        }
    }
}
