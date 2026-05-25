using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace PizzaTycoon.Workers
{
    public enum WorkerState { Idle, MovingToSource, PickingUp, MovingToTarget, Delivering }

    // NPC autônomo que automatiza coleta e entrega entre duas estações.
    // Usa NavMeshAgent se houver NavMesh baked; caso contrário usa lerp simples.
    public class WorkerAI : MonoBehaviour
    {
        public WorkerData  Data    { get; private set; }
        public int         Level   { get; private set; } = 1;
        public WorkerState State   { get; private set; } = WorkerState.Idle;

        private NavMeshAgent _agent;
        private bool         _useNavMesh;
        private Transform    _sourceStation;
        private Transform    _targetStation;
        private Coroutine    _taskCoroutine;

        // Velocidade base quando sem WorkerData
        private const float DefaultSpeed = 3.5f;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _useNavMesh = _agent != null
                && NavMesh.SamplePosition(transform.position, out _, 2f, NavMesh.AllAreas);

            if (_agent != null && !_useNavMesh)
                _agent.enabled = false; // evita erros se não há NavMesh
        }

        public void Initialize(WorkerData data, int level, Transform source, Transform target)
        {
            Data           = data;
            Level          = level;
            _sourceStation = source;
            _targetStation = target;

            float speed = data != null ? data.GetSpeed(level) : DefaultSpeed;
            if (_agent != null && _useNavMesh)
            {
                _agent.speed            = speed;
                _agent.stoppingDistance = 0.5f;
            }

            ApplyUniformColor(data?.uniformColor ?? new Color(0.9f, 0.6f, 0.2f));

            if (_taskCoroutine != null) StopCoroutine(_taskCoroutine);
            _taskCoroutine = StartCoroutine(TaskLoop());
        }

        public void SetLevel(int level)
        {
            Level = Mathf.Clamp(level, 1, Data?.maxLevel ?? 5);
            float speed = Data != null ? Data.GetSpeed(Level) : DefaultSpeed;
            if (_agent != null && _useNavMesh) _agent.speed = speed;
        }

        public void SetStations(Transform source, Transform target)
        {
            _sourceStation = source;
            _targetStation = target;
        }

        // ── Loop principal ────────────────────────────────────────────────────

        private IEnumerator TaskLoop()
        {
            // Pequeno delay inicial aleatório para desincronizar workers
            yield return new WaitForSeconds(Random.Range(0f, 2f));

            while (true)
            {
                if (_sourceStation == null || _targetStation == null)
                {
                    State = WorkerState.Idle;
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                State = WorkerState.MovingToSource;
                yield return MoveToPoint(_sourceStation.position);

                State = WorkerState.PickingUp;
                yield return new WaitForSeconds(0.6f);

                State = WorkerState.MovingToTarget;
                yield return MoveToPoint(_targetStation.position);

                State = WorkerState.Delivering;
                yield return new WaitForSeconds(0.4f);

                State = WorkerState.Idle;
                yield return new WaitForSeconds(0.3f);
            }
        }

        // ── Movimento (NavMesh ou Lerp) ───────────────────────────────────────

        private IEnumerator MoveToPoint(Vector3 destination)
        {
            destination.y = transform.position.y; // mantém Y do chão

            if (_useNavMesh && _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.SetDestination(destination);
                yield return null;
                while (_agent.pathPending) yield return null;
                while (_agent.remainingDistance > _agent.stoppingDistance)
                {
                    if (!_agent.hasPath) break;
                    yield return null;
                }
                _agent.ResetPath();
            }
            else
            {
                // Lerp fallback — funciona sem NavMesh
                float speed = Data != null ? Data.GetSpeed(Level) : DefaultSpeed;
                while (Vector3.Distance(transform.position, destination) > 0.25f)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position, destination, speed * Time.deltaTime);

                    // Vira o boneco na direção do movimento
                    Vector3 dir = (destination - transform.position);
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.01f)
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation, Quaternion.LookRotation(dir), 15f * Time.deltaTime);

                    yield return null;
                }
            }
        }

        // ── Visual ────────────────────────────────────────────────────────────

        private void ApplyUniformColor(Color color)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                // Tenta _BaseColor (URP) e _Color (Standard)
                mpb.SetColor("_BaseColor", color);
                mpb.SetColor("_Color",     color);
                r.SetPropertyBlock(mpb);
            }
        }

        private void OnDisable()
        {
            if (_taskCoroutine != null) StopCoroutine(_taskCoroutine);
            if (_agent != null && _useNavMesh && _agent.isOnNavMesh) _agent.ResetPath();
        }
    }
}
