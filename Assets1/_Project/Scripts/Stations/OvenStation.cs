using System.Collections;
using UnityEngine;
using PizzaTycoon.Items;
using PizzaTycoon.Player;
using PizzaTycoon.Managers;
using PizzaTycoon.VFX;
using PizzaTycoon.GameSystems;

namespace PizzaTycoon.Stations
{
    // Forno — converte Pizza Crua em Pizza Pronta com timer visual e VFX de fumaça
    public class OvenStation : BaseStation
    {
        [Header("Forno")]
        [SerializeField] private float _cookingTime = 5f;
        [SerializeField] private int   _ovenSlots   = 2;

        [SerializeField] private UnityEngine.UI.Slider[] _cookingProgressBars;

        private int _pizzasCooking;
        private int _pizzasCooked;

        // VFX de fumaça em loop — uma instância por slot
        private VFX_Cook[] _cookSmoke;

        // Exposto para o UpgradeManager (upgrade OvenSlots aumenta esta capacidade)
        public int OvenSlots
        {
            get => _ovenSlots;
            set
            {
                int newValue = Mathf.Max(1, value);
                if (newValue == _ovenSlots) return;
                _ovenSlots = newValue;
                System.Array.Resize(ref _cookSmoke, _ovenSlots);
            }
        }

        public float CookingTime
        {
            get => _cookingTime;
            set => _cookingTime = Mathf.Max(0.1f, value);
        }

        protected override void Awake()
        {
            base.Awake();
            _cookSmoke = new VFX_Cook[_ovenSlots];
        }

        protected override void ProcessItem(PlayerController player)
        {
            PlayerStacker stacker = player.Stacker;
            if (stacker == null) return;

            // Deposita pizza crua no forno
            if (stacker.ContainsType(ItemType.RawPizza) && _pizzasCooking < _ovenSlots)
            {
                StackableItem rawPizza = stacker.RemoveItemOfType(ItemType.RawPizza);
                if (rawPizza != null)
                {
                    ItemPool.Instance.Return(rawPizza);
                    int slot = _pizzasCooking;
                    StartCoroutine(CookPizza(slot));
                    player.OnItemCollected(transform.position);

                    // Avança tutorial no passo 4 (colocar pizza crua no forno)
                    if (TutorialManager.Instance != null &&
                        TutorialManager.Instance.IsActive &&
                        TutorialManager.Instance.CurrentStep == 4)
                    {
                        TutorialManager.Instance.AdvanceStep();
                    }
                }
                return;
            }

            // Coleta pizza pronta do forno
            if (_pizzasCooked > 0 && !stacker.IsFull)
            {
                StackableItem cooked = ItemPool.Instance.Get(ItemType.CookedPizza, transform.position);
                if (cooked != null && stacker.TryAddItem(cooked))
                {
                    _pizzasCooked--;
                    AudioManager.Instance?.PlayPizzaReady();
                    ParticleManager.Instance?.PlayPizzaReady(transform.position + Vector3.up * 0.5f);
                    player.OnItemCollected(transform.position);

                    // Avança tutorial no passo 5 (retirar pizza pronta do forno)
                    if (TutorialManager.Instance != null &&
                        TutorialManager.Instance.IsActive &&
                        TutorialManager.Instance.CurrentStep == 5)
                    {
                        TutorialManager.Instance.AdvanceStep();
                    }
                }
                else
                {
                    ItemPool.Instance.Return(cooked);
                }
            }
        }

        private IEnumerator CookPizza(int slotIndex)
        {
            _pizzasCooking++;

            // Inicia fumaça em loop para este slot
            Vector3 smokePos = transform.position + Vector3.up * 0.8f;
            _cookSmoke[slotIndex] = ParticleManager.Instance?.PlayCookLoop(smokePos);

            float elapsed = 0f;
            while (elapsed < _cookingTime)
            {
                elapsed += Time.deltaTime;
                if (_cookingProgressBars != null && slotIndex < _cookingProgressBars.Length)
                    if (_cookingProgressBars[slotIndex] != null)
                        _cookingProgressBars[slotIndex].value = elapsed / _cookingTime;

                yield return null;
            }

            _pizzasCooking--;
            _pizzasCooked++;

            // Para fumaça e dispara burst de conclusão
            if (_cookSmoke[slotIndex] != null)
            {
                ParticleManager.Instance?.StopCook(_cookSmoke[slotIndex]);
                _cookSmoke[slotIndex] = null;
            }

            ParticleManager.Instance?.PlayPizzaReady(transform.position + Vector3.up * 0.5f);
            AudioManager.Instance?.PlayPizzaReady();

            if (_cookingProgressBars != null && slotIndex < _cookingProgressBars.Length)
                if (_cookingProgressBars[slotIndex] != null)
                    _cookingProgressBars[slotIndex].value = 0f;
        }
    }
}