using UnityEngine;

/// <summary>Shared water-wake TrailRenderer setup used by both Carthaginian and Roman ships, so the look
/// stays identical without duplicating the same block in two places.</summary>
public static class ShipWakeTrail
{
    public static void Attach(GameObject ship)
    {
        if (ship.GetComponent<TrailRenderer>() != null) return;
        TrailRenderer trail = ship.AddComponent<TrailRenderer>();
        trail.time = 3.5f;
        trail.minVertexDistance = .2f;
        trail.startWidth = 2.6f;
        trail.endWidth = .3f;
        trail.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
        Gradient gradient = new Gradient();
        Color wake = new Color(.9f, .95f, 1f);
        gradient.SetKeys(new[] { new GradientColorKey(wake, 0f), new GradientColorKey(wake, 1f) },
            new[] { new GradientAlphaKey(.75f, 0f), new GradientAlphaKey(.35f, .4f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = gradient;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.numCornerVertices = 4;
        trail.numCapVertices = 4;
    }
}
