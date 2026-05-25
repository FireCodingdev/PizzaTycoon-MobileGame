using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PizzaTycoon.Player;

namespace PizzaTycoon.Editor
{
    // Adiciona BoxColliders em objetos do mapa que tem MeshRenderer mas nao tem
    // collider — assim o player nao atravessa mesas, cadeiras, paredes, etc.
    //
    // Filtra automaticamente:
    //   - Objetos com prefix "_PT_" (visuais temporarios do bubble/counter)
    //   - Waypoints / gizmos (Bubble_, Label_, Slot, QueueStart, SpawnPoint, EntryWP, OrderPoint, ExitPoint)
    //   - O proprio Player e seus filhos
    //   - Particulas, lights, UI
    //   - Meshes muito pequenas (< 5cm em qualquer eixo)
    //   - Meshes muito grandes (suspeito de ser chao — > 50m)
    public static class MapColliderTool
    {
        private static readonly string[] SkipPrefixes =
        {
            "_PT_", "Bubble_", "Label_", "Slot",
            "QueueStart", "SpawnPoint", "EntryWP", "ExitWP",
            "OrderPoint", "ExitPoint", "Queue_",
            "[DriveThru", "[CustomerSystem", "[FloatingText",
        };

        [MenuItem("PizzaTycoon/Map/Add Box Colliders to Map Objects", priority = 80)]
        public static void AddBoxCollidersToMapObjects()
        {
            int added = 0, skipped = 0;
            var allRenderers = Object.FindObjectsOfType<MeshRenderer>(true);

            foreach (var r in allRenderers)
            {
                if (!ShouldAddCollider(r, out string reason))
                {
                    skipped++;
                    continue;
                }

                var go     = r.gameObject;
                var mf     = go.GetComponent<MeshFilter>();
                var bounds = mf.sharedMesh.bounds;

                Undo.RegisterCompleteObjectUndo(go, "Add BoxCollider to Map Object");
                var box = Undo.AddComponent<BoxCollider>(go);
                box.center = bounds.center;
                box.size   = bounds.size;
                added++;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[MapColliderTool] {added} BoxColliders adicionados | {skipped} pulados.");
            EditorUtility.DisplayDialog("Map Colliders",
                $"{added} BoxColliders adicionados.\n{skipped} objetos pulados.\n\nUse Ctrl+Z se quiser reverter.",
                "OK");
        }

        [MenuItem("PizzaTycoon/Map/Remove ALL Box Colliders from Map Objects", priority = 81)]
        public static void RemoveAllMapColliders()
        {
            if (!EditorUtility.DisplayDialog("Remove Colliders",
                "Isso vai REMOVER todos BoxColliders dos objetos do mapa que tem MeshRenderer.\n\n" +
                "Triggers de estacoes e o CapsuleCollider do Player NAO sao tocados.\n\n" +
                "Continuar?", "Sim, remover", "Cancelar")) return;

            int removed = 0;
            var allBoxes = Object.FindObjectsOfType<BoxCollider>(true);

            foreach (var box in allBoxes)
            {
                if (box == null) continue;
                if (box.isTrigger) continue;                              // pula triggers
                var go = box.gameObject;
                if (go.GetComponent<PlayerController>() != null) continue;
                if (go.GetComponent<MeshRenderer>() == null) continue;    // so map mesh
                if (NameStartsWithSkip(go.name)) continue;

                Undo.DestroyObjectImmediate(box);
                removed++;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[MapColliderTool] {removed} BoxColliders removidos.");
        }

        // ── Filtros ─────────────────────────────────────────────────────────

        private static bool ShouldAddCollider(MeshRenderer r, out string reason)
        {
            reason = null;
            if (r == null) { reason = "renderer null"; return false; }

            var go = r.gameObject;

            // Ja tem qualquer collider?
            if (go.GetComponent<Collider>() != null) { reason = "ja tem collider"; return false; }

            // Prefix bloqueado?
            if (NameStartsWithSkip(go.name)) { reason = "nome bloqueado"; return false; }

            // Player ou filho do player?
            if (go.GetComponentInParent<PlayerController>() != null)
            {
                reason = "filho do Player"; return false;
            }

            // Particulas / UI / lights?
            if (go.GetComponent<ParticleSystem>() != null) { reason = "particula"; return false; }
            if (go.GetComponent<Canvas>() != null) { reason = "canvas"; return false; }

            // Mesh valida?
            var mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) { reason = "sem mesh"; return false; }

            var size = mf.sharedMesh.bounds.size;
            Vector3 lossy = go.transform.lossyScale;
            Vector3 worldSize = new Vector3(
                Mathf.Abs(size.x * lossy.x),
                Mathf.Abs(size.y * lossy.y),
                Mathf.Abs(size.z * lossy.z));

            // Muito pequena?
            if (worldSize.x < 0.05f && worldSize.y < 0.05f && worldSize.z < 0.05f)
            {
                reason = "muito pequena"; return false;
            }

            // Suspeito de ser chao gigante (mais de 50m em XZ)?
            if (worldSize.x > 50f || worldSize.z > 50f)
            {
                reason = "muito grande (provavel chao)"; return false;
            }

            return true;
        }

        private static bool NameStartsWithSkip(string name)
        {
            foreach (var p in SkipPrefixes)
                if (name.StartsWith(p, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
