using System.Collections;
using UnityEngine;

namespace PizzaTycoon.VFX
{
    // Classe base para todos os efeitos de partículas do jogo.
    // Subclasses implementam Configure() para definir o comportamento do ParticleSystem.
    [RequireComponent(typeof(ParticleSystem))]
    public abstract class BaseVFX : MonoBehaviour
    {
        protected ParticleSystem PS { get; private set; }
        protected ParticleSystemRenderer PSRenderer { get; private set; }

        public bool IsAvailable => !gameObject.activeSelf;

        protected virtual void Awake()
        {
            PS = GetComponent<ParticleSystem>();
            PSRenderer = GetComponent<ParticleSystemRenderer>();

            // Para o PS padrão que inicia automaticamente
            PS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            SetDefaultRenderer();
            Configure();
        }

        // Configura todos os módulos do ParticleSystem — implementar nas subclasses
        protected abstract void Configure();

        // Reproduce o efeito na posição indicada e retorna ao pool quando terminar
        public virtual void Play(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            gameObject.SetActive(true);
            PS.Play();
            StartCoroutine(AutoReturn());
        }

        // Ativa em loop (para efeitos contínuos como fumaça do forno)
        public virtual void PlayLoop(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            gameObject.SetActive(true);
            PS.Play();
        }

        // Para o loop e agenda retorno ao pool
        public virtual void StopLoop()
        {
            PS.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            StartCoroutine(AutoReturn());
        }

        // Retorna ao pool após o sistema terminar de emitir
        private IEnumerator AutoReturn()
        {
            yield return new WaitUntil(() => !PS.IsAlive(true));
            gameObject.SetActive(false);
        }

        // Configura material compatível com URP ou fallback
        private void SetDefaultRenderer()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Mobile/Particles/Additive");
            if (shader == null) return;

            PSRenderer.material = new Material(shader);
            PSRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        // Auxiliar: configura gradiente de cor com fade alpha
        protected static ParticleSystem.MinMaxGradient MakeGradient(
            Color startColor, Color endColor, float startAlpha = 1f, float endAlpha = 0f)
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(startColor, 0f), new GradientColorKey(endColor, 1f) },
                new[] { new GradientAlphaKey(startAlpha, 0f), new GradientAlphaKey(endAlpha, 1f) }
            );
            return new ParticleSystem.MinMaxGradient(g);
        }

        // Auxiliar: curva simples de N para 0 ao longo do tempo
        protected static ParticleSystem.MinMaxCurve CurveDown(float startValue = 1f)
        {
            AnimationCurve c = new AnimationCurve(
                new Keyframe(0f, startValue),
                new Keyframe(1f, 0f)
            );
            return new ParticleSystem.MinMaxCurve(1f, c);
        }

        // Auxiliar: curva que cresce e depois encolhe (punch)
        protected static ParticleSystem.MinMaxCurve CurvePunch(float peak = 1.3f)
        {
            AnimationCurve c = new AnimationCurve(
                new Keyframe(0f,    0f),
                new Keyframe(0.3f, peak),
                new Keyframe(0.7f, peak * 0.9f),
                new Keyframe(1f,    0f)
            );
            return new ParticleSystem.MinMaxCurve(1f, c);
        }
    }
}
