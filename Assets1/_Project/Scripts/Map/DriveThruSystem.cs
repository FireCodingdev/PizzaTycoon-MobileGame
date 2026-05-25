using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Player;
using PizzaTycoon.Items;
using PizzaTycoon.Managers;
using PizzaTycoon.Economy;
using PizzaTycoon.GameSystems;
using PizzaTycoon.VFX;

namespace PizzaTycoon.Map
{
    // Fila de carros do drive-thru. Carros entram pela rua, param no _orderPoint,
    // esperam o jogador entregar uma pizza pronta, e seguem para a saida.
    public class DriveThruSystem : MonoBehaviour
    {
        [Header("Waypoints")]
        [SerializeField] private Transform _spawnPoint;
        [Tooltip("Pontos intermediarios entre o SpawnPoint e a fila. " +
                 "O carro passa por cada um em sequencia ANTES de entrar no drive-thru. " +
                 "Use para curvas (ex: spawn virado pro norte -> EntryWP_0 ao norte -> EntryWP_1 a oeste = curva pra esquerda). " +
                 "Vazio = carro vai direto do spawn pra fila.")]
        [SerializeField] private Transform[] _entryWaypoints;
        [SerializeField] private Transform[] _queuePositions;
        [SerializeField] private Transform _orderPoint;
        [Tooltip("Pontos intermediarios entre o OrderPoint e o ExitPoint. " +
                 "Use para fazer o carro virar antes de sair. Vazio = vai direto pro exit.")]
        [SerializeField] private Transform[] _exitWaypoints;
        [SerializeField] private Transform _exitPoint;

        [Header("Frota")]
        [SerializeField] private GameObject[] _carPrefabs;
        [SerializeField] private float _carSpeed       = 4f;
        [SerializeField] private float _spawnInterval  = 12f;
        [SerializeField] private float _orderWaitTime  = 20f;
        [SerializeField] private int   _maxCarsInQueue = 3;

        [Header("Pagamento")]
        [SerializeField] private float _basePayment      = 12f;
        [SerializeField] private float _bonusFastPayment = 6f;

        [Header("Trigger de Entrega")]
        [Tooltip("Raio do gatilho ao redor do _orderPoint para detectar o player com pizza pronta")]
        [SerializeField] private float _deliveryRadius = 2.2f;

        private readonly List<DriveThruCar> _cars = new List<DriveThruCar>();
        private float _spawnTimer;
        private bool  _initialized;

        private void Start()
        {
            _initialized = ValidateRefs();
            if (!_initialized)
                Debug.LogWarning("[DriveThruSystem] Referencias incompletas — sistema desabilitado.");

            _spawnTimer = _spawnInterval * 0.5f;
        }

        private bool ValidateRefs()
        {
            if (_spawnPoint == null || _orderPoint == null || _exitPoint == null) return false;
            if (_queuePositions == null || _queuePositions.Length == 0) return false;
            if (_carPrefabs == null || _carPrefabs.Length == 0) return false;
            return true;
        }

        private void Update()
        {
            if (!_initialized) return;

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f && _cars.Count < _maxCarsInQueue)
            {
                SpawnCar();
                _spawnTimer = _spawnInterval;
            }

            for (int i = _cars.Count - 1; i >= 0; i--)
            {
                DriveThruCar car = _cars[i];
                if (car == null || car.GameObject == null) { _cars.RemoveAt(i); continue; }

                TickCar(car, i);

                if (car.State == DriveThruCarState.Done)
                {
                    Destroy(car.GameObject);
                    _cars.RemoveAt(i);
                }
            }

            CheckPlayerDelivery();
        }

        private void SpawnCar()
        {
            GameObject prefab = _carPrefabs[Random.Range(0, _carPrefabs.Length)];
            if (prefab == null) return;

            GameObject go = Instantiate(prefab, _spawnPoint.position, _spawnPoint.rotation, transform);
            var car = new DriveThruCar
            {
                GameObject = go,
                Transform  = go.transform,
                State      = HasEntryPath() ? DriveThruCarState.MovingThroughEntry : DriveThruCarState.MovingToQueue,
                QueueIndex = _cars.Count,
                PathIndex  = 0,
                Patience   = _orderWaitTime,
            };
            _cars.Add(car);
        }

        private bool HasEntryPath() => _entryWaypoints != null && _entryWaypoints.Length > 0;
        private bool HasExitPath()  => _exitWaypoints  != null && _exitWaypoints.Length  > 0;

        private void TickCar(DriveThruCar car, int idx)
        {
            // Atualiza indice na fila (carro mais perto do balcao = idx 0)
            car.QueueIndex = idx;

            Vector3 target;
            switch (car.State)
            {
                case DriveThruCarState.MovingThroughEntry:
                {
                    // Percorre _entryWaypoints em sequencia antes de entrar na fila.
                    if (!HasEntryPath() || car.PathIndex >= _entryWaypoints.Length)
                    {
                        car.State     = DriveThruCarState.MovingToQueue;
                        car.PathIndex = 0;
                        break;
                    }
                    Transform wp = _entryWaypoints[car.PathIndex];
                    if (wp == null) { car.PathIndex++; break; }

                    target = wp.position;
                    MoveTowards(car.Transform, target, _carSpeed);
                    if (Reached(car.Transform.position, target))
                    {
                        car.PathIndex++;
                        if (car.PathIndex >= _entryWaypoints.Length)
                        {
                            car.State     = DriveThruCarState.MovingToQueue;
                            car.PathIndex = 0;
                        }
                    }
                    break;
                }

                case DriveThruCarState.MovingToQueue:
                {
                    // Carro vai direto ao slot de fila correspondente.
                    // Slot 0 = posicao mais proxima ao balcao; slots seguintes = atras.
                    int slot = Mathf.Min(car.QueueIndex, _queuePositions.Length - 1);
                    target = _queuePositions[slot].position;
                    MoveTowards(car.Transform, target, _carSpeed);
                    if (Reached(car.Transform.position, target))
                    {
                        if (car.QueueIndex == 0) car.State = DriveThruCarState.MovingToOrder;
                        else car.State = DriveThruCarState.WaitingInQueue;
                    }
                    break;
                }

                case DriveThruCarState.WaitingInQueue:
                {
                    // Se foi promovido para a frente, avanca para o ponto de pedido.
                    if (car.QueueIndex == 0) { car.State = DriveThruCarState.MovingToOrder; break; }
                    int slot = Mathf.Min(car.QueueIndex, _queuePositions.Length - 1);
                    target = _queuePositions[slot].position;
                    MoveTowards(car.Transform, target, _carSpeed * 0.7f);
                    break;
                }

                case DriveThruCarState.MovingToOrder:
                {
                    target = _orderPoint.position;
                    MoveTowards(car.Transform, target, _carSpeed * 0.8f);
                    if (Reached(car.Transform.position, target))
                    {
                        car.State    = DriveThruCarState.WaitingOrder;
                        car.Patience = _orderWaitTime;
                    }
                    break;
                }

                case DriveThruCarState.WaitingOrder:
                {
                    car.Patience -= Time.deltaTime;
                    // Pulso visual de impaciencia (escala Y).
                    if (car.Transform != null)
                    {
                        float ratio = Mathf.Clamp01(car.Patience / _orderWaitTime);
                        float scale = 1f + Mathf.Sin(Time.time * (4f + (1f - ratio) * 6f)) * 0.04f * (1.5f - ratio);
                        Vector3 s = car.Transform.localScale;
                        car.Transform.localScale = new Vector3(s.x, scale, s.z);
                    }
                    if (car.Patience <= 0f)
                    {
                        // Cliente desistiu: combo break + sai.
                        ComboSystem.Instance?.BreakCombo();
                        car.State = DriveThruCarState.LeavingAngry;
                    }
                    break;
                }

                case DriveThruCarState.Served:
                case DriveThruCarState.LeavingAngry:
                {
                    // Apos atendido (ou bravo), opcionalmente percorre _exitWaypoints
                    // antes de ir pro ExitPoint final.
                    if (HasExitPath())
                    {
                        car.State     = DriveThruCarState.MovingThroughExit;
                        car.PathIndex = 0;
                    }
                    else
                    {
                        target = _exitPoint.position;
                        MoveTowards(car.Transform, target, _carSpeed * 1.1f);
                        if (Reached(car.Transform.position, target, 1.2f))
                            car.State = DriveThruCarState.Done;
                    }
                    break;
                }

                case DriveThruCarState.MovingThroughExit:
                {
                    if (!HasExitPath() || car.PathIndex >= _exitWaypoints.Length)
                    {
                        target = _exitPoint.position;
                        MoveTowards(car.Transform, target, _carSpeed * 1.1f);
                        if (Reached(car.Transform.position, target, 1.2f))
                            car.State = DriveThruCarState.Done;
                        break;
                    }
                    Transform wp = _exitWaypoints[car.PathIndex];
                    if (wp == null) { car.PathIndex++; break; }

                    target = wp.position;
                    MoveTowards(car.Transform, target, _carSpeed * 1.1f);
                    if (Reached(car.Transform.position, target))
                        car.PathIndex++;
                    break;
                }
            }
        }

        // Detecta se o player encostou no _orderPoint com pizza pronta — entrega automatica.
        private void CheckPlayerDelivery()
        {
            if (_orderPoint == null) return;
            if (_cars.Count == 0) return;
            DriveThruCar front = _cars[0];
            if (front == null || front.State != DriveThruCarState.WaitingOrder) return;

            // Procura player num pequeno raio.
            Collider[] hits = Physics.OverlapSphere(_orderPoint.position, _deliveryRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                PlayerController player = hits[i].GetComponentInParent<PlayerController>();
                if (player == null) continue;

                PlayerStacker stacker = player.Stacker;
                if (stacker == null) continue;
                if (!stacker.ContainsType(ItemType.CookedPizza)) continue;

                StackableItem pizza = stacker.RemoveItemOfType(ItemType.CookedPizza);
                if (pizza == null) continue;
                ItemPool.Instance.Return(pizza);

                float ratio   = Mathf.Clamp01(front.Patience / _orderWaitTime);
                float payment = _basePayment + (ratio > 0.5f ? _bonusFastPayment : 0f);

                MoneyManager.Instance?.AddMoney(payment, _orderPoint.position + Vector3.up * 1.5f);
                ComboSystem.Instance?.RegisterSale();
                DailyGoalSystem.Instance?.RegisterPizzaDelivered();
                PlayerProgressionSystem.Instance?.RegisterDriveThruServed();
                PlayerProgressionSystem.Instance?.RegisterPizzaDelivered();
                AudioManager.Instance?.PlayCoinPickup();
                ParticleManager.Instance?.PlayCustomerHappy(front.Transform.position + Vector3.up * 1.2f);

                front.State = DriveThruCarState.Served;
                return;
            }
        }

        private static void MoveTowards(Transform t, Vector3 target, float speed)
        {
            if (t == null) return;
            Vector3 flatTarget = new Vector3(target.x, t.position.y, target.z);
            t.position = Vector3.MoveTowards(t.position, flatTarget, speed * Time.deltaTime);
            Vector3 dir = flatTarget - t.position;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
                t.rotation = Quaternion.Slerp(t.rotation, look, Time.deltaTime * 6f);
            }
        }

        private static bool Reached(Vector3 a, Vector3 b, float tol = 0.4f)
        {
            Vector3 d = a - b; d.y = 0f;
            return d.sqrMagnitude <= tol * tol;
        }

        private enum DriveThruCarState
        {
            MovingThroughEntry,
            MovingToQueue,
            WaitingInQueue,
            MovingToOrder,
            WaitingOrder,
            Served,
            MovingThroughExit,
            LeavingAngry,
            Done,
        }

        private class DriveThruCar
        {
            public GameObject GameObject;
            public Transform  Transform;
            public DriveThruCarState State;
            public int   QueueIndex;
            public int   PathIndex;   // posicao atual em _entryWaypoints / _exitWaypoints
            public float Patience;
        }

#if UNITY_EDITOR
        // Visualizacao no Scene View — sempre visivel.
        // Verde=Spawn | Ciano=Entry/Exit waypoints (curvas) | Amarelo=Queue (Q1 = proximo a
        // ser atendido) | Vermelho=Order (com circulo do raio de entrega) | Azul=Exit.
        private void OnDrawGizmos()
        {
            DrawWP(_spawnPoint, "SPAWN", new Color(0.2f, 0.95f, 0.3f));

            if (_entryWaypoints != null)
            {
                for (int i = 0; i < _entryWaypoints.Length; i++)
                    DrawWP(_entryWaypoints[i], $"E{i}", new Color(0.2f, 0.85f, 0.95f));
            }

            if (_queuePositions != null)
            {
                for (int i = 0; i < _queuePositions.Length; i++)
                    DrawWP(_queuePositions[i], $"Q{i + 1}", new Color(0.95f, 0.85f, 0.15f));
            }
            DrawWP(_orderPoint, "ORDER", new Color(0.95f, 0.3f, 0.2f));

            if (_exitWaypoints != null)
            {
                for (int i = 0; i < _exitWaypoints.Length; i++)
                    DrawWP(_exitWaypoints[i], $"X{i}", new Color(0.55f, 0.4f, 0.95f));
            }

            DrawWP(_exitPoint,  "EXIT",  new Color(0.3f, 0.55f, 0.95f));

            if (_orderPoint != null)
            {
                Gizmos.color = new Color(0.95f, 0.3f, 0.2f, 0.35f);
                Gizmos.DrawWireSphere(_orderPoint.position, _deliveryRadius);
            }

            // Caminho: Spawn -> Entry[0..n] -> ultimo Q -> ... -> Q1 -> Order -> Exit[0..n] -> Exit
            Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.6f);

            Transform prev = _spawnPoint;
            if (_entryWaypoints != null)
            {
                for (int i = 0; i < _entryWaypoints.Length; i++)
                {
                    DrawLink(prev, _entryWaypoints[i]);
                    if (_entryWaypoints[i] != null) prev = _entryWaypoints[i];
                }
            }

            Transform last = (_queuePositions != null && _queuePositions.Length > 0)
                ? _queuePositions[_queuePositions.Length - 1]
                : _orderPoint;
            DrawLink(prev, last);

            if (_queuePositions != null)
                for (int i = _queuePositions.Length - 1; i > 0; i--)
                    DrawLink(_queuePositions[i], _queuePositions[i - 1]);

            if (_queuePositions != null && _queuePositions.Length > 0)
                DrawLink(_queuePositions[0], _orderPoint);

            prev = _orderPoint;
            if (_exitWaypoints != null)
            {
                for (int i = 0; i < _exitWaypoints.Length; i++)
                {
                    DrawLink(prev, _exitWaypoints[i]);
                    if (_exitWaypoints[i] != null) prev = _exitWaypoints[i];
                }
            }
            DrawLink(prev, _exitPoint);
        }

        private static void DrawWP(Transform t, string label, Color c)
        {
            if (t == null) return;
            Gizmos.color = c;
            Vector3 p = t.position + Vector3.up * 0.4f;
            Gizmos.DrawSphere(p, 0.35f);
            UnityEditor.Handles.color = c;
            UnityEditor.Handles.Label(t.position + Vector3.up * 1.5f, label);
        }

        private static void DrawLink(Transform a, Transform b)
        {
            if (a == null || b == null) return;
            Gizmos.DrawLine(a.position + Vector3.up * 0.3f, b.position + Vector3.up * 0.3f);
        }
#endif
    }
}
