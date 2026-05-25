using System.Collections;
using UnityEngine;
using TMPro;
using PizzaTycoon.Managers;
using PizzaTycoon.Stations;
using PizzaTycoon.Player;

namespace PizzaTycoon.GameSystems
{
    // Quadrado no chão em frente a cada estação — jogador entra e compra upgrade.
    // Não requer Inspector: configurado inteiramente por código via Init().
    public class StationUpgradePad : MonoBehaviour
    {
        // Níveis: custo em $ → intervalo de produção em segundos por item
        private static readonly float[] Costs     = { 100f,  300f,  750f, 2000f };
        private static readonly float[] Intervals = {  1.6f,  1.1f,  0.65f,  0.3f };

        private static readonly Color ColAvail = new Color(0.08f, 0.65f, 0.18f, 0.88f);
        private static readonly Color ColMaxed = new Color(0.38f, 0.38f, 0.42f, 0.70f);
        private static readonly Color ColFlash = new Color(1.00f, 0.85f, 0.06f, 1.00f);

        private BaseStation _station;
        private bool        _isLocked;      // começa bloqueado (desbloquear = nível 1)
        private int         _level;         // 0 = bloqueado / 1..4 = melhorias
        private bool        _maxed;
        private string      _prefKey;
        private Material    _padMat;
        private TextMeshPro _label;
        private bool        _playerIn;
        private float       _cooldown;
        private Coroutine   _flashCo;

        // Chamado por WorldDecorationSpawner
        public void Init(BaseStation station, Vector3 worldPos, bool startsLocked)
        {
            transform.position = worldPos;

            _station    = station;
            _isLocked   = startsLocked;
            _prefKey    = "upgpad_" + station.gameObject.GetInstanceID();
            _level      = PlayerPrefs.GetInt(_prefKey, startsLocked ? 0 : 1);
            _maxed      = _level > Costs.Length;

            if (startsLocked && _level == 0)
                station.IsActive = false; // desativa até ser desbloqueada

            if (_level > 0) ApplyLevel();
            BuildVisual();
        }

        // ── Construção visual ──────────────────────────────────────────────────

        private void BuildVisual()
        {
            // Quadrado de chão
            var quad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            quad.name = "_PT_Pad";
            quad.transform.SetParent(transform, false);
            quad.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            quad.transform.localScale    = new Vector3(1.9f, 0.05f, 1.9f);
            Destroy(quad.GetComponent<Collider>());
            _padMat       = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                       ?? Shader.Find("Standard"));
            _padMat.color = PadColor();
            quad.GetComponent<Renderer>().sharedMaterial = _padMat;

            // Borda branca fina
            var border = GameObject.CreatePrimitive(PrimitiveType.Cube);
            border.name = "_PT_PadBorder";
            border.transform.SetParent(transform, false);
            border.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            border.transform.localScale    = new Vector3(2.1f, 0.02f, 2.1f);
            Destroy(border.GetComponent<Collider>());
            var bm = new Material(_padMat);
            bm.color = new Color(1f, 1f, 1f, 0.65f);
            border.GetComponent<Renderer>().sharedMaterial = bm;

            // Label flutuante (inclinado para câmera isométrica)
            var lGO = new GameObject("PadLabel");
            lGO.transform.SetParent(transform, false);
            lGO.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            lGO.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
            _label           = lGO.AddComponent<TextMeshPro>();
            _label.fontSize  = 1.5f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontStyle = FontStyles.Bold;
            RefreshLabel();

            // Collider-trigger para detectar o jogador
            var col     = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size   = new Vector3(2.0f, 1.2f, 2.0f);
            col.center = new Vector3(0f, 0.6f, 0f);
        }

        // ── Lógica ────────────────────────────────────────────────────────────

        private void Update()
        {
            if (_cooldown > 0f) _cooldown -= Time.deltaTime;
            if (_playerIn && !_maxed && _cooldown <= 0f) TryBuy();
            if (_playerIn) RefreshLabel(); // atualiza cor "pode pagar?"
        }

        private void TryBuy()
        {
            // Custo: nível 0 → desbloquear (custo[0]), nível 1+ → melhorar
            int costIndex = Mathf.Clamp(_level, 0, Costs.Length - 1);
            float cost = Costs[costIndex];

            var money = MoneyManager.Instance;
            if (money == null || !money.CanAfford(cost)) return;
            if (!money.SpendMoney(cost)) return;

            _level++;
            _maxed = _level > Costs.Length;
            PlayerPrefs.SetInt(_prefKey, _level);

            if (_level == 1 && _isLocked)
                _station.IsActive = true; // DESBLOQUEAR

            ApplyLevel();
            RefreshLabel();
            _cooldown = 1.8f;
            if (_padMat != null) _padMat.color = PadColor();

            if (_flashCo != null) StopCoroutine(_flashCo);
            _flashCo = StartCoroutine(Flash());
            AudioManager.Instance?.PlayUpgradePurchase();
        }

        private void ApplyLevel()
        {
            if (_station == null || _level <= 0) return;
            int idx = Mathf.Clamp(_level - 1, 0, Intervals.Length - 1);
            _station.ProductionInterval = Intervals[idx];
        }

        private void RefreshLabel()
        {
            if (_label == null) return;
            if (_maxed) { _label.text = "MAX"; _label.color = new Color(0.6f, 0.6f, 0.65f); return; }

            string sName = _station != null
                ? _station.GetType().Name.Replace("Station", "").Replace("System", "")
                : "Estacao";

            int costIdx = Mathf.Clamp(_level, 0, Costs.Length - 1);
            float cost  = Costs[costIdx];
            bool canPay = MoneyManager.Instance != null && MoneyManager.Instance.CanAfford(cost);

            string action = (_isLocked && _level == 0) ? "DESBLOQUEAR" : "MELHORAR";
            _label.text  = $"<b>{action}</b>\n{sName}\n<color={(canPay ? "#FFD700" : "#FF6666")}>${cost:F0}</color>";
            _label.color = Color.white;
        }

        private Color PadColor()
        {
            if (_maxed)                       return ColMaxed;
            if (_isLocked && _level == 0)     return new Color(0.70f, 0.12f, 0.10f, 0.88f);
            return ColAvail;
        }

        private IEnumerator Flash()
        {
            if (_padMat == null) yield break;
            Color prev = _padMat.color;
            _padMat.color = ColFlash;
            yield return new WaitForSeconds(0.28f);
            _padMat.color = prev;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerController>() != null) _playerIn = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<PlayerController>() != null) _playerIn = false;
        }
    }
}
