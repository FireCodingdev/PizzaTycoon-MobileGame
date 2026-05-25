using System;
using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Utils;
using PizzaTycoon.Managers;

namespace PizzaTycoon.Leaderboard
{
    public enum LeaderboardCategory { TotalCoins, PizzasSold, BestCombo }

    [Serializable]
    public class LeaderboardEntry
    {
        public string playerName;
        public long   score;
        public string category;
        public string date; // "yyyy-MM-dd"
    }

    // Ranking offline — top 10 por categoria, preenchido com rivais simulados
    public class LeaderboardManager : Singleton<LeaderboardManager>
    {
        private const int MaxEntries = 10;

        // Rivais simulados por categoria
        private static readonly (string name, long score)[] _rivalCoins =
        {
            ("PizzaKing",    500_000), ("MammaRosa",  420_000), ("ItaloBoy",   380_000),
            ("ChefGordon",   310_000), ("NapoliPro",  270_000), ("TonyPizza",  230_000),
            ("LauraGourmet", 195_000), ("FabrizioX",  150_000), ("MarcoLento",  90_000),
            ("GiovanniN",    40_000)
        };
        private static readonly (string name, long score)[] _rivalSold =
        {
            ("PizzaKing",  2000), ("MammaRosa",  1750), ("ItaloBoy",   1500),
            ("ChefGordon", 1200), ("NapoliPro",  1000), ("TonyPizza",   850),
            ("LauraGourmet",700), ("FabrizioX",   550), ("MarcoLento",  300),
            ("GiovanniN",   120)
        };
        private static readonly (string name, long score)[] _rivalCombo =
        {
            ("PizzaKing",  50), ("MammaRosa",  45), ("ItaloBoy",   40),
            ("ChefGordon", 35), ("NapoliPro",  32), ("TonyPizza",  28),
            ("LauraGourmet",25),("FabrizioX",  22), ("MarcoLento", 15),
            ("GiovanniN",  10)
        };

        private Dictionary<LeaderboardCategory, List<LeaderboardEntry>> _boards = new();

        protected override void Awake()
        {
            base.Awake();
            InitBoards();
            LoadFromSave();
        }

        private void InitBoards()
        {
            _boards[LeaderboardCategory.TotalCoins]  = BuildRivalList(_rivalCoins,  "TotalCoins");
            _boards[LeaderboardCategory.PizzasSold]  = BuildRivalList(_rivalSold,   "PizzasSold");
            _boards[LeaderboardCategory.BestCombo]   = BuildRivalList(_rivalCombo,  "BestCombo");
        }

        private List<LeaderboardEntry> BuildRivalList((string name, long score)[] rivals, string cat)
        {
            var list = new List<LeaderboardEntry>();
            foreach (var (name, score) in rivals)
                list.Add(new LeaderboardEntry { playerName = name, score = score, category = cat, date = "2026-01-01" });
            return list;
        }

        // ── Submissão ─────────────────────────────────────────────────────────

        public void Submit(LeaderboardCategory cat, long score, string playerName = null)
        {
            playerName ??= SaveManager.Instance?.CurrentData?.playerName ?? "Você";

            var board = _boards[cat];
            var entry = new LeaderboardEntry
            {
                playerName = playerName,
                score      = score,
                category   = cat.ToString(),
                date       = DateTime.Now.ToString("yyyy-MM-dd")
            };

            // Remove entrada prévia do jogador na mesma categoria
            board.RemoveAll(e => e.playerName == playerName && e.category == cat.ToString());
            board.Add(entry);
            board.Sort((a, b) => b.score.CompareTo(a.score));
            if (board.Count > MaxEntries) board.RemoveAt(board.Count - 1);

            SaveToData();
        }

        public List<LeaderboardEntry> GetTop(LeaderboardCategory cat) =>
            _boards.TryGetValue(cat, out var b) ? b : new List<LeaderboardEntry>();

        public int GetPlayerRank(LeaderboardCategory cat, string playerName = null)
        {
            playerName ??= SaveManager.Instance?.CurrentData?.playerName ?? "Você";
            var board = GetTop(cat);
            for (int i = 0; i < board.Count; i++)
                if (board[i].playerName == playerName) return i + 1;
            return -1;
        }

        // ── Auto-submit ao fechar sessão ──────────────────────────────────────

        public void SubmitCurrentStats()
        {
            var data = SaveManager.Instance?.CurrentData;
            if (data == null) return;
            Submit(LeaderboardCategory.TotalCoins, (long)data.totalMoney);
            Submit(LeaderboardCategory.PizzasSold, data.totalPizzasSold);
            Submit(LeaderboardCategory.BestCombo,  data.bestCombo);
        }

        // ── Save / Load ───────────────────────────────────────────────────────

        private void SaveToData()
        {
            var data = SaveManager.Instance?.CurrentData;
            if (data == null) return;
            var all = new List<LeaderboardEntry>();
            foreach (var kv in _boards) all.AddRange(kv.Value);
            data.leaderboardEntries = all;
        }

        private void LoadFromSave()
        {
            var data = SaveManager.Instance?.CurrentData;
            if (data?.leaderboardEntries == null || data.leaderboardEntries.Count == 0) return;

            foreach (var entry in data.leaderboardEntries)
            {
                if (!Enum.TryParse<LeaderboardCategory>(entry.category, out var cat)) continue;
                if (!_boards.ContainsKey(cat)) continue;

                var board = _boards[cat];
                board.RemoveAll(e => e.playerName == entry.playerName && e.category == entry.category);
                board.Add(entry);
                board.Sort((a, b) => b.score.CompareTo(a.score));
            }
        }
    }
}
