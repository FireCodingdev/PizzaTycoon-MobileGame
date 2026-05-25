using UnityEngine;
using TMPro;

namespace PizzaTycoon.Localization
{
    // Adicione em qualquer TextMeshPro para localização automática
    // O texto será atualizado ao trocar idioma ou ao reativar o objeto
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _localizationKey;

        // Argumentos de formatação ({0}, {1}) — deixe vazio se não houver
        [SerializeField] private string[] _formatArgs;

        private TextMeshProUGUI _text;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= Refresh;
        }

        private void Refresh()
        {
            if (_text == null || string.IsNullOrEmpty(_localizationKey)) return;

            if (_formatArgs != null && _formatArgs.Length > 0)
                _text.text = LocalizationManager.LF(_localizationKey, (object[])_formatArgs);
            else
                _text.text = LocalizationManager.L(_localizationKey);
        }

        // Permite definir key e args em runtime
        public void SetKey(string key, params string[] args)
        {
            _localizationKey = key;
            _formatArgs      = args;
            Refresh();
        }
    }
}
