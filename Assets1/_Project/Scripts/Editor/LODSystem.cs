#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace PizzaTycoon.Editor
{
    // Adiciona LODGroups automáticos a estações e decorações via MenuItem
    public static class LODSystem
    {
        [MenuItem("PizzaTycoon/6. Setup LOD Groups", priority = 106)]
        public static void SetupLODGroups()
        {
            int count = 0;

            // Procura GameObjects marcados com tags de estação/decoração
            // ou com "Station" no nome
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

            foreach (GameObject go in allObjects)
            {
                if (!ShouldHaveLOD(go)) continue;
                if (go.GetComponent<LODGroup>() != null) continue; // já tem LOD

                AddLODGroup(go);
                count++;
            }

            Debug.Log($"[LODSystem] LODGroups adicionados a {count} objetos.");
            EditorUtility.DisplayDialog("LOD Setup", $"LODGroups configurados em {count} objetos.", "OK");
        }

        private static bool ShouldHaveLOD(GameObject go)
        {
            string name = go.name.ToLower();
            return name.Contains("station") ||
                   name.Contains("decoration") ||
                   name.Contains("building") ||
                   name.Contains("prop") ||
                   name.Contains("fence") ||
                   name.Contains("tree");
        }

        private static void AddLODGroup(GameObject go)
        {
            // Coleta todos os renderers filhos
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            LODGroup lodGroup = go.AddComponent<LODGroup>();

            // LOD0: distância < 8u — modelo completo
            // LOD1: distância 8-15u — reduz detalhes (ainda todos renderers, mas simplificado)
            // LOD2: distância > 15u — apenas bounding box (culled)

            LOD[] lods = new LOD[3];

            // LOD0 — todos os renderers
            lods[0] = new LOD(0.6f, renderers); // screenRelativeTransitionHeight

            // LOD1 — apenas renderers principais (não filhos pequenos de detalhe)
            var lod1Renderers = GetMainRenderers(renderers);
            lods[1] = new LOD(0.15f, lod1Renderers);

            // LOD2 — culled (array vazio = desativa o objeto a partir desta distância)
            lods[2] = new LOD(0.01f, new Renderer[0]);

            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();

            EditorUtility.SetDirty(go);
        }

        private static Renderer[] GetMainRenderers(Renderer[] all)
        {
            // Considera "principal" renderers com bounds maiores que 0.1 unidade
            var main = new System.Collections.Generic.List<Renderer>();
            foreach (var r in all)
            {
                if (r.bounds.size.magnitude > 0.1f)
                    main.Add(r);
            }
            return main.Count > 0 ? main.ToArray() : all;
        }
    }
}
#endif
