using System;
using System.Collections;
using UnityEngine;
using PizzaTycoon.Utils;
using PizzaTycoon.Managers;

namespace PizzaTycoon.Social
{
    // Gerencia compartilhamento, códigos de indicação e avaliação do app
    public class SocialManager : Singleton<SocialManager>
    {
        [Header("Configuração")]
        [SerializeField] private int  _rateUsMinSessions = 5;
        [SerializeField] private int  _rateUsMinPizzas   = 50;
        [SerializeField] private int  _referralBonusCoins = 200;

        [Header("UI de avaliação")]
        [SerializeField] private GameObject _rateUsPanel;

        private const string StoreURL_Android = "https://play.google.com/store/apps/details?id=com.yourcompany.pizzatycoon";
        private const string StoreURL_iOS     = "https://apps.apple.com/app/pizza-tycoon/id0000000000";

        public static event Action OnReferralApplied;

        protected override void Awake()
        {
            base.Awake();
            if (_rateUsPanel != null) _rateUsPanel.SetActive(false);
        }

        private void Start()
        {
            IncrementSessionCount();
            CheckRateUs();
        }

        // ── Compartilhamento ──────────────────────────────────────────────────

        public void ShareScore()
        {
            var data = SaveManager.Instance?.CurrentData;
            if (data == null) return;

            string text = $"Estou jogando Pizza Tycoon! Minha pizzaria já tem ${data.totalMoney:N0} e vendi {data.totalPizzasSold} pizzas! 🍕 Venha jogar também: {StoreURL_Android}";
            ShareText(text);
        }

        public void ShareScreenshot()
        {
            StartCoroutine(CaptureAndShare());
        }

        private IEnumerator CaptureAndShare()
        {
            yield return new WaitForEndOfFrame();

            string path = System.IO.Path.Combine(Application.persistentDataPath, "screenshot.png");
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSeconds(0.5f);

            // Plataformas reais usariam NativeShare ou plugins; aqui compartilhamos URL
            ShareText($"Minha pizzaria no Pizza Tycoon! 🍕 Baixe agora: {StoreURL_Android}");
            Debug.Log($"[SocialManager] Screenshot salvo em {path}");
        }

        private void ShareText(string text)
        {
#if UNITY_ANDROID
            using (var intent = new AndroidJavaObject("android.content.Intent"))
            {
                intent.Call<AndroidJavaObject>("setAction", "android.intent.action.SEND");
                intent.Call<AndroidJavaObject>("setType", "text/plain");
                intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", text);
                var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity    = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                var chooser     = AndroidJavaObject.CallStaticJavaObject(
                    "android.content.Intent", "createChooser", intent, "Compartilhar");
                activity.Call("startActivity", chooser);
            }
#else
            Debug.Log($"[SocialManager] Share (Editor): {text}");
            GUIUtility.systemCopyBuffer = text;
#endif
        }

        // ── Códigos de indicação ──────────────────────────────────────────────

        public string GenerateReferralCode()
        {
            var data = SaveManager.Instance?.CurrentData;
            string name = data?.playerName ?? "PIZZA";
            string code = $"{name.ToUpper().Substring(0, Mathf.Min(4, name.Length))}{UnityEngine.Random.Range(1000, 9999)}";
            return code;
        }

        public void ApplyReferralCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return;

            // Em produção: validar código via servidor; aqui sempre concede bônus
            MoneyManager.Instance?.AddMoney(_referralBonusCoins);
            UI.FloatingText.Show($"+${_referralBonusCoins} Indicação!", Vector3.zero, UI.FloatingTextType.Unlock);
            OnReferralApplied?.Invoke();
            Debug.Log($"[SocialManager] Código de indicação aplicado: {code} → +{_referralBonusCoins} coins");
        }

        // ── Avaliação ─────────────────────────────────────────────────────────

        private void IncrementSessionCount()
        {
            var data = SaveManager.Instance?.CurrentData;
            if (data != null) data.sessionCount++;
        }

        private void CheckRateUs()
        {
            var data = SaveManager.Instance?.CurrentData;
            if (data == null || data.rateUsShown) return;
            if (data.sessionCount < _rateUsMinSessions) return;
            if (data.totalPizzasSold < _rateUsMinPizzas) return;

            ShowRateUsPrompt();
        }

        public void ShowRateUsPrompt()
        {
            if (_rateUsPanel != null) _rateUsPanel.SetActive(true);
        }

        public void OnRateUsAccepted()
        {
            var data = SaveManager.Instance?.CurrentData;
            if (data != null) data.rateUsShown = true;

            if (_rateUsPanel != null) _rateUsPanel.SetActive(false);

            string url = Application.platform == RuntimePlatform.IPhonePlayer
                ? StoreURL_iOS : StoreURL_Android;
            Application.OpenURL(url);
        }

        public void OnRateUsDismissed()
        {
            var data = SaveManager.Instance?.CurrentData;
            if (data != null) data.rateUsShown = true;
            if (_rateUsPanel != null) _rateUsPanel.SetActive(false);
        }
    }
}
