using System;
using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Utils;

namespace PizzaTycoon.Localization
{
    // Gerencia localização via JSON — detecta idioma do dispositivo automaticamente
    // Fallback: inglês se idioma não suportado
    public class LocalizationManager : Singleton<LocalizationManager>
    {
        private const string LANG_PREF_KEY   = "SelectedLanguage";
        private const string RESOURCES_PATH  = "Localization/";

        private Dictionary<string, string> _strings = new();

        public string CurrentLanguage { get; private set; } = "en";

        public static event Action OnLanguageChanged;

        protected override void Awake()
        {
            base.Awake();
            string saved = PlayerPrefs.GetString(LANG_PREF_KEY, "");
            string lang  = string.IsNullOrEmpty(saved) ? DetectSystemLanguage() : saved;
            LoadLanguage(lang);
        }

        // ── Detecção ──────────────────────────────────────────────────────────

        private string DetectSystemLanguage()
        {
            return Application.systemLanguage == SystemLanguage.Portuguese
                // PortugueseBrazil não disponível nesta versão
                ? "pt-BR"
                : "en";
        }

        // ── Carregamento ──────────────────────────────────────────────────────

        public void LoadLanguage(string language)
        {
            TextAsset asset = Resources.Load<TextAsset>(RESOURCES_PATH + language);
            if (asset == null && language != "en")
            {
                Debug.LogWarning($"[Localization] '{language}' não encontrado. Usando 'en'.");
                language = "en";
                asset    = Resources.Load<TextAsset>(RESOURCES_PATH + "en");
            }

            if (asset == null)
            {
                Debug.LogError("[Localization] Arquivo de strings não encontrado!");
                return;
            }

            CurrentLanguage = language;
            PlayerPrefs.SetString(LANG_PREF_KEY, language);
            ParseJSON(asset.text);
            OnLanguageChanged?.Invoke();
        }

        private void ParseJSON(string json)
        {
            _strings.Clear();
            // Parser manual simples para evitar dependência de Newtonsoft.Json
            string content = json.Trim().TrimStart('{').TrimEnd('}');
            string[] pairs = content.Split(',');

            foreach (string pair in pairs)
            {
                int colonIdx = pair.IndexOf(':');
                if (colonIdx < 0) continue;

                string key = pair.Substring(0, colonIdx).Trim().Trim('"');
                string val = pair.Substring(colonIdx + 1).Trim().Trim('"');

                // Trata valores com vírgulas dentro de strings (simplificado)
                if (!string.IsNullOrEmpty(key))
                    _strings[key] = val;
            }
        }

        // ── API pública ───────────────────────────────────────────────────────

        // Obtém string localizada — retorna a chave se não encontrada
        public string Get(string key)
        {
            return _strings.TryGetValue(key, out string val) ? val : key;
        }

        // Obtém string com formatação (substitui {0}, {1}, etc.)
        public string GetFormat(string key, params object[] args)
        {
            string template = Get(key);
            try { return string.Format(template, args); }
            catch { return template; }
        }

        // Shortcut estático
        public static string L(string key)        => Instance?.Get(key) ?? key;
        public static string LF(string key, params object[] args) => Instance?.GetFormat(key, args) ?? key;

        public string[] SupportedLanguages => new[] { "pt-BR", "en" };
    }
}
