using UnityEngine;
using PizzaTycoon.Items;
using PizzaTycoon.Player;
using PizzaTycoon.Customers;
using PizzaTycoon.Managers;
using PizzaTycoon.Economy;
using PizzaTycoon.GameSystems;

namespace PizzaTycoon.Stations
{
    // Balcão de Entrega — ponto onde o jogador entrega pizzas prontas aos clientes
    public class DeliveryStation : BaseStation
    {
        [Header("Balcão de Entrega")]
        [SerializeField] private CustomerQueue _customerQueue;
        [SerializeField] private Transform _deliveryPoint; // ponto onde clientes ficam em fila
        [Tooltip("Componente CounterStorage onde pizzas são acumuladas no balcão. " +
                 "Se NULL, fallback para o modo antigo (1 pizza por venda direto do estoque do player).")]
        [SerializeField] private CounterStorage _counterStorage;

        [Header("Money Piles (Pizza Ready style)")]
        [Tooltip("Quantos pacotes de dinheiro caem no chão por entrega")]
        [SerializeField] private int _moneyPileMin = 2;
        [SerializeField] private int _moneyPileMax = 4;

        protected override void ProcessItem(PlayerController player)
        {
            if (_customerQueue == null) return;

            // 1) DROP: enquanto o player tem pizza pronta E o counter tem espaço,
            //    transfere uma por uma. Player precisa estar no trigger desta Station.
            if (_counterStorage != null && player.Stacker != null)
            {
                while (player.Stacker.ContainsType(ItemType.CookedPizza) && _counterStorage.HasRoom)
                {
                    StackableItem pizza = player.Stacker.RemoveItemOfType(ItemType.CookedPizza);
                    if (pizza == null) break;
                    ItemPool.Instance.Return(pizza);
                    _counterStorage.TryAddPizza();
                }
            }

            // 2) SELL: há cliente esperando? Tem qty suficiente no counter?
            Customer nextCustomer = _customerQueue.GetNextWaitingCustomer();
            if (nextCustomer == null) return;
            int qty = nextCustomer.OrderQuantity;

            // Modo com CounterStorage: pega N pizzas do balcão
            if (_counterStorage != null)
            {
                if (!_counterStorage.HasAtLeast(qty)) return;
                if (!_counterStorage.TryTakePizzas(qty)) return;
            }
            // Modo legado: pega 1 pizza direto do stack do player (ignora qty)
            else
            {
                PlayerStacker stacker = player.Stacker;
                if (stacker == null) return;
                if (!stacker.ContainsType(ItemType.CookedPizza)) return;
                StackableItem pizza = stacker.RemoveItemOfType(ItemType.CookedPizza);
                if (pizza == null) return;
                ItemPool.Instance.Return(pizza);
            }

            // Entrega — Deliver retorna pagamento total (basePay * qty + bonus se rápido)
            float payment = nextCustomer.Deliver();

            // Avança tutorial no passo 6 (após entregar ao cliente — último passo)
            if (TutorialManager.Instance != null &&
                TutorialManager.Instance.IsActive &&
                TutorialManager.Instance.CurrentStep == 6)
            {
                TutorialManager.Instance.AdvanceStep();
            }

            // Cai N money piles no chão — player coleta andando por cima (Pizza Ready).
            int pileCount = Mathf.Clamp(
                Mathf.RoundToInt(payment / 5f), _moneyPileMin, _moneyPileMax);
            float perPile = payment / pileCount;
            Vector3 spawnBase = nextCustomer.transform.position + Vector3.right * 0.5f;

            if (MoneyPileSpawner.Instance != null)
            {
                for (int i = 0; i < pileCount; i++)
                    MoneyPileSpawner.Instance.SpawnPile(spawnBase, perPile);
            }
            else
            {
                for (int i = 0; i < pileCount; i++)
                    MoneyPickup.Spawn(spawnBase, perPile);
            }

            AudioManager.Instance?.PlayCustomerHappy();
        }

        // Retorna o ponto de fila para que CustomerSpawner posicione os clientes
        public Transform DeliveryPoint => _deliveryPoint != null ? _deliveryPoint : transform;
    }
}