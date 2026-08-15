using UnityEngine;

/// <summary>Procedural, asset-free particle bursts for spawn smoke and destruction explosions.</summary>
public static class CombatFx
{
    private static Material _particleMaterial;

    public static void PlaySmokePuff(Vector3 position)
    {
        ParticleSystem system = CreateBurst(position, new Color(.78f, .78f, .74f, .5f), 10, .35f, .55f, 1f);
        ParticleSystem.MainModule main = system.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(.6f, 1.4f);
        main.startLifetime = .8f;
        main.gravityModifier = -.04f;
    }

    public static void PlayExplosion(Vector3 position)
    {
        ParticleSystem fire = CreateBurst(position, new Color(1f, .55f, .12f, .95f), 18, .25f, .5f, .35f);
        ParticleSystem.MainModule fireMain = fire.main;
        fireMain.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
        fireMain.startLifetime = .45f;

        ParticleSystem smoke = CreateBurst(position, new Color(.25f, .25f, .23f, .8f), 10, .4f, .7f, .6f);
        ParticleSystem.MainModule smokeMain = smoke.main;
        smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2f);
        smokeMain.startLifetime = .9f;
        smokeMain.gravityModifier = -.05f;
    }

    private static ParticleSystem CreateBurst(Vector3 position, Color color, int count, float minSize, float maxSize, float duration)
    {
        GameObject obj = new GameObject("FX Burst");
        obj.transform.position = position;
        ParticleSystem system = obj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer particleRenderer = obj.GetComponent<ParticleSystemRenderer>();
        particleRenderer.material = GetMaterial();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;

        ParticleSystem.MainModule main = system.main;
        main.duration = duration;
        main.loop = false;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = .35f;

        system.Play();
        return system;
    }

    private static Material GetMaterial()
    {
        if (_particleMaterial != null) return _particleMaterial;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        _particleMaterial = new Material(shader) { name = "CombatFx Material" };
        return _particleMaterial;
    }
}
