using UnityEngine;
using UnityEngine.SceneManagement;

namespace PizzaTycoon.Managers
{
    // Adiciona em runtime os managers que existem no código mas nunca foram
    // adicionados como componente no GameObject [Managers] da cena.
    // Coloque este script em um GameObject vazio na cena (ou ele cria sozinho).
    [DefaultExecutionOrder(-90)] // depois do GameManager (-100)
    public class RuntimeManagerInitializer : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            // Garante que existe um inicializador na cena
            if (FindObjectOfType<RuntimeManagerInitializer>() != null) return;
            var go = new GameObject("[RuntimeManagerInit]");
            go.AddComponent<RuntimeManagerInitializer>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            CreatePersistentManagers();
            SetupForCurrentScene(SceneManager.GetActiveScene());
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SetupForCurrentScene(scene);
        }

        private void CreatePersistentManagers()
        {
            EnsureManager<PizzaTycoon.Achievements.AchievementManager>("AchievementManager");
            EnsureManager<PizzaTycoon.Events.EventManager>("EventManager");
            EnsureManager<PizzaTycoon.Workers.WorkerManager>("WorkerManager");
            EnsureManager<PizzaTycoon.Seasons.SeasonManager>("SeasonManager");
            EnsureManager<PizzaTycoon.Leaderboard.LeaderboardManager>("LeaderboardManager");
            EnsureManager<PizzaTycoon.Skins.SkinManager>("SkinManager");
            EnsureManager<PizzaTycoon.Social.SocialManager>("SocialManager");
            EnsureManager<PizzaTycoon.Decorations.DecorationSystem>("DecorationSystem");
            EnsureManager<PizzaTycoon.GameSystems.TutorialManager>("TutorialManager");
            EnsureManager<PizzaTycoon.GameSystems.GameLoop>("GameLoop");
            EnsureManager<PizzaTycoon.Recipes.RecipeSystem>("RecipeSystem");
            EnsureManager<PizzaTycoon.Analytics.AnalyticsManager>("AnalyticsManager");
        }

        private void SetupForCurrentScene(Scene scene)
        {
            // Auto-spawn de objetos da cena DESABILITADO.
            // O Unity nao vai mais criar GameObjects para [MapExpander], [WorldDecoration],
            // [CustomerAutoWire], [WorkerSetup], [GameSystemsConnector], [AchievementSetup]
            // automaticamente. Adicione manualmente no Hierarchy se precisar de algum.
        }

        // Versão para objetos da cena (não DontDestroyOnLoad)
        private static void EnsureSceneObject<T>(string name) where T : Component
        {
            if (FindObjectOfType<T>() != null) return;
            try
            {
                var go = new GameObject("[" + name + "]");
                go.AddComponent<T>();
                Debug.Log($"[RuntimeManagerInit] Spawnado: {name}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RuntimeManagerInit] Falha ao criar {name}: {e.Message}");
            }
        }

        private static void EnsureManager<T>(string name) where T : Component
        {
            if (FindObjectOfType<T>() != null) return;

            // Tenta usar System.Type.GetType para verificar se o tipo existe — se não compilar,
            // ignora (alguns managers podem não existir em todos os builds)
            try
            {
                var go = new GameObject("[Mgr] " + name);
                DontDestroyOnLoad(go);
                go.AddComponent<T>();
                Debug.Log($"[RuntimeManagerInit] Manager adicionado: {name}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RuntimeManagerInit] Falha ao criar {name}: {e.Message}");
            }
        }
    }
}
