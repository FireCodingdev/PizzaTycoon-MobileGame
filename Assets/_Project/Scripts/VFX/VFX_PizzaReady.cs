using UnityEngine;

namespace PizzaTycoon.VFX
{
    // VFX de pizza pronta — burst de estrelinhas coloridas com rotação
    public class VFX_PizzaReady : BaseVFX
    {
        private static readonly Color[] StarColors = {
            new Color(0.902f, 0.494f, 0.133f), // laranja
            new Color(0.204f, 0.596f, 0.859f), // azul
            new Color(0.153f, 0.682f, 0.376f), // verde
            new Color(0.906f, 0.298f, 0.235f), // vermelho
            new Color(0.945f, 0.769f, 0.059f), // amarelo
        };

        protected override void Configure()
        {
            var main = PS.main;
            main.loop            = false;
            main.duration        = 1.2f;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(2.0f, 4.0f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.12f, 0.18f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.5f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // Cores aleatórias entre as 5 opções
            main.startColor = new ParticleSystem.MinMaxGradient(
                StarColors[Random.Range(0, StarColors.Length)],
                StarColors[Random.Range(0, StarColors.Length)]);

            var emission = PS.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

            var shape = PS.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.5f;

            // Cor: aleatório com fade alpha
            var col = PS.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(g);

            // Tamanho: cresce até 0.2 e depois encolhe (punch)
            var size = PS.sizeOverLifetime;
            size.enabled = true;
            size.size    = CurvePunch(1.4f);

            // Rotação aleatória rápida nas estrelinhas
            var rot = PS.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = false;
            rot.z = new ParticleSystem.MinMaxCurve(
                180f * Mathf.Deg2Rad,
                360f * Mathf.Deg2Rad);
        }
    }
}
