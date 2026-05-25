using System.Collections;
using UnityEngine;
using PizzaTycoon.Items;
using PizzaTycoon.Player;
using PizzaTycoon.Managers;
using PizzaTycoon.GameSystems;

namespace PizzaTycoon.Stations
{
    // Estação Campo de Trigo — gera e entrega trigo ao jogador automaticamente
    public class WheatFieldStation : BaseStation
    {
        [Header("Campo de Trigo")]
        [SerializeField] private float _wheatRegenerateTime = 1f; // tempo entre cada trigo gerado
        [SerializeField] private int _maxWheatStored = 10;

        private int _wheatStored = 0;
        private Coroutine _regenerateCoroutine;

        protected override void OnEnable()
        {
            base.OnEnable();
            _regenerateCoroutine = StartCoroutine(RegenerateWheat());
        }

        private void OnDisable()
        {
            if (_regenerateCoroutine != null)
                StopCoroutine(_regenerateCoroutine);
        }

        // O campo regenera trigo sozinho, independente do jogador estar presente
        private IEnumerator RegenerateWheat()
        {
            WaitForSeconds wait = new WaitForSeconds(_wheatRegenerateTime);
            while (true)
            {
                yield return wait;
                if (_wheatStored < _maxWheatStored)
                    _wheatStored++;
            }
        }

        // Enquanto o jogador está no campo, transfere trigo para a pilha do jogador
        protected override void ProcessItem(PlayerController player)
        {
            if (_wheatStored <= 0) return;
            if (player.Stacker == null || player.Stacker.IsFull) return;

            StackableItem wheat = ItemPool.Instance.Get(ItemType.Wheat, transform.position);
            if (wheat == null) return;

            if (player.Stacker.TryAddItem(wheat))
            {
                _wheatStored--;
                AudioManager.Instance?.PlayItemCollect();

                // Avança tutorial no passo 0 (coletar trigo no campo)
                if (TutorialManager.Instance != null &&
                    TutorialManager.Instance.IsActive &&
                    TutorialManager.Instance.CurrentStep == 0)
                {
                    TutorialManager.Instance.AdvanceStep();
                }
            }
            else
            {
                ItemPool.Instance.Return(wheat);
            }
        }

        // Override: coleta trigo um a um, mais rápido que o intervalo base
        protected override IEnumerator ProductionLoop()
        {
            while (_playerInRange != null && _isActive)
            {
                yield return new WaitForSeconds(_productionInterval);

                if (_playerInRange != null && _wheatStored > 0 && !_playerInRange.Stacker.IsFull)
                    ProcessItem(_playerInRange);
            }
        }
    }
}