using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PizzaTycoon.Achievements;

namespace PizzaTycoon.UI
{
    // Mostra popup quando uma achievement é desbloqueada.
    // 100% código — sem Inspector. Singleton inicializado automaticamente.
    public class AchievementPopupUI : MonoBehaviour
    {
        private static AchievementPopupUI _instance;
        private Canvas _canvas;
        private Queue<(AchievementData data, int milestone)> _pending = new();
        private bool _showing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            if (_instance != null) return;
            var go = new GameObject("[AchievementPopupUI]");
            _instance = go.AddComponent<AchievementPopupUI>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 998;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 844);
            gameObject.AddComponent<GraphicRaycaster>();
        }

        private void OnEnable()
        {
            AchievementManager.OnAchievementUnlocked += OnUnlocked;
        }

        private void OnDisable()
        {
            AchievementManager.OnAchievementUnlocked -= OnUnlocked;
        }

        private void OnUnlocked(AchievementData data, int milestoneIdx)
        {
            _pending.Enqueue((data, milestoneIdx));
            if (!_showing) StartCoroutine(ShowLoop());
        }

        private IEnumerator ShowLoop()
        {
            _showing = true;
            while (_pending.Count > 0)
            {
                var (data, idx) = _pending.Dequeue();
                yield return ShowOne(data, idx);
                yield return new WaitForSecondsRealtime(0.25f);
            }
            _showing = false;
        }

        private IEnumerator ShowOne(AchievementData data, int milestoneIdx)
        {
            // Cartão estilizado
            var card = new GameObject("Popup_" + data.id);
            card.transform.SetParent(_canvas.transform, false);
            var bg = card.AddComponent<Image>();
            UIStyleKit.StyleCard(bg, 28);
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(340f, 100f);
            rt.anchoredPosition = new Vector2(0f, 80f); // começa fora da tela

            // Sombra
            var sh = new GameObject("Sh");
            sh.transform.SetParent(card.transform, false);
            sh.transform.SetAsFirstSibling();
            var shImg = sh.AddComponent<Image>();
            UIStyleKit.ApplyRounded(shImg, new Color(0f, 0f, 0f, 0.3f), 28);
            var shRT = sh.GetComponent<RectTransform>();
            shRT.anchorMin = Vector2.zero;
            shRT.anchorMax = Vector2.one;
            shRT.offsetMin = new Vector2(2f, -8f);
            shRT.offsetMax = new Vector2(2f, -2f);

            // Ícone (círculo colorido)
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(card.transform, false);
            var iconImg = iconGo.AddComponent<Image>();
            UIStyleKit.ApplyRounded(iconImg, data.GetIconColor(), 36);
            var iconRT = iconGo.GetComponent<RectTransform>();
            iconRT.anchorMin = iconRT.anchorMax = new Vector2(0f, 0.5f);
            iconRT.sizeDelta = new Vector2(72f, 72f);
            iconRT.anchoredPosition = new Vector2(50f, 0f);

            var emoji = new GameObject("Emoji");
            emoji.transform.SetParent(iconGo.transform, false);
            var emojiTMP = emoji.AddComponent<TextMeshProUGUI>();
            emojiTMP.text      = "*";
            emojiTMP.fontSize  = 42;
            emojiTMP.fontStyle = FontStyles.Bold;
            emojiTMP.color     = Color.white;
            emojiTMP.alignment = TextAlignmentOptions.Center;
            var emojiRT = emoji.GetComponent<RectTransform>();
            emojiRT.anchorMin = Vector2.zero;
            emojiRT.anchorMax = Vector2.one;
            emojiRT.offsetMin = emojiRT.offsetMax = Vector2.zero;

            // Texto
            UIStyleKit.MakeText(card.transform, "Conquista desbloqueada!", 13,
                UIStyleKit.OrangeDark, new Vector2(30f, 25f), new Vector2(230f, 20f));
            var title = UIStyleKit.MakeText(card.transform, data.title, 18,
                UIStyleKit.TextDark, new Vector2(30f, 5f), new Vector2(230f, 25f));
            title.fontStyle = FontStyles.Bold;

            int reward = milestoneIdx < data.coinRewards.Length
                ? data.coinRewards[milestoneIdx] : 50;
            UIStyleKit.MakeText(card.transform, $"+${reward}", 16,
                UIStyleKit.Green, new Vector2(30f, -22f), new Vector2(230f, 20f))
                .fontStyle = FontStyles.Bold;

            // Slide in
            Vector2 start  = new Vector2(0f, 80f);
            Vector2 target = new Vector2(0f, -90f);
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                rt.anchoredPosition = Vector2.Lerp(start, target,
                    1f - Mathf.Pow(1f - t / 0.4f, 3f));
                yield return null;
            }
            rt.anchoredPosition = target;

            yield return new WaitForSecondsRealtime(2.2f);

            // Slide out
            t = 0f;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                rt.anchoredPosition = Vector2.Lerp(target, start, t / 0.3f);
                yield return null;
            }

            Destroy(card);
        }
    }
}
