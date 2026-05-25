using UnityEngine;

namespace PizzaTycoon.VFX
{
    // VFX do forno cozinhando — fumaça em loop contínuo
    // Use PlayLoop() para ativar e StopLoop() para parar
    public class VFX_Cook : BaseVFX
    {
        protected override void Configure()
        {
            var main = PS.main;
            main.loop            = true;
            main.duration        = 2.0f;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.9f, 1.4f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(1.0f, 2.0f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.3f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation   = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

            // Taxa de emissão contínua
            var emission = PS.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(15f);

            // Shape: cone saindo do topo do forno
            var shape = PS.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle     = 15f;
            shape.radius    = 0.2f;

            // Cor: branco → cinza → transparente
            var col = PS.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.6f, 0.6f, 0.6f), 0.5f),
                    new GradientColorKey(new Color(0.4f, 0.4f, 0.4f), 1f)
                },
                new[] {
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(0.4f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(g);

            // Tamanho: cresce e depois fade
            var size = PS.sizeOverLifetime;
            size.enabled = true;
            AnimationCurve sc = new AnimationCurve(
                new Keyframe(0f, 0.3f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0.0f));
            size.size = new ParticleSystem.MinMaxCurve(1f, sc);

            // Noise para movimento orgânico da fumaça
            var noise = PS.noise;
            noise.enabled   = true;
            noise.strength  = new ParticleSystem.MinMaxCurve(0.3f);
            noise.frequency = 0.5f;
            noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.4f);
            noise.quality   = ParticleSystemNoiseQuality.Low;
        }
    }
}
