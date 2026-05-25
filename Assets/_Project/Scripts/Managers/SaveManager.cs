using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using PizzaTycoon.Utils;
using PizzaTycoon.GameSystems;
using PizzaTycoon.Achievements;

namespace PizzaTycoon.Managers
{
    [Serializable]
    public class GameSaveData
    {
        // Economia
        public float money               = 0f;

        // Upgrades
        public int playerSpeedLevel      = 0;
        public int stackCapacityLevel    = 0;
        public int productionSpeedLevel  = 0;
        public List<string> unlockedStations = new List<string>();

        // Progresso
        public int   currentPhase        = 0;
        public int   totalPizzasSold     = 0;
        public int   bestCombo           = 0;
        public float totalPlayTime       = 0f;  // segundos acumulados
        public bool  tutorialCompleted   = false;

        // Metas diárias
        public DailyGoalSaveData dailyGoals = null;

        // Streaks
        public int    consecutiveDaysPlayed = 0;
        public string lastPlayDate          = "";   // "yyyy-MM-dd"

        // Timestamp para ganhos offline
        public long lastSaveTimestamp = 0;

        // Monetização
        public bool adsRemoved       = false;
        public bool isVIP            = false;
        public long starterPackExpiry= 0;  // unix timestamp de expiração do pack inicial

        // Conquistas (Phase 5)
        public List<AchievementSaveEntry> achievementProgress = new List<AchievementSaveEntry>();

        // Skins (Phase 5)
        public List<string> unlockedSkinIds        = new List<string>();
        public string       equippedPlayerSkin      = "classic";
        public string       equippedRestaurantSkin  = "classic_italian";

        // Season Pass (Phase 5)
        public int          seasonXP               = 0;
        public int          seasonLevel            = 0;
        public bool         seasonPremium          = false;
        public List<int>    claimedSeasonFree      = new List<int>();
        public List<int>    claimedSeasonPremium   = new List<int>();

        // Decorações (Phase 5)
        public List<string> purchasedDecorationIds = new List<string>();

        // Leaderboard (Phase 5)
        public List<Leaderboard.LeaderboardEntry> leaderboardEntries = new List<Leaderboard.LeaderboardEntry>();

        // Workers (Phase 5)
        public List<string>             hiredWorkerIds = new List<string>();
        public SerializableDictionary   workerLevels   = new SerializableDictionary();

        // Receitas (Phase 5)
        public List<string> unlockedRecipeIds = new List<string>();

        // Mapa (Phase 5)
        public bool deliverySectorUnlocked = false;

        // Notificações (Phase 5)
        public bool notificationsEnabled = true;

        // Social / meta (Phase 5)
        public string playerName   = "Chef";
        public bool   rateUsShown  = false;
        public int    sessionCount = 0;

        // Dinheiro total ganho (para leaderboard)
        public float totalMoney = 0f;
    }

    // Dicionário string→int serializável pelo JsonUtility
    [Serializable]
    public class SerializableDictionary : ISerializationCallbackReceiver, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string,int>>
    {
        [SerializeField] private List<string> _keys   = new List<string>();
        [SerializeField] private List<int>    _values = new List<int>();
        private Dictionary<string, int> _dict = new Dictionary<string, int>();

        public int this[string key]
        {
            get => _dict.TryGetValue(key, out int v) ? v : 0;
            set { _dict[key] = value; }
        }

        public bool ContainsKey(string key) => _dict.ContainsKey(key);
        public int GetValueOrDefault(string key, int def = 0) =>
            _dict.TryGetValue(key, out int v) ? v : def;


        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string,int>> GetEnumerator()
            => _dict.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            => _dict.GetEnumerator();

                public void OnBeforeSerialize()
        {
            _keys.Clear(); _values.Clear();
            foreach (var kv in _dict) { _keys.Add(kv.Key); _values.Add(kv.Value); }
        }

        public void OnAfterDeserialize()
        {
            _dict.Clear();
            for (int i = 0; i < Mathf.Min(_keys.Count, _values.Count); i++)
                _dict[_keys[i]] = _values[i];
        }
    }

    // Gerencia save/load via PlayerPrefs + JSON
    [DefaultExecutionOrder(-50)]
    public class SaveManager : Singleton<SaveManager>
    {
        private const string SAVE_KEY          = "PizzaTycoon_SaveData";
        private const float  AUTO_SAVE_INTERVAL= 30f;

        // Taxa de ganho offline (por segundo) — estimativa conservadora
        private const float OFFLINE_EARN_PER_SECOND = 0.5f;
        private const float MAX_OFFLINE_HOURS        = 4f;

        public GameSaveData CurrentData { get; private set; } = new GameSaveData();

        public static event Action        OnGameSaved;
        public static event Action        OnGameLoaded;
        public static event Action<float> OnOfflineEarningsApplied; // quanto ganhou offline

        protected override void Awake()
        {
            base.Awake();
            LoadGame();
        }

        private void Start()
        {
            StartCoroutine(AutoSaveRoutine());
            StartCoroutine(PlayTimeTracker());
            ApplyOfflineEarnings();
            UpdateDayStreak();
        }

        // ── Save / Load ───────────────────────────────────────────────────────

        public void SaveGame()
        {
            if (MoneyManager.Instance   != null)
            {
                CurrentData.money      = MoneyManager.Instance.CurrentMoney;
                CurrentData.totalMoney = Mathf.Max(CurrentData.totalMoney, CurrentData.money);
            }
            if (ComboSystem.Instance    != null) CurrentData.bestCombo    = Mathf.Max(CurrentData.bestCombo, ComboSystem.Instance.CurrentCombo);
            if (GameLoop.Instance       != null) CurrentData.currentPhase = GameLoop.Instance.CurrentPhase;
            if (DailyGoalSystem.Instance!= null) CurrentData.dailyGoals   = DailyGoalSystem.Instance.GetSaveData();
            if (AchievementManager.Instance != null) CurrentData.achievementProgress = new System.Collections.Generic.List<PizzaTycoon.Achievements.AchievementSaveEntry>(AchievementManager.Instance.GetSaveData());

            CurrentData.lastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            CurrentData.lastPlayDate      = DateTime.Now.ToString("yyyy-MM-dd");

            string json = JsonUtility.ToJson(CurrentData, prettyPrint: true);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();

            OnGameSaved?.Invoke();
            Debug.Log($"[SaveManager] Jogo salvo. Dinheiro: {CurrentData.money:F0}");
        }

        public void LoadGame()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY))
            {
                CurrentData = new GameSaveData();
                OnGameLoaded?.Invoke();
                return;
            }

            string json = PlayerPrefs.GetString(SAVE_KEY);
            try
            {
                CurrentData = JsonUtility.FromJson<GameSaveData>(json);
                OnGameLoaded?.Invoke();
                Debug.Log($"[SaveManager] Jogo carregado. Dinheiro: {CurrentData.money:F0}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Erro ao carregar: {e.Message}. Resetando.");
                CurrentData = new GameSaveData();
            }
        }

        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            CurrentData = new GameSaveData();
            Debug.Log("[SaveManager] Save deletado.");
        }

        // ── Helpers de campo ─────────────────────────────────────────────────

        public void UpdateUpgradeLevel(string upgradeId, int level)
        {
            switch (upgradeId)
            {
                case "playerSpeed":      CurrentData.playerSpeedLevel     = level; break;
                case "stackCapacity":    CurrentData.stackCapacityLevel   = level; break;
                case "productionSpeed":  CurrentData.productionSpeedLevel = level; break;
            }
        }

        public void UnlockStation(string stationId)
        {
            if (!CurrentData.unlockedStations.Contains(stationId))
                CurrentData.unlockedStations.Add(stationId);
        }

        public void RegisterPizzaSold()
        {
            CurrentData.totalPizzasSold++;
        }

        public void SetTutorialCompleted()
        {
            CurrentData.tutorialCompleted = true;
        }

        // ── Ganhos offline ────────────────────────────────────────────────────

        private void ApplyOfflineEarnings()
        {
            if (CurrentData.lastSaveTimestamp == 0) return;

            long nowTs      = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long elapsedSec = nowTs - CurrentData.lastSaveTimestamp;

            if (elapsedSec < 60) return; // menos de 1 minuto, ignora

            float cappedSec  = Mathf.Min(elapsedSec, MAX_OFFLINE_HOURS * 3600f);
            float earned     = cappedSec * OFFLINE_EARN_PER_SECOND;

            if (earned > 0f)
            {
                MoneyManager.Instance?.AddMoney(earned);
                OnOfflineEarningsApplied?.Invoke(earned);
                Debug.Log($"[SaveManager] Ganhos offline: ${earned:F0} ({elapsedSec}s ausente)");
            }
        }

        // ── Streak de dias ────────────────────────────────────────────────────

        private void UpdateDayStreak()
        {
            string today     = DateTime.Now.ToString("yyyy-MM-dd");
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            if (CurrentData.lastPlayDate == today) return;

            if (CurrentData.lastPlayDate == yesterday)
                CurrentData.consecutiveDaysPlayed++;
            else if (CurrentData.lastPlayDate != "")
                CurrentData.consecutiveDaysPlayed = 1;
            else
                CurrentData.consecutiveDaysPlayed = 1;

            CurrentData.lastPlayDate = today;
        }

        // ── Coroutines ────────────────────────────────────────────────────────

        private IEnumerator AutoSaveRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(AUTO_SAVE_INTERVAL);
            while (true)
            {
                yield return wait;
                if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
                    SaveGame();
            }
        }

        private IEnumerator PlayTimeTracker()
        {
            WaitForSeconds tick = new WaitForSeconds(1f);
            while (true)
            {
                yield return tick;
                if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
                    CurrentData.totalPlayTime += 1f;
            }
        }
    }
}
