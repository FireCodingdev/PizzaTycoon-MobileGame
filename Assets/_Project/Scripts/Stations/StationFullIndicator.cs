using UnityEngine;
using TMPro;

namespace PizzaTycoon.Stations
{
    // Exibe label "MAX" em WorldSpace acima da estacao quando ela esta cheia.
    [RequireComponent(typeof(BaseStation))]
    public class StationFullIndicator : MonoBehaviour
    {
        private BaseStation _station;
        private TextMeshPro _label;
        private GameObject  _labelGO;

        private void Awake()
        {
            _station = GetComponent<BaseStation>();
            BuildLabel();
        }

        private void BuildLabel()
        {
            _labelGO = new GameObject("MAX_Label");
            _labelGO.transform.SetParent(transform);
            _labelGO.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            _labelGO.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);

            _label            = _labelGO.AddComponent<TextMeshPro>();
            _label.text       = "MAX";
            _label.fontSize   = 2.5f;
            _label.fontStyle  = FontStyles.Bold;
            _label.color      = new Color(1f, 0.22f, 0.22f);
            _label.alignment  = TextAlignmentOptions.Center;

            _labelGO.SetActive(false);
        }

        private void Update()
        {
            if (_station == null || _labelGO == null) return;
            bool full = _station.IsFull;
            if (_labelGO.activeSelf != full)
                _labelGO.SetActive(full);
        }
    }
}
