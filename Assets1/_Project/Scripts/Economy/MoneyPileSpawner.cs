using UnityEngine;
using PizzaTycoon.Utils;

namespace PizzaTycoon.Economy
{
    // Singleton responsavel por criar MoneyPile no mundo quando o cliente paga.
    public class MoneyPileSpawner : Singleton<MoneyPileSpawner>
    {
        public void SpawnPile(Vector3 worldPos, float value)
        {
            var go = new GameObject("MoneyPile");
            go.transform.position = worldPos;

            // Visual: cubo verde achatado (#2ECC71)
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.35f, 0.18f, 0.45f);
            Destroy(visual.GetComponent<BoxCollider>());

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(0.18f, 0.80f, 0.36f);
                visual.GetComponent<Renderer>().sharedMaterial = mat;
            }

            // Trigger collider para detectar o player
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size      = new Vector3(1.2f, 1f, 1.2f);

            var pile = go.AddComponent<MoneyPile>();
            pile.Initialize(value);

            Debug.Log($"[MoneyPileSpawner] Pile gerado em {worldPos} com valor ${value:F0}.");
        }
    }
}
