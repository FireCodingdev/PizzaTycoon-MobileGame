using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PizzaTycoon.Recipes
{
    public class RecipeUI : MonoBehaviour
    {
        [Header("Painel")]
        [SerializeField] private CanvasGroup        _canvasGroup;
        [SerializeField] private Button             _closeButton;

        [Header("Grid")]
        [SerializeField] private Transform          _grid;
        [SerializeField] private RecipeCardUI       _cardPrefab;

        private void Awake()
        {
            _closeButton?.onClick.AddListener(Hide);
            RecipeSystem.OnRecipeUnlocked += _ => Refresh();
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            RecipeSystem.OnRecipeUnlocked -= _ => Refresh();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            Refresh();
            StartCoroutine(FadeIn());
        }

        public void Hide() => gameObject.SetActive(false);

        private void Refresh()
        {
            var sys = RecipeSystem.Instance;
            if (sys == null || _grid == null || _cardPrefab == null) return;

            foreach (Transform c in _grid) Destroy(c.gameObject);

            foreach (var recipe in sys.GetAll())
            {
                if (recipe == null) continue;
                var card = Instantiate(_cardPrefab, _grid);
                card.Setup(recipe, sys.IsUnlocked(recipe.recipeId), OnUnlock);
            }
        }

        private void OnUnlock(string id) => RecipeSystem.Instance?.TryUnlock(id);

        private IEnumerator FadeIn()
        {
            if (_canvasGroup == null) yield break;
            _canvasGroup.alpha = 0f;
            for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                _canvasGroup.alpha = t / 0.2f;
                yield return null;
            }
            _canvasGroup.alpha = 1f;
        }
    }

    // ── Card de receita ───────────────────────────────────────────────────────

    public class RecipeCardUI : MonoBehaviour
    {
        [SerializeField] private Image              _colorSwatch;
        [SerializeField] private TextMeshProUGUI    _nameText;
        [SerializeField] private TextMeshProUGUI    _descText;
        [SerializeField] private TextMeshProUGUI    _multiplierText;
        [SerializeField] private Button             _unlockButton;
        [SerializeField] private TextMeshProUGUI    _unlockLabel;
        [SerializeField] private Image              _checkmark;

        private System.Action<string> _onUnlock;
        private string _id;

        public void Setup(RecipeData data, bool unlocked, System.Action<string> onUnlock)
        {
            _id       = data.recipeId;
            _onUnlock = onUnlock;

            if (_colorSwatch    != null) _colorSwatch.color   = data.previewColor;
            if (_nameText       != null) _nameText.text       = data.displayName;
            if (_descText       != null) _descText.text       = data.description;
            if (_multiplierText != null) _multiplierText.text = $"x{data.valueMultiplier:F1}";
            if (_checkmark      != null) _checkmark.gameObject.SetActive(unlocked);

            if (_unlockButton != null)
            {
                _unlockButton.interactable = !unlocked && data.unlockCost >= 0;
                _unlockButton.onClick.AddListener(() => _onUnlock?.Invoke(_id));
            }
            if (_unlockLabel != null)
                _unlockLabel.text = unlocked ? "DESBLOQUEADO"
                    : data.unlockCost == 0 ? "DESBLOQUEAR"
                    : $"DESBLOQUEAR ${data.unlockCost}";
        }
    }
}
