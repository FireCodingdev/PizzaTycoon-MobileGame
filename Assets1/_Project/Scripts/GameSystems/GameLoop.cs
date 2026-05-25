using System;
using System.Collections;
using UnityEngine;
using PizzaTycoon.Utils;
using PizzaTycoon.Economy;
using PizzaTycoon.Managers;
using PizzaTycoon.VFX;
using PizzaTycoon.Camera;

namespace PizzaTycoon.GameSystems
{
    // Controla as 4 fases do jogo desbloqueadas por dinheiro acumulado
    // Fases: 0=$0 / 1=$200 / 2=$800 / 3=$2000
    public class GameLoop : Singleton<GameLoop>
    {
        private static readonly float[] PhaseUnlockCosts = { 0f, 200f, 800f, 2000f };

        public int CurrentPhase { get; private set; }

        public static event Action<int> OnPhaseUnlocked; // nova fase

        private void Start()
        {
            if (MoneyManager.Instance != null)
                MoneyManager.OnMoneyChanged += CheckPhaseUnlock;

            // Exibe dica inicial se o jogador comecar sem dinheiro
            if (MoneyManager.Instance != null && MoneyManager.Instance.CurrentMoney <= 0f)
                TutorialManager.Instance?.ShowMessage("Colete o trigo!");
        }

        private void OnDestroy()
        {
            MoneyManager.OnMoneyChanged -= CheckPhaseUnlock;
        }

        private void CheckPhaseUnlock(float totalMoney)
        {
            int next = CurrentPhase + 1;
            if (next >= PhaseUnlockCosts.Length) return;
            if (totalMoney >= PhaseUnlockCosts[next])
                StartCoroutine(UnlockPhase(next));
        }

        private IEnumerator UnlockPhase(int phase)
        {
            CurrentPhase = phase;

            // Pan da câmera para o centro e volta
            yield return StartCoroutine(CameraPan());

            // Confetti VFX no centro do mapa
            if (ParticleManager.Instance != null)
                for (int i = 0; i < 5; i++)
                {
                    Vector3 pos = new Vector3(
                        UnityEngine.Random.Range(3f, 17f), 0f,
                        UnityEngine.Random.Range(3f, 17f));
                    ParticleManager.Instance.PlayPizzaReady(pos);
                    yield return new WaitForSeconds(0.15f);
                }

            OnPhaseUnlocked?.Invoke(phase);
        }

        private IEnumerator CameraPan()
        {
            // Shake suave como sinal de desbloqueio
            CameraShake shake = FindObjectOfType<CameraShake>();
            if (shake != null) shake.Shake(0.15f, 0.5f);
            yield return new WaitForSeconds(0.5f);
        }

        // Chamado pelo SaveManager ao carregar jogo salvo
        public void RestorePhase(int savedPhase)
        {
            CurrentPhase = Mathf.Clamp(savedPhase, 0, PhaseUnlockCosts.Length - 1);
        }
    }
}
