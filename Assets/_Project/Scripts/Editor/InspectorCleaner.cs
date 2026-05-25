using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PizzaTycoon.Editor
{
    /// <summary>
    /// Utilitário para limpar erros residuais do Inspector causados por
    /// GameObjects destruídos via DestroyImmediate durante scripts de setup.
    /// Menu: PizzaTycoon > Z. Clean Inspector Errors
    /// </summary>
    public static class InspectorCleaner
    {
        [MenuItem("PizzaTycoon/Z. Clean Inspector Errors")]
        public static void CleanInspectorErrors()
        {
            // 1. Deseleciona tudo para forçar o Inspector a descarregar editores pendentes
            Selection.activeObject  = null;
            Selection.objects       = new Object[0];

            // 2. Força o Unity a encerrar todos os Editors abertos que possam ter
            //    referências nulas (NullReferenceException em OnDisable/OnEnable)
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");

            // 3. Libera assets não usados (limpa referências soltas no heap do Editor)
            EditorUtility.UnloadUnusedAssetsImmediate(true);

            // 4. Força reimportação do banco de dados de assets
            AssetDatabase.Refresh();

            // 5. Salva assets pendentes para garantir consistência
            AssetDatabase.SaveAssets();

            Debug.Log("[PizzaTycoon] Inspector limpo com sucesso. " +
                      "Os erros de SerializedObject devem ter desaparecido.");
        }

        /// <summary>
        /// Chamado internamente por UIBuilder e SceneBootstrapper antes de
        /// DestroyImmediate para evitar NullReferenceException no Inspector.
        /// </summary>
        public static void PrepareForDestroy()
        {
            Selection.activeObject = null;
            Selection.objects      = new Object[0];
        }
    }
}
