using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PizzaTycoon.Utils;

namespace PizzaTycoon.GameSystems
{
    // Tutorial em 6 passos com seta no mundo + texto na tela.
    // O tutorial é OBRIGATÓRIO — não há botão de pular.
    // Cada passo só avança quando o jogador realiza a ação correta na estação certa.
    // ShowMessage() exibe mensagens toast com fade.
    public class TutorialManager : Singleton<TutorialManager>
    {
        [Header("UI")]
        [SerializeField] private GameObject _tutorialPanel;
        [SerializeField] private TextMeshProUGUI _stepText;
        // _skipButton removido intencionalmente — tutorial é obrigatório

        [Header("Toast Message")]
        [SerializeField] private TMP_Text _tutorialText;

        [Header("Seta no mundo")]
        [SerializeField] private Transform _worldArrow; // GameObject com visual de seta

        public bool IsActive      { get; private set; }
        public bool WasCompleted  { get; private set; }

        // Expõe CurrentStep para as estações consultarem
        public int CurrentStep => _currentStep;

        private Coroutine _messageCoroutine;
        private Coroutine _bounceCoroutine;

        private int _currentStep;

        private static readonly string[] StepMessages =
        {
            "Vá ao campo de trigo e colete!",         // 0 — avança em WheatFieldStation ao coletar 1 trigo
            "Leve o trigo à estação de massa.",        // 1 — avança em DoughStation ao depositar trigo
            "Colete a massa pronta.",                  // 2 — avança em DoughStation ao coletar massa
            "Leve a massa à mesa de montagem.",        // 3 — avança em PizzaAssemblyStation ao depositar massa
            "Coloque a pizza crua no forno.",          // 4 — avança em OvenStation ao depositar pizza crua
            "Retire a pizza pronta do forno.",         // 5 — avança em OvenStation ao coletar pizza cozida
            "Entregue a pizza ao cliente!",            // 6 — avança em DeliveryStation ao entregar
        };

        // ALTERAÇÃO 1: coordenadas devem ser ajustadas no Unity conforme sua cena.
        // Selecione cada estação na cena, copie a posição X/Z do Inspector e substitua aqui.
        private static readonly Vector3[] StepTargets =
        {
            new Vector3(28f,   0f, 23f),     // 0 — campo de trigo
            new Vector3(28f,   0f, 23f),     // 1 — campo de trigo (ainda, com trigo na mão)
            new Vector3(-14f,  0f, 16f),     // 2 — estação de massa (coletar)
            new Vector3(-14f,  0f, 16f),     // 3 — estação de massa (depositar)
            new Vector3(-27f,  0f, 15.5f),   // 4 — montagem → forno
            new Vector3(-13f,  0f, 22f),     // 5 — forno (retirar)
            new Vector3(-22f,  0f, 11.5f),   // 6 — entrega
        };

        protected override void Awake()
        {
            base.Awake();

            // Não mostra tutorial se já foi concluído alguma vez
            if (PlayerPrefs.GetInt("TutorialEnabled", 1) == 0)
            {
                WasCompleted = true;
            }

            // Botão skip NÃO é mais conectado — tutorial é obrigatório
        }

        public void StartTutorial()
        {
            if (WasCompleted) return;
            IsActive     = true;
            _currentStep = 0;
            SetPanelVisible(true);
            StartCoroutine(RunTutorial());
        }

        // Chamado pelas estações quando o jogador realiza a ação correta
        public void AdvanceStep()
        {
            if (!IsActive) return;
            _currentStep++;
        }

        private IEnumerator RunTutorial()
        {
            while (_currentStep < StepMessages.Length)
            {
                ShowStep(_currentStep);
                int snapshot = _currentStep;
                // Aguarda até que _currentStep aumente — sem timeout, sem skip
                yield return new WaitUntil(() => _currentStep > snapshot || !IsActive);
                yield return new WaitForSeconds(0.4f); // pequena pausa entre passos
            }
            Complete();
        }

        private void ShowStep(int step)
        {
            if (_stepText != null)
                _stepText.text = $"<b>Passo {step + 1}/{StepMessages.Length}</b>\n{StepMessages[step]}";

            if (_worldArrow != null && step < StepTargets.Length)
            {
                _worldArrow.gameObject.SetActive(true);
                _worldArrow.position = StepTargets[step] + Vector3.up * 2f;

                // Reinicia o bounce para a nova posição
                if (_bounceCoroutine != null) StopCoroutine(_bounceCoroutine);
                _bounceCoroutine = StartCoroutine(BounceArrow());
            }
        }

        private IEnumerator BounceArrow()
        {
            if (_worldArrow == null) yield break;
            Vector3 basePos = _worldArrow.position;
            while (IsActive)
            {
                float y = Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.2f;
                _worldArrow.position = basePos + Vector3.up * y;
                yield return null;
            }
        }

        private void Complete()
        {
            IsActive     = false;
            WasCompleted = true;
            SetPanelVisible(false);

            if (_bounceCoroutine != null) StopCoroutine(_bounceCoroutine);
            if (_worldArrow != null) _worldArrow.gameObject.SetActive(false);

            // Persiste conclusão do tutorial via PlayerPrefs
            PlayerPrefs.SetInt("TutorialEnabled", 0);
            PlayerPrefs.Save();

            // Registra conclusão do tutorial pro sistema de progressão (level 1 → 2)
            PlayerProgressionSystem.Instance?.RegisterTutorialComplete();
        }

        private void SetPanelVisible(bool visible)
        {
            if (_tutorialPanel != null) _tutorialPanel.SetActive(visible);
        }

        // Exibe mensagem toast com fade-in/out. Não interfere no tutorial passo-a-passo.
        public void ShowMessage(string msg, float duration = 3f)
        {
            if (_messageCoroutine != null) StopCoroutine(_messageCoroutine);
            _messageCoroutine = StartCoroutine(ShowMessageCoroutine(msg, duration));
        }

        private IEnumerator ShowMessageCoroutine(string msg, float duration)
        {
            if (_tutorialText == null) yield break;

            _tutorialText.text = msg;
            var panel = _tutorialText.transform.parent?.gameObject;
            panel?.SetActive(true);

            // Fade in
            _tutorialText.alpha = 0f;
            float t = 0f;
            const float FadeDuration = 0.3f;
            while (t < FadeDuration)
            {
                t += Time.deltaTime;
                _tutorialText.alpha = t / FadeDuration;
                yield return null;
            }
            _tutorialText.alpha = 1f;

            yield return new WaitForSeconds(duration);

            // Fade out
            t = 0f;
            while (t < FadeDuration)
            {
                t += Time.deltaTime;
                _tutorialText.alpha = 1f - t / FadeDuration;
                yield return null;
            }
            _tutorialText.alpha = 0f;
            panel?.SetActive(false);
        }
    }
}