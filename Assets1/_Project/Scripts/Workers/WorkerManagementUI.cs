using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PizzaTycoon.Workers
{
    public class WorkerManagementUI : MonoBehaviour
    {
        [Header("Painel")]
        [SerializeField] private CanvasGroup        _canvasGroup;
        [SerializeField] private Button             _closeButton;

        [Header("Grid de workers")]
        [SerializeField] private Transform          _grid;
        [SerializeField] private WorkerCardUI       _cardPrefab;

        private void Awake()
        {
            _closeButton?.onClick.AddListener(Hide);
            WorkerManager.OnWorkerHired    += _ => Refresh();
            WorkerManager.OnWorkerUpgraded += (_, __) => Refresh();
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            WorkerManager.OnWorkerHired    -= _ => Refresh();
            WorkerManager.OnWorkerUpgraded -= (_, __) => Refresh();
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
            var mgr = WorkerManager.Instance;
            if (mgr == null || _grid == null || _cardPrefab == null) return;

            foreach (Transform c in _grid) Destroy(c.gameObject);

            foreach (var data in mgr.GetAll())
            {
                if (data == null) continue;
                var card = Instantiate(_cardPrefab, _grid);
                card.Setup(data, mgr.IsHired(data.workerId), mgr.GetLevel(data.workerId),
                           OnHire, OnUpgrade);
            }
        }

        private void OnHire(string id)    => WorkerManager.Instance?.TryHire(id);
        private void OnUpgrade(string id) => WorkerManager.Instance?.TryUpgrade(id);

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

    // ── Card de worker ────────────────────────────────────────────────────────

    public class WorkerCardUI : MonoBehaviour
    {
        [SerializeField] private Image              _colorBadge;
        [SerializeField] private TextMeshProUGUI    _nameText;
        [SerializeField] private TextMeshProUGUI    _typeText;
        [SerializeField] private TextMeshProUGUI    _levelText;
        [SerializeField] private TextMeshProUGUI    _speedText;
        [SerializeField] private Button             _hireButton;
        [SerializeField] private TextMeshProUGUI    _hireLabel;
        [SerializeField] private Button             _upgradeButton;
        [SerializeField] private TextMeshProUGUI    _upgradeLabel;

        private System.Action<string> _onHire;
        private System.Action<string> _onUpgrade;
        private string _id;

        public void Setup(WorkerData data, bool hired, int level,
                          System.Action<string> onHire, System.Action<string> onUpgrade)
        {
            _id        = data.workerId;
            _onHire    = onHire;
            _onUpgrade = onUpgrade;

            if (_colorBadge != null) _colorBadge.color = data.uniformColor;
            if (_nameText   != null) _nameText.text    = data.displayName;
            if (_typeText   != null) _typeText.text    = data.type.ToString();
            if (_levelText  != null) _levelText.text   = hired ? $"Nível {level}" : "Não contratado";
            if (_speedText  != null) _speedText.text   = hired ? $"Vel: {data.GetSpeed(level):F1}" : "";

            if (_hireButton != null)
            {
                _hireButton.gameObject.SetActive(!hired);
                _hireButton.onClick.AddListener(() => _onHire?.Invoke(_id));
            }
            if (_hireLabel    != null) _hireLabel.text    = $"CONTRATAR ${data.hiringCost}";

            bool maxed = level >= data.maxLevel;
            if (_upgradeButton != null)
            {
                _upgradeButton.gameObject.SetActive(hired);
                _upgradeButton.interactable = !maxed;
                _upgradeButton.onClick.AddListener(() => _onUpgrade?.Invoke(_id));
            }
            if (_upgradeLabel != null)
                _upgradeLabel.text = maxed ? "MÁXIMO" : $"UP ${data.GetUpgradeCost(level)}";
        }
    }
}
