using UnityEngine;

/// <summary>Rotates the scene's sun through a full day/night loop, blending its color/intensity and the
/// scene's ambient light and sky brightness along the way via curves — dawn, day, dusk, night, repeat.
/// Attach to the Directional Light itself (or point the Sun field at it from anywhere). Switches
/// RenderSettings to flat ambient in Awake() so the ambient color/intensity below actually takes effect —
/// skybox-sourced ambient wouldn't update live as the sun moves without a realtime GI bake.</summary>
[RequireComponent(typeof(Light))]
public class DayNightCycle : MonoBehaviour
{
    [SerializeField] private Light sun;
    [Tooltip("How long a full day/night loop takes, in real seconds.")]
    [SerializeField, Min(1f)] private float dayLengthSeconds = 300f;
    [Tooltip("0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset.")]
    [SerializeField, Range(0f, 1f)] private float startTimeOfDay = .3f;
    [Tooltip("Compass heading the sun rises/sets along (the light's Y rotation).")]
    [SerializeField] private float sunHeading = -30f;

    [Header("Sun")]
    [SerializeField] private Gradient sunColor = BuildSunColorGradient();
    [SerializeField] private AnimationCurve sunIntensity = BuildSunIntensityCurve();

    [Header("Ambient")]
    [SerializeField] private Gradient ambientColor = BuildAmbientGradient();
    [SerializeField] private AnimationCurve ambientIntensity = BuildAmbientIntensityCurve();

    [Header("Sky")]
    [Tooltip("Dims the skybox itself at night. Leave empty to skip touching the skybox.")]
    [SerializeField] private AnimationCurve skyExposure = BuildSkyExposureCurve();

    private Material _skyboxInstance;

    public float TimeOfDay01 { get; private set; }
    public bool IsNight => TimeOfDay01 < .22f || TimeOfDay01 > .78f;

    public void SetTimeOfDay(float timeOfDay01)
    {
        TimeOfDay01 = Mathf.Repeat(timeOfDay01, 1f);
        Apply();
    }

    private void Awake()
    {
        if (sun == null) sun = GetComponent<Light>();
        sun.useColorTemperature = false;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

        // Instanced so this never mutates the shared/built-in skybox material other scenes might reference.
        if (skyExposure != null && RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Exposure"))
        {
            _skyboxInstance = new Material(RenderSettings.skybox);
            RenderSettings.skybox = _skyboxInstance;
        }

        TimeOfDay01 = startTimeOfDay;
        Apply();
    }

    private void Update()
    {
        TimeOfDay01 += Time.deltaTime / dayLengthSeconds;
        if (TimeOfDay01 >= 1f) TimeOfDay01 -= 1f;
        Apply();
    }

    private void Apply()
    {
        // Noon (0.5) points the sun straight down; midnight (0) puts it straight up on the opposite side
        // (fully below the horizon, as expected). Sunrise (0.25) and sunset (0.75) sit level with it.
        float pitch = TimeOfDay01 * 360f - 90f;
        sun.transform.rotation = Quaternion.Euler(pitch, sunHeading, 0f);
        sun.color = sunColor.Evaluate(TimeOfDay01);
        sun.intensity = sunIntensity.Evaluate(TimeOfDay01);

        RenderSettings.ambientLight = ambientColor.Evaluate(TimeOfDay01);
        RenderSettings.ambientIntensity = ambientIntensity.Evaluate(TimeOfDay01);

        if (_skyboxInstance != null) _skyboxInstance.SetFloat("_Exposure", skyExposure.Evaluate(TimeOfDay01));
    }

    private static Gradient BuildSunColorGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(.08f, .10f, .22f), 0f),
                new GradientColorKey(new Color(.95f, .45f, .25f), .23f),
                new GradientColorKey(new Color(1f, .95f, .85f), .32f),
                new GradientColorKey(new Color(1f, .98f, .92f), .5f),
                new GradientColorKey(new Color(1f, .95f, .85f), .68f),
                new GradientColorKey(new Color(.95f, .40f, .25f), .77f),
                new GradientColorKey(new Color(.08f, .10f, .22f), 1f),
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return gradient;
    }

    private static AnimationCurve BuildSunIntensityCurve() => new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(.22f, 0f),
        new Keyframe(.27f, .6f),
        new Keyframe(.35f, 1.8f),
        new Keyframe(.5f, 2f),
        new Keyframe(.65f, 1.8f),
        new Keyframe(.73f, .6f),
        new Keyframe(.78f, 0f),
        new Keyframe(1f, 0f));

    private static Gradient BuildAmbientGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(.04f, .05f, .09f), 0f),
                new GradientColorKey(new Color(.35f, .24f, .22f), .25f),
                new GradientColorKey(new Color(.55f, .58f, .65f), .5f),
                new GradientColorKey(new Color(.35f, .24f, .22f), .75f),
                new GradientColorKey(new Color(.04f, .05f, .09f), 1f),
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return gradient;
    }

    private static AnimationCurve BuildAmbientIntensityCurve() => new AnimationCurve(
        new Keyframe(0f, .15f),
        new Keyframe(.25f, .5f),
        new Keyframe(.5f, 1f),
        new Keyframe(.75f, .5f),
        new Keyframe(1f, .15f));

    private static AnimationCurve BuildSkyExposureCurve() => new AnimationCurve(
        new Keyframe(0f, .15f),
        new Keyframe(.25f, .5f),
        new Keyframe(.5f, 1.3f),
        new Keyframe(.75f, .5f),
        new Keyframe(1f, .15f));
}
