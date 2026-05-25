using UnityEngine;
using PizzaTycoon.Items;
using PizzaTycoon.Player;
using PizzaTycoon.Managers;
using PizzaTycoon.GameSystems;

namespace PizzaTycoon.Stations
{
    // Mesa de Montagem — converte Massa em Pizza Crua
    // O jogador deposita massa e retira pizza crua pronta para o forno
    public class PizzaAssemblyStation : BaseStation
    {
        [Header("Mesa de Montagem")]
        [SerializeField] private ParticleSystem _assemblyParticles;
        [SerializeField] private int _maxRawPizzasQueued = 5;

        private int _doughDeposited = 0;
        private int _rawPizzasReady = 0;

        protected override void ProcessItem(PlayerController player)
        {
            PlayerStacker stacker = player.Stacker;
            if (stacker == null) return;

            // Deposita massa se o jogador carrega
            if (stacker.ContainsType(ItemType.Dough) && _doughDeposited < _maxRawPizzasQueued)
            {
                StackableItem dough = stacker.RemoveItemOfType(ItemType.Dough);
                if (dough != null)
                {
                    ItemPool.Instance.Return(dough);
                    _doughDeposited++;
                    AssemblePizza();

                    // Avança tutorial no passo 3 (depositar massa na montagem)
                    if (TutorialManager.Instance != null &&
                        TutorialManager.Instance.IsActive &&
                        TutorialManager.Instance.CurrentStep == 3)
                    {
                        TutorialManager.Instance.AdvanceStep();
                    }
                }
                return;
            }

            // Coleta pizza crua se disponível e jogador tem espaço
            if (_rawPizzasReady > 0 && !stacker.IsFull)
            {
                StackableItem rawPizza = ItemPool.Instance.Get(ItemType.RawPizza, transform.position);
                if (rawPizza != null && stacker.TryAddItem(rawPizza))
                {
                    _rawPizzasReady--;
                    AudioManager.Instance?.PlayItemCollect();

                    // Avança tutorial no passo 3 também — REMOVIDO.
                    // O avanço fica no depósito de massa (acima), não na coleta.
                    // A coleta da pizza crua é implícita ao depositar massa montada.
                }
                else
                {
                    ItemPool.Instance.Return(rawPizza);
                }
            }
        }

        private void AssemblePizza()
        {
            if (_doughDeposited <= 0) return;
            _doughDeposited--;
            _rawPizzasReady++;
            // Unity's fake-null não é capturado por ?. — usar comparação explícita.
            if (_assemblyParticles != null) _assemblyParticles.Play();
        }
    }
}