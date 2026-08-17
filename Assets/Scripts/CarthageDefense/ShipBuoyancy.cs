using UnityEngine;

/// <summary>Makes a ship bob up/down and sway side to side, approximating the ocean shader's rolling waves
/// without reading the shader itself (the waves are pure GPU vertex displacement — there's no CPU-side
/// water height to sample). Runs in LateUpdate, after whatever moves the ship horizontally that frame, and
/// applies its own offset as a per-frame delta (add this frame's, remove last frame's) so it layers on top
/// of navigation/combat logic instead of fighting it or drifting over time.</summary>
public class ShipBuoyancy : MonoBehaviour
{
    [SerializeField, Min(0f)] private float bobAmplitude = .18f;
    [SerializeField, Min(0.01f)] private float bobPeriod = 4.2f;
    [SerializeField, Min(0f)] private float swayDegrees = 3.5f;
    [SerializeField, Min(0.01f)] private float swayPeriod = 5.5f;

    private float _phase;
    private float _lastBob;
    private float _lastSway;

    private void Awake()
    {
        // Random phase per ship so a whole fleet doesn't bob in unison.
        _phase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void LateUpdate()
    {
        float bob = Mathf.Sin(Time.time * (Mathf.PI * 2f / bobPeriod) + _phase) * bobAmplitude;
        Vector3 position = transform.position;
        position.y += bob - _lastBob;
        transform.position = position;
        _lastBob = bob;

        float sway = Mathf.Sin(Time.time * (Mathf.PI * 2f / swayPeriod) + _phase * 1.3f) * swayDegrees;
        transform.Rotate(Vector3.forward, sway - _lastSway, Space.Self);
        _lastSway = sway;
    }
}
