using System.Collections;
using UnityEngine;
using PizzaTycoon.Managers;

namespace PizzaTycoon.Economy
{
    // Pequeno cubo verde de dinheiro que cai no chao quando um cliente paga.
    // Estilo Pizza Ready / Supercent: o player anda por cima para coletar.
    //
    // Construido programaticamente (sem prefab) — toda a estrutura visual
    // e fisica e criada em Spawn(), entao o caller so chama uma linha.
    public class MoneyPickup : MonoBehaviour
    {
        private float _value;
        private bool  _canPickup;

        private const float PickupDelay   = 0.35f; // tempo ate ser coletavel (apos o bounce)
        private const float BounceHeight  = 1.2f;
        private const float BounceDuration = 0.45f;
        private const float ScatterRadius = 0.6f;  // distancia aleatoria do ponto de spawn

        // Spawn estatico — todo MoneyPickup nasce por aqui. Cria visual + collider.
        public static MoneyPickup Spawn(Vector3 worldPos, float value)
        {
            var go = new GameObject("MoneyPickup");
            go.transform.position = worldPos;

            // Visual: cubo verde achatado (parece bolacha de dinheiro)
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.35f, 0.18f, 0.45f);
            // Remove o collider da primitiva — a coleta usa o trigger do root
            Object.Destroy(visual.GetComponent<BoxCollider>());

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(0.22f, 0.78f, 0.36f); // verde dinheiro
                visual.GetComponent<Renderer>().sharedMaterial = mat;
            }

            // Trigger collider grande no root para detectar o player
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.6f;

            var pickup = go.AddComponent<MoneyPickup>();
            pickup._value = value;
            return pickup;
        }

        private void Start()
        {
            StartCoroutine(BounceArc());
        }

        // Pula em uma parabola da posicao de origem para um ponto aleatorio
        // proximo, simulando o "pop" de dinheiro saindo do balcao.
        private IEnumerator BounceArc()
        {
            Vector3 start = transform.position;
            Vector3 offset = new Vector3(
                Random.Range(-ScatterRadius, ScatterRadius),
                0f,
                Random.Range(-ScatterRadius, ScatterRadius * 0.6f)); // tende pra frente
            Vector3 end = start + offset;
            end.y = start.y; // mesmo nivel do chao

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / BounceDuration;
                float tClamped = Mathf.Clamp01(t);
                // Parabola: max altura no meio do arco (sin(pi*t))
                float arc = Mathf.Sin(tClamped * Mathf.PI) * BounceHeight;
                transform.position = Vector3.Lerp(start, end, tClamped) + Vector3.up * arc;
                // Gira para dar vida
                transform.Rotate(0f, 540f * Time.deltaTime, 0f);
                yield return null;
            }
            transform.position = end;

            // Pequena espera antes de poder ser coletado (evita coleta imediata
            // se o player ja estiver em cima do ponto de spawn)
            yield return new WaitForSeconds(PickupDelay);
            _canPickup = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_canPickup) return;
            if (!other.CompareTag("Player")) return;

            // Repassa o pagamento para o MoneyManager. AddMoney ja toca som,
            // atualiza HUD e exibe o FloatingText "+$X" na posicao.
            MoneyManager.Instance?.AddMoney(_value, transform.position);
            Destroy(gameObject);
        }
    }
}
