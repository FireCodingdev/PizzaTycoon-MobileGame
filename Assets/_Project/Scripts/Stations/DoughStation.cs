using System.Collections;
using UnityEngine;
using PizzaTycoon.Items;
using PizzaTycoon.Player;
using PizzaTycoon.Managers;
using PizzaTycoon.GameSystems;

namespace PizzaTycoon.Stations
{
    // Estação de Massa — converte Trigo em Massa de Pizza
    // Jogador deposita trigo → estação processa → jogador coleta massa
    public class DoughStation : BaseStation
    {
        [Header("Estação de Massa")]
        [SerializeField] private int _wheatPerDough = 2; // quantos trigos fazem uma massa
        [SerializeField] private ParticleSystem _processingParticles;

        private int _wheatDeposited = 0;
        private int _doughReady = 0;

        protected override void ProcessItem(PlayerController player)
        {
            PlayerStacker stacker = player.Stacker;
            if (stacker == null) return;

            // Fase 1: Jogador deposita trigo na estação
            if (stacker.ContainsType(ItemType.Wheat))
            {
                StackableItem wheat = stacker.RemoveItemOfType(ItemType.Wheat);
                if (wheat != null)
                {
                    ItemPool.Instance.Return(wheat);
                    _wheatDeposited++;
                    TryConvertWheatToDough();

                    // Avança tutorial no passo 1 (depositar trigo na estação de massa)
                    if (TutorialManager.Instance != null &&
                        TutorialManager.Instance.IsActive &&
                        TutorialManager.Instance.CurrentStep == 1)
                    {
                        TutorialManager.Instance.AdvanceStep();
                    }
                }
                return;
            }

            // Fase 2: Jogador coleta massa pronta
            if (_doughReady > 0 && !stacker.IsFull)
            {
                StackableItem dough = ItemPool.Instance.Get(ItemType.Dough, transform.position);
                if (dough != null && stacker.TryAddItem(dough))
                {
                    _doughReady--;
                    AudioManager.Instance?.PlayItemCollect();

                    // Avança tutorial no passo 2 (coletar massa pronta)
                    if (TutorialManager.Instance != null &&
                        TutorialManager.Instance.IsActive &&
                        TutorialManager.Instance.CurrentStep == 2)
                    {
                        TutorialManager.Instance.AdvanceStep();
                    }
                }
                else
                {
                    ItemPool.Instance.Return(dough);
                }
            }
        }

        private void TryConvertWheatToDough()
        {
            while (_wheatDeposited >= _wheatPerDough)
            {
                _wheatDeposited -= _wheatPerDough;
                _doughReady++;
                // Unity's fake-null não é capturado por ?. — usar comparação explícita.
                if (_processingParticles != null) _processingParticles.Play();
            }
        }
    }
}