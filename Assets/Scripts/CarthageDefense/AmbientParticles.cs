using UnityEngine;

/// <summary>Continuous, looping sparkle/mist drifting slowly upward over the play area — pure atmosphere,
/// no gameplay meaning. Unlike CombatFx's one-shot bursts, this is a single long-lived particle system.</summary>
public class AmbientParticles : MonoBehaviour
{
    [SerializeField, Min(0f)] private float emissionRate = 6f;

    public static void Ensure()
    {
        if (FindAnyObjectByType<AmbientParticles>() != null) return;
        GameObject obj = new GameObject("Ambient Particles");
        AmbientParticles ambient = obj.AddComponent<AmbientParticles>();

        // Default to a modest area at the origin; if a configured camera is present, use its actual map
        // bounds instead — those default script values are nowhere near the real play area, which is why
        // this was invisible before (spawning far off from wherever the map is actually centered).
        Vector3 center = Vector3.zero;
        Vector3 size = new Vector3(60f, 1f, 60f);
        TopDownCameraController cam = FindAnyObjectByType<TopDownCameraController>();
        if (cam != null && cam.UsesMapBounds)
        {
            Vector2 min = cam.MinimumMapPosition;
            Vector2 max = cam.MaximumMapPosition;
            center = new Vector3((min.x + max.x) * .5f, 1f, (min.y + max.y) * .5f);
            size = new Vector3(Mathf.Max(10f, max.x - min.x), 1f, Mathf.Max(10f, max.y - min.y));
        }
        ambient.Setup(center, size);
    }

    // Deferred out of Awake() on purpose: Ensure() needs to compute center/size from the camera's map
    // bounds, but AddComponent() runs Awake() synchronously before it gets the chance to pass them in.
    private void Setup(Vector3 center, Vector3 size)
    {
        // Y and overall scale tuned by hand in the Editor to sit right for this map — keep the
        // camera-bounds-derived X/Z centering, but pin the height and scale to what actually looked right.
        transform.position = new Vector3(center.x, 36.9f, center.z);
        transform.localScale = Vector3.one * 3.955452f;
        ParticleSystem system = gameObject.AddComponent<ParticleSystem>();
        ParticleSystemRenderer particleRenderer = GetComponent<ParticleSystemRenderer>();
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        particleRenderer.material = new Material(shader) { name = "AmbientParticles Material" };
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;

        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(.15f, .4f);
        main.startSize = new ParticleSystem.MinMaxCurve(.08f, .2f);
        main.startColor = new Color(1f, .96f, .85f, .5f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -.01f;
        main.maxParticles = 250;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = emissionRate;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = size;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .2f), new GradientAlphaKey(1f, .7f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        system.Play();
    }
}
