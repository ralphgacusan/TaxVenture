using UnityEngine;

/// <summary>
/// PURPOSE:
/// Small celebration effects for the market income toolkit, built from two
/// runtime ParticleSystems (no assets): a quick sparkle burst where a note
/// lands or a stroke finishes, and a confetti shower for finishing the
/// challenge. Real (unscaled) time, world space, shared Sprites/Default
/// material owned by the ToolkitBuilder.
/// </summary>
public sealed class ToolkitFx
{
    private readonly ParticleSystem sparks;
    private readonly ParticleSystem confetti;

    private static readonly Color[] ConfettiColors =
    {
        new Color32(255, 206, 64, 255), new Color32(70, 170, 104, 255), new Color32(52, 120, 210, 255),
        new Color32(236, 93, 98, 255), new Color32(250, 246, 234, 255), new Color32(196, 128, 32, 255)
    };

    public ToolkitFx(Transform parent, ToolkitBuilder builder)
    {
        Material material = builder.Clear(Color.white, "Particles");
        sparks = Make("Sparkles", parent, builder.Layer, material, 300, 1.1f, spin: false);
        confetti = Make("Confetti", parent, builder.Layer, material, 400, 0.45f, spin: true);
    }

    private static ParticleSystem Make(string name, Transform parent, int layer, Material material, int max, float gravity, bool spin)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent, false);
        ParticleSystem system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = system.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1f;
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = max;
        main.gravityModifier = gravity;
        main.startLifetime = 0.6f;
        main.startSpeed = 0f;
        main.startSize = 0.01f;
        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = false;

        ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, spin ? 0.8f : 0.2f));
        if (spin)
        {
            ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            ParticleSystem.VelocityOverLifetimeModule drag = system.velocityOverLifetime;
            drag.enabled = false;
            ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.dampen = 0.12f;
            limit.limit = 0.35f;
        }

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        system.Play();
        return system;
    }

    /// <summary>A small upward burst (note landed, stroke finished, pip lit).</summary>
    public void Sparkle(Vector3 position, Vector3 up, Color color, int count = 14, float speed = 0.45f)
    {
        if (sparks == null) return;
        for (int i = 0; i < count; i++)
        {
            Vector3 dir = (up * Random.Range(0.6f, 1.2f) + Random.insideUnitSphere * 0.8f).normalized;
            var p = new ParticleSystem.EmitParams
            {
                position = position + Random.insideUnitSphere * 0.006f,
                velocity = dir * speed * Random.Range(0.5f, 1.1f),
                startSize = Random.Range(0.004f, 0.01f),
                startLifetime = Random.Range(0.3f, 0.55f),
                startColor = Color.Lerp(color, Color.white, Random.Range(0f, 0.45f))
            };
            sparks.Emit(p, 1);
        }
    }

    /// <summary>A confetti shower over the desk (challenge mastered).</summary>
    public void Confetti(Vector3 position, Vector3 up, int count = 120)
    {
        if (confetti == null) return;
        for (int i = 0; i < count; i++)
        {
            Vector3 dir = (up * Random.Range(1f, 1.6f) + Random.insideUnitSphere).normalized;
            var p = new ParticleSystem.EmitParams
            {
                position = position + Random.insideUnitSphere * 0.05f,
                velocity = dir * Random.Range(0.6f, 1.3f),
                startSize = Random.Range(0.008f, 0.016f),
                startLifetime = Random.Range(1.1f, 1.8f),
                rotation = Random.Range(0f, 360f),
                startColor = ConfettiColors[Random.Range(0, ConfettiColors.Length)]
            };
            confetti.Emit(p, 1);
        }
    }

    public void Clear()
    {
        if (sparks != null) sparks.Clear();
        if (confetti != null) confetti.Clear();
    }
}
