using System;
using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Utils;
using PizzaTycoon.Managers;

namespace PizzaTycoon.Workers
{
    // Gerencia contratação, upgrade e posicionamento dos workers
    public class WorkerManager : Singleton<WorkerManager>
    {
        [Header("Dados")]
        [SerializeField] private WorkerData[] _workerCatalog;

        [Header("Prefab do NPC")]
        [SerializeField] private WorkerAI _workerPrefab;

        [Header("Pontos de spawn")]
        [SerializeField] private Transform _spawnParent;

        [Header("Stations para assignment")]
        [SerializeField] private Transform _wheatStation;
        [SerializeField] private Transform _doughStation;
        [SerializeField] private Transform _ovenStation;
        [SerializeField] private Transform _deliveryStation;

        private Dictionary<string, WorkerAI> _activeWorkers = new();
        private Dictionary<string, int>      _workerLevels  = new();

        public static event Action<string, int> OnWorkerUpgraded; // (id, newLevel)
        public static event Action<string>      OnWorkerHired;

        protected override void Awake()
        {
            base.Awake();
            LoadFromSave();
        }

        // ── Contratar ─────────────────────────────────────────────────────────

        public bool TryHire(string workerId)
        {
            WorkerData data = Find(workerId);
            if (data == null || _activeWorkers.ContainsKey(workerId)) return false;
            if (!MoneyManager.Instance.SpendMoney(data.hiringCost)) return false;

            SpawnWorker(data, _workerLevels.GetValueOrDefault(workerId, 1));
            SaveManager.Instance?.CurrentData?.hiredWorkerIds.Add(workerId);
            OnWorkerHired?.Invoke(workerId);
            return true;
        }

        public bool IsHired(string workerId) => _activeWorkers.ContainsKey(workerId);

        // ── Upgrade ───────────────────────────────────────────────────────────

        public bool TryUpgrade(string workerId)
        {
            WorkerData data = Find(workerId);
            if (data == null || !_activeWorkers.TryGetValue(workerId, out var ai)) return false;

            int currentLevel = _workerLevels.GetValueOrDefault(workerId, 1);
            if (currentLevel >= data.maxLevel) return false;

            int cost = data.GetUpgradeCost(currentLevel);
            if (!MoneyManager.Instance.SpendMoney(cost)) return false;

            int newLevel = currentLevel + 1;
            _workerLevels[workerId] = newLevel;
            ai.SetLevel(newLevel);

            var saveData = SaveManager.Instance?.CurrentData;
            if (saveData?.workerLevels != null)
                saveData.workerLevels[workerId] = newLevel;

            OnWorkerUpgraded?.Invoke(workerId, newLevel);
            return true;
        }

        public int GetLevel(string workerId) => _workerLevels.GetValueOrDefault(workerId, 1);

        // ── Spawn / Setup de stations ─────────────────────────────────────────

        private void SpawnWorker(WorkerData data, int level)
        {
            if (_workerPrefab == null) return;

            Vector3 pos = _spawnParent != null ? _spawnParent.position : Vector3.zero;
            var go = Instantiate(_workerPrefab, pos, Quaternion.identity,
                                 _spawnParent != null ? _spawnParent : transform);

            var (src, tgt) = GetStationsForType(data.type);
            go.Initialize(data, level, src, tgt);
            _activeWorkers[data.workerId] = go;
            _workerLevels[data.workerId]  = level;
        }

        private (Transform src, Transform tgt) GetStationsForType(WorkerType type) => type switch
        {
            WorkerType.Collector  => (_wheatStation,  _doughStation),
            WorkerType.Baker      => (_doughStation,  _ovenStation),
            WorkerType.Deliverer  => (_ovenStation,   _deliveryStation),
            WorkerType.Cleaner    => (_deliveryStation, _wheatStation),
            _                     => (null, null)
        };

        // ── Consultas ─────────────────────────────────────────────────────────

        public WorkerData[] GetAll() => _workerCatalog;

        private WorkerData Find(string id)
        {
            if (_workerCatalog == null) return null;
            foreach (var w in _workerCatalog)
                if (w != null && w.workerId == id) return w;
            return null;
        }

        // ── Save / Load ───────────────────────────────────────────────────────

        private void LoadFromSave()
        {
            var data = SaveManager.Instance?.CurrentData;
            if (data == null) return;

            // SerializableDictionary não é IEnumerable — dados carregados via GetValueOrDefault
            // Os níveis são restaurados ao spawnar cada worker pelo id salvo

            if (data.hiredWorkerIds != null)
                foreach (var id in data.hiredWorkerIds)
                {
                    WorkerData wd = Find(id);
                    if (wd != null) SpawnWorker(wd, _workerLevels.GetValueOrDefault(id, 1));
                }
        }
    }
}
