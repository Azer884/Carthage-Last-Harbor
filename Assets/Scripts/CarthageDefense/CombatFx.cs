using UnityEngine;

/// <summary>Procedural, asset-free particle bursts for spawn smoke, destruction, combat hits, and upgrades.</summary>
public static class CombatFx
{
    private static Material _particleMaterial;

    public static void PlaySmokePuff(Vector3 position)
    {
        ParticleSystem system = CreateBurst(position, new Color(.78f, .78f, .74f, .5f), 16, .35f, .6f, 1f);
        ParticleSystem.MainModule main = system.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(.6f, 1.6f);
        main.startLifetime = .85f;
        main.gravityModifier = -.04f;
    }

    // `scale` lets callers dial the same explosion up for a bigger moment (the heart) without a separate
    // effect to maintain.
    public static void PlayExplosion(Vector3 position, float scale = 1f)
    {
        ParticleSystem fire = CreateBurst(position, new Color(1f, .55f, .12f, .95f), Mathf.RoundToInt(34 * scale), .3f * scale, .65f * scale, .45f * scale);
        ParticleSystem.MainModule fireMain = fire.main;
        fireMain.startSpeed = new ParticleSystem.MinMaxCurve(3f * scale, 5.5f * scale);
        fireMain.startLifetime = .55f * scale;

        ParticleSystem smoke = CreateBurst(position, new Color(.25f, .25f, .23f, .8f), Mathf.RoundToInt(18 * scale), .5f * scale, .9f * scale, .7f * scale);
        ParticleSystem.MainModule smokeMain = smoke.main;
        smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(1.2f * scale, 2.4f * scale);
        smokeMain.startLifetime = 1.1f * scale;
        smokeMain.gravityModifier = -.05f;

        ParticleSystem sparks = CreateBurst(position, new Color(1f, .9f, .5f, 1f), Mathf.RoundToInt(16 * scale), .08f * scale, .16f * scale, .3f * scale);
        ParticleSystem.MainModule sparksMain = sparks.main;
        sparksMain.startSpeed = new ParticleSystem.MinMaxCurve(5f * scale, 8f * scale);
        sparksMain.startLifetime = .4f * scale;
        sparksMain.gravityModifier = .6f;
    }

    // Quick, bright clash of sparks for every weapon hit landing — ships trading blows, stationary towers
    // and the dragon firing without a projectile prefab assigned yet. Deliberately smaller/faster than
    // PlayExplosion so it reads as "hit" rather than "destroyed".
    public static void PlayImpactSpark(Vector3 position)
    {
        ParticleSystem spark = CreateBurst(position, new Color(1f, .85f, .35f, 1f), 10, .06f, .14f, .22f);
        ParticleSystem.MainModule sparkMain = spark.main;
        sparkMain.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 6.5f);
        sparkMain.startLifetime = .25f;
        sparkMain.gravityModifier = .3f;
        ParticleSystem.ShapeModule sparkShape = spark.shape;
        sparkShape.shapeType = ParticleSystemShapeType.Sphere;
        sparkShape.radius = .15f;
    }

    // Scales gently with tier (0-based level after the upgrade): a few more sparks and a brighter gold at
    // higher tiers. `scale` is a direct multiplier on the whole effect's size (callers pass e.g. 5 for a
    // normal tower, 15 for Sidi Bou Said) — speed only scales by its square root so a big `scale` spreads
    // particles further without flinging them absurdly far. The ring is a flat horizontal disc (rotated
    // into the XZ plane) expanding outward along the ground, not a vertical circle facing the camera.
    public static void PlayUpgradeBurst(Vector3 position, int tier, float scale = 1f)
    {
        int level = Mathf.Max(0, tier);
        float intensity = 1f + level * .25f;
        float spread = .4f * Mathf.Max(scale, .01f);
        float speedScale = Mathf.Sqrt(Mathf.Max(scale, .01f));
        Color color = Color.Lerp(new Color(.8f, .55f, .2f, .95f), new Color(1f, .92f, .45f, 1f), Mathf.Clamp01(level / 3f));

        ParticleSystem sparkle = CreateBurst(position, color, Mathf.RoundToInt(16 * intensity), spread * .5f, spread * 1.1f, .5f + level * .08f);
        ParticleSystem.MainModule sparkleMain = sparkle.main;
        sparkleMain.startSpeed = new ParticleSystem.MinMaxCurve(1.5f * intensity * speedScale, 3f * intensity * speedScale);
        sparkleMain.startLifetime = .5f + level * .1f;
        sparkleMain.gravityModifier = -.1f;
        ParticleSystem.ShapeModule sparkleShape = sparkle.shape;
        sparkleShape.shapeType = ParticleSystemShapeType.Cone;
        sparkleShape.angle = 20f;
        sparkleShape.radius = spread * .5f;

        ParticleSystem ring = CreateBurst(position, new Color(color.r, color.g, color.b, .55f), Mathf.RoundToInt(22 * intensity), spread * .3f, spread * .6f, .4f);
        ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        ParticleSystem.MainModule ringMain = ring.main;
        ringMain.startSpeed = new ParticleSystem.MinMaxCurve(2f * intensity * speedScale, 2f * intensity * speedScale);
        ringMain.startLifetime = .4f;
        ParticleSystem.ShapeModule ringShape = ring.shape;
        ringShape.shapeType = ParticleSystemShapeType.Circle;
        ringShape.radius = spread * .35f;
        ringShape.radiusThickness = 0f;
    }

    // Burst wherever the player clicks — pure feedback, no gameplay meaning. Sized to read clearly even
    // from the usual zoomed-out top-down camera height.
    public static void PlayClickSpark(Vector3 position)
    {
        const float scale = 1.5f;
        ParticleSystem spark = CreateBurst(position, new Color(1f, 1f, .85f, .95f), 12, .18f * scale, .38f * scale, .3f);
        ParticleSystem.MainModule main = spark.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f * scale, 5f * scale);
        main.startLifetime = .3f;
        main.gravityModifier = .1f;
        ParticleSystem.ShapeModule shape = spark.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = .12f * scale;
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
