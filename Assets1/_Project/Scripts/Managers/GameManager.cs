using UnityEngine;
using System;
using PizzaTycoon.Utils;

namespace PizzaTycoon.Managers
{
    // Estados possíveis do jogo
    public enum GameState { MainMenu, Playing, Paused, GameOver }

    // Gerenciador central — ponto de entrada para todos os sistemas
    // Inicializa primeiro de todos para que os outros managers possam se registrar
    [DefaultExecutionOrder(-100)]
    public class GameManager : Singleton<GameManager>
    {
        [SerializeField] private GameState _initialState = GameState.MainMenu;

        public GameState CurrentState { get; private set; }

        // Eventos globais de estado
        public static event Action<GameState> OnGameStateChanged;

        // Referências públicas aos managers (evita FindObjectOfType em outros scripts)
        public MoneyManager MoneyManager { get; private set; }
        public SaveManager SaveManager { get; private set; }
        public AudioManager AudioManager { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            CacheManagers();
        }

        private void Start()
        {
            ChangeState(_initialState);
        }

        private void CacheManagers()
        {
            MoneyManager = MoneyManager.Instance;
            SaveManager = SaveManager.Instance;
            AudioManager = AudioManager.Instance;
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            OnGameStateChanged?.Invoke(newState);

            switch (newState)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    break;
            }

            Debug.Log($"[GameManager] Estado mudou para: {newState}");
        }

        public void StartGame()
        {
            SaveManager.Instance?.LoadGame();
            ChangeState(GameState.Playing);
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "SampleScene")
                SceneLoader.Instance?.LoadScene("SampleScene");
        }

        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
                ChangeState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
                ChangeState(GameState.Playing);
        }

        public void GoToMainMenu()
        {
            SaveManager.Instance?.SaveGame();
            ChangeState(GameState.MainMenu);
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu")
                SceneLoader.Instance?.LoadScene("MainMenu");
        }

        public bool IsPlaying => CurrentState == GameState.Playing;
    }
}
