using UnityEngine;

namespace PizzaTycoon.VFX
{
    // VFX de cliente feliz — corações rosa flutuando para cima
    public class VFX_CustomerHappy : BaseVFX
    {
        protected override void Configure()
        {
            var main = PS.main;
            main.loop            = false;
            main.duration        = 1.2f;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.8f, 1.1f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.15f, 0.22f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.8f); // sobe devagar
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor      = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.41f, 0.71f),  // rosa #FF69B4
                new Color(1f, 0.6f, 0.8f));

            var emission = PS.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 5) });

            // Shape: hemisférico para cima (como corações saindo do cliente)
            var shape = PS.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius    = 0.25f;

            // Cor: rosa → transparente
            var col = PS.colorOverLifetime;
            col.enabled = true;
            col.color   = MakeGradient(
                new Color(1f, 0.41f, 0.71f),
                new Color(1f, 0.7f, 0.85f),
                startAlpha: 1f, endAlpha: 0f);

            // Tamanho: pulse leve e depois some
            var size = PS.sizeOverLifetime;
            size.enabled = true;
            AnimationCurve sc = new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(0.3f, 1f),
                new Keyframe(1f, 0f));
            size.size = new ParticleSystem.MinMaxCurve(1f, sc);

            // Oscilação suave nos corações via noise
            var noise = PS.noise;
            noise.enabled   = true;
            noise.strength  = new ParticleSystem.MinMaxCurve(0.15f);
            noise.frequency = 1f;
        }
    }
}
