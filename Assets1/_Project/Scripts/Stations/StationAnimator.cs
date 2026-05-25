using System.Collections;
using UnityEngine;

namespace PizzaTycoon.Stations
{
    // Animações visuais das estações via código — sem Animator clips
    // Cada método static recebe a Transform relevante e executa a animação na coroutine do owner.
    public class StationAnimator : MonoBehaviour
    {
        [Header("DoughStation — Rolo giratório")]
        [SerializeField] private Transform _rollerTransform;
        [SerializeField] private float     _rollerRpm = 120f;

        [Header("OvenStation — Luz pulsante")]
        [SerializeField] private Light _ovenLight;
        [SerializeField] private Color _ovenColdColor = new Color(0.4f, 0.4f, 0.4f);
        [SerializeField] private Color _ovenHotColor  = new Color(1f, 0.4f, 0.1f);
        [SerializeField] private float _pulsePeriod   = 1.2f;

        [Header("DeliveryStation — Seta saltitante")]
        [SerializeField] private Transform _arrowTransform;
        [SerializeField] private float     _arrowBounceAmp  = 0.12f;
        [SerializeField] private float     _arrowBounceHz   = 2.0f;

        [Header("WheatField — Crescimento")]
        [SerializeField] private Transform[] _wheatStalks;
        [SerializeField] private float       _growthDuration = 1.0f;

        private bool  _ovenActive;
        private float _ovenCookProgress; // 0..1

        private Vector3 _arrowBasePos;

        private void Start()
        {
            if (_arrowTransform != null)
                _arrowBasePos = _arrowTransform.localPosition;
        }

        private void Update()
        {
            SpinRoller();
            PulseOvenLight();
            BounceArrow();
        }

        // ── DoughStation: rolo gira enquanto ativo ───────────────────────────

        private void SpinRoller()
        {
            if (_rollerTransform == null) return;
            _rollerTransform.Rotate(Vector3.right, _rollerRpm * 6f * Time.deltaTime);
        }

        // ── OvenStation: luz muda cor conforme progresso e pulsa ────────────

        public void SetOvenProgress(float t)
        {
            _ovenCookProgress = Mathf.Clamp01(t);
            _ovenActive       = t > 0f;
        }

        private void PulseOvenLight()
        {
            if (_ovenLight == null || !_ovenActive) return;

            float pulse = (Mathf.Sin(Time.time * Mathf.PI * 2f / _pulsePeriod) + 1f) * 0.5f;
            Color base_  = Color.Lerp(_ovenColdColor, _ovenHotColor, _ovenCookProgress);
            _ovenLight.color     = Color.Lerp(base_ * 0.7f, base_, pulse);
            _ovenLight.intensity = Mathf.Lerp(0.4f, 1.2f, pulse * _ovenCookProgress);
        }

        // ── DeliveryStation: seta salta verticalmente ────────────────────────

        private void BounceArrow()
        {
            if (_arrowTransform == null) return;

            float y = _arrowBasePos.y + Mathf.Abs(Mathf.Sin(Time.time * _arrowBounceHz * Mathf.PI)) * _arrowBounceAmp;
            _arrowTransform.localPosition = new Vector3(_arrowBasePos.x, y, _arrowBasePos.z);
        }

        // ── WheatField: anima crescimento dos talos ──────────────────────────

        public void AnimateWheatGrowth()
        {
            if (_wheatStalks == null || _wheatStalks.Length == 0) return;
            foreach (Transform stalk in _wheatStalks)
                StartCoroutine(GrowStalk(stalk));
        }

        private IEnumerator GrowStalk(Transform stalk)
        {
            if (stalk == null) yield break;

            Vector3 fullScale = stalk.localScale;
            stalk.localScale  = new Vector3(fullScale.x, 0f, fullScale.z);

            float elapsed = 0f;
            while (elapsed < _growthDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _growthDuration);
                stalk.localScale = new Vector3(fullScale.x, fullScale.y * t, fullScale.z);
                yield return null;
            }
            stalk.localScale = fullScale;
        }

        // ── Helper: punch scale em qualquer transform ─────────────────────────

        public void PunchScale(Transform target, float peakScale = 1.25f, float duration = 0.2f)
        {
            StartCoroutine(PunchScaleRoutine(target, peakScale, duration));
        }

        private IEnumerator PunchScaleRoutine(Transform target, float peak, float duration)
        {
            if (target == null) yield break;

            Vector3 original = target.localScale;
            float   half     = duration * 0.5f;

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                target.localScale = Vector3.Lerp(original, original * peak, t / half);
                yield return null;
            }
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                target.localScale = Vector3.Lerp(original * peak, original, t / half);
                yield return null;
            }
            target.localScale = original;
        }
    }
}
