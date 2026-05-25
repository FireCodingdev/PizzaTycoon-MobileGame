using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PizzaTycoon.Utils;

namespace PizzaTycoon.Accessibility
{
    public enum UISize       { Normal, Large, ExtraLarge }
    public enum AnimSpeed    { Normal, Reduced, None }

    // Gerencia configurações de acessibilidade — salvas em PlayerPrefs
    public class AccessibilityManager : Singleton<AccessibilityManager>
    {
        private const string KEY_UI_SIZE    = "Acc_UISize";
        private const string KEY_ANIM_SPEED = "Acc_AnimSpeed";
        private const string KEY_CONTRAST   = "Acc_HighContrast";
        private const string KEY_FONT_SCALE = "Acc_FontScale";
        private const string KEY_HAPTICS    = "Acc_Haptics";

        // Escala de referência por modo UI
        private static readonly float[] UISizeMultipliers = { 1.0f, 1.25f, 1.5f };

        public UISize    CurrentUISize    { get; private set; }
        public AnimSpeed CurrentAnimSpeed { get; private set; }
        public bool      HighContrast     { get; private set; }
        public float     FontScale        { get; private set; } = 1f; // 0.8–1.5
        public bool      HapticsEnabled   { get; private set; } = true;

        public static event Action OnSettingsChanged;

        protected override void Awake()
        {
            base.Awake();
            LoadSettings();
        }

        private void Start()
        {
            ApplyAll();
        }

        // ── Load / Save ───────────────────────────────────────────────────────

        private void LoadSettings()
        {
            CurrentUISize    = (UISize)   PlayerPrefs.GetInt(KEY_UI_SIZE,    0);
            CurrentAnimSpeed = (AnimSpeed) PlayerPrefs.GetInt(KEY_ANIM_SPEED, 0);
            HighContrast     = PlayerPrefs.GetInt(KEY_CONTRAST,  0) == 1;
            FontScale        = PlayerPrefs.GetFloat(KEY_FONT_SCALE, 1f);
            HapticsEnabled   = PlayerPrefs.GetInt(KEY_HAPTICS,   1) == 1;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt(KEY_UI_SIZE,     (int)CurrentUISize);
            PlayerPrefs.SetInt(KEY_ANIM_SPEED,  (int)CurrentAnimSpeed);
            PlayerPrefs.SetInt(KEY_CONTRAST,    HighContrast ? 1 : 0);
            PlayerPrefs.SetFloat(KEY_FONT_SCALE, FontScale);
            PlayerPrefs.SetInt(KEY_HAPTICS,     HapticsEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ── Setters públicos ─────────────────────────────────────────────────

        public void SetUISize(UISize size)
        {
            CurrentUISize = size;
            ApplyUISize();
            Save();
        }

        public void SetAnimSpeed(AnimSpeed speed)
        {
            CurrentAnimSpeed = speed;
            ApplyAnimSpeed();
            Save();
        }

        public void SetHighContrast(bool enabled)
        {
            HighContrast = enabled;
            ApplyContrast();
            Save();
        }

        public void SetFontScale(float scale)
        {
            FontScale = Mathf.Clamp(scale, 0.8f, 1.5f);
            ApplyFontScale();
            Save();
        }

        public void SetHaptics(bool enabled)
        {
            HapticsEnabled = enabled;
            Save();
        }

        // Vibração (haptic feedback) — use este método em vez de Handheld.Vibrate() diretamente
        public void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (HapticsEnabled) Handheld.Vibrate();
#endif
        }

        // ── Aplicação ─────────────────────────────────────────────────────────

        public void ApplyAll()
        {
            ApplyUISize();
            ApplyAnimSpeed();
            ApplyContrast();
            ApplyFontScale();
        }

        private void ApplyUISize()
        {
            float mult = UISizeMultipliers[(int)CurrentUISize];
            // Ajusta todos os CanvasScalers na cena
            foreach (CanvasScaler scaler in FindObjectsOfType<CanvasScaler>())
            {
                scaler.referenceResolution = new Vector2(
                    Mathf.RoundToInt(1080f / mult),
                    Mathf.RoundToInt(1920f / mult));
            }
            OnSettingsChanged?.Invoke();
        }

        private void ApplyAnimSpeed()
        {
            float scale = CurrentAnimSpeed switch
            {
                AnimSpeed.Normal  => 1.0f,
                AnimSpeed.Reduced => 0.5f,
                AnimSpeed.None    => 0.0f,
                _                 => 1.0f
            };
            Time.timeScale = scale > 0f ? scale : Time.timeScale;
            // Nota: usar Time.unscaledDeltaTime em animações de UI para manter responsividade
        }

        private void ApplyContrast()
        {
            // Satura/dessatura todas as câmeras — implementação simples sem pós-processamento
            float saturation = HighContrast ? 1.5f : 1f;
            // Com URP Post Processing: use ColorAdjustments.saturation
            // Sem pós-pro: aplicar contraste via Material em tela inteira (não implementado aqui)
            Debug.Log($"[Accessibility] Alto contraste: {HighContrast} (saturation={saturation})");
            OnSettingsChanged?.Invoke();
        }

        private void ApplyFontScale()
        {
            // Ajusta fontSize de todos os TextMeshPro na cena
            foreach (TextMeshProUGUI tmp in FindObjectsOfType<TextMeshProUGUI>())
            {
                // Preserva proporção relativa — usa um base size fixo escalado
                // (idealmente todos os TMP teriam um BaseSize salvo, mas simplificamos aqui)
                if (tmp.GetComponent<Localization.LocalizedText>() != null)
                    tmp.fontSizeMax = Mathf.RoundToInt(tmp.fontSize * FontScale);
            }
            OnSettingsChanged?.Invoke();
        }

        private void Save()
        {
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }
    }
}
