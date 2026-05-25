using System.Collections;
using UnityEngine;
using PizzaTycoon.Utils;

namespace PizzaTycoon.Performance
{
    public enum DeviceTier { Low, Mid, High }

    // Detecta tier do dispositivo e ajusta qualidade automaticamente
    // Auto-rebaixa tier se FPS médio cair abaixo de 25 por 3s consecutivos
    public class PerformanceManager : Singleton<PerformanceManager>
    {
        private const string TIER_PREF_KEY = "DeviceTier";

        [Header("Monitoramento")]
        [SerializeField] private float _fpsCheckInterval = 1f;
        [SerializeField] private float _lowFpsThreshold  = 25f;
        [SerializeField] private float _lowFpsWindow     = 3f;   // segundos consecutivos

        public DeviceTier CurrentTier { get; private set; }

        private float _lowFpsTimer;
        private float _fpsAccum;
        private int   _fpsSamples;

        protected override void Awake()
        {
            base.Awake();
            CurrentTier = LoadOrDetectTier();
            ApplyTier(CurrentTier);
        }

        private void Start()
        {
            StartCoroutine(MonitorFPS());
        }

        // ── Detecção ──────────────────────────────────────────────────────────

        private DeviceTier LoadOrDetectTier()
        {
            if (PlayerPrefs.HasKey(TIER_PREF_KEY))
                return (DeviceTier)PlayerPrefs.GetInt(TIER_PREF_KEY);

            DeviceTier tier = Detect();
            PlayerPrefs.SetInt(TIER_PREF_KEY, (int)tier);
            return tier;
        }

        public static DeviceTier Detect()
        {
            int ram = SystemInfo.systemMemorySize;    // MB
            int gpu = SystemInfo.graphicsMemorySize;  // MB

            // Low-end: < 3 GB RAM ou < 1 GB VRAM
            if (ram < 3000 || gpu < 1000) return DeviceTier.Low;
            // High-end: >= 6 GB RAM e >= 3 GB VRAM
            if (ram >= 6000 && gpu >= 3000) return DeviceTier.High;
            return DeviceTier.Mid;
        }

        // ── Aplicação de qualidade ─────────────────────────────────────────────

        public void ApplyTier(DeviceTier tier)
        {
            CurrentTier = tier;
            switch (tier)
            {
                case DeviceTier.Low:  ApplyLow();  break;
                case DeviceTier.Mid:  ApplyMid();  break;
                case DeviceTier.High: ApplyHigh(); break;
            }
            Debug.Log($"[Performance] Tier: {tier} | RAM: {SystemInfo.systemMemorySize}MB | GPU: {SystemInfo.graphicsMemorySize}MB");
        }

        private void ApplyLow()
        {
            QualitySettings.SetQualityLevel(0, applyExpensiveChanges: true);
            Application.targetFrameRate  = 30;
            QualitySettings.shadows      = ShadowQuality.Disable;
            QualitySettings.antiAliasing = 0;
            if (UnityEngine.Camera.main != null) UnityEngine.Camera.main.farClipPlane = 40f;
            ReduceParticleEmission(0.5f);
        }

        private void ApplyMid()
        {
            QualitySettings.SetQualityLevel(2, applyExpensiveChanges: true);
            Application.targetFrameRate  = 60;
            QualitySettings.shadows      = ShadowQuality.HardOnly;
            QualitySettings.antiAliasing = 0;
            if (UnityEngine.Camera.main != null) UnityEngine.Camera.main.farClipPlane = 60f;
            ReduceParticleEmission(1f);
        }

        private void ApplyHigh()
        {
            QualitySettings.SetQualityLevel(4, applyExpensiveChanges: true);
            // API moderna RefreshRateRatio substitui refreshRate em Unity 2022.2+
            var rate = Screen.currentResolution.refreshRateRatio;
            float hz = rate.denominator > 0 ? (float)rate.numerator / rate.denominator : 60f;
            Application.targetFrameRate  = hz >= 120f ? 120 : 60;
            QualitySettings.shadows      = ShadowQuality.All;
            QualitySettings.antiAliasing = 2;
            if (UnityEngine.Camera.main != null) UnityEngine.Camera.main.farClipPlane = 80f;
            ReduceParticleEmission(1.5f);
        }

        // ── Partículas ───────────────────────────────────────────────────────

        private void ReduceParticleEmission(float multiplier)
        {
            // Aplica em todos os ParticleSystems ativos na cena
            foreach (ParticleSystem ps in FindObjectsOfType<ParticleSystem>())
            {
                var emission = ps.emission;
                var rate     = emission.rateOverTime;
                // Escala apenas sistemas com emissão contínua (não bursts)
                if (rate.constant > 0)
                {
                    emission.rateOverTime = new ParticleSystem.MinMaxCurve(rate.constant * multiplier);
                }
            }
        }

        // ── Monitoramento de FPS ──────────────────────────────────────────────

        private IEnumerator MonitorFPS()
        {
            WaitForSeconds wait = new WaitForSeconds(_fpsCheckInterval);
            while (true)
            {
                yield return wait;
                float fps = 1f / Time.unscaledDeltaTime;

                if (fps < _lowFpsThreshold)
                {
                    _lowFpsTimer += _fpsCheckInterval;
                    if (_lowFpsTimer >= _lowFpsWindow)
                    {
                        _lowFpsTimer = 0f;
                        TryDowngrade();
                    }
                }
                else
                {
                    _lowFpsTimer = 0f;
                }
            }
        }

        private void TryDowngrade()
        {
            if (CurrentTier == DeviceTier.Low) return;
            DeviceTier newTier = CurrentTier - 1;
            Debug.LogWarning($"[Performance] FPS baixo — rebaixando para {newTier}");
            PlayerPrefs.SetInt(TIER_PREF_KEY, (int)newTier);
            ApplyTier(newTier);
        }
    }
}
