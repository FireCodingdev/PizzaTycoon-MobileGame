using UnityEngine;

namespace PizzaTycoon.VFX
{
    // VFX ao receber dinheiro — burst de moedinhas verdes com trail
    public class VFX_Money : BaseVFX
    {
        protected override void Configure()
        {
            var main = PS.main;
            main.loop            = false;
            main.duration        = 1.0f;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.10f, 0.14f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation   = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

            var emission = PS.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

            // Shape: cone apontado para cima
            var shape = PS.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle     = 30f;
            shape.radius    = 0.1f;

            // Cor: verde → amarelo ouro, com fade alpha
            var col = PS.colorOverLifetime;
            col.enabled = true;
            col.color   = MakeGradient(
                new Color(0.153f, 0.682f, 0.376f),  // verde #27AE60
                new Color(0.945f, 0.769f, 0.059f),   // amarelo #F1C40F
                startAlpha: 1f, endAlpha: 0f);

            // Tamanho: 0.12 → 0.05
            var size = PS.sizeOverLifetime;
            size.enabled = true;
            AnimationCurve sc = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.7f, 0.5f),
                new Keyframe(1f, 0.3f));
            size.size = new ParticleSystem.MinMaxCurve(1f, sc);

            // Trail de moedinhas
            var trails = PS.trails;
            trails.enabled          = true;
            trails.mode             = ParticleSystemTrailMode.PerParticle;
            trails.lifetime         = new ParticleSystem.MinMaxCurve(0.2f);
            trails.widthOverTrail   = new ParticleSystem.MinMaxCurve(0.02f);
            trails.dieWithParticles = true;
        }
    }
}
