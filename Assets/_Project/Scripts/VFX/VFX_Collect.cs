using UnityEngine;

namespace PizzaTycoon.VFX
{
    // VFX de coleta de item — burst de partículas amarelas para cima
    public class VFX_Collect : BaseVFX
    {
        protected override void Configure()
        {
            var main = PS.main;
            main.loop            = false;
            main.duration        = 0.5f;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.4f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.06f, 0.10f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.5f); // flutua para cima
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor      = new ParticleSystem.MinMaxGradient(
                new Color(0.957f, 0.816f, 0.247f), // amarelo trigo
                new Color(1f, 0.9f, 0.3f));

            // Burst único de 8 partículas
            var emission = PS.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

            // Shape: esfera
            var shape = PS.shape;
            shape.enabled    = true;
            shape.shapeType  = ParticleSystemShapeType.Sphere;
            shape.radius     = 0.3f;

            // Cor: amarelo → transparente
            var col = PS.colorOverLifetime;
            col.enabled = true;
            col.color   = MakeGradient(
                new Color(0.957f, 0.816f, 0.247f),
                new Color(1f, 0.95f, 0.5f),
                startAlpha: 1f, endAlpha: 0f);

            // Tamanho: diminui até 0
            var size = PS.sizeOverLifetime;
            size.enabled = true;
            size.size    = CurveDown(1f);
        }
    }
}
