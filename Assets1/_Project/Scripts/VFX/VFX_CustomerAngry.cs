using UnityEngine;

namespace PizzaTycoon.VFX
{
    // VFX de cliente irritado — partículas vermelhas com rotação agitada
    public class VFX_CustomerAngry : BaseVFX
    {
        protected override void Configure()
        {
            var main = PS.main;
            main.loop            = false;
            main.duration        = 0.7f;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(2.5f, 4.0f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.8f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation   = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor      = new ParticleSystem.MinMaxGradient(
                new Color(0.906f, 0.298f, 0.235f),  // vermelho
                new Color(0.8f, 0.1f, 0.1f));

            var emission = PS.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });

            // Shape: hemisférico apontado para cima (raiva sai da cabeça)
            var shape = PS.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius    = 0.2f;

            // Cor: vermelho → preto com fade
            var col = PS.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.906f, 0.298f, 0.235f), 0f),
                    new GradientColorKey(new Color(0.2f, 0.05f, 0.05f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(g);

            // Tamanho: shrink rápido
            var size = PS.sizeOverLifetime;
            size.enabled = true;
            size.size    = CurveDown(1f);

            // Rotação rápida para efeito caótico
            var rot = PS.rotationOverLifetime;
            rot.enabled      = true;
            rot.separateAxes = false;
            rot.z            = new ParticleSystem.MinMaxCurve(
                -360f * Mathf.Deg2Rad,
                 360f * Mathf.Deg2Rad);
        }
    }
}
